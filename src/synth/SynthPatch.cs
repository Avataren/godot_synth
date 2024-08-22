using System;
using System.Collections.Generic;
using Godot;
using Synth;
public class SynthPatch
{
    private readonly object _lock = new object();
    public const int MaxOscillators = 5;
    public const int MaxLFOs = 4;
    public const int MaxEnvelopes = 5;
    
    List<WaveTableOscillatorNode> oscillators = new List<WaveTableOscillatorNode>();
    List<LFONode> LFOs = new List<LFONode>();
    List<EnvelopeNode> AmpEnvelopes = new List<EnvelopeNode>();
    List<EnvelopeNode> CustomEnvelopes = new List<EnvelopeNode>();

    // EnvelopeNode ampEnvelope;
    WaveTableBank waveTableBank;
    public AudioGraph graph { get; set; } = new AudioGraph();
    List<EnvelopeNode> envelopes = new List<EnvelopeNode>();
    ConstantNode freq;
    DelayEffectNode delayEffectNode;
    ReverbEffectNode reverbEffectNode;
    MoogFilterNode moogFilterNode;
    PassThroughNode speakerNode;

    public SynthPatch(WaveTableBank waveTableBank, int bufferSize, float sampleRate = 44100)
    {
        
//        BufferSize = bufferSize * Oversampling;
        //SampleRate = sampleRate;
        freq = graph.CreateNode<ConstantNode>("Freq");

        var mix1 = graph.CreateNode<MixerNode>("Mix1");
        moogFilterNode = graph.CreateNode<MoogFilterNode>("MoogFilter");
        delayEffectNode = graph.CreateNode<DelayEffectNode>("DelayEffect");
        reverbEffectNode = graph.CreateNode<ReverbEffectNode>("ReverbEffect");
        speakerNode = graph.CreateNode<PassThroughNode>("Speaker");
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
        for (int i = 0; i < 100; i++)
        {
            envelopes[0].ScheduleGateOpen(i * 0.5);
            envelopes[0].ScheduleGateClose(i * 0.5 + 0.3);
            int note = i % 12 + 60;

            freq.SetValueAtTime(440.0f * (float)Math.Pow(2.0, (note - 69) / 12.0), i * 0.5);
        }
#endif

        // for (int i = 0; i < MaxEnvelopes; i++)
        // {
        //     CustomEnvelopes.Add(graph.CreateNode<EnvelopeNode>("CustomEnv" + i, BufferSize, SampleRate));
        // }

        for (int i = 0; i < MaxLFOs; i++)
        {
            LFOs.Add(graph.CreateNode<LFONode>("LFO" + i));
        }
        //ampEnvelope = graph.CreateNode<EnvelopeNode>("Env1", BufferSize, SampleRate);

        //envelopes.Add(ampEnvelope);

        //graph.Connect(ampEnvelope, mix1, AudioParam.Gain, ModulationType.Multiply);
        graph.Connect(mix1, moogFilterNode, AudioParam.StereoInput, ModulationType.Add);
        graph.Connect(moogFilterNode, delayEffectNode, AudioParam.StereoInput, ModulationType.Add);
        graph.Connect(delayEffectNode, reverbEffectNode, AudioParam.StereoInput, ModulationType.Add);
        graph.Connect(reverbEffectNode, speakerNode, AudioParam.StereoInput, ModulationType.Add);

        this.waveTableBank = waveTableBank;
        // Initialize the patch
        // for (int idx = 0; idx < MaxOscillators; idx++)
        // {
        //     oscillators.Add(new WaveTableOscillatorNode(BufferSize, SampleRate));
        //     AmpEnvelopes.Add(new EnvelopeNode(BufferSize, SampleRate){
        //         Enabled = false
        //     });
        // }

        oscillators[0].Enabled = true;
        graph.TopologicalSort();

        //disable effects by default
        reverbEffectNode.RoomSize = 0.5f;
        graph.SetNodeEnabled(reverbEffectNode, false);
        graph.SetNodeEnabled(delayEffectNode, false);
    }

