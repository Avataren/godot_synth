using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Synth
{
    public class AudioGraph
    {
        // static AudioGraph _instance = null;
        // public static AudioGraph GetInstance()
        // {
        //     if (_instance == null)
        //     {
        //         _instance = new AudioGraph();
        //     }
        //     return _instance;
        // }

        public T CreateNode<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string name, int bufferSize, float sampleRate = 44100) where T : AudioNode
        {
            var constructorInfo = typeof(T).GetConstructor([typeof(int), typeof(float)]) ?? throw new InvalidOperationException($"Type {typeof(T)} does not have a constructor with parameters (int, float).");

            T node = (T)constructorInfo.Invoke([bufferSize, sampleRate]);
            node.Name = name;

            RegisterNode(node);
            return node;
        }

        protected List<AudioNode> Nodes = new List<AudioNode>();
        protected List<AudioNode> SortedNodes = null;

        public AudioNode GetNode(string name)
        {
            return Nodes.Find(node => node.Name == name);
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
        public void RegisterNode(AudioNode node)
        {
            Nodes.Add(node);
            SortedNodes = null;
        }

        public void RemoveNode(string name)
        {
            Nodes.RemoveAll(node => node.Name == name);
            SortedNodes = null;
        }

        public void Process(float increment)
        {
            if (SortedNodes == null)
            {
                TopologicalSort();
            }

            foreach (AudioNode node in SortedNodes)
            {
                if (!node.Enabled)
                {
                    continue;
                }
                node.Process(increment);
            }
        }

        public void Connect(AudioNode source, AudioNode destination, AudioParam param)
        {
            if (!destination.AudioParameters.ContainsKey(param))
            {
                destination.AudioParameters[param] = new List<AudioNode>();
            }
            destination.AudioParameters[param].Add(source);
            SortedNodes = null;
        }

        public void Disconnect(AudioNode source, AudioNode destination, AudioParam param)
        {
            if (destination.AudioParameters.ContainsKey(param))
            {
                destination.AudioParameters[param].Remove(source);
            }
            SortedNodes = null;
        }

        void TopologicalSort()
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
            // SortedNodes should now be in the correct order without needing to reverse.
        }

        void Visit(AudioNode node, HashSet<AudioNode> visited, HashSet<AudioNode> stack)
        {
            if (stack.Contains(node))
            {
                throw new InvalidOperationException("Cycle detected in the audio graph");
            }
            if (!visited.Contains(node))
            {
                stack.Add(node);
                visited.Add(node);
                // Recursively visit all dependencies first
                foreach (var paramlist in node.AudioParameters.Values)
                {
                    foreach (AudioNode dependentNode in paramlist)
                    {
                        Visit(dependentNode, visited, stack);
                    }
                }
                stack.Remove(node);
                // Add the node to the sorted list after all its dependencies are already added
                SortedNodes.Add(node);
            }
        }



    }
}