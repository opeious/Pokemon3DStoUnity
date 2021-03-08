using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace P3DS2U.Editor
{
    public class Pokemon3DSToUnityEditorWindow : EditorWindow
    {
        private static Pokemon3DSToUnityEditorWindow instance;
        private P3ds2USettingsScriptableObject settings;
        private P3ds2USettingsScriptableObjectEditor editor;

        [MenuItem("3DStoUnity/Settings Window %F9")]
        public static void ShowWindow()
        {
            instance = GetWindow<Pokemon3DSToUnityEditorWindow>("3DStoUnity Editor");
        }


        private void Awake()
        {
          
        }

        private void OnEnable()
        {
            settings = SettingsUtils.GetOrCreateSettings();
            editor = (P3ds2USettingsScriptableObjectEditor)UnityEditor.Editor.CreateEditor(settings, typeof(P3ds2USettingsScriptableObjectEditor));
        }

        private void OnDisable()
        {
            DestroyImmediate(editor);
        }

        private void OnGUI()
        {
            if (settings != null)
            {
                 editor.OnInspectorGUI();
            }
        }
    }
}