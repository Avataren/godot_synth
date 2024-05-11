using System;
using System.Collections.Generic;
using System.Linq;

namespace Synth
{
    public class LFOManager
    {
        public enum LFOName
        {
            Frequency,
            Amplitude,
            Pitch,
            Filter
        }

        private LFONode[] lfos = new LFONode[4];  // Array to store the LFOs
        private Dictionary<LFOName, LFONode> routingTable;  // Routing table to map parameters to LFOs
        private Dictionary<LFONode, LFOName> reverseLookup;  // To ensure exclusive mapping

        public LFOManager()
        {
            // Initialize LFOs with default settings
            for (int i = 0; i < lfos.Length; i++)
            {
                lfos[i] = new LFONode(44100, 1.0f, 1.0f); // Initialize with some default values
            }

            // Initialize the routing and reverse lookup tables
            routingTable = new Dictionary<LFOName, LFONode>();
            reverseLookup = new Dictionary<LFONode, LFOName>();
        }

        public void RouteLFO(int lfoIndex, LFOName target)
        {
            if (lfoIndex < 0 || lfoIndex >= lfos.Length)
            {
                throw new ArgumentOutOfRangeException("lfoIndex", "Index is out of range of available LFOs.");
            }

            var lfo = lfos[lfoIndex];

            // Check if LFO is already routed and remove the old route
            if (reverseLookup.TryGetValue(lfo, out LFOName currentTarget))
            {
                if (currentTarget == target) return;  // No action needed if rerouting to the same target
                routingTable.Remove(currentTarget);
                reverseLookup.Remove(lfo);
            }

            routingTable[target] = lfo;
            reverseLookup[lfo] = target;
        }

        public LFONode GetRoutedLFO(LFOName target)
        {
            routingTable.TryGetValue(target, out LFONode lfo);
            return lfo;
        }

        public IEnumerable<LFOName> GetUnmappedLFONames()
        {
            return Enum.GetValues<LFOName>().Except(routingTable.Keys);
        }

        public void Process(float increment)
        {
            foreach (var lfo in lfos.Where(l => l.Enabled))
            {
                lfo.Process(increment);
            }
        }

        public void OpenGate()
        {
            foreach (var lfo in lfos)
            {
                lfo.OpenGate();
            }
        }

        public void CloseGate()
        {
            foreach (var lfo in lfos)
            {
                lfo.CloseGate();
            }
        }
    }
}
