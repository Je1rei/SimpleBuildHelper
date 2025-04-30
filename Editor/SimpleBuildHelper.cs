using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SimpleBuildHelper.Editor
{
    public class SimpleBuildHelper : EditorWindow
    {
        private BuildHistory _history;

        private Tab _currentTab;
        private Vector2 _scroll;
        private GUIStyle _centeredLabel;

        private string _projectName;
        private BuildTarget _buildTarget;
        private string _buildName;
        private string _buildRoot;
        private string _buildOutput;
        private string _buildPath;
        private bool _manualBuildName;

        // Settings
        private bool _useCustomFolder;
        private string _customFolderPath;
        private bool _createZip;
        private bool _generateLogs = false;
        private bool _generateHeavy;
        private bool _generateUnityLog;
        private bool _generateBuildLog;
        private bool _suppressDeleteConfirm;
        private bool _showBuildAdvanced;
        private bool _showHistoryAdvanced;

        // Status
        private string _statusMessage;
        private Color _statusColor;
        private float _lastBuildSizeMB;
        private float _lastZipSizeMB;
        private double _lastBuildTimeSec;
        private bool _logsGenerated;

        [MenuItem(Constants.MenuPath, priority = 251)]
        private static void OpenWindow()
        {
            var w = GetWindow<SimpleBuildHelper>(Constants.WindowTitle);
            w.minSize = new Vector2(580, 460);
            w.Show();
        }

        private void OnEnable()
        {
            _projectName = Application.productName;
            _buildTarget = EditorUserBuildSettings.activeBuildTarget;
            _customFolderPath = EditorPrefs.GetString(Constants.BuildOutputPathKey, Constants.BuildsRootFolder);
            _createZip = EditorPrefs.GetBool(Constants.CreateZipKey, true);
            _generateLogs = EditorPrefs.GetBool(Constants.GenerateLogsKey, false);
            _generateHeavy = EditorPrefs.GetBool(Constants.GenerateHeavyKey, false);
            _generateUnityLog = EditorPrefs.GetBool(Constants.GenerateUnityLogKey, false);
            _generateBuildLog = EditorPrefs.GetBool(Constants.GenerateBuildLogKey, false);
            _useCustomFolder = true;
            _suppressDeleteConfirm = EditorPrefs.GetBool(Constants.SuppressDeleteKey, false);
            _manualBuildName = false;
            _statusMessage = string.Empty;
            _logsGenerated = false;
            _showBuildAdvanced = false;
            _showHistoryAdvanced = false;

            _history = HistoryService.Load();
            RegenerateBuildInfo();
        }

        private void OnGUI()
        {
            if (_centeredLabel == null)
            {
                _centeredLabel = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
            }

            EditorGUILayout.BeginHorizontal();
            _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab, new[] { Constants.TabBuild, Constants.TabHistory });
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);

            if (_currentTab == Tab.Build)
                DrawBuildTab();
            else
                DrawHistoryTab();
        }

        private void DrawBuildTab()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                DrawStatusBox();
                EditorGUILayout.Space(6);
            }

            EditorGUI.BeginChangeCheck();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawProjectInfo();
            DrawAdvancedOptions();

            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
                ClearStatus();

            if (GUILayout.Button(Constants.ButtonBuildNow, GUILayout.Height(36)))
                Build();

            EditorGUILayout.Space(6);
        }

        private void DrawStatusBox()
        {
            var box = new GUIStyle(EditorStyles.helpBox) { richText = true };
            EditorGUILayout.BeginVertical(box, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            var icon = _statusColor == Color.green ? "<color=green>\u2714</color>" : "<color=red>\u2716</color>";
            GUILayout.Label($"{icon} <b>{_statusMessage}</b>", _centeredLabel);

            if (_statusColor == Color.green)
            {
                EditorGUILayout.Space(4);
                GUILayout.Label($"<color=white>\u2714</color> Build Size: <b>{_lastBuildSizeMB:F2} MB</b>",
                    _centeredLabel);
                if (_createZip)
                    GUILayout.Label($"<color=white>\u2714</color> ZIP Size: <b>{_lastZipSizeMB:F2} MB</b>",
                        _centeredLabel);

                GUILayout.Label($"<color=white>\u2714</color> Build Time: <b>{_lastBuildTimeSec:F1} s</b>",
                    _centeredLabel);
                GUILayout.Label(_logsGenerated
                    ? "<color=white>\u2714</color> Logs: <b>Yes</b>"
                    : "<color=red>\u2716</color> Logs: <b>No</b>", _centeredLabel);

                if (_generateHeavy)
                    GUILayout.Label("<color=white>\u2714</color> Heavy files log generated", _centeredLabel);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void DrawProjectInfo()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(Constants.SectionProjectInfo, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Project Name:", _projectName);
            EditorGUILayout.LabelField("Platform:", _buildTarget.ToString());

            EditorGUI.BeginChangeCheck();
            var newName = EditorGUILayout.TextField("Build Name:", _buildName);
            if (EditorGUI.EndChangeCheck())
            {
                _manualBuildName = true;
                BuildService.SetManualName(newName);
                RegenerateBuildInfo();
            }

            EditorGUILayout.LabelField("Root Folder:", _buildRoot);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);
        }

        private void DrawAdvancedOptions()
        {
            _showBuildAdvanced =
                EditorGUILayout.ToggleLeft(Constants.LabelAdvanced, _showBuildAdvanced, EditorStyles.boldLabel);
            if (!_showBuildAdvanced) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            _useCustomFolder = EditorGUILayout.ToggleLeft("Use Custom Output Folder", _useCustomFolder);
            if (_useCustomFolder)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("    Folder:", GUILayout.Width(60));
                EditorGUILayout.TextField(_customFolderPath);
                if (GUILayout.Button("…", GUILayout.Width(26)))
                {
                    var sel = EditorUtility.OpenFolderPanel("Select Output Folder", _customFolderPath, "");
                    if (!string.IsNullOrEmpty(sel))
                    {
                        _customFolderPath = sel;
                        EditorPrefs.SetString(Constants.BuildOutputPathKey, sel);
                        RegenerateBuildInfo();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            _createZip = EditorGUILayout.ToggleLeft(Constants.LabelCreateZip, _createZip);
            _generateLogs = EditorGUILayout.ToggleLeft(Constants.LabelGenerateLogs, _generateLogs);
            EditorPrefs.SetBool(Constants.CreateZipKey, _createZip);
            EditorPrefs.SetBool(Constants.GenerateLogsKey, _generateLogs);

            if (_generateLogs)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUI.BeginDisabledGroup(true);
                _generateHeavy = EditorGUILayout.ToggleLeft(
                    new GUIContent(Constants.LabelGenerateHeavy, "In Next Update"),
                    _generateHeavy
                );
                _generateHeavy = false;
                EditorGUI.EndDisabledGroup();

                _generateUnityLog = EditorGUILayout.ToggleLeft(
                    Constants.LabelGenerateUnity, _generateUnityLog);
                _generateBuildLog = EditorGUILayout.ToggleLeft(
                    Constants.LabelGenerateBuild, _generateBuildLog);

                EditorPrefs.SetBool(Constants.GenerateUnityLogKey, _generateUnityLog);
                EditorPrefs.SetBool(Constants.GenerateBuildLogKey, _generateBuildLog);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);
        }

        private void DrawHistoryTab()
        {
            // Внешний контейнер-бокс всей вкладки History
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                /* ---------- шапка таблицы ---------- */
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("", GUILayout.Width(Constants.HistoryColStatusW));
                    GUILayout.Label("Date", GUILayout.Width(Constants.HistoryColDateW));
                    GUILayout.Label("Name", GUILayout.Width(Constants.HistoryColNameW));
                    GUILayout.Label("Size", GUILayout.Width(Constants.HistoryColSizeW));
                    GUILayout.Label("Time", GUILayout.Width(Constants.HistoryColTimeW));
                    GUILayout.Label("Logs", GUILayout.Width(Constants.HistoryColLogsW));
                    GUILayout.Label("Open", GUILayout.Width(Constants.HistoryColOpenW));
                    GUILayout.Label(new GUIContent("Delete", Constants.DeleteTooltip),
                        GUILayout.Width(Constants.HistoryColDeleteW));
                }

                /* ---------- список билдов ---------- */
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                using (var sv = new EditorGUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = sv.scrollPosition;

                    foreach (var rec in _history.Records.AsEnumerable().Reverse())
                    {
                        using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
                        {
                            /* статус */
                            var icon = rec.Success
                                ? "<color=green>\u2714</color>"
                                : "<color=red>\u2716</color>";
                            GUILayout.Label(icon, _centeredLabel,
                                GUILayout.Width(Constants.HistoryColStatusW));

                            /* данные */
                            GUILayout.Label(rec.Timestamp, GUILayout.Width(Constants.HistoryColDateW));
                            GUILayout.Label(rec.BuildName, GUILayout.Width(Constants.HistoryColNameW));
                            GUILayout.Label($"{rec.BuildSizeMB:F2} MB", GUILayout.Width(Constants.HistoryColSizeW));
                            GUILayout.Label(rec.BuildTimeSec.ToString("F1"),
                                GUILayout.Width(Constants.HistoryColTimeW));
                            GUILayout.Label(rec.LogsGenerated ? "Yes" : "No",
                                GUILayout.Width(Constants.HistoryColLogsW));

                            /* кнопка Open */
                            var baseDir = _useCustomFolder
                                ? _customFolderPath
                                : Path.Combine(Constants.BuildsRootFolder, _buildTarget.ToString());
                            var path = Path.Combine(baseDir, rec.BuildName);

                            using (new EditorGUI.DisabledScope(!Directory.Exists(path)))
                            {
                                if (GUILayout.Button("Open", GUILayout.Width(Constants.HistoryColOpenW)))
                                    EditorUtility.RevealInFinder(path);
                            }

                            /* кнопка Delete (заблокирована) */
                            EditorGUI.BeginDisabledGroup(true);
                            GUILayout.Button(new GUIContent("Delete", "In Next Update"),
                                GUILayout.Width(Constants.HistoryColDeleteW));
                            EditorGUI.EndDisabledGroup();
                        }
                    }
                }

                /* ---------- блок Advanced Options ---------- */
                _showHistoryAdvanced = EditorGUILayout.ToggleLeft(
                    Constants.LabelAdvanced,
                    _showHistoryAdvanced,
                    EditorStyles.boldLabel);

                if (_showHistoryAdvanced)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Export History"))
                            HistoryService.Export(_history);
                        if (GUILayout.Button("Clear History"))
                            HistoryService.Clear();
                    }

                    GUILayout.Space(4);
                }
            }

            GUILayout.Space(6);
        }

        private void HandleDelete(string baseFolder, string buildName)
        {
            var path = Path.Combine(baseFolder, buildName);

            if (!_suppressDeleteConfirm)
            {
                if (!DeleteConfirmWindow.Show($"Delete the build folder:\n{buildName}?", out var dontAsk)) return;
                if (dontAsk)
                {
                    _suppressDeleteConfirm = true;
                    EditorPrefs.SetBool(Constants.SuppressDeleteKey, true);
                }
            }

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Repaint();
        }

        private void Build()
        {
            RegenerateBuildInfo();
            Directory.CreateDirectory(_buildRoot);
            Directory.CreateDirectory(_buildOutput);

            var report = BuildService.Build(_buildPath, _buildTarget, out var seconds);
            var summary = report.summary;

            _lastBuildTimeSec = summary.result == BuildResult.Succeeded ? seconds : summary.totalTime.TotalSeconds;
            _lastBuildSizeMB = summary.totalSize / 1048576f;
            _logsGenerated = false;

            if (_generateBuildLog)
            {
                BuildService.WriteSummary(_buildRoot, summary, _buildName, _projectName, _buildTarget);
                _logsGenerated = true;
            }

            if (_generateUnityLog)
            {
                BuildService.CopyEditorLog(_buildRoot, _buildName);
                _logsGenerated = true;
            }

            if (_generateHeavy)
            {
                BuildService.WriteHeavyFilesLog(report, _buildRoot, _buildName);
                _logsGenerated = true;
            }

            if (summary.result == BuildResult.Succeeded && _createZip)
            {
                BuildService.CreateZip(_buildRoot, _buildName);
                var zi = new FileInfo(Path.Combine(_buildRoot, _buildName + ".zip"));
                _lastZipSizeMB = zi.Exists ? zi.Length / 1048576f : 0;
            }

            _history.Records.Add(new BuildRecord
            {
                Timestamp = System.DateTime.Now.ToString(Constants.DateFormat + " HH:mm:ss"),
                BuildName = _buildName,
                BuildSizeMB = _lastBuildSizeMB,
                ZipSizeMB = _lastZipSizeMB,
                BuildTimeSec = _lastBuildTimeSec,
                LogsGenerated = _logsGenerated,
                Success = summary.result == BuildResult.Succeeded
            });

            HistoryService.Save(_history);

            if (summary.result == BuildResult.Succeeded)
                SetStatus(Constants.StatusBuildComplete, true);
            else
                SetStatus(string.Format(Constants.StatusBuildFailedFmt, summary.totalErrors, summary.totalWarnings),
                    false);

            EditorUtility.RevealInFinder(_buildRoot);
            Repaint();
        }

        private void RegenerateBuildInfo()
        {
            BuildService.RegenerateBuildInfo(
                _projectName,
                _buildTarget,
                _manualBuildName,
                _useCustomFolder ? _customFolderPath : null,
                out _buildName,
                out _buildRoot,
                out _buildOutput,
                out _buildPath
            );
        }

        private void ClearStatus()
        {
            _statusMessage = string.Empty;
            _lastBuildSizeMB = 0;
            _lastZipSizeMB = 0;
            _lastBuildTimeSec = 0;
            _logsGenerated = false;
        }

        private void SetStatus(string message, bool ok)
        {
            _statusMessage = message;
            _statusColor = ok ? Color.green : Color.red;
        }
    }
}