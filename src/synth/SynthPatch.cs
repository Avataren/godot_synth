using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Synth;
public class SynthPatch
{
    private readonly object _lock = new object();
    public const int MaxOscillators = 5;
    public const int MaxLFOs = 2;
    public const int MaxEnvelopes = 3;
    public float PortamentoTime = 0.0f;
    WaveTableBank waveTableBank;
    public AudioGraph graph { get; set; } = new AudioGraph();
    DelayEffectNode delayEffectNode;
    ReverbEffectNode reverbEffectNode;
    public FilterNode filterNode;
    PassThroughNode speakerNode;
    public NoiseNode noiseNode;
    public FuzzNode fuzzNode;
    public ChorusEffectNode chorusEffectNode;
    public ChorusEffectNode flangerEffectNode;
    MixerNode mix1;
    //Dictionary<int, float> NoteVelocityRegister = new Dictionary<int, float>();
    Stack<int> NoteVelocityRegister = new Stack<int>();
    bool initialized = false;

    private const int MaxVoices = 8;
    public Voice[] voices = new Voice[MaxVoices];
    VoiceMixerNode voiceMixerNode;
    Dictionary<int, int> VoiceMidiDictionary = new Dictionary<int, int>();
    bool[] VoiceActive = new bool[MaxVoices];

