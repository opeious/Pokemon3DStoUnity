using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace P3DS2U.Editor
{
   public static class SettingsUtils
   {
      private const string SettingsFileName = "3DStoUnitySettings.asset";
      public static void GetOrCreateSettings ()
      {
         const string filePath = PokemonImporter.ImportPath + SettingsFileName;
         if (File.Exists (filePath)) {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<P3ds2USettingsScriptableObject> (filePath);
         } else {
            AssetDatabase.CreateAsset (ScriptableObject.CreateInstance<P3ds2USettingsScriptableObject> (), filePath);
            GetOrCreateSettings ();
         }
      }
   }

   [Serializable]
   public class P3ds2UShaderProperties
   {
      [SerializeField] public Shader bodyShader;
      [SerializeField] public Shader irisShader;
      
      // public string BaseMap = Shader.PropertyToID ("_BaseMap");
      public string BaseMap =  ("_BaseMap");
      public string BaseMapTiling =  ("_BaseMapTiling");
      public string NormalMap =  ("_NormalMap");
      public string NormalMapTiling =  ("_NormalMapTiling");
      public string OcclusionMap =  ("_OcclusionMap");
      public string OcclusionMapTiling =  ("_OcclusionMapTiling");
   }
   
   [Serializable]
   public class MergedBinary : PropertyAttribute
   {
      [SerializeField] public List<string> PokemonMergedBinary;
   }
 
   [CustomPropertyDrawer(typeof(MergedBinary))]
   public class MergedBinaryEditor : PropertyDrawer
   {
      public override float GetPropertyHeight(SerializedProperty property,
         GUIContent label)
      {
         return EditorGUI.GetPropertyHeight(property, label, true);
      }
 
      public override void OnGUI(Rect position,
         SerializedProperty property,
         GUIContent label)
      {
         GUI.enabled = false;
         EditorGUI.PropertyField(position, property, label, true);
         GUI.enabled = true;
      }
   }

   [Serializable]
   public class WhatToImport
   {
      public bool ImportModel;
      public bool ImportTextures;
      public bool ImportMaterials;
      public bool ApplyMaterials;
      public bool SkeletalAnimations;
      [Tooltip("Feature in Progress")]
      public bool MaterialAnimationsWip;
      public bool RenameGeneratedAnimationFiles;
   }
   
   public class P3ds2USettingsScriptableObject : ScriptableObject
   {

      private bool _generated;
      [SerializeField] public WhatToImport whatToImport;
      
      [SerializeField] private List<MergedBinary> mergedBinariesPreview;
      
      public P3ds2UShaderProperties customShaderSettings;

      public static P3ds2USettingsScriptableObject Instance;

      [HideInInspector] public int chosenFormat; // 0 or 1
      public static bool ImportInProgress;
      
      private P3ds2USettingsScriptableObject ()
      {
         customShaderSettings = new P3ds2UShaderProperties ();
         whatToImport = new WhatToImport {
            ImportModel = true,
            ImportTextures = true,
            ImportMaterials = true,
            ApplyMaterials = true,
            SkeletalAnimations = true,
            MaterialAnimationsWip = false,
            RenameGeneratedAnimationFiles = true
         };
         Instance = this;
         RegeneratePreview ();
         chosenFormat = 0;
      }
      
      private void OnEnable ()
      {
         if (!_generated) {
            customShaderSettings.bodyShader = Shader.Find ("Shader Graphs/LitPokemonShader");
            customShaderSettings.irisShader = Shader.Find ("Shader Graphs/LitPokemonIrisShader");
            _generated = true;
         }
      }

      private Dictionary<string, List<string>> ScenesDict = new Dictionary<string, List<string>> ();

      public void StartImporting ()
      {
         ImportInProgress = true;
         PokemonImporter.StartImportingBinaries (this, ScenesDict);
         ImportInProgress = false;
      }

      public void RegeneratePreview ()
      {
         if (ImportInProgress) return;
         
         ScenesDict = new Dictionary<string, List<string>> ();
         if (chosenFormat == 1) {
            var allFiles = DirectoryUtils.GetAllFilesRecursive (PokemonImporter.ImportPath);
            foreach (var singleFile in allFiles) {
               var trimmedName = Path.GetFileName (singleFile);
               if (!ScenesDict.ContainsKey (trimmedName)) {
                  ScenesDict.Add (trimmedName, new List<string> {singleFile});
               } else {
                  ScenesDict[trimmedName].Add (singleFile);
               }
            }  
         } else {
            var allFolders = Directory.GetDirectories (PokemonImporter.ImportPath);
            foreach (var singleFolder in allFolders) {
               var allFiles = Directory.GetFiles (singleFolder).ToList ();
               for (var i = allFiles.Count - 1; i >= 0; i--) {
                  if (allFiles[i].Contains (".meta")) {
                     allFiles.RemoveAt (i);
                  }
               }

               var trimmedFolderName = Path.GetFileName (singleFolder);
               foreach (var singleFile in allFiles) {
                  if (!ScenesDict.ContainsKey (trimmedFolderName)) {
                     ScenesDict.Add (trimmedFolderName, new List<string> {singleFile});
                  } else {
                     ScenesDict[trimmedFolderName].Add (singleFile);
                  }
               }
            }
         }
         
         mergedBinariesPreview = new List<MergedBinary> ();
         foreach (var scene in ScenesDict) {
            mergedBinariesPreview.Add (new MergedBinary {
               PokemonMergedBinary = scene.Value
            });
         }
      }
   }

   [CustomEditor(typeof(P3ds2USettingsScriptableObject))]
   public class P3ds2USettingsScriptableObjectEditor : UnityEditor.Editor
   {
      public override void OnInspectorGUI ()
      {
         var settingsTarget = target as P3ds2USettingsScriptableObject;
         if (settingsTarget != null) {
            EditorGUILayout.BeginVertical ();
            EditorGUILayout.BeginScrollView (Vector2.zero, GUILayout.Width (400), GUILayout.Height (115));
            
            if (GUI.Button (new Rect (5, 10, 100, 50), "Import")) {
               settingsTarget.StartImporting ();
            }
            if (GUI.Button (new Rect (5, 65, 100, 50), "Refresh")) {
               settingsTarget.RegeneratePreview ();
            }
            GUI.Label (new Rect (105, 35, 600, 40), " If you are not sure what settings to use,\n try the defaults!");
            EditorGUILayout.EndScrollView ();
            EditorGUI.BeginChangeCheck ();
            
            settingsTarget.chosenFormat = GUILayout.SelectionGrid (settingsTarget.chosenFormat, new[] {"Each pokemon is in a separate folder","Each folder has a type of binary ('Mdls','Tex','Etc')" }, 2, GUI.skin.toggle);
            if (EditorGUI.EndChangeCheck ()) {
               settingsTarget.RegeneratePreview ();
            }
            
            EditorGUI.BeginChangeCheck ();
            DrawDefaultInspector ();
            if (EditorGUI.EndChangeCheck ()) {
               var wti = settingsTarget.whatToImport;
               if (!wti.ImportModel) {
                  wti.ApplyMaterials = false;
                  wti.SkeletalAnimations = false;
                  wti.MaterialAnimationsWip = false;
                  wti.RenameGeneratedAnimationFiles = false;
               }

               if (!wti.ImportTextures) {
                  wti.ImportMaterials = false;
               }

               if (!wti.ImportMaterials) {
                  wti.ApplyMaterials = false;
                  wti.MaterialAnimationsWip = false;
               }

               if (!wti.ApplyMaterials) {
                  wti.MaterialAnimationsWip = false;
               }

               if (!wti.SkeletalAnimations) {
                  wti.MaterialAnimationsWip = false;
                  wti.RenameGeneratedAnimationFiles = false;
               }

               if (wti.MaterialAnimationsWip) {
                  wti.MaterialAnimationsWip = false;
               }
            }
            EditorGUILayout.BeginScrollView (Vector2.zero, GUILayout.Width (400), GUILayout.Height (100));
            EditorGUILayout.EndScrollView ();
            EditorGUILayout.EndVertical ();
         }
         serializedObject.Update ();
         GUILayout.ExpandHeight (true);
         GUILayout.ExpandWidth (true);

         serializedObject.ApplyModifiedProperties();
      }
   }
}