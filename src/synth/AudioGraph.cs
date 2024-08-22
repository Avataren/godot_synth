using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Godot;

namespace Synth
{
    public class AudioGraph
    {
        private readonly object _lock = new object();
        protected List<AudioNode> Nodes = new List<AudioNode>();
        protected List<AudioNode> SortedNodes = null;

        // Store original connections for each node
        //private Dictionary<AudioNode, List<(AudioNode Source, AudioNode Destination, AudioParam Param)>> originalConnections = new Dictionary<AudioNode, List<(AudioNode, AudioNode, AudioParam)>>();
        private Dictionary<AudioNode, List<(AudioNode Source, AudioNode Destination, AudioParam Param, ModulationType ModType, float Strength)>> originalConnections =
    new Dictionary<AudioNode, List<(AudioNode, AudioNode, AudioParam, ModulationType, float)>>();


        // Factory method to create and register nodes
        //public T CreateNode<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string name, int bufferSize, float sampleRate = 44100) where T : AudioNode
        public T CreateNode<T>(string name) where T : AudioNode, new()
        {
            //var constructorInfo = typeof(T).GetConstructor(new Type[] { typeof(int), typeof(float) }) ?? throw new InvalidOperationException($"Type {typeof(T)} does not have a constructor with parameters (int, float).");

            //T node = (T)constructorInfo.Invoke(new object[] { bufferSize, sampleRate });
            T node = (T)Activator.CreateInstance(typeof(T));
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
                    foreach (var paramNode in param.Value)
                    {
                        Godot.GD.Print($"    {paramNode.SourceNode.Name}");
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

        public void Connect(AudioNode source, AudioNode destination, AudioParam param, ModulationType modType, float strength = 1.0f)
        {
            lock (_lock)
            {
                if (!destination.AudioParameters.ContainsKey(param))
                {
                    destination.AudioParameters[param] = new List<ParameterConnection>();
                }
                //GD.Print("Adding connection from " + source.Name + " to " + destination.Name + " with param " + param);
                destination.AudioParameters[param].Add(new ParameterConnection(source, strength, modType));
                //GD.Print("Destination " + destination.Name + " now has " + destination.AudioParameters[param].Count + " connections, they are:");
                foreach (var paramCon in destination.AudioParameters[param])
                {
                    GD.Print(paramCon.SourceNode.Name);
                }
                SortedNodes = null;
            }
        }

        public void Disconnect(AudioNode source, AudioNode destination, AudioParam param)
        {
            lock (_lock)
            {
                if (destination.AudioParameters.ContainsKey(param))
                {
                    //GD.Print("Removing connection from " + source.Name + " to " + destination.Name + " with param " + param);

                    destination.AudioParameters[param].RemoveAll(x => x.SourceNode == source);
                }
            }
            SortedNodes = null;
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
                originalConnections[node] = new List<(AudioNode, AudioNode, AudioParam, ModulationType, float)>();
            }

            // Disconnect the node and reroute its inputs
            foreach (var param in node.AudioParameters.Keys)
            {
                // Create a copy of the list to avoid modifying the collection while iterating
                var connectionsCopy = node.AudioParameters[param].ToList();

                foreach (var connection in connectionsCopy)
                {
                    var inputNode = connection.SourceNode;

                    foreach (var dependentNode in Nodes)
                    {
                        if (dependentNode.AudioParameters.ContainsKey(param))
                        {
                            var dependentConnections = dependentNode.AudioParameters[param];

                            // Check if the dependent node has a connection from the current node
                            var dependentConnection = dependentConnections.FirstOrDefault(c => c.SourceNode == node);
                            if (dependentConnection != null)
                            {
                                // Save the original connection, including ModType and Strength
                                originalConnections[node].Add((inputNode, dependentNode, param, connection.ModType, connection.Strength));

                                // Disconnect the current node from the dependent node
                                Disconnect(node, dependentNode, param);

                                // Avoid duplicate connections
                                if (!dependentConnections.Any(c => c.SourceNode == inputNode))
                                {
                                    // Connect inputNode directly to the dependentNode with the same ModType and Strength
                                    Connect(inputNode, dependentNode, param, connection.ModType, connection.Strength);
                                }
                            }
                        }
                    }
                }
            }
        }


        // private void RerouteConnections(AudioNode node)
        // {
        //     if (!originalConnections.ContainsKey(node))
        //     {
        //         originalConnections[node] = new List<(AudioNode, AudioNode, AudioParam, ModulationType, float)>();
        //     }

        //     // Disconnect the node and reroute its inputs
        //     foreach (var param in node.AudioParameters.Keys)
        //     {
        //         foreach (var connection in node.AudioParameters[param])
        //         {
        //             var inputNode = connection.SourceNode;

        //             foreach (var dependentNode in Nodes)
        //             {
        //                 if (dependentNode.AudioParameters.ContainsKey(param))
        //                 {
        //                     var dependentConnections = dependentNode.AudioParameters[param];

        //                     // Check if the dependent node has a connection from the current node
        //                     var dependentConnection = dependentConnections.FirstOrDefault(c => c.SourceNode == node);
        //                     if (dependentConnection != null)
        //                     {
        //                         // Save the original connection, including ModType and Strength
        //                         originalConnections[node].Add((inputNode, dependentNode, param, connection.ModType, connection.Strength));

        //                         // Disconnect the current node from the dependent node
        //                         Disconnect(node, dependentNode, param);

        //                         // Avoid duplicate connections
        //                         if (!dependentConnections.Any(c => c.SourceNode == inputNode))
        //                         {
        //                             // Connect inputNode directly to the dependentNode with the same ModType and Strength
        //                             Connect(inputNode, dependentNode, param, connection.ModType, connection.Strength);
        //                         }
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // }


        // private void RerouteConnections(AudioNode node)
        // {
        //     if (!originalConnections.ContainsKey(node))
        //     {
        //         originalConnections[node] = new List<(AudioNode, AudioNode, AudioParam)>();
        //     }

        //     // Disconnect the node and reroute its inputs
        //     foreach (var param in node.AudioParameters.Keys)
        //     {
        //         foreach (var connection in node.AudioParameters[param])
        //         {
        //             var inputNode = connection.SourceNode;

        //             foreach (var dependentNode in Nodes)
        //             {
        //                 if (dependentNode.AudioParameters.ContainsKey(param))
        //                 {
        //                     var dependentConnections = dependentNode.AudioParameters[param];

        //                     // Check if the dependent node has a connection from the current node
        //                     if (dependentConnections.Any(c => c.SourceNode == node))
        //                     {
        //                         // Save the original connection
        //                         originalConnections[node].Add((inputNode, dependentNode, param));

        //                         // Disconnect the current node from the dependent node
        //                         Disconnect(node, dependentNode, param);

        //                         // Avoid duplicate connections
        //                         if (!dependentConnections.Any(c => c.SourceNode == inputNode))
        //                         {
        //                             // Connect inputNode directly to the dependentNode
        //                             Connect(inputNode, dependentNode, param);
        //                         }
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // }

        private void RestoreConnections(AudioNode node)
        {
            // Restore the original connections
            if (originalConnections.ContainsKey(node))
            {
                foreach (var (source, destination, param, modType, strength) in originalConnections[node])
                {
                    // Find the ParameterConnection where the SourceNode is the source
                    var existingConnection = destination.AudioParameters[param]
                        .FirstOrDefault(c => c.SourceNode == source);

                    if (existingConnection != null)
                    {
                        // Disconnect the rerouted connection
                        Disconnect(source, destination, param);
                    }

                    // Check if the original node is already connected
                    var originalConnection = destination.AudioParameters[param]
                        .FirstOrDefault(c => c.SourceNode == node);

                    if (originalConnection == null)
                    {
                        // Reconnect the original node with the saved ModType and Strength
                        Connect(node, destination, param, modType, strength);
                    }
                }

                // Clear stored original connections
                originalConnections[node].Clear();
            }
        }


        // private void RestoreConnections(AudioNode node)
        // {
        //     // Restore the original connections
        //     if (originalConnections.ContainsKey(node))
        //     {
        //         foreach (var (source, destination, param) in originalConnections[node])
        //         {
        //             // Find the ParameterConnection where the SourceNode is the source
        //             var existingConnection = destination.AudioParameters[param]
        //                 .FirstOrDefault(c => c.SourceNode == source);

        //             if (existingConnection != null)
        //             {
        //                 // Disconnect the rerouted connection
        //                 Disconnect(source, destination, param);
        //             }

        //             // Check if the original node is already connected
        //             var originalConnection = destination.AudioParameters[param]
        //                 .FirstOrDefault(c => c.SourceNode == node);

        //             if (originalConnection == null)
        //             {
        //                 // Reconnect the original node
        //                 Connect(node, destination, param);
        //             }
        //         }

        //         // Clear stored original connections
        //         originalConnections[node].Clear();
        //     }
        // }


        // private void RerouteConnections(AudioNode node)
        // {
        //     if (!originalConnections.ContainsKey(node))
        //     {
        //         originalConnections[node] = new List<(AudioNode, AudioNode, AudioParam)>();
        //     }

        //     // Disconnect the node and reroute its inputs
        //     foreach (var param in node.AudioParameters.Keys)
        //     {
        //         foreach (var inputNode in node.AudioParameters[param])
        //         {
        //             foreach (var dependentNode in Nodes)
        //             {
        //                 if (dependentNode.AudioParameters.ContainsKey(param) && dependentNode.AudioParameters[param].Contains(node))
        //                 {
        //                     // Save the original connection
        //                     originalConnections[node].Add((inputNode, dependentNode, param));

        //                     // Disconnect the current node from the dependent node
        //                     Disconnect(node, dependentNode, param);

        //                     // Avoid duplicate connections
        //                     if (!dependentNode.AudioParameters[param].Contains(inputNode))
        //                     {
        //                         // Connect inputNode directly to the dependentNode
        //                         Connect(inputNode, dependentNode, param);
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // }

        // private void RestoreConnections(AudioNode node)
        // {
        //     // Restore the original connections
        //     if (originalConnections.ContainsKey(node))
        //     {
        //         foreach (var (source, destination, param) in originalConnections[node])
        //         {
        //             // Avoid duplicate disconnections
        //             if (destination.AudioParameters[param].Contains(source))
        //             {
        //                 // Disconnect the rerouted connection
        //                 Disconnect(source, destination, param);
        //             }

        //             // Avoid duplicate connections
        //             if (!destination.AudioParameters[param].Contains(node))
        //             {
        //                 // Reconnect the original node
        //                 Connect(node, destination, param);
        //             }
        //         }

        //         // Clear stored original connections
        //         originalConnections[node].Clear();
        //     }
        // }

        public void Process(double increment)
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
                //throw new InvalidOperationException("Cycle detected in the audio graph");
                return;
            }
            if (!visited.Contains(node))
            {
                if (node.Enabled) // Skip disabled nodes
                {
                    stack.Add(node);
                    visited.Add(node);

                    foreach (var paramlist in node.AudioParameters.Values)
                    {
                        foreach (var connection in paramlist)
                        {
                            AudioNode dependentNode = connection.SourceNode;
                            Visit(dependentNode, visited, stack);
                        }
                    }

                    stack.Remove(node);
                    SortedNodes.Add(node);
                }
            }
        }


        // private void Visit(AudioNode node, HashSet<AudioNode> visited, HashSet<AudioNode> stack)
        // {
        //     if (stack.Contains(node))
        //     {
        //         throw new InvalidOperationException("Cycle detected in the audio graph");
        //     }
        //     if (!visited.Contains(node))
        //     {
        //         if (node.Enabled) // Skip disabled nodes
        //         {
        //             stack.Add(node);
        //             visited.Add(node);

        //             foreach (var paramlist in node.AudioParameters.Values)
        //             {
        //                 foreach (AudioNode dependentNode in paramlist)
        //                 {
        //                     Visit(dependentNode, visited, stack);
        //                 }
        //             }

        //             stack.Remove(node);
        //             SortedNodes.Add(node);
        //         }
        //     }
        // }
    }
}
