#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SimpleBuildHelper.Editor
{
    internal class DeleteConfirmWindow : EditorWindow
    {
        private string _message;
        private bool _dontAsk;
        private bool _result;

        public static bool Show(string message, out bool dontAskAgain)
        {
            var win = CreateInstance<DeleteConfirmWindow>();
            win._message = message;
            win.titleContent = new GUIContent(Constants.DeleteConfirmTitle);

            // фиксированный размер
            win.minSize = new Vector2(350, 120);
            win.maxSize = win.minSize;

            // Центрируем по главному окну Unity
            win.CenterToMainWindow();

            // Модально
            win.ShowModalUtility();

            dontAskAgain = win._dontAsk;
            return win._result;
        }

        private void OnGUI()
        {
            // Оборачиваем всё в одну большую группу
            EditorGUILayout.BeginVertical();

            // Центрированный стиль для текста
            var centered = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };

            GUILayout.FlexibleSpace();
            GUILayout.Label(_message, centered, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            // Центрируем чекбокс
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            _dontAsk = GUILayout.Toggle(_dontAsk, "Don't show again", GUILayout.ExpandWidth(false));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Кнопки по центру
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Delete", GUILayout.Width(100)))
            {
                _result = true;
                Close();
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(100)))
            {
                _result = false;
                Close();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void CenterToMainWindow()
        {
            // через рефлексию получаем позицию главного окна Unity
            var mi = typeof(EditorGUIUtility).GetMethod(
                "GetMainWindowPosition",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (mi != null)
            {
                var main = (Rect)mi.Invoke(null, null);
                var w = position;
                w.x = main.x + (main.width - w.width) * 0.5f;
                w.y = main.y + (main.height - w.height) * 0.5f;
                position = w;
            }
        }
    }
}
#endif