    public SynthPatch(WaveTableBank waveTableBank, int bufferSize, float sampleRate = 44100)
    {
        for (int i = 0; i < MaxVoices; i++)
        {
            voices[i] = new Voice(waveTableBank);
            VoiceActive[i] = false;
        }
        voiceMixerNode = graph.CreateNode<VoiceMixerNode>("VoiceMixer");
        mix1 = graph.CreateNode<MixerNode>("Mix1");
        speakerNode = graph.CreateNode<PassThroughNode>("Speaker");
        fuzzNode = graph.CreateNode<FuzzNode>("Fuzz");
        chorusEffectNode = graph.CreateNode<ChorusEffectNode>("ChorusEffect");
        flangerEffectNode = graph.CreateNode<ChorusEffectNode>("ChorusEffect");
        delayEffectNode = graph.CreateNode<DelayEffectNode>("DelayEffect");
        reverbEffectNode = graph.CreateNode<ReverbEffectNode>("ReverbEffect");

        // #if false
        //         float speed = 0.3f; // Adjust speed to match the desired tempo
        //         int repeatCount = 10000; // Number of times to repeat the melody
        //         var scheduler = AudioContext.Scheduler; // Get the instance of the scheduler

        //         // Lists to hold the events for bulk scheduling
        //         var freqEvents = new List<(double timeInSeconds, double value)>();
        //         var gateEvents = new List<(double timeInSeconds, double isOpen)>();

        //         // MIDI notes for "Twinkle, Twinkle, Little Star" full song
        //         int[] melodyNotes = {
        //     60, 60, 67, 67, 69, 69, 67, // A Section
        //     65, 65, 64, 64, 62, 62, 60, // B Section
        //     67, 67, 65, 65, 64, 64, 62, // C Section
        //     60, 60, 67, 67, 69, 69, 67, // Repeat A Section
        //     65, 65, 64, 64, 62, 62, 60, // Repeat B Section
        //     60, 60, 67, 67, 69, 69, 67, // Repeat A Section
        //     65, 65, 64, 64, 62, 62, 60  // Repeat B Section
        // };

        //         double[] noteDurations = {
        //     1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0, // Durations in seconds
        //     1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0,
        //     1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0,
        //     1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0,
        //     1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0,
        //     1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0,
        //     1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 2.0
        // };

        //         double currentTime = 0.0; // Initialize currentTime

        //         for (int repeat = 0; repeat < repeatCount; repeat++)
        //         {
        //             for (int i = 0; i < melodyNotes.Length; i++)
        //             {
        //                 int note = melodyNotes[i];
        //                 double freqValue = 440.0 * Math.Pow(2.0, (note - 69) / 12.0);
        //                 double gateLength = noteDurations[i] * speed;

        //                 freqEvents.Add((currentTime, freqValue)); // Add frequency event
        //                 gateEvents.Add((currentTime, 1)); // Gate open event
        //                 gateEvents.Add((currentTime + gateLength * 0.95, 0)); // Gate close event to ensure a clean note end

        //                 currentTime += gateLength; // Move to the next note's start time
        //             }
        //             currentTime += 1.0; // Add a small pause between repetitions to clear the melody
        //         }

        //         // Now, schedule all frequency and gate events
        //         scheduler.ScheduleValuesAtTimeBulk(freq, AudioParam.ConstValue, freqEvents);
        //         for (int j = 0; j < MaxEnvelopes; j++)
        //         {
        //             scheduler.ScheduleValuesAtTimeBulk(envelopes[j], AudioParam.Gate, gateEvents);
        //         }

        // #endif






        // #if true
        //         float speed = 0.25f;
        //         var scheduler = AudioContext.Scheduler; // Get the instance of the scheduler

        //         // Lists to hold the events for bulk scheduling
        //         var freqEvents = new List<(double timeInSeconds, double value)>();
        //         var gateEvents = new List<(double timeInSeconds, double isOpen)>();

        //         for (int i = 0; i < 10000; i++)
        //         {
        //             // Rhythm pattern for the bassline
        //             double timeOffset = 0.75 * speed;  // Consistent rhythm, adjusted for speed
        //             double gateLength = 0.5 * speed;  // Slightly longer gate for a sustained bass sound

        //             // Define a chord progression or root note changes
        //             int[] rootNotes = { 36, 37, 41, 39 }; // C1, D1, F1, G1 (MIDI notes)
        //             int rootNote = rootNotes[(i / 16) % rootNotes.Length];  // Change root note every 16 steps (4 bars)

        //             // Low-low-high-high bass pattern with octave shifts, relative to the root note
        //             int[] bassPattern = { rootNote, rootNote, rootNote + 12, rootNote, rootNote + 10, rootNote + 12, rootNote, rootNote + 12 };  // Adjusted to the current root note
        //             int note = bassPattern[i % bassPattern.Length];  // Cycle through the pattern

        //             // Calculate the frequency based on the note
        //             double freqValue = 440.0f * (float)Math.Pow(2.0, (note - 69) / 12.0) * 0.5;
        //             freqEvents.Add((i * timeOffset, freqValue)); // Add to the list for bulk scheduling

        //             // Schedule the gate open and close events
        //             for (int j = 0; j < MaxEnvelopes; j++)
        //             {
        //                 gateEvents.Add((i * timeOffset, 1.0)); // Add gate open event
        //                 gateEvents.Add((i * timeOffset + gateLength, 0.0)); // Add gate close event
        //             }
        //         }

        //         // Now use ScheduleValuesAtTimeBulk to schedule all events at once
        //         scheduler.ScheduleValuesAtTimeBulk(freq, AudioParam.ConstValue, freqEvents);

        //         for (int j = 0; j < MaxEnvelopes; j++)
        //         {
        //             scheduler.ScheduleValuesAtTimeBulk(envelopes[j], AudioParam.Gate, gateEvents);
        //         }
        // #endif

        graph.Connect(voiceMixerNode, mix1, AudioParam.StereoInput, ModulationType.Add);
        graph.Connect(mix1, fuzzNode, AudioParam.StereoInput, ModulationType.Add);
        graph.Connect(fuzzNode, flangerEffectNode, AudioParam.StereoInput, ModulationType.Add);
        graph.Connect(flangerEffectNode, chorusEffectNode, AudioParam.StereoInput, ModulationType.Add);
        graph.Connect(chorusEffectNode, delayEffectNode, AudioParam.StereoInput, ModulationType.Add);
        graph.Connect(delayEffectNode, reverbEffectNode, AudioParam.StereoInput, ModulationType.Add);
        graph.Connect(reverbEffectNode, speakerNode, AudioParam.StereoInput, ModulationType.Add);

        //graph.Connect(voiceMixerNode, speakerNode, AudioParam.StereoInput, ModulationType.Add);
        graph.TopologicalSortWorkingGraph();
        GD.Print("Initial setup:");

        this.waveTableBank = waveTableBank;
        //graph.SetNodeEnabled(filterNode, false);
        graph.SetNodeEnabled(flangerEffectNode, false);
        graph.SetNodeEnabled(chorusEffectNode, false);
        //graph.SetNodeEnabled(noiseNode, false);
        graph.SetNodeEnabled(fuzzNode, false);
        graph.SetNodeEnabled(reverbEffectNode, false);
        graph.SetNodeEnabled(delayEffectNode, false);

        initialized = true;
    }

    public void ConnectInVoices(string srcName, string dstName, string param, ModulationType modType, float strength = 1.0f)
    {
        foreach (var voice in voices)
        {
            voice.Connect(srcName, dstName, param, modType, strength);
        }
    }

