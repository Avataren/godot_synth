using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
namespace Synth
{
    public class MixerNode : AudioNode
    {
        public SynthType Gain = 0.5f;
        private readonly Vector<SynthType> negOne = new Vector<SynthType>(-1.0f);
        private readonly Vector<SynthType> posOne = new Vector<SynthType>(1.0f);

        // Pre-allocated arrays for parameter loading
        private readonly SynthType[] gainArray;
        private readonly SynthType[] balanceArray1;
        private readonly SynthType[] balanceArray2;

        // Vectorized accumulators
        private readonly Vector<SynthType>[] leftAccumulators;
        private readonly Vector<SynthType>[] rightAccumulators;

        public MixerNode()
        {
            RightBuffer = new SynthType[NumSamples];
            LeftBuffer = new SynthType[NumSamples];

            int vectorSize = Vector<SynthType>.Count;
            int numVectors = (NumSamples + vectorSize - 1) / vectorSize;

            gainArray = new SynthType[vectorSize];
            balanceArray1 = new SynthType[vectorSize];
            balanceArray2 = new SynthType[vectorSize];

            leftAccumulators = new Vector<SynthType>[numVectors];
            rightAccumulators = new Vector<SynthType>[numVectors];
        }

        public override void Process(double increment)
        {
            List<AudioNode> nodes = GetParameterNodes(AudioParam.Input);
            if (nodes == null || nodes.Count == 0)
            {
                Array.Clear(LeftBuffer, 0, NumSamples);
                Array.Clear(RightBuffer, 0, NumSamples);
                return;
            }

            int vectorSize = Vector<SynthType>.Count;
            int numVectors = leftAccumulators.Length;

            // Clear accumulators
            Array.Clear(leftAccumulators, 0, numVectors);
            Array.Clear(rightAccumulators, 0, numVectors);

            foreach (var node in nodes)
            {
                if (node == null || !node.Enabled) continue;

                for (int i = 0; i < numVectors; i++)
                {
                    ProcessVectorizedSamples(i * vectorSize, node, ref leftAccumulators[i], ref rightAccumulators[i]);
                }
            }

            // Copy accumulated results back to the output buffers
            for (int i = 0; i < numVectors; i++)
            {
                int offset = i * vectorSize;
                leftAccumulators[i].CopyTo(LeftBuffer, offset);
                rightAccumulators[i].CopyTo(RightBuffer, offset);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ProcessVectorizedSamples(int startIndex, AudioNode node, ref Vector<SynthType> leftAccumulator, ref Vector<SynthType> rightAccumulator)
        {
            Vector<SynthType> nodeBalanceVector = new Vector<SynthType>(node.Balance);
            ReadOnlySpan<SynthType> nodeBuffer = node.GetBuffer().Slice(startIndex, Vector<SynthType>.Count);

            LoadParameters(startIndex, node);

            var gainParam = new Vector<SynthType>(gainArray);
            var balanceParam1 = new Vector<SynthType>(balanceArray1) + nodeBalanceVector;
            var balanceParam2 = new Vector<SynthType>(balanceArray2);
            var sample = new Vector<SynthType>(nodeBuffer) * gainParam;

            var balance = Vector.Max(negOne, Vector.Min(balanceParam1 * balanceParam2, posOne));

            var leftVolume = Vector.ConditionalSelect(Vector.LessThan(balance, Vector<SynthType>.Zero), posOne, posOne - balance);
            var rightVolume = Vector.ConditionalSelect(Vector.GreaterThan(balance, Vector<SynthType>.Zero), posOne, posOne + balance);

            leftAccumulator += leftVolume * sample * Gain;
            rightAccumulator += rightVolume * sample * Gain;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadParameters(int startIndex, AudioNode node)
        {
            for (int j = 0; j < Vector<SynthType>.Count; j++)
            {
                gainArray[j] = GetParameter(AudioParam.Gain, startIndex + j).Item2;
                balanceArray1[j] = node.GetParameter(AudioParam.Balance, startIndex + j).Item1;
                balanceArray2[j] = node.GetParameter(AudioParam.Balance, startIndex + j).Item2;
            }
        }
    }
}

/*
    old non simd implementation
using System;
using System.Collections.Generic;
using Godot;

namespace Synth
{

    public class MixerNode : AudioNode
    {
        public float Gain = 0.5f;
        public MixerNode()
        {
            RightBuffer = new float[NumSamples];
            LeftBuffer = new float[NumSamples];
        }

        public override void Process(double increment)
        {
            List<AudioNode> nodes = GetParameterNodes(AudioParam.Input);
            if (nodes == null || nodes.Count == 0)
            {
                return;
            }
            Array.Clear(LeftBuffer);
            Array.Clear(RightBuffer);

            foreach (var node in nodes)
            {
                if (node == null || !node.Enabled)
                {
                    continue;
                }
                // Retrieve node-specific parameters once per node
                float nodeBalance = node.Balance;
                for (int i = 0; i < NumSamples; i++)
                {
                    var gainParam = GetParameter(AudioParam.Gain, i);
                    var balanceParam = node.GetParameter(AudioParam.Balance, i);

                    float sample = node[i] * gainParam.Item2;
                    float balance = (balanceParam.Item1 + nodeBalance) * balanceParam.Item2;

                    // Clip balance to -1 to 1
                    balance = Math.Max(-1, Math.Min(1, balance));

                    // Calculate left and right channel volumes based on balance
                    float leftVolume = balance < 0 ? 1.0f : 1.0f - balance;
                    float rightVolume = balance > 0 ? 1.0f : 1.0f + balance;

                    // Mix into left and right buffers
                    LeftBuffer[i] += leftVolume * sample * Gain;
                    RightBuffer[i] += rightVolume * sample * Gain;
                }
            }
        }

    }
}    
*/