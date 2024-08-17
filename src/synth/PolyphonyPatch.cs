using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Synth;

public class PolyphonyPatch {
    int MaxVoices;
    int CurrentVoice = 0;
    List<SynthPatch> Voices = new List<SynthPatch>();
    Dictionary<int, SynthPatch> ActiveVoices = new Dictionary<int, SynthPatch>();
    public PolyphonyPatch(WaveTableBank waveTableBank,int maxVoices = 4)
    {
        MaxVoices = maxVoices;
        for (int idx = 0; idx < MaxVoices; idx++)
        {
            Voices.Add(new SynthPatch(waveTableBank));
        }
    }

    public void UpdateFromPatch(SynthPatch updatePatch)
    {
        foreach (var patch in Voices)
        {
            for (int idx = 0; idx < SynthPatch.MaxOscillators; idx++)
            {
                patch.SetADSR(updatePatch.GetEnvelope(idx).AttackTime, 
                    updatePatch.GetEnvelope(idx).DecayTime, 
                    updatePatch.GetEnvelope(idx).SustainLevel,
                    updatePatch.GetEnvelope(idx).ReleaseTime, 
                    idx);
                patch.GetOscillator(idx).WaveTableMemory = updatePatch.GetOscillator(idx).WaveTableMemory;
            } 
        }
    }

    public void NoteOn(int note, float velocity = 1.0f)
    {
        //cycle through voices
        if (!ActiveVoices.ContainsKey(note))
        {
            //make sure that dictionary is not full, if so, just ignore note
            if (ActiveVoices.Count == MaxVoices)
            {
                return;
            }
            ActiveVoices[note] = Voices[CurrentVoice];
            Voices[CurrentVoice].NoteOn(note, velocity);
            CurrentVoice = (CurrentVoice + 1) % MaxVoices;
        }
    }

    public void NoteOff(int note)
    {
        if (ActiveVoices.ContainsKey(note))
        {
            ActiveVoices[note].NoteOff();
            ActiveVoices.Remove(note);
        }
    }
}