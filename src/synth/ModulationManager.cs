
using System;
using System.Collections;
using System.Collections.Generic;

namespace Synth
{

    public class ModulationConnection {
        public AudioNode Source { get; set; } // typically an LFO
        public AudioNode Destination { get; set; } // typically an oscillator
        public string DestinationProperty { get; set; }
        public AudioParam Amount { get; set; }
        public bool HardSync { get; set; }

        public ModulationConnection(AudioNode source, AudioNode destination, string destinationProperty, AudioParam amount, bool hardSync){
            Source = source;
            Destination = destination;
            DestinationProperty = destinationProperty;
            Amount = amount;
            HardSync = hardSync;
        }
    }

    public class ModulationManager{

        public Dictionary<AudioNode, List<ModulationConnection>> ModulationConnections = new Dictionary<AudioNode, List<ModulationConnection>>();

        public void AddConnection(AudioNode source, AudioNode destination, string destinationProperty, AudioParam amount, bool hardSync){
            if(!ModulationConnections.ContainsKey(destination)){
                ModulationConnections[destination] = new List<ModulationConnection>();
            }
            ModulationConnections[destination].Add(new ModulationConnection(source, destination, destinationProperty, amount, hardSync));
        }

        public void RemoveConnection(AudioNode source, AudioNode destination){
            if(ModulationConnections.ContainsKey(destination)){
                ModulationConnections[destination].RemoveAll(connection => connection.Source == source);
            }
        }

        public ModulationConnection GetModulationConnection(AudioNode destination, AudioNode source){
            if(ModulationConnections.ContainsKey(destination)){
                return ModulationConnections[destination].Find(connection => connection.Source == source);
            }
            return null;
        }
    }
}