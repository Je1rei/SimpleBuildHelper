#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace SimpleBuildHelper.Editor
{
    [Serializable]
    public class BuildHistory
    {
        public List<BuildRecord> Records = new();
    }
}
#endif