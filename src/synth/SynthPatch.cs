using System;
using System.Collections.Generic;
using Godot;
using Synth;
public class SynthPatch
{
    public const int MaxOscillators = 4;
    const int BufferSize = 1024;
    const int SampleRate = 44100;
    List<WaveTableOscillatorNode> Oscillators = new List<WaveTableOscillatorNode>();
    List<EnvelopeNode> AmpEnvelopes = new List<EnvelopeNode>();
    WaveTableBank waveTableBank;

    public SynthPatch(WaveTableBank waveTableBank)
    {
        this.waveTableBank = waveTableBank;
        // Initialize the patch
        for (int idx = 0; idx < MaxOscillators; idx++)
        {
            Oscillators.Add(new WaveTableOscillatorNode(BufferSize, SampleRate, WaveTableRepository.SinOsc()));
            AmpEnvelopes.Add(new EnvelopeNode(BufferSize));
        }
        Oscillators[0].Enabled = true;
    }

    public void SetAmplitude(float amplitude, int OscillatorIndex = -1)
    {
        if (OscillatorIndex >= 0 && OscillatorIndex < Oscillators.Count)
        {
            Oscillators[OscillatorIndex].Amplitude = amplitude;
            return;
        }
        
        for (int idx = 0; idx < Oscillators.Count; idx++)
        {
            Oscillators[idx].Amplitude = amplitude;
        }
    }


    public void SetOscillatorEnabled(bool enabled, int OscillatorIndex = -1)
    {
        if (OscillatorIndex >= 0 && OscillatorIndex < Oscillators.Count)
        {
            Oscillators[OscillatorIndex].Enabled = enabled;
            return;
        }
        
        for (int idx = 0; idx < Oscillators.Count; idx++)
        {
            Oscillators[idx].Enabled = enabled;
        }
    }

    public void SetAttack(float attack, int EnvelopeIndex = -1)
    {
        if (EnvelopeIndex >= 0 && EnvelopeIndex < AmpEnvelopes.Count)
        {
            GD.Print("Setting attack for envelope " + EnvelopeIndex);
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
            GD.Print("Setting decay for envelope " + EnvelopeIndex);
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
            GD.Print("Setting sustain for envelope " + EnvelopeIndex);
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
            GD.Print("Setting release for envelope " + EnvelopeIndex);
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
        if (OscillatorIndex >= 0 && OscillatorIndex < Oscillators.Count)
        {
            Oscillators[OscillatorIndex].WaveTableMem = waveTableBank.GetWave(waveType);
            return;
        }
        
        for (int idx = 0; idx < Oscillators.Count; idx++)
        {
            Oscillators[idx].WaveTableMem = waveTableBank.GetWave(waveType);
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
        return Oscillators[idx];
    }

    public EnvelopeNode GetEnvelope(int idx)
    {
        return AmpEnvelopes[idx];
    }

    public void NoteOn(int note, float velocity = 1.0f)
    {
        // Start the envelope
        for (int idx = 0; idx < Oscillators.Count; idx++)
        {
            AmpEnvelopes[idx].OpenGate();
        }

        // Set the frequency of the oscillators
        for (int idx = 0; idx < Oscillators.Count; idx++)
        {
            var osc = Oscillators[idx];
//            osc.Amplitude = velocity;
            osc.Frequency = 440.0f * (float)Math.Pow(2.0, (note - 69) / 12.0);
        }
    }

    public void NoteOff()
    {
        // Stop the envelope
        for (int idx = 0; idx < Oscillators.Count; idx++)
        {
            AmpEnvelopes[idx].CloseGate();
        }
    }

    public void Process(float increment, float[] buffer)
    {
        Array.Clear(buffer, 0, BufferSize);
        
        // Mix the oscillators
        for (int oscIdx = 0; oscIdx < Oscillators.Count; oscIdx++)
        {
            var osc = Oscillators[oscIdx];
            if (!osc.Enabled){
                continue;
            }
            var env = AmpEnvelopes[oscIdx];

            osc.Process(increment);
            env.Process(increment);
        }
        
        for (int oscIdx = 0; oscIdx < Oscillators.Count; oscIdx++)
        {
            var osc = Oscillators[oscIdx];
            if (!osc.Enabled){
                continue;
            }            
            var env = AmpEnvelopes[oscIdx];

            for (int i = 0; i < BufferSize; i++)
            {
                buffer[i] += osc[i] * env[i];
            }
        }

        float maxVal = float.MinValue;
        float minVal = float.MaxValue;
        for (int i = 0; i < BufferSize; i++)
        {
            maxVal = Math.Max(maxVal, buffer[i]);
            minVal = Math.Min(minVal, buffer[i]);
        }
        // GD.Print("Max: " + maxVal + " Min: " + minVal);
    }
}