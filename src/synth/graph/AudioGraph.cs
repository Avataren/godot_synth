using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Synth
{
    public class AudioConnection
    {
        public AudioNode Source { get; set; }
        public AudioNode Destination { get; set; }
        public string Param { get; set; }
        public string ModType { get; set; }
        public float Strength { get; set; }

        public AudioConnection(AudioNode source, AudioNode destination, string param, string modType, float strength)
        {
            Source = source;
            Destination = destination;
            Param = param;
            ModType = modType;
            Strength = strength;
        }
    }

    public class AudioGraph
    {
        private readonly object _graphLock = new object();
        private List<AudioNode> Nodes = new List<AudioNode>();
        private List<AudioNode> SortedNodes = null;
        private Dictionary<AudioNode, List<AudioConnection>> OriginalConnections = new Dictionary<AudioNode, List<AudioConnection>>();

        public T CreateNode<T>(string name) where T : AudioNode, new()
        {
            try
            {
                T node = new T { Name = name };
                RegisterNode(node);
                return node;
            }
            catch (Exception e)
            {
                GD.PrintErr($"Error creating node: {e.Message}");
                if (e.InnerException != null)
                {
                    GD.PrintErr($"Inner exception: {e.InnerException.Message}");
                    GD.PrintErr(e.InnerException.StackTrace);
                }
                throw;
            }
        }

        public void RegisterNode(AudioNode node)
        {
            lock (_graphLock)
            {
                Nodes.Add(node);
                OriginalConnections[node] = new List<AudioConnection>();
            }
        }

        private void AddConnectionToWorkingGraph(AudioNode source, AudioNode destination, AudioParam param, ModulationType modType, float strength = 1.0f)
        {
            if (!destination.AudioParameters.ContainsKey(param))
            {
                destination.AudioParameters[param] = new List<ParameterConnection>();
            }

            destination.AudioParameters[param].Add(new ParameterConnection(source, strength, modType));
        }

        public void Connect(AudioNode source, AudioNode destination, AudioParam param, ModulationType modType, float strength = 1.0f)
        {
            lock (_graphLock)
            {
                if (!OriginalConnections.ContainsKey(source))
                {
                    OriginalConnections[source] = new List<AudioConnection>();
                }

                GD.Print($"Connecting {source.Name} to {destination.Name} on {param} with {modType} (strength {strength})");

                // Ensure no existing connection to this destination/parameter remains
                Disconnect(source, destination, param);

                // Add the new connection
                var newConnection = new AudioConnection(source, destination, param.ToString(), modType.ToString(), strength);
                OriginalConnections[source].Add(newConnection);
                AddConnectionToWorkingGraph(source, destination, param, modType, strength);

                // Update the working graph and sort it
                ReconstructWorkingGraph();
                TopologicalSortWorkingGraph();
            }
        }

        public void Disconnect(AudioNode source, AudioNode destination, AudioParam param)
        {
            lock (_graphLock)
            {
                GD.Print($"Disconnecting {source.Name} from {destination.Name} on {param}");

                if (OriginalConnections.ContainsKey(source))
                {
                    // Remove the connection from OriginalConnections
                    var connectionToRemove = OriginalConnections[source]
                        .FirstOrDefault(c => c.Destination == destination && c.Param == param.ToString());

                    if (connectionToRemove != null)
                    {
                        OriginalConnections[source].Remove(connectionToRemove);
                        GD.Print($"Removed connection from {source.Name} to {destination.Name} on {param}");
                    }
                }

                // Remove the connection from the working graph
                if (destination.AudioParameters.ContainsKey(param))
                {
                    destination.AudioParameters[param].RemoveAll(x => x.SourceNode == source);
                    if (destination.AudioParameters[param].Count == 0)
                    {
                        destination.AudioParameters.Remove(param);
                    }

                    GD.Print($"Removed connection in working graph from {source.Name} to {destination.Name} on {param}");
                }

                // Update the working graph and sort it
                ReconstructWorkingGraph();
                TopologicalSortWorkingGraph();
            }
        }

        private AudioNode FindMixerNode()
        {
            // Implement logic to find the mixer node in your graph
            return Nodes.FirstOrDefault(node => node is MixerNode); // Assuming MixerNode is the type for mixers
        }

        public void SetNodeEnabled(AudioNode node, bool enabled)
        {
            lock (_graphLock)
            {
                if (node.Enabled != enabled)
                {
                    node.Enabled = enabled;
                    ReconstructWorkingGraph();
                    TopologicalSortWorkingGraph();
                }
            }
        }

        private void ReconstructWorkingGraph()
        {
            // Clear all audio parameters for each node
            foreach (var node in Nodes)
            {
                node.AudioParameters.Clear();
            }

            // Rebuild connections based on enabled status
            foreach (var source in OriginalConnections.Keys)
            {
                if (source.Enabled)
                {
                    foreach (var connection in OriginalConnections[source])
                    {
                        var destination = connection.Destination;

                        if (destination.Enabled)
                        {
                            AddConnectionToWorkingGraph(
                                source,
                                destination,
                                Enum.Parse<AudioParam>(connection.Param),
                                Enum.Parse<ModulationType>(connection.ModType),
                                connection.Strength
                            );
                        }
                        else
                        {
                            var reroutedNode = FindNextEnabledDownstreamNode(destination);
                            if (reroutedNode != null)
                            {
                                AddConnectionToWorkingGraph(
                                    source,
                                    reroutedNode,
                                    Enum.Parse<AudioParam>(connection.Param),
                                    Enum.Parse<ModulationType>(connection.ModType),
                                    connection.Strength
                                );
                            }
                        }
                    }
                }
            }
        }

        private AudioNode FindNextEnabledDownstreamNode(AudioNode node, HashSet<AudioNode> visited = null)
        {
            if (visited == null)
            {
                visited = new HashSet<AudioNode>();
            }

            if (visited.Contains(node))
            {
                return null; // Cycle detected
            }

            visited.Add(node);

            var downstreamNodes = FindDownstreamNodes(node);

            foreach (var downstreamNode in downstreamNodes)
            {
                if (downstreamNode.Enabled)
                {
                    return downstreamNode;
                }
                else
                {
                    var nextNode = FindNextEnabledDownstreamNode(downstreamNode, visited);
                    if (nextNode != null)
                    {
                        return nextNode;
                    }
                }
            }

            return null;
        }

        private IEnumerable<AudioNode> FindDownstreamNodes(AudioNode node)
        {
            if (OriginalConnections.TryGetValue(node, out var connections))
            {
                return connections.Select(connection => connection.Destination);
            }
            return Enumerable.Empty<AudioNode>();
        }

        public void Process(double increment)
        {
            lock (_graphLock)
            {
                if (SortedNodes == null)
                {
                    TopologicalSortWorkingGraph();
                }

                foreach (AudioNode node in SortedNodes)
                {
                    if (node.Enabled)
                    {
                        node.Process(increment);
                    }
                }
            }
        }

        public void TopologicalSortWorkingGraph()
        {
            SortedNodes = new List<AudioNode>();
            HashSet<AudioNode> visited = new HashSet<AudioNode>();
            HashSet<AudioNode> stack = new HashSet<AudioNode>();

            foreach (AudioNode node in Nodes.Where(n => n.Enabled))
            {
                if (!visited.Contains(node))
                {
                    if (!Visit(node, visited, stack))
                    {
                        throw new InvalidOperationException($"Cycle detected in graph involving node: {node.Name}");
                    }
                }
            }

            // Reverse the list to get correct processing order
            SortedNodes.Reverse();
        }

        private bool Visit(AudioNode node, HashSet<AudioNode> visited, HashSet<AudioNode> stack)
        {
            if (stack.Contains(node))
            {
                return false; // Cycle detected
            }
            if (!visited.Contains(node))
            {
                stack.Add(node);
                visited.Add(node);

                foreach (var paramConnections in node.AudioParameters.Values)
                {
                    foreach (var connection in paramConnections)
                    {
                        AudioNode dependentNode = connection.SourceNode;
                        if (dependentNode.Enabled)
                        {
                            if (!Visit(dependentNode, visited, stack))
                            {
                                return false; // Cycle detected further down
                            }
                        }
                    }
                }

                stack.Remove(node);
                SortedNodes.Add(node);
            }

            return true; // No cycle detected
        }

        public AudioNode GetNode(string name)
        {
            return Nodes.FirstOrDefault(n => n.Name == name);
        }
    }
}
