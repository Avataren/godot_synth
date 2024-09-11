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
        public FilterNode filterNode;
        List<WaveTableOscillatorNode> oscillators = new List<WaveTableOscillatorNode>();
        List<LFONode> LFOs = new List<LFONode>();
        public List<EnvelopeNode> envelopes = new List<EnvelopeNode>();
        WaveTableBank waveTableBank;
        public MixerNode mixerNode;
        ConstantNode freqNode;
        NoiseNode noiseNode;
        public AudioGraph graph { get; set; } = new AudioGraph();
        // let Voice inherit audio node and have a dedicated graph for each?
        // patch can then mix the voices as needed
        // and voices can easily be processed in parallel
        public Voice(WaveTableBank waveTableBank)
        {
            this.waveTableBank = waveTableBank;
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

        public void Connect(string srcName, string dstName, string param, ModulationType modType, float strength = 1.0f)
        {
            var srcNode = graph.GetNode(srcName);
            var dstNode = graph.GetNode(dstName);
            var paramEnum = (AudioParam)Enum.Parse(typeof(AudioParam), param);
            if (srcName.StartsWith("Osc"))
            {
                graph.Disconnect(srcNode, graph.GetNode("Mix"), AudioParam.Input);
            }
            graph.Connect(srcNode, dstNode, paramEnum, modType, strength);
        }

        public void Disconnect(string srcName, string dstName, string param)
        {
            var srcNode = graph.GetNode(srcName);
            var dstNode = graph.GetNode(dstName);
            var paramEnum = (AudioParam)Enum.Parse(typeof(AudioParam), param);
            graph.Disconnect(srcNode, dstNode, paramEnum);
            if (srcName.StartsWith("Osc"))
            {
                graph.Connect(srcNode, graph.GetNode("Mix"), AudioParam.Input, ModulationType.Add, 1.0f);
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

        public void SetFeedback(float feedback, int OscillatorIndex = -1)
        {
            if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
            {
                oscillators[OscillatorIndex].SelfModulationStrength = feedback;
                return;
            }

            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                oscillators[idx].SelfModulationStrength = feedback;
            }
        }

        public void Silence()
        {

            // lock (_lock)
            {
                // NoteVelocityRegister.Clear();
                var now = AudioContext.Instance.CurrentTimeInSeconds;

                foreach (var env in envelopes)
                {
                    if (env.Enabled)
                    {
                        env.ScheduleGateClose(now);  // Open with full envelope
                    }
                }
                foreach (var osc in oscillators)
                {
                    osc.ScheduleGateClose(now);  // Open oscillator gates
                }
            }
        }

        public void SetBalance(float balance, int OscillatorIndex = -1)
        {
            if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
            {
                oscillators[OscillatorIndex].Balance = balance;
                return;
            }

            for (int idx = 0; idx < oscillators.Count; idx++)
            {

                oscillators[idx].Balance = balance;
            }
        }

        public void SetModulationStrength(float strength, int OscillatorIndex = -1)
        {
            GD.Print("Setting modulation strength for oscillator " + OscillatorIndex + " to " + strength);
            if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
            {

                oscillators[OscillatorIndex].ModulationStrength = strength;
                return;
            }

            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                oscillators[idx].ModulationStrength = strength;
            }
        }

        public void SetPWM(float pwm, int OscillatorIndex = -1)
        {
            if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
            {
                oscillators[OscillatorIndex].PWMDutyCycle = pwm;
                return;
            }

            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                oscillators[idx].PWMDutyCycle = pwm;
            }
        }


        public void SetDetuneOctaves(float detuneOctaves, int OscillatorIndex = -1)
        {
            if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
            {
                oscillators[OscillatorIndex].DetuneOctaves = detuneOctaves;
                return;
            }

            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                oscillators[idx].DetuneOctaves = detuneOctaves;
            }
        }

        public void SetDetuneSemi(float detuneSemi, int OscillatorIndex = -1)
        {
            if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
            {
                oscillators[OscillatorIndex].DetuneSemitones = detuneSemi;
                return;
            }

            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                oscillators[idx].DetuneSemitones = detuneSemi;
            }
        }

        public void SetDetuneCents(float detuneCents, int OscillatorIndex = -1)
        {
            if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
            {
                oscillators[OscillatorIndex].DetuneCents = detuneCents;
                return;
            }

            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                oscillators[idx].DetuneCents = detuneCents;
            }
        }

        public void SetLFOWaveform(LFOWaveform waveform, int LFOIndex = -1)
        {
            //convert from waveTypeName to enum
            if (LFOIndex >= 0 && LFOIndex < LFOs.Count)
            {
                LFOs[LFOIndex].CurrentWaveform = waveform;
                return;
            }

            for (int idx = 0; idx < LFOs.Count; idx++)
            {
                LFOs[idx].CurrentWaveform = waveform;
            }
        }

        public void SetLFOFrequency(float freq, int LFOIndex = -1)
        {
            GD.Print("Trying to set LFO frequency to " + freq);
            if (LFOIndex >= 0 && LFOIndex < LFOs.Count)
            {
                GD.Print("Setting LFO " + LFOIndex + " frequency to " + freq);
                LFOs[LFOIndex].Frequency = freq;
                return;
            }

            for (int idx = 0; idx < LFOs.Count; idx++)
            {
                LFOs[idx].Frequency = freq;
            }
        }

        public void SetLFOGain(float gain, int LFOIndex = -1)
        {
            if (LFOIndex >= 0 && LFOIndex < LFOs.Count)
            {
                GD.Print("Setting LFO " + LFOIndex + " gain to " + gain);
                LFOs[LFOIndex].Amplitude = gain;
                return;
            }

            for (int idx = 0; idx < LFOs.Count; idx++)
            {
                LFOs[idx].Amplitude = gain;
            }
        }

        public void SetOscillatorPhaseOffset(float phase, int OscillatorIndex = -1)
        {
            if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
            {
                oscillators[OscillatorIndex].PhaseOffset = phase;
                return;
            }

            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                oscillators[idx].PhaseOffset = phase;
            }
        }

        public void SetHardSync(bool enabled, int OscillatorIndex = -1)
        {
            if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
            {
                oscillators[OscillatorIndex].HardSync = enabled;
                return;
            }
            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                oscillators[idx].HardSync = enabled;
            }
        }

        public void SetAmplitude(float amplitude, int OscillatorIndex = -1)
        {
            if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
            {
                oscillators[OscillatorIndex].Amplitude = amplitude;
                return;
            }

            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                oscillators[idx].Amplitude = amplitude;
            }
        }

        public void SetOscillatorEnabled(bool enabled, int OscillatorIndex = -1)
        {
            if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
            {
                graph.SetNodeEnabled(oscillators[OscillatorIndex], enabled);
                //graph.DebugPrint();
                ResetAllOscillatorPhases();
                return;
            }

            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                graph.SetNodeEnabled(oscillators[idx], enabled);
                //graph.DebugPrint();
            }
            ResetAllOscillatorPhases();
        }

        public void ResetAllOscillatorPhases()
        {
            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                oscillators[idx].ResetPhase();
            }
        }

        public void SetWaveform(WaveTableWaveType waveType, int OscillatorIndex = -1)
        {
            if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
            {
                if (waveType == WaveTableWaveType.PWM)
                {
                    oscillators[OscillatorIndex].IsPWM = true;
                    oscillators[OscillatorIndex].WaveTableMemory = waveTableBank.GetWave(WaveTableWaveType.SAWTOOTH);
                    oscillators[OscillatorIndex].UpdateSampleFunction();
                }
                else
                {
                    oscillators[OscillatorIndex].IsPWM = false;
                    oscillators[OscillatorIndex].WaveTableMemory = waveTableBank.GetWave(waveType);
                    oscillators[OscillatorIndex].UpdateSampleFunction();
                }
                return;
            }

            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                if (waveType == WaveTableWaveType.PWM)
                {
                    oscillators[idx].IsPWM = true;
                    oscillators[idx].WaveTableMemory = waveTableBank.GetWave(WaveTableWaveType.SAWTOOTH);
                    oscillators[idx].UpdateSampleFunction();
                }
                else
                {
                    oscillators[idx].IsPWM = false;
                    oscillators[idx].WaveTableMemory = waveTableBank.GetWave(waveType);
                    oscillators[idx].UpdateSampleFunction();
                }
            }
        }



    }

}