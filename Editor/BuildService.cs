using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SimpleBuildHelper.Editor
{
    public static class BuildService
    {
        public static void RegenerateBuildInfo(
            string projectName,
            BuildTarget target,
            bool manualName,
            string customRoot,
            out string buildName,
            out string buildRoot,
            out string buildOutput,
            out string buildPath)
        {
            var date = DateTime.Now.ToString(Constants.DateFormat);
            var baseFolder = string.IsNullOrEmpty(customRoot)
                ? Path.Combine(Constants.BuildsRootFolder, target.ToString())
                : customRoot;

            if (!manualName)
            {
                if (!Directory.Exists(baseFolder))
                {
                    Directory.CreateDirectory(baseFolder);
                }

                var prefix = $"Build_{projectName}_{date}_";
                var existing = Directory
                    .GetDirectories(baseFolder)
                    .Select(Path.GetFileName)
                    .Where(n => n.StartsWith(prefix))
                    .Select(n => int.TryParse(n[prefix.Length..], out var x) ? x : 0);

                var next = existing.Any() ? existing.Max() + 1 : 1;
                buildName = $"{prefix}{next}";
            }
            else
            {
                buildName = _lastManualName;
            }

            buildRoot = Path.Combine(baseFolder, buildName);
            buildOutput = Path.Combine(buildRoot, buildName);
            var ext = GetExtension(target);
            buildPath = Path.Combine(buildOutput, buildName + ext);
        }

        public static BuildReport Build(
            string buildPath,
            BuildTarget target,
            out double seconds)
        {
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = target
            };

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var report = BuildPipeline.BuildPlayer(opts);
            sw.Stop();

            seconds = sw.Elapsed.TotalSeconds;
            return report;
        }

        public static void CreateZip(string root, string name)
        {
            var src = Path.Combine(root, name);
            var dst = Path.Combine(root, name + ".zip");

            if (File.Exists(dst))
            {
                File.Delete(dst);
            }

            ZipFile.CreateFromDirectory(src, dst);
        }

        public static void WriteSummary(string folder, BuildSummary sum, string name, string project,
            BuildTarget target)
        {
            var path = Path.Combine(folder, name + ".txt");
            using var w = new StreamWriter(path, false);
            w.WriteLine($"Build Name: {name}");
            w.WriteLine($"Product:    {project}");
            w.WriteLine($"Target:     {target}");
            w.WriteLine($"Date: {DateTime.Now.ToString(Constants.DateFormat + " HH:mm:ss")}");
            w.WriteLine($"Result:     {sum.result}");
            w.WriteLine($"Errors:     {sum.totalErrors}");
            w.WriteLine($"Warnings:   {sum.totalWarnings}");

            if (sum.result == BuildResult.Succeeded)
            {
                w.WriteLine($"Size:       {sum.totalSize / 1048576f:F2} MB");
                w.WriteLine($"Time:       {sum.totalTime.TotalSeconds:F1} s");
            }
        }

        public static bool CopyEditorLog(string folder, string name)
        {
            var src = FindEditorLogPath();
            if (string.IsNullOrEmpty(src) || !File.Exists(src))
                return false;

            var dst = Path.Combine(folder, name + "_UnityLog.txt");
            try
            {
                using var fs = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var lines = sr.ReadToEnd()
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                    .Reverse()
                    .Take(800)
                    .Reverse()
                    .ToArray();
                File.WriteAllLines(dst, lines);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void WriteHeavyFilesLog(BuildReport report, string outputRoot, string buildName, int topN = 20)
        {
            var fileList = report.GetFiles();
            
            if (fileList == null || fileList.Length == 0)
            {
                ParseEditorLogForHeavy(outputRoot, buildName, topN);
                return;
            }

            var top = fileList
                .OrderByDescending(f => f.size) // размер в байтах
                .Take(topN);

            var logPath = Path.Combine(outputRoot, buildName + "_HeavyFiles.txt");
            using var sw = new StreamWriter(logPath, false);
            sw.WriteLine($"Top {topN} assets in build (uncompressed size):");
            sw.WriteLine("---------------------------------------------");

            foreach (var f in top)
            {
                // Unity пишет абсолютные пути; переводим в относительные внутри проекта
                var rel = MakeRelativePath(f.path);
                sw.WriteLine($"{f.size / 1048576f,6:F2} MB  {rel}");
            }
        }

        private static string MakeRelativePath(string path)
        {
            var proj = Path.GetFullPath(Application.dataPath).Replace("/Assets", "");
            path = Path.GetFullPath(path);
            return path.StartsWith(proj) ? path.Substring(proj.Length + 1) : path;
        }

        private static void ParseEditorLogForHeavy(string outputRoot, string buildName, int topN)
        {
            var src = FindEditorLogPath();
            if (string.IsNullOrEmpty(src) || !File.Exists(src)) return;

            var lines = File.ReadAllLines(src);
            var idx = Array.FindLastIndex(lines, l =>
                l.StartsWith("Used Assets") || l.StartsWith("Used assets")); // начало секции
            if (idx < 0) return;

            var heavy = new List<(float sizeMb, string path)>();
            for (int i = idx + 1; i < lines.Length; i++)
            {
                var ln = lines[i].Trim();
                if (string.IsNullOrEmpty(ln)) break; // закончилась секция
                // ожидаемый формат:  7.3 mb  12.8% Assets/…
                var parts = ln.Split('\t', ' ');
                if (!float.TryParse(parts[0], out var mb)) continue;
                var asset = ln.Substring(ln.LastIndexOf(' ') + 1);
                heavy.Add((mb, asset));
            }

            heavy = heavy.OrderByDescending(h => h.sizeMb).Take(topN).ToList();
            if (heavy.Count == 0) return;

            var logPath = Path.Combine(outputRoot, buildName + "_HeavyFiles.txt");
            using var sw = new StreamWriter(logPath, false);
            sw.WriteLine($"Top {topN} assets in build (uncompressed size) — parsed from Editor.log:");
            sw.WriteLine("---------------------------------------------");
            foreach (var h in heavy)
                sw.WriteLine($"{h.sizeMb,6:F2} MB  {h.path}");
        }

        public static string FindEditorLogPath()
        {
            var local = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Unity", "Editor", "Editor.log");

            if (File.Exists(local)) return local;

            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Unity");

            if (Directory.Exists(root))
            {
                foreach (var d in Directory.GetDirectories(root, "Editor*"))
                {
                    var cand = Path.Combine(d, "Editor.log");
                    if (File.Exists(cand)) return cand;
                }
            }

            var mac = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "Library", "Logs", "Unity", "Editor.log");

            return File.Exists(mac) ? mac : null;
        }

        private static string GetExtension(BuildTarget t) => t switch
        {
            BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => ".exe",
            BuildTarget.Android => ".apk",
            BuildTarget.WebGL => "",
            _ => ""
        };

        private static string _lastManualName;
        public static void SetManualName(string name) => _lastManualName = name;
    }
}