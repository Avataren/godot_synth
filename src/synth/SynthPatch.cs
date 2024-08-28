using System;
using System.Collections.Generic;
using Godot;
using Synth;
public class SynthPatch
{
    private readonly object _lock = new object();
    public const int MaxOscillators = 5;
    public const int MaxLFOs = 2;
    public const int MaxEnvelopes = 3;
    public float PortamentoTime = 0.0f;
    List<WaveTableOscillatorNode> oscillators = new List<WaveTableOscillatorNode>();
    List<LFONode> LFOs = new List<LFONode>();
    // List<EnvelopeNode> AmpEnvelopes = new List<EnvelopeNode>();
    WaveTableBank waveTableBank;
    public AudioGraph graph { get; set; } = new AudioGraph();
    List<EnvelopeNode> envelopes = new List<EnvelopeNode>();
    ConstantNode freq;
    DelayEffectNode delayEffectNode;
    ReverbEffectNode reverbEffectNode;
    public FilterNode filterNode;
    PassThroughNode speakerNode;
    public NoiseNode noiseNode;
    public FuzzNode fuzzNode;
    MixerNode mix1;

    //Dictionary<int, float> NoteVelocityRegister = new Dictionary<int, float>();
    Stack<int> NoteVelocityRegister = new Stack<int>();

    public SynthPatch(WaveTableBank waveTableBank, int bufferSize, float sampleRate = 44100)
    {

        freq = graph.CreateNode<ConstantNode>("Freq");
        mix1 = graph.CreateNode<MixerNode>("Mix1");
        filterNode = graph.CreateNode<FilterNode>("MoogFilter");
        delayEffectNode = graph.CreateNode<DelayEffectNode>("DelayEffect");
        reverbEffectNode = graph.CreateNode<ReverbEffectNode>("ReverbEffect");
        speakerNode = graph.CreateNode<PassThroughNode>("Speaker");
        fuzzNode = graph.CreateNode<FuzzNode>("Fuzz");
        noiseNode = graph.CreateNode<NoiseNode>("Noise");
        for (int i = 0; i < MaxOscillators; i++)
        {
            var osc = graph.CreateNode<WaveTableOscillatorNode>("Osc" + i);
            oscillators.Add(osc);
            graph.Connect(osc, mix1, AudioParam.Input, ModulationType.Add);
            graph.Connect(freq, osc, AudioParam.Pitch, ModulationType.Add);
        }

        for (int i = 0; i < MaxEnvelopes; i++)
        {
            //CustomEnvelopes.Add(graph.CreateNode<EnvelopeNode>("CustomEnv" + i, BufferSize, SampleRate));
            var env = graph.CreateNode<EnvelopeNode>("Envelope" + (i + 1));
            envelopes.Add(env);
            if (i == 0)
            {
                graph.Connect(env, mix1, AudioParam.Gain, ModulationType.Multiply);
            }
        }

#if false
        float speed = 0.35f;
        for (int i = 0; i < 1000; i++)
        {
            // Rhythm pattern for the bassline
            double timeOffset = 0.75 * speed;  // Consistent rhythm, adjusted for speed
            double gateLength = 0.5 * speed;  // Slightly longer gate for a sustained bass sound

            // Define a chord progression or root note changes
            int[] rootNotes = { 36, 37, 41, 39 }; // C1, D1, F1, G1 (MIDI notes)
            int rootNote = rootNotes[(i / 16) % rootNotes.Length];  // Change root note every 16 steps (4 bars)

            // Low-low-high-high bass pattern with octave shifts, relative to the root note
            int[] bassPattern = { rootNote, rootNote, rootNote + 12, rootNote, rootNote + 10, rootNote + 12, rootNote, rootNote + 12 };  // Adjusted to the current root note
            int note = bassPattern[i % bassPattern.Length];  // Cycle through the pattern

            // Set the frequency based on the note
            freq.SetValueAtTime(440.0f * (float)Math.Pow(2.0, (note - 69) / 12.0) * 0.5, i * timeOffset);

            // Schedule the gate open and close for each step
            for (int j = 0; j < MaxEnvelopes; j++)
            {
                envelopes[j].ScheduleGateOpen(i * timeOffset, true);
                envelopes[j].ScheduleGateClose(i * timeOffset + gateLength);
            }
        }
#endif

        for (int i = 0; i < MaxLFOs; i++)
        {
            LFOs.Add(graph.CreateNode<LFONode>("LFO" + i));
        }
        graph.Connect(noiseNode, mix1, AudioParam.Input, ModulationType.Add);
        graph.Connect(mix1, fuzzNode, AudioParam.StereoInput, ModulationType.Add);
        graph.Connect(fuzzNode, filterNode, AudioParam.StereoInput, ModulationType.Add);

        //graph.Connect(mix1, moogFilterNode, AudioParam.StereoInput, ModulationType.Add);
        graph.Connect(filterNode, delayEffectNode, AudioParam.StereoInput, ModulationType.Add);
        graph.Connect(delayEffectNode, reverbEffectNode, AudioParam.StereoInput, ModulationType.Add);
        graph.Connect(reverbEffectNode, speakerNode, AudioParam.StereoInput, ModulationType.Add);


        graph.TopologicalSortWorkingGraph();
        GD.Print("Initial setup:");

        // disable various nodes by default
        for (int i = 1; i < oscillators.Count; i++)
        {
            graph.SetNodeEnabled(oscillators[i], false);
        }
        this.waveTableBank = waveTableBank;
        //graph.SetNodeEnabled(filterNode, false);
        graph.SetNodeEnabled(noiseNode, false);
        graph.SetNodeEnabled(fuzzNode, false);
        graph.SetNodeEnabled(reverbEffectNode, false);
        graph.SetNodeEnabled(delayEffectNode, false);
    }

