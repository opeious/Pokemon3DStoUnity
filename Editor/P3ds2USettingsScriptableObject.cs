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
          public static P3ds2USettingsScriptableObject GetOrCreateSettings (bool _focus = false)
          {
                var settings = FindSettingsInProject();
                if (settings == null)
                {
                    string filePath = EditorUtility.SaveFilePanel("Choose the folder where to save the new Import Settings", PokemonImporter.ImportPath, SettingsFileName, "asset");
                    if (string.IsNullOrEmpty(filePath))
                        filePath = PokemonImporter.ImportPath + SettingsFileName;

                    AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<P3ds2USettingsScriptableObject>(), filePath);
                    return GetOrCreateSettings(_focus);
                }

                if (_focus)
                    Selection.activeObject = settings;

                return settings;
          }

        public static void GetOrCreateSettingsInImporterPath()
        {
            const string filePath = PokemonImporter.ImportPath + SettingsFileName;
            if (File.Exists(filePath))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<P3ds2USettingsScriptableObject>(filePath);
            }
            else
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<P3ds2USettingsScriptableObject>(), filePath);
                GetOrCreateSettingsInImporterPath();
            }
        }

        public static P3ds2USettingsScriptableObject FindSettingsInProject()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:P3ds2USettingsScriptableObject");
            foreach (string ttype in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(ttype);
                P3ds2USettingsScriptableObject settings = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(P3ds2USettingsScriptableObject)) as P3ds2USettingsScriptableObject;
                if (settings != null)
                {
                    return settings;
                }
            }

            return null;
        }

        public static List<P3ds2USettingsScriptableObject> FindAllSettingsInProject<T>()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:P3ds2USettingsScriptableObject");
            List<P3ds2USettingsScriptableObject> finalList = new List<P3ds2USettingsScriptableObject>();
            foreach (string ttype in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(ttype);
                P3ds2USettingsScriptableObject val = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(P3ds2USettingsScriptableObject)) as P3ds2USettingsScriptableObject;
                if (val != null)
                {
                    finalList.Add(val);
                }
            }

            return finalList;
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
      public string BaseMapOffset = ("_BaseMapOffset");
      public string NormalMap =  ("_NormalMap");
      public string NormalMapTiling =  ("_NormalMapTiling");
      public string NormalMapOffset = ("_NormalOffset");
      public string OcclusionMap =  ("_OcclusionMap");
      public string OcclusionMapTiling =  ("_OcclusionMapTiling");
      public string OcclusionMapOffset =  ("_OcclusionMapOffset");
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
   public class AnimationImportOptions: SerializableDictionary<string, bool>{}

   [Serializable]
   public class WhatToImport
   {
      [Header("Models import range")]
      [Min(0)] 
      public int StartIndex;
      [Min(0)]
      public int EndIndex;
      [Space(10)]
      public bool ImportModel;
      public bool ImportTextures;
      public bool ImportMaterials;
      public bool ApplyMaterials;
      public bool SkeletalAnimations;
      public AnimationImportOptions FightAnimationsToImport;
      public AnimationImportOptions PetAnimationsToImport;
      public AnimationImportOptions MovementAnimationsToImport;
      [Tooltip("Feature in Progress")]
      public bool MaterialAnimations;
      public bool VisibilityAnimations;
      public bool InterpolateAnimations;
      public bool RenameGeneratedAnimationFiles;
      [HideInInspector] public string ExportPath;
      [HideInInspector] public string ImportPath;
   }
   
   public class P3ds2USettingsScriptableObject : ScriptableObject
   {

      private bool _generated;
      [SerializeField] public WhatToImport ImporterSettings;
      
      [SerializeField] private List<MergedBinary> mergedBinariesPreview;
      
      public P3ds2UShaderProperties customShaderSettings;

      public static P3ds2USettingsScriptableObject Instance;

      [HideInInspector] public int chosenFormat; // 0 or 1
      public static bool ImportInProgress;
      
        public string ExportPath { get { return ImporterSettings.ExportPath; } }
        public string ImportPath { get { return ImporterSettings.ImportPath; } }

      private P3ds2USettingsScriptableObject ()
      {
         customShaderSettings = new P3ds2UShaderProperties ();
            ImporterSettings = new WhatToImport {
                StartIndex = 0,
                EndIndex = 0,
                ImportModel = true,
                ImportTextures = true,
                ImportMaterials = true,
                ApplyMaterials = true,
                SkeletalAnimations = true,
                MaterialAnimations = true,
                InterpolateAnimations = false,
                VisibilityAnimations = true,
                RenameGeneratedAnimationFiles = true,
                FightAnimationsToImport = new AnimationImportOptions(),
                PetAnimationsToImport = new AnimationImportOptions(),
                MovementAnimationsToImport = new AnimationImportOptions(),
                ExportPath = PokemonImporter.ExportPath,
                ImportPath = PokemonImporter.ImportPath,
            };
         foreach (string animationName in AnimationNaming.animationNames["Fight"])
         {
            ImporterSettings.FightAnimationsToImport.Add(animationName, true);
         }
         foreach (string animationName in AnimationNaming.animationNames["Pet"])
         {
            ImporterSettings.PetAnimationsToImport.Add(animationName, true);
         }
         foreach (string animationName in AnimationNaming.animationNames["Movement"])
         {
            ImporterSettings.MovementAnimationsToImport.Add(animationName, true);
         }
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

        public void SetImportPath()
        {
            string path = EditorUtility.SaveFolderPanel("Choose Import folder", "Assets/", "Bin3DS");
            if (!string.IsNullOrEmpty(path))
            {
                ImporterSettings.ImportPath = "Assets" + path.Replace(Application.dataPath+"/", "/")+"/";
            }
        }

        public void ResetPaths(int _path = 0)
        {
            switch (_path)
            {
                case 0:
                    ImporterSettings.ImportPath = PokemonImporter.ImportPath;
                    break;
                case 1:
                    ImporterSettings.ExportPath = PokemonImporter.ExportPath;
                    break;
                default:
                    ImporterSettings.ImportPath = PokemonImporter.ImportPath;
                    ImporterSettings.ExportPath = PokemonImporter.ExportPath;
                    break;
            }
        }

        public void SetExportPath()
        {
            string path = EditorUtility.SaveFolderPanel("Choose Export folder", "Assets/", "Exported");
            if (!string.IsNullOrEmpty(path))
            {
                ImporterSettings.ExportPath = "Assets" + path.Replace(Application.dataPath + "/", "/") + "/";
            }
        }

      public void RegeneratePreview ()
      {
         if (ImportInProgress) 
                return;

         if (!System.IO.Directory.Exists(ImportPath))
            Directory.CreateDirectory(ImportPath);

         if (!System.IO.Directory.Exists(ExportPath))
            Directory.CreateDirectory(ExportPath);
 
         ScenesDict = new Dictionary<string, List<string>> ();
         if (chosenFormat == 1) {
            var allFiles = DirectoryUtils.GetAllFilesRecursive (ImporterSettings.ImportPath);
            foreach (var singleFile in allFiles) {
               var trimmedName = Path.GetFileName (singleFile);
               if (!ScenesDict.ContainsKey (trimmedName)) {
                  ScenesDict.Add (trimmedName, new List<string> {singleFile});
               } else {
                  ScenesDict[trimmedName].Add (singleFile);
               }
            }  
         } else {
            var allFolders = Directory.GetDirectories (ImporterSettings.ImportPath);
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

         ImporterSettings.EndIndex = mergedBinariesPreview.Count - 1;
      }
   }

   [CustomEditor(typeof(P3ds2USettingsScriptableObject))]
   public class P3ds2USettingsScriptableObjectEditor : UnityEditor.Editor
   {
      public override void OnInspectorGUI ()
      {
         var settingsTarget = target as P3ds2USettingsScriptableObject;
         if (settingsTarget != null) {

            var wti = settingsTarget.ImporterSettings;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Paths");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("...", GUILayout.Width(50), GUILayout.Width(50)))
            {
                settingsTarget.SetImportPath();
            }
            GUILayout.Label("Import path: " + wti.ImportPath);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("...", GUILayout.Width(50), GUILayout.Width(50)))
            {
                settingsTarget.SetExportPath();
            }

            GUILayout.Label("Export path: " + wti.ExportPath);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Reset Path", GUILayout.Width(200), GUILayout.Width(150)))
            {
                settingsTarget.ResetPaths(0);
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Reset Path", GUILayout.Width(200), GUILayout.Width(150)))
            {
                settingsTarget.ResetPaths(1);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical ();
            EditorGUILayout.BeginScrollView (Vector2.zero, GUILayout.Width (100), GUILayout.Height (140));
            
            if (GUILayout.Button ("Import", GUILayout.Width (100), GUILayout.Height (50))) {
               settingsTarget.StartImporting ();
            }
            if (GUILayout.Button ("Refresh", GUILayout.Width (100), GUILayout.Height (50))) {
               settingsTarget.RegeneratePreview ();
            }
            GUILayout.Label ("If you are not sure what settings to use, try the defaults!");
            EditorGUILayout.EndScrollView ();
            EditorGUI.BeginChangeCheck ();
            
            settingsTarget.chosenFormat = GUILayout.SelectionGrid (settingsTarget.chosenFormat, new[] {"Each pokemon is in a separate folder","Each folder has a type of binary ('Mdls','Tex','Etc')" }, 2, GUI.skin.toggle);
            if (EditorGUI.EndChangeCheck ()) {
               settingsTarget.RegeneratePreview ();
            }
            
            EditorGUI.BeginChangeCheck ();
            DrawDefaultInspector ();
            if (EditorGUI.EndChangeCheck ()) {
       
               if (!wti.ImportModel) {
                  wti.ApplyMaterials = false;
                  wti.SkeletalAnimations = false;
                  wti.MaterialAnimations = false;
                  wti.RenameGeneratedAnimationFiles = false;
                  wti.VisibilityAnimations = false;
               }

               if (!wti.ImportTextures) {
                  wti.ImportMaterials = false;
               }

               if (!wti.ImportMaterials) {
                  wti.ApplyMaterials = false;
                  wti.MaterialAnimations = false;
               }

               if (!wti.ApplyMaterials) {
                  wti.MaterialAnimations = false;
               }

               if (!wti.SkeletalAnimations) {
                  wti.MaterialAnimations = false;
                  wti.RenameGeneratedAnimationFiles = false;
                  wti.VisibilityAnimations = false;
               }
            }
            EditorGUILayout.BeginScrollView (Vector2.zero, GUILayout.Width (100), GUILayout.Height (10));
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
