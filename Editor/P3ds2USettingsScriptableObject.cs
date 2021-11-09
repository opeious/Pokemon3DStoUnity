using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
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
                    string filePath = EditorUtility.SaveFilePanelInProject("Choose the folder where to save the new Import Settings", SettingsFileName, "asset", "Save", PokemonImporter.ImportPath);
                    if (string.IsNullOrEmpty(filePath))
                        filePath = PokemonImporter.ImportPath + SettingsFileName;

                    AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<P3ds2USettingsScriptableObject>(), filePath);
                    return GetOrCreateSettings(_focus);
                }

                if (_focus) {
                   Selection.activeObject = settings;
                }

                Pokemon3DSToUnityEditorWindow.settings = settings;
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
      [SerializeField] public Shader fireCoreShader;
      [SerializeField] public Shader fireStencilShader;
      
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
      public string Constant4Color =  ("_Constant4Color");
      public string Constant3Color =  ("_Constant3Color");

      public string Tex0TranslateX = "material._BaseMapOffset.x";
      public string Tex1TranslateX = "material._OcclusionMapOffset.x";
      public string Tex2TranslateX = "material._NormalMapOffset.x";
      public string Tex0TranslateY = "material._BaseMapOffset.y";
      public string Tex1TranslateY = "material._OcclusionMapOffset.y";
      public string Tex2TranslateY = "material._NormalMapOffset.y";
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
    public class P3ds2UAnimatorProperties
    {
        [SerializeField] public UnityEditor.Animations.AnimatorController baseController;

        #region Fight Animations
        public string Fight_Idle = ("Idle");
        public string Appear = ("Appear");
        public string Transform = ("Transform");
        public string Release = ("Release");
        public string Dropping = ("Dropping");
        public string Landing = ("Landing");
        public string Release_without_Landing = ("Release_without_Landing");
        public string Mega_Upgraded = ("Mega_Upgraded");
        public string Attack = ("Attack");
        public string Attack_2 = ("Attack_2");
        public string Attack_3 = ("Attack_3");
        public string Attack_4 = ("Attack_4");
        public string No_Touch_Attack = ("No_Touch_Attack");
        public string No_Touch_Attack_2 = ("No_Touch_Attack_2");
        public string No_Touch_Attack_3 = ("No_Touch_Attack_3");
        public string No_Touch_Attack_4 = ("No_Touch_Attack_4");
        public string Be_Attacked = ("Be_Attacked");
        public string Lost = ("Lost");
        public string Fight_Empty = ("Empty");
        public string Fight_Eye_Emotion = ("Eye_Emotion");
        public string Fight_Eye_2_Emotion = ("Eye_2_Emotion");
        public string Fight_Eye_3_Emotion = ("Eye_3_Emotion");
        public string Fight_Mouth_Emotion = ("Mouth_Emotion");
        public string Fight_Mouth_2_Emotion = ("Mouth_2_Emotion");
        public string Fight_Mouth_3_Emotion = ("Mouth_3_Emotion");
        public string Fight_State = ("State");
        public string Fight_State_2 = ("State_2");
        public string Fight_State_3 = ("State_3");
        public string Fight_State_4 = ("State_4");
        #endregion

        #region Pet Animations
        public string Pet_Idle = ("Idle");
        public string Turn = ("Turn");
        public string Look_Back = ("Look_Back");
        public string Look_Back_Happily = ("Look_Back_Happily");
        public string Falling_Asleep = ("Falling_Asleep");
        public string Sleepy = ("Sleepy");
        public string Sleepy_Awaken = ("Sleepy_Awaken");
        public string Sleeping = ("Sleeping");
        public string Awaken = ("Awaken");
        public string Refuse = ("Refuse");
        public string Thinking = ("Thinking");
        public string Agree = ("Agree");
        public string Happy = ("Happy");
        public string Very_Happy = ("Very_Happy");
        public string Look_Around = ("Look_Around");
        public string Rub_Eyes = ("Rub_Eyes");
        public string Comfortable = ("Comfortable");
        public string Relax = ("Relax");
        public string Sad = ("Sad");
        public string Salutate = ("Salutate");
        public string Happy_2 = ("Happy_2");
        public string Angry = ("Angry");
        public string Begin_Eating = ("Begin_Eating");
        public string Eating = ("Eating");
        public string Eating_Finished = ("Eating_Finished");
        public string No_Eating = ("No_Eating");
        public string Pet_Empty = ("Empty");
        public string Pet_Eye_Emotion = ("Eye_Emotion");
        public string Pet_Eye_2_Emotion = ("Eye_2_Emotion");
        public string Pet_Eye_3_Emotion = ("Eye_3_Emotion");
        public string Pet_Mouth_Emotion = ("Mouth_Emotion");
        public string Pet_Mouth_2_Emotion = ("Mouth_2_Emotion");
        public string Pet_Mouth_3_Emotion = ("Mouth_3_Emotion");
        public string Pet_State = ("State");
        public string Pet_State_2 = ("State_2");
        public string Pet_State_3 = ("State_3");
        public string Pet_State_4 = ("State_4");
        #endregion

        #region Movement Animations
        public string Movement_Idle = ("Idle");
        public string Movement_Empty = ("Empty");
        public string Walk = ("Walk");
        public string Run = ("Run");
        public string Empty_2 = ("Empty_2");
        public string Start_Walk = ("Start_Walk");
        public string End_Walk = ("End_Walk");
        public string Empty_3 = ("Empty_3");
        public string Start_Run = ("Start_Run");
        public string End_Run = ("End_Run");
        public string Empty_4 = ("Empty_4");
        public string Start_Run_2 = ("Start_Run_2");
        public string End_Run_2 = ("End_Run_2");
        public string Empty_5 = ("Empty_5");
        public string Movement_Eye_Emotion = ("Eye_Emotion");
        public string Movement_Eye_2_Emotion = ("Eye_2_Emotion");
        public string Movement_Eye_3_Emotion = ("Eye_3_Emotion");
        public string Movement_Mouth_Emotion = ("Mouth_Emotion");
        public string Movement_Mouth_2_Emotion = ("Mouth_2_Emotion");
        public string Movement_Mouth_3_Emotion = ("Mouth_3_Emotion");
        public string Movement_State = ("State");
        public string Movement_State_2 = ("State_2");
        public string Movement_State_3 = ("State_3");
        public string Movement_State_4 = ("State_4");
        #endregion

        //public string test = AnimationNaming.animationNames["Fight"][0];
    }

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
      public bool ImportShinyMaterials;
      public bool ApplyMaterials;
      public bool ApplyShinyMaterials;
      public bool ImportFireMaterials;
      
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
      [HideInInspector] public string PackageVersion = "";

         private bool _generated;
      [SerializeField] public WhatToImport ImporterSettings;
      
      [SerializeField] private List<MergedBinary> mergedBinariesPreview;
      
      public P3ds2UShaderProperties customShaderSettings;

      [Tooltip("Name clips same as in custom controller.")] public P3ds2UAnimatorProperties customAnimatorSettings;

      public static P3ds2USettingsScriptableObject Instance;

      [HideInInspector] public int chosenFormat; // 0 or 1
      public static bool ImportInProgress;
      
        public string ExportPath { get { return ImporterSettings.ExportPath; } }
        public string ImportPath { get { return ImporterSettings.ImportPath; } }

        [Serializable]
        public class PackageJsonObject
        {
           public string version;
        }
        
      private P3ds2USettingsScriptableObject ()
      {
         try {
            var packageJsonObject =
               JsonUtility.FromJson<PackageJsonObject> (File.ReadAllText ("Assets/3dsToUnity/package.json"));
            PackageVersion = packageJsonObject.version;
         }
         catch (Exception) {
            //ignored
         }
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
                ImportFireMaterials = false,
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
            customShaderSettings.fireCoreShader = Shader.Find ("Shader Graphs/LitPokemonFireCoreShader");
            customShaderSettings.fireStencilShader = Shader.Find ("Shader Graphs/LitPokemonFireStencil");
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
            GUILayout.Label("Version: " + settingsTarget.PackageVersion + "\n");
            EditorGUILayout.EndHorizontal();

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
            EditorGUILayout.BeginScrollView (Vector2.zero, GUILayout.Width (400), GUILayout.Height (140));
            
            if (GUILayout.Button ("Import", GUILayout.Width (100), GUILayout.Height (50))) {
               settingsTarget.StartImporting ();
            }
            if (GUILayout.Button ("Refresh", GUILayout.Width (100), GUILayout.Height (50))) {
               settingsTarget.RegeneratePreview ();
            }
            GUILayout.Label ("If you are not sure what settings to use, try the defaults!", GUILayout.ExpandWidth (true));
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
                  wti.ImportFireMaterials = false;
               }

               if (!wti.ImportTextures) {
                  wti.ImportMaterials = false;
                  wti.ImportShinyMaterials = false;
                  wti.ImportFireMaterials = false;
               }
               
               if (!wti.ImportMaterials) {
                  wti.ApplyMaterials = false;
               }

               if (!wti.ImportShinyMaterials) {
                  wti.ApplyShinyMaterials = false;
               }

               if (!wti.ImportMaterials && !wti.ImportShinyMaterials) {
                  wti.MaterialAnimations = false;
                  wti.ImportFireMaterials = false;
               }

               if (!wti.ApplyMaterials && !wti.ApplyShinyMaterials) {
                  wti.MaterialAnimations = false;
               }

               if (!wti.SkeletalAnimations) {
                  wti.MaterialAnimations = false;
                  wti.RenameGeneratedAnimationFiles = false;
                  wti.VisibilityAnimations = false;
               }

               if (wti.ImportFireMaterials) {
                  FireImporterSettingsUtil.ThrowWarningAndChangeSettings ();
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