    public void SetMasterGain(float gain)
    {
        //speakerNode.Gain = gain;
        mix1.Gain = gain;
    }

    public SynthType[] CreateWaveform(WaveTableWaveType waveType, int bufSize)
    {
        return waveTableBank.GenerateFullWaveform(waveType, bufSize);
    }

    public void SetReverbEffect_Enabled(bool enabled)
    {
        GD.Print("Setting reverb enabled to " + enabled);
        graph.SetNodeEnabled(reverbEffectNode, enabled);
        reverbEffectNode.Mute();
        //graph.DebugPrint();
    }

    public void SetReverbEffect_RoomSize(float roomSize)
    {
        reverbEffectNode.RoomSize = roomSize;
    }

    public void SetReverbEffect_Damp(float damp)
    {
        // GD.Print("Setting damp to " + damp);
        reverbEffectNode.Damp = damp;
    }

    public void SetReverbEffect_Wet(float wet)
    {
        reverbEffectNode.Wet = wet;
    }

    public void SetReverbEffect_Dry(float dry)
    {
        reverbEffectNode.Dry = dry;
    }

    public void SetReverbEffect_Width(float width)
    {
        reverbEffectNode.Width = width;
    }

    public void SetDelayEffect_Enabled(bool enabled)
    {
        GD.Print("Setting delay enabled to " + enabled);
        graph.SetNodeEnabled(delayEffectNode, enabled);
        delayEffectNode.Mute();
        //graph.DebugPrint();
    }

    public void SetDelayEffect_Delay(int delay)
    {
        delayEffectNode.DelayTimeInMs = delay;
    }

    public void SetDelayEffect_Feedback(float feedback)
    {
        delayEffectNode.Feedback = feedback;
    }

    public void SetDelayEffect_DryMix(float dryMix)
    {
        delayEffectNode.DryMix = dryMix;
    }

    public void SetDelayEffect_WetMix(float wetMix)
    {
        delayEffectNode.WetMix = wetMix;
    }

    public void SetDrive(float drive)
    {
        //filterNode.Drive = drive;
        GD.Print("Not implemented");
    }

