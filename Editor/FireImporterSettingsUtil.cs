using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

namespace P3DS2U.Editor
{
    public class FireImporterSettingsUtil
    {
        
        private static SerializedObject manager = null;
        private static SerializedProperty layersProp = null;

        public static void ThrowWarningAndChangeSettings ()
        {
            if (CheckForLayers ()) {
                
            } else {
                bool permission = EditorUtility.DisplayDialog ("Are you sure?",
                    "Turning on this option requires changing rendering settings for it to work properly, refer to the GitHub link!",
                    "OK");

                P3ds2USettingsScriptableObject.Instance.ImporterSettings.ImportFireMaterials = false;
            }
        }

        public static bool CheckForLayers ()
        {
            manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            layersProp = manager.FindProperty("layers");
            var found = false;
            for (var i = 0; i <= 31; i++)
            {
                var sp = layersProp.GetArrayElementAtIndex(i);
                if (sp != null && "FireCore".Equals(sp.stringValue))
                {
                    found = true;
                    break;
                }
            }
            return found;
        }
        
        public static void AddLayers(string[] layerNames)
        {
            foreach (string name in layerNames)
            {
                // check if layer is present
                SerializedProperty slot = null;
                for (int i = 8; i <= 31; i++)
                {
                    SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
                    if (sp != null && string.IsNullOrEmpty(sp.stringValue))
                    {
                        slot = sp;
                        break;
                    }
                }
 
                if (slot != null)
                {
                    slot.stringValue = name;
                }
                else
                {
                    Debug.LogError("Could not find an open Layer Slot for: " + name);
                }
            }
            manager.ApplyModifiedProperties();
        }
 
        public static void CheckTags(string[] tagNames)
        {
            SerializedProperty tagsProp = manager.FindProperty("tags");
 
            List<string> DefaultTags = new List<string>(){ "Untagged", "Respawn", "Finish", "EditorOnly", "MainCamera", "Player", "GameController" };
 
            foreach (string name in tagNames)
            {
                if (DefaultTags.Contains(name)) continue;
 
                // check if tag is present
                bool found = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                    if (t.stringValue.Equals(name)) { found = true; break; }
                }
 
                // if not found, add it
                if (!found)
                {
                    tagsProp.InsertArrayElementAtIndex(0);
                    SerializedProperty n = tagsProp.GetArrayElementAtIndex(0);
                    n.stringValue = name;
                }
            }
 
            // save
            manager.ApplyModifiedProperties();
        }
 
        public static void CheckSortLayers(string[] tagNames)
        {
            SerializedProperty sortLayersProp = manager.FindProperty("m_SortingLayers");
 
            //for (int i = 0; i < sortLayersProp.arraySize; i++)
            //{ // used to figure out how all of this works and what properties values look like
            //    SerializedProperty entry = sortLayersProp.GetArrayElementAtIndex(i);
            //    SerializedProperty name = entry.FindPropertyRelative("name");
            //    SerializedProperty unique = entry.FindPropertyRelative("uniqueID");
            //    SerializedProperty locked = entry.FindPropertyRelative("locked");
            //    Debug.Log(name.stringValue + " => " + unique.intValue + " => " + locked.boolValue);
            //}
 
            foreach (string name in tagNames)
            {
                // check if tag is present
                bool found = false;
                for (int i = 0; i < sortLayersProp.arraySize; i++)
                {
                    SerializedProperty entry = sortLayersProp.GetArrayElementAtIndex(i);
                    SerializedProperty t = entry.FindPropertyRelative("name");
                    if (t.stringValue.Equals(name)) { found = true; break; }
                }
 
                // if not found, add it
                if (!found)
                {
                    manager.ApplyModifiedProperties();
                    AddSortingLayer();
                    manager.Update();
 
                    int idx = sortLayersProp.arraySize - 1;
                    SerializedProperty entry = sortLayersProp.GetArrayElementAtIndex(idx);
                    SerializedProperty t = entry.FindPropertyRelative("name");
                    t.stringValue = name;
                }
            }
 
            // save
            manager.ApplyModifiedProperties();
        }
 
// you need 'using System.Reflection;' for these
private static Assembly editorAsm;
private static MethodInfo AddSortingLayer_Method;
 
    /// <summary> add a new sorting layer with default name </summary>
     public static void AddSortingLayer()
     {
       if (AddSortingLayer_Method == null)
       {
         if (editorAsm == null) editorAsm = Assembly.GetAssembly(typeof(UnityEditor.Editor));
         System.Type t = editorAsm.GetType("UnityEditorInternal.InternalEditorUtility");
         AddSortingLayer_Method = t.GetMethod("AddSortingLayer", (BindingFlags.Static | BindingFlags.NonPublic), null, new System.Type[0], null);
       }
       AddSortingLayer_Method.Invoke(null, null);
     }
    }
}