    public void DisconnectInVoices(string srcName, string dstName, string param)
    {
        foreach (var voice in voices)
        {
            voice.Disconnect(srcName, dstName, param);
        }
    }

    public void SetMasterGain(float gain)
    {
        //speakerNode.Gain = gain;
        //mix1.Gain = gain;
        foreach (var voice in voices)
        {
            voice.mixerNode.Gain = gain;
        }
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
        foreach (var voice in voices)
        {
            voice.filterNode.CutOff = cutoff;
        }
    }
    public void SetResonance(float resonance)
    {
        filterNode.Resonance = resonance;
    }
    // public void SetAttack(float attack, int oscillatorIndex = 0)
    // {
    //     envelopes[oscillatorIndex].AttackTime = attack;
    // }
    // public void SetDecay(float decay, int oscillatorIndex = 0)
    // {
    //     envelopes[oscillatorIndex].DecayTime = decay;
    // }
    // public void SetSustain(float sustain, int oscillatorIndex = 0)
    // {
    //     envelopes[oscillatorIndex].SustainLevel = sustain;
    // }
    // public void SetRelease(float release, int oscillatorIndex = 0)
    // {
    //     envelopes[oscillatorIndex].ReleaseTime = release;
    // }

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
        foreach (var voice in voices)
        {
            voice.SetFeedback(feedback, OscillatorIndex);
        }
    }

    public void SetBalance(float balance, int OscillatorIndex = -1)
    {
        foreach (var voice in voices)
        {
            voice.SetBalance(balance, OscillatorIndex);
        }
    }

    public void SetModulationStrength(float strength, int OscillatorIndex = -1)
    {
        foreach (var voice in voices)
        {
            voice.SetModulationStrength(strength, OscillatorIndex);
        }
    }

    public void SetPWM(float pwm, int OscillatorIndex = -1)
    {
        foreach (var voice in voices)
        {
            voice.SetPWM(pwm, OscillatorIndex);
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


    public void SetLFOWaveform(LFOWaveform waveform, int LFOIndex = -1)
    {
        foreach (var voice in voices)
        {
            voice.SetLFOWaveform(waveform, LFOIndex);
        }
    }

    public void SetLFOFrequency(float freq, int LFOIndex = -1)
    {
        GD.Print("Trying to set LFO frequency to " + freq);
        if (!initialized)
        {
            GD.Print("SynthPatch not initialized");
            return;
        }
        foreach (var voice in voices)
        {
            voice.SetLFOFrequency(freq, LFOIndex);
        }
    }

    public void SetLFOGain(float gain, int LFOIndex = -1)
    {
        foreach (var voice in voices)
        {
            voice.SetLFOGain(gain, LFOIndex);
        }
    }

    public void SetOscillatorPhaseOffset(float phase, int OscillatorIndex = -1)
    {
        foreach (var voice in voices)
        {
            voice.SetOscillatorPhaseOffset(phase, OscillatorIndex);
        }
    }

    public void SetHardSync(bool enabled, int OscillatorIndex = -1)
    {
        foreach (var voice in voices)
        {
            voice.SetHardSync(enabled, OscillatorIndex);
        }
    }

    public void SetAmplitude(float amplitude, int OscillatorIndex = -1)
    {
        foreach (var voice in voices)
        {
            voice.SetAmplitude(amplitude, OscillatorIndex);
        }
    }


    public void SetOscillatorEnabled(bool enabled, int OscillatorIndex = -1)
    {
        foreach (var voice in voices)
        {
            voice.SetOscillatorEnabled(enabled, OscillatorIndex);
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
        foreach (var voice in voices)
        {
            voice.SetWaveform(waveType, OscillatorIndex);
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

    public EnvelopeNode GetEnvelope(int idx)
    {
        return voices[0].envelopes[idx];
    }

    public void SetDetuneOctaves(float detuneOctaves, int OscillatorIndex = -1)
    {
        foreach (var voice in voices)
        {
            voice.SetDetuneOctaves(detuneOctaves, OscillatorIndex);
        }
    }

    public void SetDetuneSemi(float detuneSemi, int OscillatorIndex = -1)
    {
        foreach (var voice in voices)
        {
            voice.SetDetuneSemi(detuneSemi, OscillatorIndex);
        }
    }

    public void SetDetuneCents(float detuneCents, int OscillatorIndex = -1)
    {
        foreach (var voice in voices)
        {
            voice.SetDetuneCents(detuneCents, OscillatorIndex);
        }
    }

    public void ClearKeyStack()
    {
        GD.Print("Clearing key stack");
        lock (_lock)
        {
            VoiceMidiDictionary.Clear();
            foreach (var voice in voices)
            {
                voice.Silence();
            }
            //NoteVelocityRegister.Clear();
            // var now = AudioContext.Instance.CurrentTimeInSeconds;

            // foreach (var env in envelopes)
            // {
            //     if (env.Enabled)
            //     {
            //         env.ScheduleGateClose(now);  // Open with full envelope
            //     }
            // }
            // foreach (var osc in oscillators)
            // {
            //     osc.ScheduleGateClose(now);  // Open oscillator gates
            // }
        }
    }

    public void NoteOn(int note, float velocity = 1.0f)
    {
        lock (_lock)
        {
            if (VoiceMidiDictionary.ContainsKey(note))
            {
                //currentVoice = VoiceDictionary[note];
                GD.Print("VoiceMidiDictionary already contains note");
            }
            else
            {
                for (int i = 0; i < MaxVoices; i++)
                {
                    if (!VoiceActive[i])
                    {
                        VoiceMidiDictionary[note] = i;
                        VoiceActive[i] = true;
                        voices[i].NoteOn(note, velocity);
                        //currentVoice = i;
                        break;
                    }
                }
            }


            // if (NoteVelocityRegister.Contains(note))
            // {
            //     GD.Print("Note already playing");
            //     //this will cause an issue when key is release
            //     return;
            // }

            // float newFrequency = 440.0f * (float)Math.Pow(2.0, (note - 69) / 12.0);
            // var now = AudioContext.Instance.CurrentTimeInSeconds;

            // if (NoteVelocityRegister.Count == 0 || PortamentoTime < 0.001)
            // {
            //     // No note is currently playing, start the new note with full envelope
            //     NoteVelocityRegister.Push(note);

            //     freq.SetValueAtTime(newFrequency, now);

            //     foreach (var env in envelopes)
            //     {
            //         if (env.Enabled)
            //         {
            //             env.ScheduleGateOpen(now, true);  // Open with full envelope
            //         }
            //     }
            //     foreach (var osc in oscillators)
            //     {
            //         osc.ScheduleGateOpen(now, true);  // Open oscillator gates
            //     }
            // }
            // else
            // {
            //     // A note is already playing, apply portamento (legato)
            //     NoteVelocityRegister.Push(note);
            //     freq.ExponentialRampToValueAtTime(newFrequency, now + PortamentoTime);  // Glide to new note
            // }
        }
    }


    public void NoteOff(int note)
    {
        lock (_lock)
        {
            if (VoiceMidiDictionary.ContainsKey(note))
            {
                int voiceIndex = VoiceMidiDictionary[note];
                VoiceMidiDictionary.Remove(note);
                VoiceActive[voiceIndex] = false;
                voices[voiceIndex].NoteOff(note);
            }
            //CurrentVoice.NoteOff(note);
            // var now = AudioContext.Instance.CurrentTimeInSeconds;

            // // Remove the released note from the stack
            // var tempStack = new Stack<int>();
            // while (NoteVelocityRegister.Count > 0)
            // {
            //     var n = NoteVelocityRegister.Pop();
            //     if (n != note)
            //     {
            //         tempStack.Push(n);
            //     }
            // }

            // // Restore the remaining notes back into the original stack
            // while (tempStack.Count > 0)
            // {
            //     NoteVelocityRegister.Push(tempStack.Pop());
            // }

            // // Now determine the behavior based on the remaining notes
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
            // {
            //     // No more notes, stop the sound
            //     foreach (var env in envelopes)
            //     {
            //         env.ScheduleGateClose(now);  // Close envelope gates
            //     }
            //     foreach (var osc in oscillators)
            //     {
            //         osc.ScheduleGateClose(now);  // Close oscillator gates
            //     }
            // }
        }
    }


    public PassThroughNode Process(double increment)
    {
        lock (_lock)
        {
            voiceMixerNode.Clear();
            // process voices in parallel
            Parallel.For(0, MaxVoices, i =>
            {
                voices[i].Process(increment);
            });


            for (int i = 0; i < MaxVoices; i++)
            {
                voiceMixerNode.MixIn(voices[i]);
            }

            // CurrentVoice.Process(increment);
            // voiceMixerNode.MixIn(CurrentVoice);

            // GD.Print(CurrentVoice.GetOuputNode().LeftBuffer[0]);

            graph.Process(increment);
            //var node = graph.GetNode("Speaker") as PassThroughNode;
            return speakerNode;
        }
    }
}