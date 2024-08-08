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
                TopoligicalSort();
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
                TopoligicalSort();
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

        void TopoligicalSort()
        {
            SortedNodes = new List<AudioNode>();
            List<AudioNode> visited = new List<AudioNode>();
            foreach (AudioNode node in Nodes)
            {
                Visit(node, visited);
            }
            //SortedNodes.Reverse();
        }

        void Visit(AudioNode node, List<AudioNode> visited)
        {
            if (visited.Contains(node))
            {
                return;
            }
            visited.Add(node);
            foreach (var paramlist in node.AudioParameters.Values)
            {
                foreach (AudioNode param in paramlist)
                {
                    Visit(param, visited);
                }
            }
            SortedNodes.Add(node);
        }

    }
}