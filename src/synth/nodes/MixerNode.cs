using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
namespace Synth
{
    public class MixerNode : AudioNode
    {
        public float Gain = 0.5f;
        private readonly Vector<float> negOne = new Vector<float>(-1.0f);
        private readonly Vector<float> posOne = new Vector<float>(1.0f);

        // Pre-allocated arrays for parameter loading
        private readonly float[] gainArray;
        private readonly float[] balanceArray1;
        private readonly float[] balanceArray2;

        // Vectorized accumulators
        private readonly Vector<float>[] leftAccumulators;
        private readonly Vector<float>[] rightAccumulators;

        public MixerNode()
        {
            RightBuffer = new float[NumSamples];
            LeftBuffer = new float[NumSamples];

            int vectorSize = Vector<float>.Count;
            int numVectors = (NumSamples + vectorSize - 1) / vectorSize;

            gainArray = new float[vectorSize];
            balanceArray1 = new float[vectorSize];
            balanceArray2 = new float[vectorSize];

            leftAccumulators = new Vector<float>[numVectors];
            rightAccumulators = new Vector<float>[numVectors];
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

            int vectorSize = Vector<float>.Count;
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
        private void ProcessVectorizedSamples(int startIndex, AudioNode node, ref Vector<float> leftAccumulator, ref Vector<float> rightAccumulator)
        {
            Vector<float> nodeBalanceVector = new Vector<float>(node.Balance);
            ReadOnlySpan<float> nodeBuffer = node.GetBuffer().Slice(startIndex, Vector<float>.Count);

            LoadParameters(startIndex, node);

            Vector<float> gainParam = new Vector<float>(gainArray);
            Vector<float> balanceParam1 = new Vector<float>(balanceArray1) + nodeBalanceVector;
            Vector<float> balanceParam2 = new Vector<float>(balanceArray2);
            Vector<float> sample = new Vector<float>(nodeBuffer) * gainParam;

            Vector<float> balance = Vector.Max(negOne, Vector.Min(balanceParam1 * balanceParam2, posOne));

            Vector<float> leftVolume = Vector.ConditionalSelect(Vector.LessThan(balance, Vector<float>.Zero), posOne, posOne - balance);
            Vector<float> rightVolume = Vector.ConditionalSelect(Vector.GreaterThan(balance, Vector<float>.Zero), posOne, posOne + balance);

            leftAccumulator += leftVolume * sample * Gain;
            rightAccumulator += rightVolume * sample * Gain;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadParameters(int startIndex, AudioNode node)
        {
            for (int j = 0; j < Vector<float>.Count; j++)
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