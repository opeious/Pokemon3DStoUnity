using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace P3DS2U.Editor
{
    public class Pokemon3DSToUnityEditorWindow : EditorWindow
    {
        private static Pokemon3DSToUnityEditorWindow instance;
        private static P3ds2USettingsScriptableObject _settings;

        public static P3ds2USettingsScriptableObject settings {
            get => _settings;
            set {
                _settings = value;
                editor = (P3ds2USettingsScriptableObjectEditor)UnityEditor.Editor.CreateEditor(settings, typeof(P3ds2USettingsScriptableObjectEditor));
            }
        }
        private static P3ds2USettingsScriptableObjectEditor editor;

        [MenuItem("3DStoUnity/Settings Window %F9")]
        public static void ShowWindow()
        {
            instance = GetWindow<Pokemon3DSToUnityEditorWindow>("3DStoUnity Editor");
        }

        private void Awake()
        {
            instance = this;
        }

        private void OnEnable()
        {
            settings = SettingsUtils.GetOrCreateSettings();
        }

        private void OnDisable()
        {
            DestroyImmediate(editor);
        }

        private Vector2 MScrollViewPos;

        private void OnGUI()
        {
            if (settings != null) {
                MScrollViewPos = EditorGUILayout.BeginScrollView (MScrollViewPos);
                 editor.OnInspectorGUI();
                 EditorGUILayout.EndScrollView ();
            } else {
                var s = new GUIStyle (EditorStyles.textField) {normal = {textColor = Color.red}};
                EditorGUILayout.LabelField ("Couldn't find settings, Generate a new settings file by: 3DStoUnity -> Find Settings Object", s);
            }
        }
    }
}