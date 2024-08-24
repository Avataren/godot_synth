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
            T node = new T();
            node.Name = name;
            RegisterNode(node);
            return node;
        }

        public void RegisterNode(AudioNode node)
        {
            lock (_lock)
            {
                OriginalNodes.Add(node);
                WorkingNodes.Add(node); // Start with the same nodes in the working graph
                OriginalConnections[node] = new List<AudioConnection>();
                GD.Print($"Registered node: {node.Name}");
            }
        }

        public void Connect(AudioNode source, AudioNode destination, AudioParam param, ModulationType modType, float strength = 1.0f)
        {
            lock (_lock)
            {
                if (!OriginalConnections[source].Any(c => c.Destination == destination && c.Param == param.ToString()))
                {
                    OriginalConnections[source].Add(new AudioConnection(source, destination, param.ToString(), modType.ToString(), strength));
                    AddConnectionToWorkingGraph(source, destination, param, modType, strength);
                    GD.Print($"Connected {source.Name} to {destination.Name} on param {param}");
                }
                else
                {
                    GD.Print($"Connection from {source.Name} to {destination.Name} on param {param} already exists");
                }
            }
        }

        private void AddConnectionToWorkingGraph(AudioNode source, AudioNode destination, AudioParam param, ModulationType modType, float strength)
        {
            if (!destination.AudioParameters.ContainsKey(param))
            {
                destination.AudioParameters[param] = new List<ParameterConnection>();
            }

            destination.AudioParameters[param].Add(new ParameterConnection(source, strength, modType));
            GD.Print($"[Working Graph] Connected {source.Name} to {destination.Name} on param {param} (Strength: {strength}, ModType: {modType})");
        }

        public void Disconnect(AudioNode source, AudioNode destination, AudioParam param)
        {
            lock (_lock)
            {
                if (destination.AudioParameters.ContainsKey(param))
                {
                    destination.AudioParameters[param].RemoveAll(x => x.SourceNode == source);
                    if (destination.AudioParameters[param].Count == 0)
                    {
                        destination.AudioParameters.Remove(param);
                    }
                    GD.Print($"Disconnected {source.Name} from {destination.Name} on param {param}");
                }
            }
        }

        public void SetNodeEnabled(AudioNode node, bool enabled)
        {
            lock (_lock)
            {
                if (node.Enabled != enabled)
                {
                    GD.Print($"Setting node {node.Name} enabled to {enabled}");
                    node.Enabled = enabled;
                    ReconstructWorkingGraph();
                    TopologicalSortWorkingGraph();
                    GD.Print("Working graph reconstruction and topological sort completed");
                }
            }
        }

        private void ReconstructWorkingGraph()
        {
            GD.Print("[Working Graph] Reconstructing working graph...");

            // Clear the working graph connections
            foreach (var node in WorkingNodes)
            {
                GD.Print($"[Working Graph] Clearing connections for node {node.Name}");
                node.AudioParameters.Clear();
            }

            // Rebuild the working graph connections based on the enabled nodes
            foreach (var source in OriginalConnections.Keys)
            {
                if (source.Enabled)
                {
                    GD.Print($"[Working Graph] Processing enabled node {source.Name}");

                    foreach (var connection in OriginalConnections[source])
                    {
                        var destination = connection.Destination;

                        if (destination.Enabled)
                        {
                            GD.Print($"[Working Graph] Reconnecting {source.Name} -> {destination.Name} on param {connection.Param}");
                            AddConnectionToWorkingGraph(source, destination, Enum.Parse<AudioParam>(connection.Param), Enum.Parse<ModulationType>(connection.ModType), connection.Strength);
                        }
                        else
                        {
                            // Attempt to reroute the connection to maintain signal flow
                            var reroutedNode = FindNextEnabledDownstreamNode(destination);

                            if (reroutedNode != null)
                            {
                                GD.Print($"[Working Graph] Rerouting {source.Name} -> {reroutedNode.Name} instead of {destination.Name} on param {connection.Param}");
                                AddConnectionToWorkingGraph(source, reroutedNode, Enum.Parse<AudioParam>(connection.Param), Enum.Parse<ModulationType>(connection.ModType), connection.Strength);
                            }
                            else
                            {
                                GD.Print($"[Working Graph] No enabled downstream node found for {destination.Name}, skipping connection.");
                            }
                        }
                    }
                }
                else
                {
                    GD.Print($"[Working Graph] Skipping disabled node {source.Name}");
                }
            }

            // Print the final state of the working graph connections
            PrintConnections();
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
                    // Recursively search for an enabled downstream node
                    var nextNode = FindNextEnabledDownstreamNode(downstreamNode);
                    if (nextNode != null)
                    {
                        return nextNode;
                    }
                }
            }

            return null; // No enabled downstream node found
        }

        private IEnumerable<AudioNode> FindDownstreamNodes(AudioNode node)
        {
            // This method should return a list of nodes that are connected downstream of the provided node
            return OriginalConnections.Values
                .SelectMany(connections => connections)
                .Where(connection => connection.Source == node)
                .Select(connection => connection.Destination);
        }

        private AudioNode FindImmediateParentNode(AudioNode node)
        {
            return OriginalNodes.FirstOrDefault(n => n.AudioParameters.Values.Any(paramList => paramList.Any(conn => conn.SourceNode == node)));
        }

        public void Process(double increment)
        {
            lock (_lock)
            {
                if (SortedNodes == null)
                {
                    GD.Print("[Process] SortedNodes is null, running topological sort");
                    TopologicalSortWorkingGraph();
                }

                foreach (AudioNode node in SortedNodes)
                {
                    if (node.Enabled)
                    {
                        //GD.Print($"[Process] Processing node {node.Name}");

                        // Assuming each node processes its inputs and produces an output
                        node.Process(increment);

                        // Add logging to track the output of each node
                        //GD.Print($"[Process] Node {node.Name} output state: {node.OutputState()}"); // Hypothetical method
                    }
                    else
                    {
                        GD.Print($"[Process] Skipping disabled node {node.Name}");
                    }
                }
            }
        }


        public void TopologicalSortWorkingGraph()
        {
            SortedNodes = new List<AudioNode>();
            HashSet<AudioNode> visited = new HashSet<AudioNode>();
            HashSet<AudioNode> stack = new HashSet<AudioNode>();

            //GD.Print("[TopologicalSort] Starting topological sort...");

            foreach (AudioNode node in WorkingNodes)
            {
                if (!visited.Contains(node) && node.Enabled)  // Ensure only enabled nodes are considered
                {
                    //GD.Print($"[TopologicalSort] Visiting node {node.Name}");
                    Visit(node, visited, stack);
                }
            }

            //GD.Print("[TopologicalSort] Topological sort completed. Node order:");
            // foreach (var node in SortedNodes)
            // {
            //     GD.Print($"[TopologicalSort] Node: {node.Name}");
            // }
        }

        private void Visit(AudioNode node, HashSet<AudioNode> visited, HashSet<AudioNode> stack)
        {
            if (stack.Contains(node))
            {
                GD.Print($"[Visit] Node {node.Name} is already in the stack, skipping.");
                return;
            }
            if (!visited.Contains(node))
            {
                GD.Print($"[Visit] Visiting node {node.Name} for the first time");

                stack.Add(node);
                visited.Add(node);

                foreach (var paramlist in node.AudioParameters.Values)
                {
                    foreach (var connection in paramlist)
                    {
                        AudioNode dependentNode = connection.SourceNode;
                        if (dependentNode.Enabled)  // Ensure only enabled nodes are considered
                        {
                            GD.Print($"[Visit] Node {node.Name} depends on {dependentNode.Name}, visiting...");
                            Visit(dependentNode, visited, stack);
                        }
                        else
                        {
                            GD.Print($"[Visit] Node {dependentNode.Name} is disabled, skipping.");
                        }
                    }
                }

                stack.Remove(node);
                SortedNodes.Add(node);
                GD.Print($"[Visit] Added node {node.Name} to SortedNodes");
            }
        }

        public void DebugPrint()
        {
            if (SortedNodes == null)
            {
                TopologicalSortWorkingGraph();
            }
            GD.Print("AudioGraph debug print:");
            foreach (AudioNode node in SortedNodes)
            {
                GD.Print(node.Name);
                foreach (var param in node.AudioParameters)
                {
                    GD.Print($"param:  {param.Key}");
                    foreach (var paramNode in param.Value)
                    {
                        GD.Print($"    {paramNode.SourceNode.Name}");
                    }
                }
            }
        }

        public AudioNode GetNode(string name)
        {
            GD.Print($"[GetNode] Looking for node with name: {name}");
            var node = OriginalNodes.FirstOrDefault(n => n.Name == name);
            if (node != null)
            {
                GD.Print($"[GetNode] Found node: {node.Name}");
            }
            else
            {
                GD.Print($"[GetNode] Node with name {name} not found");
            }
            return node;
        }

        private void PrintConnections()
        {
            GD.Print("Current working connections:");
            foreach (var node in WorkingNodes)
            {
                foreach (var param in node.AudioParameters)
                {
                    foreach (var connection in param.Value)
                    {
                        GD.Print($"  {connection.SourceNode.Name} -> {node.Name} on param {param.Key}");
                    }
                }
            }
        }
    }
}
