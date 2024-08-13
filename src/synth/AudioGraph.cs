using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Synth
{
    public class AudioGraph
    {
        private readonly object _lock = new object();
        protected List<AudioNode> Nodes = new List<AudioNode>();
        protected List<AudioNode> SortedNodes = null;

        // Store original connections for each node
        private Dictionary<AudioNode, List<(AudioNode Source, AudioNode Destination, AudioParam Param)>> originalConnections = new Dictionary<AudioNode, List<(AudioNode, AudioNode, AudioParam)>>();

        // Factory method to create and register nodes
        public T CreateNode<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string name, int bufferSize, float sampleRate = 44100) where T : AudioNode
        {
            var constructorInfo = typeof(T).GetConstructor(new Type[] { typeof(int), typeof(float) }) ?? throw new InvalidOperationException($"Type {typeof(T)} does not have a constructor with parameters (int, float).");

            T node = (T)constructorInfo.Invoke(new object[] { bufferSize, sampleRate });
            node.Name = name;

            RegisterNode(node);
            return node;
        }

        public void DebugPrint()
        {
            if (SortedNodes == null)
            {
                TopologicalSort();
            }
            Godot.GD.Print("AudioGraph debug print:");
            foreach (AudioNode node in SortedNodes)
            {
                Godot.GD.Print(node.Name);
                foreach (var param in node.AudioParameters)
                {
                    Godot.GD.Print($"param:  {param.Key}");
                    foreach (AudioNode paramNode in param.Value)
                    {
                        Godot.GD.Print($"    {paramNode.Name}");
                    }
                }
            }
        }

        public AudioNode GetNode(string name)
        {
            lock (_lock)
            {
                return Nodes.Find(node => node.Name == name);
            }
        }

        public void RegisterNode(AudioNode node)
        {
            lock (_lock)
            {
                Nodes.Add(node);
                SortedNodes = null;
            }
        }

        public void RemoveNode(string name)
        {
            lock (_lock)
            {
                Nodes.RemoveAll(node => node.Name == name);
                SortedNodes = null;
            }
        }

        public void Connect(AudioNode source, AudioNode destination, AudioParam param)
        {
            lock (_lock)
            {
                if (!destination.AudioParameters.ContainsKey(param))
                {
                    destination.AudioParameters[param] = new List<AudioNode>();
                }
                destination.AudioParameters[param].Add(source);
                SortedNodes = null;
            }
        }

        public void Disconnect(AudioNode source, AudioNode destination, AudioParam param)
        {
            lock (_lock)
            {
                if (destination.AudioParameters.ContainsKey(param))
                {
                    destination.AudioParameters[param].Remove(source);
                }
                SortedNodes = null;
            }
        }

        // Handle enabling/disabling nodes with rerouting logic
        public void SetNodeEnabled(AudioNode node, bool enabled)
        {
            lock (_lock)
            {
                if (node.Enabled != enabled)
                {
                    node.Enabled = enabled;

                    if (enabled)
                    {
                        RestoreConnections(node);
                    }
                    else
                    {
                        RerouteConnections(node);
                    }

                    TopologicalSort(); // Re-sort after changing connections
                }
            }
        }

        private void RerouteConnections(AudioNode node)
        {
            if (!originalConnections.ContainsKey(node))
            {
                originalConnections[node] = new List<(AudioNode, AudioNode, AudioParam)>();
            }

            // Disconnect the node and reroute its inputs
            foreach (var param in node.AudioParameters.Keys)
            {
                foreach (var inputNode in node.AudioParameters[param])
                {
                    foreach (var dependentNode in Nodes)
                    {
                        if (dependentNode.AudioParameters.ContainsKey(param) && dependentNode.AudioParameters[param].Contains(node))
                        {
                            // Save the original connection
                            originalConnections[node].Add((inputNode, dependentNode, param));

                            // Disconnect the current node from the dependent node
                            Disconnect(node, dependentNode, param);

                            // Connect inputNode directly to the dependentNode
                            Connect(inputNode, dependentNode, param);
                        }
                    }
                }
            }
        }

        private void RestoreConnections(AudioNode node)
        {
            // Restore the original connections
            if (originalConnections.ContainsKey(node))
            {
                foreach (var (source, destination, param) in originalConnections[node])
                {
                    // Disconnect the rerouted connection
                    Disconnect(source, destination, param);

                    // Reconnect the original node
                    Connect(node, destination, param);
                }

                // Clear stored original connections
                originalConnections[node].Clear();
            }
        }

        public void Process(float increment)
        {
            lock (_lock)
            {
                if (SortedNodes == null)
                {
                    TopologicalSort();
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

        public void TopologicalSort()
        {
            SortedNodes = new List<AudioNode>();
            HashSet<AudioNode> visited = new HashSet<AudioNode>();
            HashSet<AudioNode> stack = new HashSet<AudioNode>();

            foreach (AudioNode node in Nodes)
            {
                if (!visited.Contains(node))
                {
                    Visit(node, visited, stack);
                }
            }
        }

        private void Visit(AudioNode node, HashSet<AudioNode> visited, HashSet<AudioNode> stack)
        {
            if (stack.Contains(node))
            {
                throw new InvalidOperationException("Cycle detected in the audio graph");
            }
            if (!visited.Contains(node))
            {
                if (node.Enabled) // Skip disabled nodes
                {
                    stack.Add(node);
                    visited.Add(node);

                    foreach (var paramlist in node.AudioParameters.Values)
                    {
                        foreach (AudioNode dependentNode in paramlist)
                        {
                            Visit(dependentNode, visited, stack);
                        }
                    }

                    stack.Remove(node);
                    SortedNodes.Add(node);
                }
            }
        }
    }
}
