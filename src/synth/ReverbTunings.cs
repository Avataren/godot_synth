namespace Synth
{
    public static class ReverbTunings
    {
        public const int NumCombs = 8;
        public const int NumAllpasses = 4;
        public const float Muted = 0f;
        public const float FixedGain = 0.015f;
        public const float ScaleWet = 3f;
        public const float ScaleDry = 2f;
        public const float ScaleDamp = 0.4f;
        public const float ScaleRoom = 0.28f;
        public const float OffsetRoom = 0.7f;
        public const float InitialRoom = 0.5f;
        public const float InitialDamp = 0.5f;
        public const float InitialWet = 1f / ScaleWet;
        public const float InitialDry = 0f;
        public const float InitialWidth = 1f;
        public const float InitialMode = 0f;
        public const float FreezeMode = 0.5f;
        public const int StereoSpread = 23;

        public const int CombTuningL1 = 1116;
        public const int CombTuningR1 = CombTuningL1 + StereoSpread;
        public const int CombTuningL2 = 1188;
        public const int CombTuningR2 = CombTuningL2 + StereoSpread;
        public const int CombTuningL3 = 1277;
        public const int CombTuningR3 = CombTuningL3 + StereoSpread;
        public const int CombTuningL4 = 1356;
        public const int CombTuningR4 = CombTuningL4 + StereoSpread;
        public const int CombTuningL5 = 1422;
        public const int CombTuningR5 = CombTuningL5 + StereoSpread;
        public const int CombTuningL6 = 1491;
        public const int CombTuningR6 = CombTuningL6 + StereoSpread;
        public const int CombTuningL7 = 1557;
        public const int CombTuningR7 = CombTuningL7 + StereoSpread;
        public const int CombTuningL8 = 1617;
        public const int CombTuningR8 = CombTuningL8 + StereoSpread;

        public const int AllpassTuningL1 = 556;
        public const int AllpassTuningR1 = AllpassTuningL1 + StereoSpread;
        public const int AllpassTuningL2 = 441;
        public const int AllpassTuningR2 = AllpassTuningL2 + StereoSpread;
        public const int AllpassTuningL3 = 341;
        public const int AllpassTuningR3 = AllpassTuningL3 + StereoSpread;
        public const int AllpassTuningL4 = 225;
        public const int AllpassTuningR4 = AllpassTuningL4 + StereoSpread;
    }

}