    public void SetCutoff(float cutoff)
    {
        filterNode.CutOff = cutoff;
    }
    public void SetResonance(float resonance)
    {
        filterNode.Resonance = resonance;
    }
    public void SetAttack(float attack, int oscillatorIndex = 0)
    {
        envelopes[oscillatorIndex].AttackTime = attack;
    }
    public void SetDecay(float decay, int oscillatorIndex = 0)
    {
        envelopes[oscillatorIndex].DecayTime = decay;
    }
    public void SetSustain(float sustain, int oscillatorIndex = 0)
    {
        envelopes[oscillatorIndex].SustainLevel = sustain;
    }
    public void SetRelease(float release, int oscillatorIndex = 0)
    {
        envelopes[oscillatorIndex].ReleaseTime = release;
    }

    // public void SetCustomAttack(float attack, int idx)
    // {
    //     GD.Print("Setting custom envelope " + idx + " attack to " + attack);
    //     CustomEnvelopes[idx].AttackTime = attack;
    // }

    // public void SetCustomDecay(float decay, int idx)
    // {
    //     // GD.Print("Setting custom envelope " + idx + " decay to " + decay);
    //     CustomEnvelopes[idx].DecayTime = decay;
    // }

    // public void SetCustomSustain(float sustain, int idx)
    // {
    //     // GD.Print("Setting custom envelope " + idx + " sustain to " + sustain);
    //     CustomEnvelopes[idx].SustainLevel = sustain;
    // }

    // public void SetCustomRelease(float release, int idx)
    // {
    //     // GD.Print("Setting custom envelope " + idx + " release to " + release);
    //     CustomEnvelopes[idx].ReleaseTime = release;
    // }

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