    public float[] CreateWaveform(WaveTableWaveType waveType, int bufSize)
    {
        return waveTableBank.GenerateFullWaveform(waveType, bufSize);
    }

    public void SetReverbEffect_Enabled(bool enabled)
    {
        graph.SetNodeEnabled(reverbEffectNode, enabled);
        reverbEffectNode.Mute();
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
        graph.SetNodeEnabled(delayEffectNode, enabled);
        delayEffectNode.Mute();
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
        moogFilterNode.Drive = drive;
    }

    public void SetCutoff(float cutoff)
    {
        moogFilterNode.CutOff = cutoff;
    }
    public void SetResonance(float resonance)
    {
        moogFilterNode.Resonance = resonance;
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
        //GD.Print("Setting modulation strength for oscillator " + OscillatorIndex + " to " + strength);
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

    public void SetADSREnabled(bool enabled, int EnvelopeIndex = -1)
    {
        if (EnvelopeIndex >= 0 && EnvelopeIndex < AmpEnvelopes.Count)
        {
            graph.SetNodeEnabled(AmpEnvelopes[EnvelopeIndex], enabled);
            return;
        }

        for (int idx = 0; idx < AmpEnvelopes.Count; idx++)
        {
            graph.SetNodeEnabled(AmpEnvelopes[idx], enabled);
        }
    }


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
            ResetAllOscillatorPhases();
            return;
        }

        for (int idx = 0; idx < oscillators.Count; idx++)
        {
            graph.SetNodeEnabled(oscillators[idx], enabled);
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

    public void SetADSR(double attack, double decay, double sustain, double release, int EnvelopeIndex = -1)
    {
        if (EnvelopeIndex >= 0 && EnvelopeIndex < AmpEnvelopes.Count)
        {
            AmpEnvelopes[EnvelopeIndex].AttackTime = attack;
            AmpEnvelopes[EnvelopeIndex].DecayTime = decay;
            AmpEnvelopes[EnvelopeIndex].SustainLevel = sustain;
            AmpEnvelopes[EnvelopeIndex].ReleaseTime = release;
            return;
        }

        for (int idx = 0; idx < AmpEnvelopes.Count; idx++)
        {
            AmpEnvelopes[idx].AttackTime = attack;
            AmpEnvelopes[idx].DecayTime = decay;
            AmpEnvelopes[idx].SustainLevel = sustain;
            AmpEnvelopes[idx].ReleaseTime = release;
        }
    }

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

    public void NoteOn(int note, float velocity = 1.0f)
    {
        lock (_lock)
        {
            freq.SetValueAtTime(440.0f * (float)Math.Pow(2.0, (note - 69) / 12.0), 0.0f);


            foreach (var env in envelopes)
            {
                //env.OpenGate();
                env.ScheduleGateOpen(0);
            }


            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                var osc = oscillators[idx];
                if (!osc.Enabled)
                {
                    continue;
                }
                if (osc.HardSync)
                {
                    osc.ResetPhase();
                }
            }
        }
    }

    public void NoteOff()
    {
        lock (_lock)
        {
            foreach (var env in envelopes)
            {
                //env.CloseGate();
                env.ScheduleGateClose(0);
            }
            //LFO_Manager.CloseGate();
            //ampEnvelope.CloseGate();
            // Stop the envelope
            // for (int idx = 0; idx < oscillators.Count; idx++)
            // {
            //     envelopes[idx].CloseGate();
            // }

            // for (int idx = 0; idx < MaxEnvelopes; idx++)
            // {
            //     CustomEnvelopes[idx].CloseGate();
            // }
        }
    }

    public PassThroughNode Process(double increment)
    {
        lock (_lock)
        {
            graph.Process(increment);
            var node = graph.GetNode("Speaker") as PassThroughNode;
            return node;
        }
    }
}