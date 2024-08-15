using System;
using System.Collections.Generic;
using Godot;
using Synth;
public class SynthPatch
{
    private readonly object _lock = new object();
    public const int MaxOscillators = 5;
    public const int MaxLFOs = 4;
    public static int Oversampling = 4;
    static int BufferSize = 512 * Oversampling;
    float SampleRate = 44100 * Oversampling;
    List<WaveTableOscillatorNode> oscillators = new List<WaveTableOscillatorNode>();
    List<LFONode> LFOs = new List<LFONode>();
    List<EnvelopeNode> AmpEnvelopes = new List<EnvelopeNode>();
    EnvelopeNode ampEnvelope;
    WaveTableBank waveTableBank;
    public AudioGraph graph { get; set; } = new AudioGraph();
    List<EnvelopeNode> envelopes = new List<EnvelopeNode>();
    ConstantNode freq;
    DelayEffectNode delayEffectNode;
    ReverbEffectNode reverbEffectNode;
    MoogFilterNode moogFilterNode;
    PassThroughNode speakerNode;

    public SynthPatch(WaveTableBank waveTableBank)
    {
        SampleRate = AudioServer.GetMixRate() * Oversampling;
        freq = graph.CreateNode<ConstantNode>("Freq", BufferSize, SampleRate);
        var mix1 = graph.CreateNode<MixerNode>("Mix1", BufferSize, SampleRate);
        moogFilterNode = graph.CreateNode<MoogFilterNode>("MoogFilter", BufferSize, SampleRate);
        delayEffectNode = graph.CreateNode<DelayEffectNode>("DelayEffect", BufferSize, SampleRate);
        reverbEffectNode = graph.CreateNode<ReverbEffectNode>("ReverbEffect", BufferSize, SampleRate);
        speakerNode = graph.CreateNode<PassThroughNode>("Speaker", BufferSize, SampleRate);
        for (int i = 0; i < MaxOscillators; i++)
        {
            var osc = graph.CreateNode<WaveTableOscillatorNode>("Osc" + i, BufferSize, SampleRate);
            oscillators.Add(osc);
            graph.Connect(osc, mix1, AudioParam.Input);
            graph.Connect(freq, osc, AudioParam.Frequency);

            var env = graph.CreateNode<EnvelopeNode>("OscEnv" + i, BufferSize, SampleRate);
            env.Enabled = false;
            AmpEnvelopes.Add(env);
            graph.Connect(env, osc, AudioParam.Gain);
        }

        for (int i = 0; i < MaxLFOs; i++)
        {
            LFOs.Add(graph.CreateNode<LFONode>("LFO" + i, BufferSize, SampleRate));
        }
        ampEnvelope = graph.CreateNode<EnvelopeNode>("Env1", BufferSize, SampleRate);

        envelopes.Add(ampEnvelope);

        graph.Connect(ampEnvelope, mix1, AudioParam.Gain);
        graph.Connect(mix1, moogFilterNode, AudioParam.StereoInput);
        graph.Connect(moogFilterNode, delayEffectNode, AudioParam.StereoInput);
        graph.Connect(delayEffectNode, reverbEffectNode, AudioParam.StereoInput);
        graph.Connect(reverbEffectNode, speakerNode, AudioParam.StereoInput);

        this.waveTableBank = waveTableBank;
        // Initialize the patch
        for (int idx = 0; idx < MaxOscillators; idx++)
        {
            oscillators.Add(new WaveTableOscillatorNode(BufferSize, SampleRate));
            AmpEnvelopes.Add(new EnvelopeNode(BufferSize, false));
        }

        oscillators[0].Enabled = true;
        graph.TopologicalSort();

        //disable effects by default
        reverbEffectNode.RoomSize = 0.5f;
        graph.SetNodeEnabled(reverbEffectNode, false);
        graph.SetNodeEnabled(delayEffectNode, false);
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
        GD.Print("Setting damp to " + damp);
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
        moogFilterNode.Cutoff = cutoff;
    }
    public void SetResonance(float resonance)
    {
        moogFilterNode.Resonance = resonance;
    }
    public void SetAttack(float attack)
    {
        ampEnvelope.AttackTime = attack;
    }
    public void SetDecay(float decay)
    {
        ampEnvelope.DecayTime = decay;
    }
    public void SetSustain(float sustain)
    {
        ampEnvelope.SustainLevel = sustain;
    }
    public void SetRelease(float release)
    {
        ampEnvelope.ReleaseTime = release;
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

    public void SetAttack(float attack, int EnvelopeIndex = -1)
    {
        if (EnvelopeIndex >= 0 && EnvelopeIndex < AmpEnvelopes.Count)
        {
            AmpEnvelopes[EnvelopeIndex].AttackTime = attack;
            return;
        }

        for (int idx = 0; idx < AmpEnvelopes.Count; idx++)
        {
            AmpEnvelopes[idx].AttackTime = attack;
        }
    }

    public void SetDecay(float decay, int EnvelopeIndex = -1)
    {
        if (EnvelopeIndex >= 0 && EnvelopeIndex < AmpEnvelopes.Count)
        {
            AmpEnvelopes[EnvelopeIndex].DecayTime = decay;
            return;
        }

        for (int idx = 0; idx < AmpEnvelopes.Count; idx++)
        {
            AmpEnvelopes[idx].DecayTime = decay;
        }
    }

    public void SetSustain(float sustain, int EnvelopeIndex = -1)
    {
        if (EnvelopeIndex >= 0 && EnvelopeIndex < AmpEnvelopes.Count)
        {
            AmpEnvelopes[EnvelopeIndex].SustainLevel = sustain;
            return;
        }

        for (int idx = 0; idx < AmpEnvelopes.Count; idx++)
        {
            AmpEnvelopes[idx].SustainLevel = sustain;
        }
    }

    public void SetRelease(float release, int EnvelopeIndex = -1)
    {
        if (EnvelopeIndex >= 0 && EnvelopeIndex < AmpEnvelopes.Count)
        {
            AmpEnvelopes[EnvelopeIndex].ReleaseTime = release;
            return;
        }

        for (int idx = 0; idx < AmpEnvelopes.Count; idx++)
        {
            AmpEnvelopes[idx].ReleaseTime = release;
        }
    }

    public void SetWaveform(WaveTableWaveType waveType, int OscillatorIndex = -1)
    {
        if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
        {
            if (waveType == WaveTableWaveType.PWM)
            {
                oscillators[OscillatorIndex].Is_PWM = true;
                oscillators[OscillatorIndex].WaveTableMem = waveTableBank.GetWave(WaveTableWaveType.SAWTOOTH);
                oscillators[OscillatorIndex].UpdateSampleFunction();
            }
            else
            {
                oscillators[OscillatorIndex].Is_PWM = false;
                oscillators[OscillatorIndex].WaveTableMem = waveTableBank.GetWave(waveType);
                oscillators[OscillatorIndex].UpdateSampleFunction();
            }
            return;
        }

        for (int idx = 0; idx < oscillators.Count; idx++)
        {
            if (waveType == WaveTableWaveType.PWM)
            {
                oscillators[idx].Is_PWM = true;
                oscillators[idx].WaveTableMem = waveTableBank.GetWave(WaveTableWaveType.SAWTOOTH);
                oscillators[idx].UpdateSampleFunction();
            }
            else
            {
                oscillators[idx].Is_PWM = false;
                oscillators[idx].WaveTableMem = waveTableBank.GetWave(waveType);
                oscillators[idx].UpdateSampleFunction();
            }
        }
    }

    public void SetADSR(float attack, float decay, float sustain, float release, int EnvelopeIndex = -1)
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
        return AmpEnvelopes[idx];
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
        freq.Value = 440.0f * (float)Math.Pow(2.0, (note - 69) / 12.0);
        lock (_lock)
        {

            foreach (var env in envelopes)
            {
                env.OpenGate();
            }

            // AmpEnvelope.OpenGate();
            // // Start the envelope
            // for (int idx = 0; idx < Oscillators.Count; idx++)
            // {
            //     AmpEnvelopes[idx].OpenGate();
            // }

            // LFO_Manager.OpenGate();

            // // Set the frequency of the oscillators
            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                AmpEnvelopes[idx].OpenGate();
                var osc = oscillators[idx];
                if (!osc.Enabled)
                {
                    continue;
                }
                //            osc.Amplitude = velocity;
                //osc.Frequency = 440.0f * (float)Math.Pow(2.0, (note - 69) / 12.0);
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
                env.CloseGate();
            }
            //LFO_Manager.CloseGate();
            ampEnvelope.CloseGate();
            // Stop the envelope
            for (int idx = 0; idx < oscillators.Count; idx++)
            {
                AmpEnvelopes[idx].CloseGate();
            }
        }
    }

    public PassThroughNode Process(float increment)
    {
        lock (_lock)
        {
            graph.Process(increment);
            var node = graph.GetNode("Speaker") as PassThroughNode;
            return node;
        }
    }
}