    public void SetBalance(float balance, int OscillatorIndex = -1)
    {
        if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
        {
            //GD.Print("Setting balance for oscillator " + OscillatorIndex + " to " + balance);
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

    // public void SetADSREnabled(bool enabled, int EnvelopeIndex = -1)
    // {
    //     if (EnvelopeIndex >= 0 && EnvelopeIndex < AmpEnvelopes.Count)
    //     {
    //         graph.SetNodeEnabled(AmpEnvelopes[EnvelopeIndex], enabled);
    //         return;
    //     }

    //     for (int idx = 0; idx < AmpEnvelopes.Count; idx++)
    //     {
    //         graph.SetNodeEnabled(AmpEnvelopes[idx], enabled);
    //     }
    // }


    public void SetLFOWaveform(string waveTypeName, int LFOIndex = -1)
    {
        //convert from waveTypeName to enum
        LFONode.LFOWaveform waveform = (LFONode.LFOWaveform)System.Enum.Parse(typeof(LFONode.LFOWaveform), waveTypeName);
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

    // public void SetAttack(float attack, int EnvelopeIndex = -1)
    // {
    //     if (EnvelopeIndex >= 0 && EnvelopeIndex < AmpEnvelopes.Count)
    //     {
    //         AmpEnvelopes[EnvelopeIndex].AttackTime = attack;
    //         return;
    //     }

    //     for (int idx = 0; idx < AmpEnvelopes.Count; idx++)
    //     {
    //         AmpEnvelopes[idx].AttackTime = attack;
    //     }
    // }

    // public void SetDecay(float decay, int EnvelopeIndex = -1)
    // {
    //     if (EnvelopeIndex >= 0 && EnvelopeIndex < AmpEnvelopes.Count)
    //     {
    //         AmpEnvelopes[EnvelopeIndex].DecayTime = decay;
    //         return;
    //     }

    //     for (int idx = 0; idx < AmpEnvelopes.Count; idx++)
    //     {
    //         AmpEnvelopes[idx].DecayTime = decay;
    //     }
    // }

    // public void SetSustain(float sustain, int EnvelopeIndex = -1)
    // {
    //     if (EnvelopeIndex >= 0 && EnvelopeIndex < AmpEnvelopes.Count)
    //     {
    //         AmpEnvelopes[EnvelopeIndex].SustainLevel = sustain;
    //         return;
    //     }

    //     for (int idx = 0; idx < AmpEnvelopes.Count; idx++)
    //     {
    //         AmpEnvelopes[idx].SustainLevel = sustain;
    //     }
    // }

    // public void SetRelease(float release, int EnvelopeIndex = -1)
    // {
    //     if (EnvelopeIndex >= 0 && EnvelopeIndex < AmpEnvelopes.Count)
    //     {
    //         AmpEnvelopes[EnvelopeIndex].ReleaseTime = release;
    //         return;
    //     }

    //     for (int idx = 0; idx < AmpEnvelopes.Count; idx++)
    //     {
    //         AmpEnvelopes[idx].ReleaseTime = release;
    //     }
    // }

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

    // public void SetADSR(double attack, double decay, double sustain, double release, int EnvelopeIndex = -1)
    // {
    //     if (EnvelopeIndex >= 0 && EnvelopeIndex < AmpEnvelopes.Count)
    //     {
    //         AmpEnvelopes[EnvelopeIndex].AttackTime = attack;
    //         AmpEnvelopes[EnvelopeIndex].DecayTime = decay;
    //         AmpEnvelopes[EnvelopeIndex].SustainLevel = sustain;
    //         AmpEnvelopes[EnvelopeIndex].ReleaseTime = release;
    //         return;
    //     }

    //     for (int idx = 0; idx < AmpEnvelopes.Count; idx++)
    //     {
    //         AmpEnvelopes[idx].AttackTime = attack;
    //         AmpEnvelopes[idx].DecayTime = decay;
    //         AmpEnvelopes[idx].SustainLevel = sustain;
    //         AmpEnvelopes[idx].ReleaseTime = release;
    //     }
    // }

    public WaveTableOscillatorNode GetOscillator(int idx)
    {
        return oscillators[idx];
    }

    public EnvelopeNode GetEnvelope(int idx)
    {
        return envelopes[idx];
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

    public void ClearKeyStack()
    {
        GD.Print("Clearing key stack");
        lock (_lock)
        {
            NoteVelocityRegister.Clear();
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

    public void NoteOn(int note, float velocity = 1.0f)
    {
        lock (_lock)
        {
            if (NoteVelocityRegister.Contains(note))
            {
                GD.Print("Note already playing");
                //this will cause an issue when key is release
                return;
            }

            float newFrequency = 440.0f * (float)Math.Pow(2.0, (note - 69) / 12.0);
            var now = AudioContext.Instance.CurrentTimeInSeconds;

            if (NoteVelocityRegister.Count == 0 || PortamentoTime < 0.001)
            {
                // No note is currently playing, start the new note with full envelope
                NoteVelocityRegister.Push(note);

                freq.SetValueAtTime(newFrequency, now);

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
            else
            {
                // A note is already playing, apply portamento (legato)
                NoteVelocityRegister.Push(note);
                freq.ExponentialRampToValueAtTime(newFrequency, now + PortamentoTime);  // Glide to new note
            }
        }
    }

    public void NoteOff(int note)
    {
        lock (_lock)
        {
            var now = AudioContext.Instance.CurrentTimeInSeconds;

            // Remove the released note from the stack
            var tempStack = new Stack<int>();
            while (NoteVelocityRegister.Count > 0)
            {
                var n = NoteVelocityRegister.Pop();
                if (n != note)
                {
                    tempStack.Push(n);
                }
            }

            // Restore the remaining notes back into the original stack
            while (tempStack.Count > 0)
            {
                NoteVelocityRegister.Push(tempStack.Pop());
            }

            // Now determine the behavior based on the remaining notes
            if (NoteVelocityRegister.Count > 0)
            {
                // If there's another note in the stack, glide to it
                int nextNote = NoteVelocityRegister.Peek();
                float nextFrequency = 440.0f * (float)Math.Pow(2.0, (nextNote - 69) / 12.0);
                if (PortamentoTime > 0.001)
                {
                    freq.LinearRampToValueAtTime(nextFrequency, now + PortamentoTime);
                }
                else
                {
                    freq.SetValueAtTime(nextFrequency, now);
                }
            }
            else
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
    }


    public PassThroughNode Process(double increment)
    {
        lock (_lock)
        {
            graph.Process(increment);
            //var node = graph.GetNode("Speaker") as PassThroughNode;
            return speakerNode;
        }
    }
}