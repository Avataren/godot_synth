using System;
using System.Collections.Generic;
using Godot;
using Synth;
public class SynthPatch
{
    public const int MaxOscillators = 5;
    public static int Oversampling = 4;
    static int BufferSize = 512 * Oversampling;
    float SampleRate = 44100 * Oversampling;
    List<WaveTableOscillatorNode> oscillators = new List<WaveTableOscillatorNode>();
    List<EnvelopeNode> AmpEnvelopes = new List<EnvelopeNode>();
    EnvelopeNode ampEnvelope;
    WaveTableBank waveTableBank;
    AudioGraph graph = new AudioGraph();
    List<EnvelopeNode> envelopes = new List<EnvelopeNode>();
    ConstantNode freq;
    DelayEffectNode delayEffectNode;
    MoogFilterNode moogFilterNode;
    public SynthPatch(WaveTableBank waveTableBank)
    {
        freq = graph.CreateNode<ConstantNode>("Freq", BufferSize, SampleRate);
        var mix1 = graph.CreateNode<MixerNode>("Mix1", BufferSize, SampleRate);
        moogFilterNode = graph.CreateNode<MoogFilterNode>("MoogFilter", BufferSize, SampleRate);
        delayEffectNode = graph.CreateNode<DelayEffectNode>("DelayEffect", BufferSize, SampleRate);
        for (int i = 0; i < MaxOscillators; i++)
        {
            var osc = graph.CreateNode<WaveTableOscillatorNode>("Osc" + i, BufferSize, SampleRate);
            oscillators.Add(osc);
            if (i > 0)
            {
                graph.Connect(osc, mix1, AudioParam.Input);
            }
            graph.Connect(freq, osc, AudioParam.Frequency);

            var env = graph.CreateNode<EnvelopeNode>("OscEnv" + i, BufferSize, SampleRate);
            env.Enabled = false;
            AmpEnvelopes.Add(env);
            graph.Connect(env, osc, AudioParam.Gain);
        }
        ampEnvelope = graph.CreateNode<EnvelopeNode>("Env1", BufferSize, SampleRate);

        envelopes.Add(ampEnvelope);

        graph.Connect(ampEnvelope, mix1, AudioParam.Gain);
        graph.Connect(mix1, moogFilterNode, AudioParam.StereoInput);
        graph.Connect(moogFilterNode, delayEffectNode, AudioParam.StereoInput);
        graph.DebugPrint();

        SampleRate = AudioServer.GetMixRate();
        this.waveTableBank = waveTableBank;
        // Initialize the patch
        for (int idx = 0; idx < MaxOscillators; idx++)
        {
            oscillators.Add(new WaveTableOscillatorNode(BufferSize, SampleRate));
            AmpEnvelopes.Add(new EnvelopeNode(BufferSize, false));
        }

        graph.Connect(oscillators[0], oscillators[1], AudioParam.Phase);
        //graph.Connect(oscillators[1], oscillators[2], AudioParam.Phase);
        //graph.Connect(oscillators[2], oscillators[3], AudioParam.Phase);
        //ampEnvelope = new EnvelopeNode(BufferSize, true);
        oscillators[0].Enabled = true;
        graph.TopologicalSort();
        //FrequencyLFO = new LFONode(BufferSize, 4.0f, 5.0f);

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
            AmpEnvelopes[EnvelopeIndex].Enabled = enabled;
            graph.TopologicalSort();
            return;
        }

        for (int idx = 0; idx < AmpEnvelopes.Count; idx++)
        {
            AmpEnvelopes[idx].Enabled = enabled;
        }
        graph.TopologicalSort();
    }
    public void SetHardSync(bool enabled, int OscillatorIndex = -1)
    {
        if (OscillatorIndex >= 0 && OscillatorIndex < oscillators.Count)
        {
            oscillators[OscillatorIndex].HardSync = enabled;
            graph.TopologicalSort();
            return;
        }
        for (int idx = 0; idx < oscillators.Count; idx++)
        {
            oscillators[idx].HardSync = enabled;
        }
        graph.TopologicalSort();
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
            oscillators[OscillatorIndex].Enabled = enabled;
            graph.TopologicalSort();
            return;
        }

        for (int idx = 0; idx < oscillators.Count; idx++)
        {
            oscillators[idx].Enabled = enabled;
        }
        graph.TopologicalSort();
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
                osc.Phase = 0.0f;
            }
        }
    }

    public void NoteOff()
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

    public DelayEffectNode Process(float increment)
    {
        graph.Process(increment);
        var node = graph.GetNode("DelayEffect") as DelayEffectNode;
        return node;
    }
}