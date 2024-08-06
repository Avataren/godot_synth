using System.Collections.Generic;

namespace Synth
{
    public class AudioGraph
    {
        static AudioGraph _instance = null;
        public static AudioGraph GetInstance()
        {
            if (_instance == null)
            {
                _instance = new AudioGraph();
            }
            return _instance;
        }

        public static void CreateNode<T>(string name) where T : AudioNode, new()
        {
            AudioNode node = new T
            {
                Name = name
            };
            GetInstance().RegisterNode(node);
        }

        protected List<AudioNode> Nodes = new List<AudioNode>();
        protected List<AudioNode> SortedNodes = null;

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
                node.Process(increment);
            }
        }

        void Connect(AudioNode source, AudioNode destination, AudioParam param)
        {
            if (!destination.AudioParameters.ContainsKey(param))
            {
                destination.AudioParameters[param] = new List<AudioNode>();
            }
            destination.AudioParameters[param].Add(source);
            SortedNodes = null;
        }

        void Disconnect(AudioNode source, AudioNode destination, AudioParam param)
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
            SortedNodes.Reverse();
        }

        void Visit(AudioNode node, List<AudioNode> visited)
        {
            if (visited.Contains(node))
            {
                return;
            }
            visited.Add(node);
            foreach (var paramlist in node.AudioParameters.Values){
                foreach (AudioNode param in paramlist){
                    Visit(param, visited);
                }
            }
            SortedNodes.Add(node);
        }

    }
}