using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SimpleBuildHelper.Editor
{
    public static class HistoryService
    {
        public static BuildHistory Load()
        {
            var json = EditorPrefs.GetString(Constants.HistoryKey, string.Empty);
            return string.IsNullOrEmpty(json)
                ? new BuildHistory()
                : JsonUtility.FromJson<BuildHistory>(json);
        }

        public static void Save(BuildHistory history)
        {
            var json = JsonUtility.ToJson(history);
            EditorPrefs.SetString(Constants.HistoryKey, json);
        }

        public static void Export(BuildHistory history)
        {
            var path = EditorUtility.SaveFilePanel("Export Build History", "", "BuildHistory.txt", "txt");
            if (string.IsNullOrEmpty(path)) return;

            using var sw = new StreamWriter(path, false);
            var width = history.Records.Any()
                ? history.Records.Max(r => r.BuildName.Length)
                : "BuildName".Length;

            sw.WriteLine($"S Timestamp           { "BuildName".PadRight(width) } Size   ZIP    Time  Logs");
            foreach (var r in history.Records)
            {
                var status = r.Success ? "✔" : "✖";
                sw.WriteLine(
                    $"{status} {r.Timestamp} {r.BuildName.PadRight(width)} " +
                    $"{r.BuildSizeMB,6:F2} {r.ZipSizeMB,6:F2} {r.BuildTimeSec,5:F1} {(r.LogsGenerated ? "Yes" : "No")}"
                );
            }

            EditorUtility.RevealInFinder(Path.GetDirectoryName(path));
        }

        public static void Clear()
        {
            if (!EditorUtility.DisplayDialog("Clear Build History", "Are you sure?", "Yes", "No")) return;
            EditorPrefs.DeleteKey(Constants.HistoryKey);
        }
    }
}