#if UNITY_EDITOR
using System;

namespace SimpleBuildHelper.Editor
{
    [Serializable]
    public sealed class BuildRecord
    {
        public string Timestamp;
        public string BuildName;
        public float BuildSizeMB;
        public float ZipSizeMB;
        public double BuildTimeSec;
        public bool LogsGenerated;
        public bool Success;
    }
}
#endif