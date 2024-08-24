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
        private readonly object _lock = new object();
        protected List<AudioNode> OriginalNodes = new List<AudioNode>();
        protected List<AudioNode> WorkingNodes = new List<AudioNode>();
        protected List<AudioNode> SortedNodes = null;
        private Dictionary<AudioNode, List<AudioConnection>> OriginalConnections = new Dictionary<AudioNode, List<AudioConnection>>();

        public T CreateNode<T>(string name) where T : AudioNode, new()
        {
            T node = new T { Name = name };
            RegisterNode(node);
            return node;
        }

        public void RegisterNode(AudioNode node)
        {
            lock (_lock)
            {
                OriginalNodes.Add(node);
                WorkingNodes.Add(node);
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

        // Flag to prevent recursive calls when restoring oscillator connections
        private bool _restoringOscillatorConnection = false;

        public void Connect(AudioNode source, AudioNode destination, AudioParam param, ModulationType modType, float strength = 1.0f)
        {
            lock (_lock)
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
            lock (_lock)
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
            return OriginalNodes.FirstOrDefault(node => node is MixerNode); // Assuming MixerNode is the type for mixers
        }

        public void SetNodeEnabled(AudioNode node, bool enabled)
        {
            lock (_lock)
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
            foreach (var node in WorkingNodes)
            {
                node.AudioParameters.Clear();
            }

            foreach (var source in OriginalConnections.Keys)
            {
                if (source.Enabled)
                {
                    foreach (var connection in OriginalConnections[source])
                    {
                        var destination = connection.Destination;

                        if (destination.Enabled)
                        {
                            AddConnectionToWorkingGraph(source, destination, Enum.Parse<AudioParam>(connection.Param), Enum.Parse<ModulationType>(connection.ModType), connection.Strength);
                        }
                        else
                        {
                            var reroutedNode = FindNextEnabledDownstreamNode(destination);
                            if (reroutedNode != null)
                            {
                                AddConnectionToWorkingGraph(source, reroutedNode, Enum.Parse<AudioParam>(connection.Param), Enum.Parse<ModulationType>(connection.ModType), connection.Strength);
                            }
                        }
                    }
                }
            }
        }

        private AudioNode FindNextEnabledDownstreamNode(AudioNode node)
        {
            var downstreamNodes = FindDownstreamNodes(node);

            foreach (var downstreamNode in downstreamNodes)
            {
                if (downstreamNode.Enabled)
                {
                    return downstreamNode;
                }
                else
                {
                    var nextNode = FindNextEnabledDownstreamNode(downstreamNode);
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
            return OriginalConnections.Values
                .SelectMany(connections => connections)
                .Where(connection => connection.Source == node)
                .Select(connection => connection.Destination);
        }

        public void Process(double increment)
        {
            lock (_lock)
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

        // Handle the potential cycles in Topological Sort
        // Handle the potential cycles in Topological Sort
        public void TopologicalSortWorkingGraph()
        {
            SortedNodes = new List<AudioNode>();
            HashSet<AudioNode> visited = new HashSet<AudioNode>();
            HashSet<AudioNode> stack = new HashSet<AudioNode>();

            foreach (AudioNode node in WorkingNodes)
            {
                if (!visited.Contains(node) && node.Enabled)
                {
                    if (!Visit(node, visited, stack))
                    {
                        // Handle cycle detection (perhaps log it, throw an exception, or handle it based on your application logic)
                        GD.PrintErr($"Cycle detected in graph with node: {node.Name}");
                        return;
                    }
                }
            }

            GD.Print("Topological sort completed. Node order:");
            foreach (var node in SortedNodes)
            {
                GD.Print($" - {node.Name}");
            }
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

                foreach (var paramlist in node.AudioParameters.Values)
                {
                    foreach (var connection in paramlist)
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
            return OriginalNodes.FirstOrDefault(n => n.Name == name);
        }
    }
}
