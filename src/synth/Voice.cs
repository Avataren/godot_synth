using System;
using System.Collections.Generic;
using Godot;

namespace Synth
{
    public class Voice : AudioNode
    {
        private const int MaxOscillators = 6;
        private const int MaxEnvelopes = 3;
        private const int MaxLFOs = 3;
        FilterNode filterNode;
        List<WaveTableOscillatorNode> oscillators = new List<WaveTableOscillatorNode>();
        List<LFONode> LFOs = new List<LFONode>();
        public List<EnvelopeNode> envelopes = new List<EnvelopeNode>();
        MixerNode mixerNode;
        ConstantNode freqNode;
        NoiseNode noiseNode;
        public AudioGraph graph { get; set; } = new AudioGraph();
        // let Voice inherit audio node and have a dedicated graph for each?
        // patch can then mix the voices as needed
        // and voices can easily be processed in parallel
        public Voice()
        {
            Initialize();
        }

        public AudioNode GetOuputNode()
        {
            return filterNode;
        }

        private void Initialize()
        {
            freqNode = graph.CreateNode<ConstantNode>("Freq");
            mixerNode = graph.CreateNode<MixerNode>("Mix");
            filterNode = graph.CreateNode<FilterNode>("MoogFilter");
            noiseNode = graph.CreateNode<NoiseNode>("Noise");
            for (int i = 0; i < MaxOscillators; i++)
            {
                var osc = graph.CreateNode<WaveTableOscillatorNode>("Osc" + i);
                oscillators.Add(osc);
                graph.Connect(osc, mixerNode, AudioParam.Input, ModulationType.Add);
                graph.Connect(freqNode, osc, AudioParam.Pitch, ModulationType.Add);
            }

            for (int i = 0; i < MaxEnvelopes; i++)
            {
                //CustomEnvelopes.Add(graph.CreateNode<EnvelopeNode>("CustomEnv" + i, BufferSize, SampleRate));
                var env = graph.CreateNode<EnvelopeNode>("Envelope" + (i + 1));
                envelopes.Add(env);
                if (i == 0)
                {
                    graph.Connect(env, mixerNode, AudioParam.Gain, ModulationType.Multiply);
                }
            }

            for (int i = 0; i < MaxLFOs; i++)
            {
                LFOs.Add(graph.CreateNode<LFONode>("LFO" + i));
            }

            graph.Connect(noiseNode, mixerNode, AudioParam.Input, ModulationType.Add);
            graph.Connect(mixerNode, filterNode, AudioParam.StereoInput, ModulationType.Add);
            // will filterNode be the last node in the chain?
            graph.SetNodeEnabled(noiseNode, false);
            for (int i = 1; i < oscillators.Count; i++)
            {
                graph.SetNodeEnabled(oscillators[i], false);
            }            
        }

        public void NoteOn(int note, float velocity = 1.0f)
        {
            // if (NoteVelocityRegister.Contains(note))
            // {
            //     GD.Print("Note already playing");
            //     //this will cause an issue when key is release
            //     return;
            // }
            GD.Print("Voice noite on ", note);
            float newFrequency = 440.0f * (float)Math.Pow(2.0, (note - 69) / 12.0);
            var now = AudioContext.Instance.CurrentTimeInSeconds;

            //if (NoteVelocityRegister.Count == 0 || PortamentoTime < 0.001)
            {
                // No note is currently playing, start the new note with full envelope
                //NoteVelocityRegister.Push(note);

                freqNode.SetValueAtTime(newFrequency, now);

                foreach (var env in envelopes)
                {
                    if (env.Enabled)
                    {
                        env.ScheduleGateOpen(now, true);  // Open with full envelope
                    }
                }
                foreach (var osc in oscillators)
                {
                    osc.ScheduleGateOpen(now, true);  // Open oscillator gates
                }
            }
            // else
            // {
            //     A note is already playing, apply portamento (legato)
            //     NoteVelocityRegister.Push(note);
            //     freqNode.ExponentialRampToValueAtTime(newFrequency, now + PortamentoTime);  // Glide to new note
            // }
        }

        public void NoteOff(int note)
        {
            GD.Print("Voice noite off ", note);
            var now = AudioContext.Instance.CurrentTimeInSeconds;

            // Remove the released note from the stack
            // var tempStack = new Stack<int>();
            // while (NoteVelocityRegister.Count > 0)
            // {
            //     var n = NoteVelocityRegister.Pop();
            //     if (n != note)
            //     {
            //         tempStack.Push(n);
            //     }
            // }

            // Restore the remaining notes back into the original stack
            // while (tempStack.Count > 0)
            // {
            //     NoteVelocityRegister.Push(tempStack.Pop());
            // }

            // Now determine the behavior based on the remaining notes
            // if (NoteVelocityRegister.Count > 0)
            // {
            //     // If there's another note in the stack, glide to it
            //     int nextNote = NoteVelocityRegister.Peek();
            //     float nextFrequency = 440.0f * (float)Math.Pow(2.0, (nextNote - 69) / 12.0);
            //     if (PortamentoTime > 0.001)
            //     {
            //         freq.LinearRampToValueAtTime(nextFrequency, now + PortamentoTime);
            //     }
            //     else
            //     {
            //         freq.SetValueAtTime(nextFrequency, now);
            //     }
            // }
            // else
            {
                // No more notes, stop the sound
                foreach (var env in envelopes)
                {
                    env.ScheduleGateClose(now);  // Close envelope gates
                }
                foreach (var osc in oscillators)
                {
                    osc.ScheduleGateClose(now);  // Close oscillator gates
                }
            }            
        }

        public override void Process(double increment)
        {
            graph.Process(increment);
        }
    }

}