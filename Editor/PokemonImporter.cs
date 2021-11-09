﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CurveExtended;
using P3DS2U.Editor.SPICA;
using P3DS2U.Editor.SPICA.COLLADA;
using P3DS2U.Editor.SPICA.Commands;
using P3DS2U.Editor.SPICA.H3D;
using P3DS2U.Editor.SPICA.H3D.Animation;
using P3DS2U.Editor.SPICA.H3D.Model;
using P3DS2U.Editor.SPICA.H3D.Model.Mesh;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

/*
 *TODO: Separate the model and the container prefabs, to have the model drag-droppable in the animation previews.
 *TODO: Autogenerate emmision maps and emission shaders
 *TODO: Shaders in ase format (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null && UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
 *TODO: AssetBundles/Addressable build size optimizations
 *TODO: Environments/Characters
 */

namespace P3DS2U.Editor
{
    public class PokemonImporter : EditorWindow
    {
        public const string ImportPath = "Assets/Bin3DS/";
        public const string ExportPath = "Assets/Exported/";

        private static int _processedCount;

        private delegate void OnAnimationImported (AnimationClip clip);
        private static OnAnimationImported OnAnimationImportedEvent;
        public static void RaiseOnAnimationImportedEvent (AnimationClip clip) => OnAnimationImportedEvent?.Invoke (clip);
        private static H3D h3DScene = null;
        private static int CurrentAnimationIndex = 0;
        private static string CurrentAnimationExportFolder = "";
        public static List<AnimationImportOptions> AnimationImportOptions = new List<AnimationImportOptions>();

        [MenuItem ("3DStoUnity/Find Settings Object")]
        private static void ImportPokemonAction ()
        {
            if (!Directory.Exists (ImportPath)) {
                Directory.CreateDirectory (ImportPath);
                EditorUtility.DisplayDialog ("Created Folder " + ImportPath,
                    "Created Folder" + ImportPath +
                    " \nPlease place .bin files to be imported in that directory or subdirectories, Files with the same name will be merged together",
                    "ok");
            }
            SettingsUtils.GetOrCreateSettings(true);
        }
        
        public static void StartImportingBinaries (P3ds2USettingsScriptableObject importSettings, Dictionary<string, List<string>> scenesDict)
        {
            try {
                string ExportPath = importSettings.ExportPath;
                AnimationImportOptions.Add(importSettings.ImporterSettings.FightAnimationsToImport);
                AnimationImportOptions.Add(importSettings.ImporterSettings.PetAnimationsToImport);
                AnimationImportOptions.Add(importSettings.ImporterSettings.MovementAnimationsToImport);
                _processedCount = 0;
                for (int i = importSettings.ImporterSettings.StartIndex; i <= importSettings.ImporterSettings.EndIndex; i++)
                {
                    var kvp = scenesDict.ElementAt(i);
                    EditorUtility.ClearProgressBar ();
                    EditorUtility.DisplayProgressBar ("Importing", kvp.Key.Replace (".bin", ""),
                        (float) _processedCount / scenesDict.Count);

                    h3DScene = new H3D ();

                    //Add all non-animation binaries first
                    foreach (var singleFileToBeMerged in kvp.Value) {
                        var fileType = BinaryUtils.GetBinaryFileType (singleFileToBeMerged);
                        if (fileType == BinaryUtils.FileType.Animation) continue;
                        H3DDict<H3DBone> skeleton = null;
                        if (h3DScene.Models.Count > 0) skeleton = h3DScene.Models[0].Skeleton;
                        var data = FormatIdentifier.IdentifyAndOpen (singleFileToBeMerged, skeleton);
                        if (data != null) h3DScene.Merge (data);
                    }

                    int animFilesCount = 0;
                    //Merge animation binaries
                    foreach (var singleFileToBeMerged in kvp.Value) {
                        var fileType = BinaryUtils.GetBinaryFileType (singleFileToBeMerged);
                        if (fileType != BinaryUtils.FileType.Animation) continue;
                        H3DDict<H3DBone> skeleton = null;
                        if (h3DScene.Models.Count > 0) skeleton = h3DScene.Models[0].Skeleton;
                        var data = FormatIdentifier.IdentifyAndOpen (singleFileToBeMerged, skeleton, animFilesCount);
                        animFilesCount++;
                        if (data != null) h3DScene.Merge (data);
                    }

                    var combinedExportFolder = ExportPath + kvp.Key.Replace (".bin", "") + "/Files/";
                    if (!Directory.Exists (combinedExportFolder)) {
                        Directory.CreateDirectory (combinedExportFolder);
                    } else {
                        Directory.Delete (ExportPath + kvp.Key.Replace (".bin", "") + "/", true);
                        Directory.CreateDirectory (combinedExportFolder);
                    }

                    if (importSettings.ImporterSettings.ImportTextures) {
                        GenerateTextureFiles (h3DScene, combinedExportFolder);   
                    }

                    var meshDict = new Dictionary<string, SkinnedMeshRenderer> ();
                    if (importSettings.ImporterSettings.ImportModel) {
                        try {
                            meshDict = GenerateMeshInUnityScene (h3DScene, combinedExportFolder, importSettings);
                        }
                        catch (Exception e) {
                            Debug.LogError (
                                "Check your settings! Are you sure, you are using the correct input format, is each pokemon's binaries in a separate folder?\n" +
                                e.Message + "\n" + e.StackTrace);
                        }
                    }

                    var matDict = new Dictionary<string, Material> ();
                    
                    if (importSettings.ImporterSettings.ImportShinyMaterials) {
                        matDict = GenerateShinyMaterialFiles (h3DScene, combinedExportFolder, importSettings.customShaderSettings);
                    }

                    if (importSettings.ImporterSettings.ImportMaterials) {
                        matDict = GenerateMaterialFiles (h3DScene, combinedExportFolder, importSettings.customShaderSettings);   
                    }

                    if (importSettings.ImporterSettings.ApplyMaterials) {
                        AddMaterialsToGeneratedMeshes (meshDict, matDict, h3DScene);   
                    } else if (importSettings.ImporterSettings.ApplyShinyMaterials) {
                        AddMaterialsToGeneratedMeshes (meshDict, matDict, h3DScene);
                    }

                    if (importSettings.ImporterSettings.SkeletalAnimations) {
                        GenerateSkeletalAnimations (h3DScene, combinedExportFolder);
                    }
                    
                    if (importSettings.ImporterSettings.VisibilityAnimations) {
                        GenerateVisibilityAnimations (h3DScene, combinedExportFolder);
                    }

                    if (importSettings.ImporterSettings.MaterialAnimations) {
                        GenerateMaterialAnimations (h3DScene, combinedExportFolder, importSettings);   
                    }

                    var modelGo = GameObject.Find ("GeneratedUnityObject");
                    if (modelGo != null) {
                        var modelName = kvp.Key.Replace (".bin", "");
                        if (importSettings.ImporterSettings.SkeletalAnimations) {
                            if (importSettings.customAnimatorSettings.baseController == null)
                            { GenerateAnimationController(modelGo, combinedExportFolder, modelName); }
                            else
                            { GenerateAnimationOverrideController(modelGo, combinedExportFolder, modelName, importSettings.customAnimatorSettings); }
                        }
                        
                        var go = new GameObject ("GeneratedUnityObject");
                        modelGo.transform.SetParent (go.transform);
                        modelGo.name = "Model";
                        
                        go.name = modelName + " (Container)";
                        var prefabPath =
                            AssetDatabase.GenerateUniqueAssetPath (ExportPath + kvp.Key.Replace (".bin", "") + "/" + kvp.Key.Replace (".bin", "") + ".prefab");
                        PrefabUtility.SaveAsPrefabAssetAndConnect (go, prefabPath, InteractionMode.UserAction);

                        go.transform.localPosition = new Vector3 {
                            x = Random.Range (-100f, 100f),
                            y = 0,
                            z = Random.Range (-100f, 100f)
                        };   
                    }
                }
            }
            catch (Exception e) {
                Debug.LogError ("Something went horribly wrong! Hmu, I'll try to fix it.\n" + e.Message + "\n" +
                                e.StackTrace);
            }

            EditorUtility.ClearProgressBar();
        }

        private static void GenerateAnimationController(GameObject modelGo, string combinedExportFolder,
            string modelName)
        {
            var animationsFolderPath = combinedExportFolder + "Animations/";
            var animator = modelGo.AddComponent<Animator> ();

            var animatorController = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(animationsFolderPath + $"animController-{modelName}.controller");

            var files = Directory.GetFiles (animationsFolderPath);

            // animatorController.layers[0].stateMachine.AddState ("1");
            foreach (var animationFilePath in files) {
                var animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip> (animationFilePath);
                if (animationClip != null) {
                    animatorController.AddMotion (animationClip);
                }
            }

            animator.runtimeAnimatorController = animatorController;
            AssetDatabase.Refresh();
        }

        private static void GenerateAnimationOverrideController(GameObject modelGo, string combinedExportFolder,
            string modelName, P3ds2UAnimatorProperties animatorImportSettings)
        {
            var animationsFolderPath = combinedExportFolder + "Animations/";
            var animator = modelGo.AddComponent<Animator>();
            var animatorOverrideController = AssetHelper.GetAsset<AnimatorOverrideController>($"animController-{modelName}", animationsFolderPath + $"animController-{modelName}.overrideController");
            animatorOverrideController.runtimeAnimatorController = animatorImportSettings.baseController;

            var files = Directory.GetFiles(animationsFolderPath);
            var overrides = new List<KeyValuePair<string, string>>();
            
            #region Animation Key Pairs
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Idle".ToLower(), animatorImportSettings.Fight_Idle));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Appear".ToLower(), animatorImportSettings.Appear));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Transform".ToLower(), animatorImportSettings.Transform));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Release".ToLower(), animatorImportSettings.Release));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Dropping".ToLower(), animatorImportSettings.Dropping));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Landing".ToLower(), animatorImportSettings.Landing));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Release_without_Landing".ToLower(), animatorImportSettings.Release_without_Landing));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Mega_Upgraded".ToLower(), animatorImportSettings.Mega_Upgraded));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Attack".ToLower(), animatorImportSettings.Attack));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Attack_2".ToLower(), animatorImportSettings.Attack_2));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Attack_3".ToLower(), animatorImportSettings.Attack_3));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Attack_4".ToLower(), animatorImportSettings.Attack_4));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "No_Touch_Attack".ToLower(), animatorImportSettings.No_Touch_Attack));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "No_Touch_Attack_2".ToLower(), animatorImportSettings.No_Touch_Attack_2));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "No_Touch_Attack_3".ToLower(), animatorImportSettings.No_Touch_Attack_3));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "No_Touch_Attack_4".ToLower(), animatorImportSettings.No_Touch_Attack_4));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Be_Attacked".ToLower(), animatorImportSettings.Be_Attacked));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Lost".ToLower(), animatorImportSettings.Lost));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Empty".ToLower(), animatorImportSettings.Fight_Empty));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Eye_Emotion".ToLower(), animatorImportSettings.Fight_Eye_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Eye_2_Emotion".ToLower(), animatorImportSettings.Fight_Eye_2_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Eye_3_Emotion".ToLower(), animatorImportSettings.Fight_Eye_3_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Mouth_Emotion".ToLower(), animatorImportSettings.Fight_Mouth_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Mouth_2_Emotion".ToLower(), animatorImportSettings.Fight_Mouth_2_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "Mouth_3_Emotion".ToLower(), animatorImportSettings.Fight_Mouth_3_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "State".ToLower(), animatorImportSettings.Fight_State));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "State_2".ToLower(), animatorImportSettings.Fight_State_2));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "State_3".ToLower(), animatorImportSettings.Fight_State_3));
            overrides.Add(new KeyValuePair<string, string>("Fight_" + "State_4".ToLower(), animatorImportSettings.Fight_State_4));

            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Idle".ToLower(), animatorImportSettings.Pet_Idle));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Turn".ToLower(), animatorImportSettings.Turn));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Look_Back".ToLower(), animatorImportSettings.Look_Back));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Look_Back_Happily".ToLower(), animatorImportSettings.Look_Back_Happily));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Falling_Aasleep".ToLower(), animatorImportSettings.Falling_Asleep));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Sleepy".ToLower(), animatorImportSettings.Sleepy));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Sleepy_Awaken".ToLower(), animatorImportSettings.Sleepy_Awaken));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Sleeping".ToLower(), animatorImportSettings.Sleeping));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Awaken".ToLower(), animatorImportSettings.Awaken));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Refuse".ToLower(), animatorImportSettings.Refuse));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Thinking".ToLower(), animatorImportSettings.Thinking));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Agree".ToLower(), animatorImportSettings.Agree));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Happy".ToLower(), animatorImportSettings.Happy));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Very_Happy".ToLower(), animatorImportSettings.Very_Happy));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Look_Around".ToLower(), animatorImportSettings.Look_Around));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Rub_Eyes".ToLower(), animatorImportSettings.Rub_Eyes));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Comfortable".ToLower(), animatorImportSettings.Comfortable));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Sad".ToLower(), animatorImportSettings.Sad));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Salutate".ToLower(), animatorImportSettings.Salutate));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Happy_2".ToLower(), animatorImportSettings.Happy_2));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Angry".ToLower(), animatorImportSettings.Angry));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Begin_Eating".ToLower(), animatorImportSettings.Begin_Eating));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Eating".ToLower(), animatorImportSettings.Eating));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Eating_Finished".ToLower(), animatorImportSettings.Eating_Finished));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "No_Eating".ToLower(), animatorImportSettings.No_Eating));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Empty".ToLower(), animatorImportSettings.Pet_Empty));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Eye_Emotion".ToLower(), animatorImportSettings.Pet_Eye_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Eye_2_Emotion".ToLower(), animatorImportSettings.Pet_Eye_2_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Eye_3_Emotion".ToLower(), animatorImportSettings.Pet_Eye_3_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Mouth_Emotion".ToLower(), animatorImportSettings.Pet_Mouth_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Mouth_2_Emotion".ToLower(), animatorImportSettings.Pet_Mouth_2_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "Mouth_3_Emotion".ToLower(), animatorImportSettings.Pet_Mouth_3_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "State".ToLower(), animatorImportSettings.Pet_State));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "State_2".ToLower(), animatorImportSettings.Pet_State_2));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "State_3".ToLower(), animatorImportSettings.Pet_State_3));
            overrides.Add(new KeyValuePair<string, string>("Pet_" + "State_4".ToLower(), animatorImportSettings.Pet_State_4));

            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Idle".ToLower(), animatorImportSettings.Movement_Idle));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Empty".ToLower(), animatorImportSettings.Movement_Empty));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Walk".ToLower(), animatorImportSettings.Walk));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Run".ToLower(), animatorImportSettings.Run));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Empty_2".ToLower(), animatorImportSettings.Empty_2));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Start_Walk".ToLower(), animatorImportSettings.Start_Walk));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "End_Walk".ToLower(), animatorImportSettings.End_Walk));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Empty_3".ToLower(), animatorImportSettings.Empty_3));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Start_Run".ToLower(), animatorImportSettings.Start_Run));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "End_Run".ToLower(), animatorImportSettings.End_Run));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Empty_4".ToLower(), animatorImportSettings.Empty_4));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Start_Run_2".ToLower(), animatorImportSettings.Start_Run_2));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "End_Run_2".ToLower(), animatorImportSettings.End_Run_2));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Empty_5".ToLower(), animatorImportSettings.Empty_5));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Eye_Emotion".ToLower(), animatorImportSettings.Movement_Eye_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Eye_2_Emotion".ToLower(), animatorImportSettings.Movement_Eye_2_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Eye_3_Emotion".ToLower(), animatorImportSettings.Movement_Eye_3_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Mouth_Emotion".ToLower(), animatorImportSettings.Movement_Mouth_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Mouth_2_Emotion".ToLower(), animatorImportSettings.Movement_Mouth_2_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "Mouth_3_Emotion".ToLower(), animatorImportSettings.Movement_Mouth_3_Emotion));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "State".ToLower(), animatorImportSettings.Movement_State));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "State_2".ToLower(), animatorImportSettings.Movement_State_2));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "State_3".ToLower(), animatorImportSettings.Movement_State_3));
            overrides.Add(new KeyValuePair<string, string>("Movement_" + "State_4".ToLower(), animatorImportSettings.Movement_State_4));

            #endregion

            Debug.Log("Fight_" + "Idle".ToLower());
            foreach (var animationFilePath in files)
            {
                var animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationFilePath);
                if (animationClip != null)
                {
                    foreach (KeyValuePair<string, string> anim in overrides)
                    {
                        Debug.Log("Animation Clip name is: " + animationClip.name + " and Key is: " + anim.Key);
                        if (animationClip.name == anim.Key)
                        {
                            
                            try
                            { animatorOverrideController[anim.Value] = animationClip; }
                            catch (Exception e)
                            {
                                Debug.LogWarning("Override Animation" + anim.Value + "either does not exist or is written incorrectly.\n" + e.Message + "\n" +
                                e.StackTrace);
                            }
                            break;
                        }
                    }
                }
            }

            
            animatorOverrideController.SaveAsset(animationsFolderPath + $"animController-{modelName}.overrideController");
            animator.runtimeAnimatorController = animatorOverrideController;
            AssetDatabase.Refresh();
        }

        private static void GenerateVisibilityAnimations (H3D h3DScene, string combinedExportFolder)
        {
            var modelTransform = GameObject.Find ("GeneratedUnityObject").transform;

            foreach (var currentVisAnim in h3DScene.VisibilityAnimations) {
                var fileCreated = false;
                var animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip> (combinedExportFolder + "/Animations/" + currentVisAnim.Name + ".anim");
                if (animationClip == null) {
                    animationClip = new AnimationClip {name = currentVisAnim.Name};
                    var clipSettings = AnimationUtility.GetAnimationClipSettings (animationClip);
                    clipSettings.loopTime = currentVisAnim.AnimationFlags == H3DAnimationFlags.IsLooping;
                    AnimationUtility.SetAnimationClipSettings (animationClip, clipSettings);
                    fileCreated = true;
                }

                foreach (var animationElement in currentVisAnim.Elements) {
                    switch (animationElement.PrimitiveType) {
                        case H3DPrimitiveType.Boolean:
                            if (animationElement.TargetType == H3DTargetType.MeshNodeVisibility) {
                                var visCurve = new AnimationCurve ();
                                if (animationElement.Content is H3DAnimBoolean h3DAnimBoolean) {
                                    var added = false;
                                    for (var i = 0; i < h3DAnimBoolean.Values.Count; i++) {
                                        visCurve.AddKey (KeyframeUtil.GetNew (
                                            AnimationUtils.GetTimeAtFrame (animationClip, i),
                                            h3DAnimBoolean.Values[i] ? 1f : 0f,
                                            TangentMode.Stepped));
                                        if (h3DAnimBoolean.Values[i] == false) {
                                            added = true;
                                        }
                                    }

                                    if (added) {
                                        var skms = modelTransform.GetComponentsInChildren<Transform> ();
                                        foreach (var skm in skms) {
                                            if (skm.name.Replace (" (Instance)", "").Contains(animationElement.Name)) {
                                                var cbp = AnimationUtility.CalculateTransformPath (skm.transform,
                                                    modelTransform);
                                                animationClip.SetCurve (cbp, typeof(GameObject), "m_IsActive", visCurve);
                                            }
                                        }
                                    }
                                }
                            }

                            break;
                    }
                }

                if (fileCreated) {
                    AssetDatabase.CreateAsset (animationClip, combinedExportFolder + "/Animations/" + currentVisAnim.Name + ".anim");
                }
                AssetDatabase.SaveAssets ();
                AssetDatabase.Refresh ();
            }
        }

        private static void GenerateMaterialAnimations (H3D h3DScene, string combinedExportFolder, P3ds2USettingsScriptableObject importerSettings)
        {
            var modelTransform = GameObject.Find ("GeneratedUnityObject").transform;

            for (var index = 0; index < h3DScene.MaterialAnimations.Count; index++) {
                var currentMatAnim = h3DScene.MaterialAnimations[index];
                var fileCreated = false;
                var animationClip =
                    AssetDatabase.LoadAssetAtPath<AnimationClip> (combinedExportFolder + "/Animations/" +
                                                                  currentMatAnim.Name + ".anim");
                if (animationClip == null) {
                    animationClip = new AnimationClip {name = currentMatAnim.Name};
                    var clipSettings = AnimationUtility.GetAnimationClipSettings (animationClip);
                    clipSettings.loopTime = currentMatAnim.AnimationFlags == H3DAnimationFlags.IsLooping;
                    AnimationUtility.SetAnimationClipSettings (animationClip, clipSettings);
                    fileCreated = true;
                }
                
                var newCurves = new Dictionary<AnimationUtils.MatAnimationModifier, AnimationCurve> ();
                foreach (var animationElement in currentMatAnim.Elements) {
                    switch (animationElement.PrimitiveType) {
                        case H3DPrimitiveType.Vector2D:
                            var targetType = animationElement.TargetType;
                            if (targetType != H3DTargetType.MaterialTexCoord0Trans &&
                                animationElement.TargetType != H3DTargetType.MaterialTexCoord1Trans &&
                                animationElement.TargetType != H3DTargetType.MaterialTexCoord2Trans) {
                                continue;
                            }
                            
                            
                            AnimationCurve curveY = null;
                            switch (targetType) {
                                case H3DTargetType.MaterialTexCoord0Trans:
                                    curveY = AnimationUtils.GetOrAddCurve (newCurves,
                                        AnimationUtils.MatAnimationModifier.Tex0TranslateY);
                                    break;
                                case H3DTargetType.MaterialTexCoord1Trans:
                                    curveY = AnimationUtils.GetOrAddCurve (newCurves,
                                        AnimationUtils.MatAnimationModifier.Tex1TranslateY);
                                    break;
                                case H3DTargetType.MaterialTexCoord2Trans:
                                    curveY = AnimationUtils.GetOrAddCurve (newCurves,
                                        AnimationUtils.MatAnimationModifier.Tex2TranslateY);
                                    break;
                            }

                            AnimationCurve curveX = null;
                            switch (targetType) {
                                case H3DTargetType.MaterialTexCoord0Trans:
                                    curveX = AnimationUtils.GetOrAddCurve (newCurves,
                                        AnimationUtils.MatAnimationModifier.Tex0TranslateX);
                                    break;
                                case H3DTargetType.MaterialTexCoord1Trans:
                                    curveX = AnimationUtils.GetOrAddCurve (newCurves,
                                        AnimationUtils.MatAnimationModifier.Tex1TranslateX);
                                    break;
                                case H3DTargetType.MaterialTexCoord2Trans:
                                    curveX = AnimationUtils.GetOrAddCurve (newCurves,
                                        AnimationUtils.MatAnimationModifier.Tex2TranslateX);
                                    break;
                            }

                            if (animationElement.Content is H3DAnimVector2D h3DAnimVector2D) {
                                var interpolateY = false;
                                foreach (var singleYFrame in h3DAnimVector2D.Y.KeyFrames) {
                                    var lhs = singleYFrame.InSlope;
                                    var rhs = singleYFrame.OutSlope;
                                    TangentMode tangentMode;
                                    if (lhs == 0 && rhs == 0) {
                                        tangentMode = TangentMode.Stepped;
                                    } else {
                                        tangentMode = TangentMode.Linear;
                                    }

                                    if (!importerSettings.ImporterSettings.InterpolateAnimations) {
                                        tangentMode = TangentMode.Stepped;
                                    }
                                    var time = AnimationUtils.GetTimeAtFrame (
                                        animationClip,
                                        (int) singleYFrame.Frame);
                                    var keyFrame = KeyframeUtil.GetNew (time, 1 - singleYFrame.Value,tangentMode);
                                    curveY.AddKey (keyFrame);

                                    if (tangentMode == TangentMode.Linear) {
                                        interpolateY = true;
                                    }
                                }
                                if (interpolateY) {
                                    curveY.UpdateAllLinearTangents ();
                                }
                            }

                            if (animationElement.Content is H3DAnimVector2D h3DAnimVector2d) {
                                var interpolateX = false;
                                foreach (var singleXFrame in h3DAnimVector2d.X.KeyFrames) {
                                    var lhs = singleXFrame.InSlope;
                                    var rhs = singleXFrame.OutSlope;
                                    TangentMode tangentMode;
                                    if (lhs == 0 && rhs == 0) {
                                        tangentMode = TangentMode.Stepped;
                                    } else {
                                        tangentMode = TangentMode.Linear;
                                    }

                                    if (!importerSettings.ImporterSettings.InterpolateAnimations) {
                                        tangentMode = TangentMode.Stepped;
                                    }

                                    float value;
                                    if (targetType != H3DTargetType.MaterialTexCoord2Trans) {
                                        value = (float) Math.Abs (Math.Round (1 - singleXFrame.Value,
                                            MidpointRounding.AwayFromZero));
                                    } else {
                                        value = 2 - singleXFrame.Value;
                                    }
                                    var time = AnimationUtils.GetTimeAtFrame (animationClip, (int) singleXFrame.Frame);
                                    var keyFrame = KeyframeUtil.GetNew (time, value,tangentMode);
                                    curveX.AddKey (keyFrame);
                                    
                                    if (tangentMode == TangentMode.Linear) {
                                        interpolateX = true;
                                    }
                                }
                                if (interpolateX) {
                                    curveX.UpdateAllLinearTangents ();   
                                }
                            }

                            foreach (var kvp in newCurves) {
                                var skms = modelTransform.GetComponentsInChildren<SkinnedMeshRenderer> ();
                                foreach (var skm in skms) {
                                    if (skm.sharedMaterial.name.Replace (" (Instance)", "") == animationElement.Name) {
                                        var cbp = AnimationUtility.CalculateTransformPath (skm.transform,
                                            modelTransform);
                                        var shaderPropName = AnimationUtils.MatModifierToShaderProp (kvp.Key);
                                        animationClip.SetCurve (cbp, typeof(SkinnedMeshRenderer),
                                            shaderPropName, kvp.Value);
                                    }
                                }
                            }

                            break;
                    }
                }

                if (fileCreated) {
                    AssetDatabase.CreateAsset (animationClip,
                        combinedExportFolder + "/Animations/" + currentMatAnim.Name + ".anim");
                }

                AssetDatabase.SaveAssets ();
                AssetDatabase.Refresh ();
            }
        }

        private static void OnAnimationPostprocessed (AnimationClip daeClip)
        {
            var newClip = new AnimationClip ();

            var x = AnimationUtility.GetCurveBindings (daeClip);
            foreach (var y in x) {
                AnimationUtility.SetEditorCurve (newClip, y,
                    AnimationUtility.GetEditorCurve (daeClip, y));
            }
            
            var clipSettings = AnimationUtility.GetAnimationClipSettings (newClip);
            clipSettings.loopTime = h3DScene.SkeletalAnimations[0].AnimationFlags == H3DAnimationFlags.IsLooping;
            AnimationUtility.SetAnimationClipSettings(newClip, clipSettings);
            if (!string.IsNullOrEmpty (CurrentAnimationExportFolder)) {
                PokemonAnimationImporter.IsEnabled = false;
                AssetDatabase.CreateAsset (newClip, CurrentAnimationExportFolder + h3DScene.SkeletalAnimations[CurrentAnimationIndex++].Name + ".anim");
                AssetDatabase.Refresh ();
            }
        }

        
        //NOTE: doing it this way because unity does keyframe compression on asset import.
        //which won't happen if I generate the animation inside of Unity, also generating animations is hard =/
        //Hit me up, if you know a better way of loading the animations from the binaries
        private static void GenerateSkeletalAnimations (H3D h3DScene, string combinedExportFolder)
        {
            CurrentAnimationExportFolder = combinedExportFolder + "/Animations/";
            var tempFilePath = CurrentAnimationExportFolder + "/BinInterimAnimation.dae";
            if (!Directory.Exists (CurrentAnimationExportFolder)) {
                Directory.CreateDirectory (CurrentAnimationExportFolder);
            }
            CurrentAnimationIndex = 0;
            for (var i = 0; i < h3DScene.SkeletalAnimations.Count; i++) {
                OnAnimationImportedEvent += OnAnimationPostprocessed;
                var dae = new DaeAnimations (h3DScene, 0, i);
                dae.Save (tempFilePath);
                PokemonAnimationImporter.IsEnabled = true;
                AssetDatabase.ImportAsset (tempFilePath);
                AssetDatabase.Refresh(); //<- continue from AssetImporter.cs
                OnAnimationImportedEvent -= OnAnimationPostprocessed;   
            }
            PokemonAnimationImporter.IsEnabled = false;

            if (File.Exists (tempFilePath)) {
                File.Delete (tempFilePath);
                File.Delete (tempFilePath + ".meta");
            }
            AssetDatabase.Refresh ();
        }

        private static void AddMaterialsToGeneratedMeshes (IReadOnlyDictionary<string, SkinnedMeshRenderer> meshDict,
            IReadOnlyDictionary<string, Material> matDict, H3D h3dScene)
        {
            var h3DModel = h3dScene.Models[0];
            foreach (var h3DMesh in h3DModel.Meshes)
            foreach (var h3DSubMesh in h3DMesh.SubMeshes) {
                var h3dSubMeshName = h3DModel.MeshNodesTree.Find (h3DMesh.NodeIndex) + "_" +
                                     h3DModel.Meshes.IndexOf (h3DMesh) + "_" + h3DMesh.SubMeshes.IndexOf (h3DSubMesh);
                var h3dMaterial = h3DModel.Materials[h3DMesh.MaterialIndex];
                meshDict[h3dSubMeshName].sharedMaterial = matDict[h3dMaterial.Name];
            }

            AssetDatabase.Refresh ();
        }

        private static void GenerateTextureFiles (H3D h3DScene, string exportPath)
        {
            //Get raw texture data from Scene
            var textureDict = new Dictionary<string, Texture2D> ();
            foreach (var h3DTexture in h3DScene.Textures) {
                try {
                    var width = h3DTexture.Width;
                    var height = h3DTexture.Height;

                    var colorArray = new List<Color32> ();
                    var buffer = h3DTexture.ToRGBA ();
                    for (var i = 0; i < buffer.Length; i += 4) {
                        var col = new Color32 (buffer[i + 0], buffer[i + 1], buffer[i + 2],
                            buffer[i + 3]);
                        colorArray.Add (col);
                    }

                    var texture = new Texture2D (width, height, TextureFormat.ARGB32, false) {name = h3DTexture.Name};
                    var colorCounter = 0;
                    for (var y = 0; y < texture.height; y++)
                    for (var x = 0; x < texture.width; x++)
                        texture.SetPixel (x, y, colorArray[colorCounter++]);

                    texture.Apply ();
                    textureDict.Add (texture.name, texture);
                }
                catch (Exception e) {
                    Debug.LogError ("Error importing texture: " + h3DTexture.Name + "\n" + e.Message + "\n" +
                                    e.StackTrace);
                }
            }

            var currentTextureExportFolder = exportPath + "/Textures/";
            if (!Directory.Exists (currentTextureExportFolder)) {
                Directory.CreateDirectory (currentTextureExportFolder);
            }
            foreach (var kvp in textureDict)
                File.WriteAllBytes (currentTextureExportFolder + kvp.Key + ".png", kvp.Value.EncodeToPNG ());

            AssetDatabase.Refresh ();
        }

        public enum MaterialType
        {
            Body,
            Iris,
            Core,
            Stencil
        }

        private static Dictionary<string, Material> GenerateShinyMaterialFiles (H3D h3DScene, string exportPath,
            P3ds2UShaderProperties shaderImportSettings)
        {
            var currentMaterialExportFolder = exportPath + "/Materials/Shiny/";
            if (!Directory.Exists (currentMaterialExportFolder)) {
                Directory.CreateDirectory (currentMaterialExportFolder);
            }
            
            var matDict = new Dictionary<string, Material> ();
            foreach (var h3dMaterial in h3DScene.Models[0].Materials) {
                var filePath = currentMaterialExportFolder + h3dMaterial.Name + ".mat";
                Shader shaderToApply;
                MaterialType materialType;
                if ((int) h3dMaterial.MaterialParams.MetaData[0].Values[0] == 2) {
                    shaderToApply = shaderImportSettings.irisShader;
                    materialType = MaterialType.Iris;   
                } else {
                    shaderToApply = shaderImportSettings.bodyShader;
                    materialType = MaterialType.Body;
                }
                if (h3dMaterial.MaterialParams.StencilTest.Enabled) {
                    if (h3dMaterial.MaterialParams.StencilTest.Function == (PICATestFunc) 1) {
                        shaderToApply = shaderImportSettings.fireCoreShader;
                        materialType = MaterialType.Core;
                    }
                    if (h3dMaterial.MaterialParams.StencilTest.Function == (PICATestFunc) 2) {
                        shaderToApply = shaderImportSettings.fireStencilShader;
                        materialType = MaterialType.Stencil;
                    }
                }
                
                Material newMaterial;
                if (File.Exists (filePath)) {
                    newMaterial = AssetDatabase.LoadAssetAtPath<Material> (filePath);
                    newMaterial.shader = shaderToApply;
                } else {
                    newMaterial = new Material (shaderToApply);
                }

                newMaterial.shaderKeywords = new[]
                    {"_MAIN_LIGHT_CALCULATE_SHADOWS", "_MAIN_LIGHT_SHADOW_CASCADE", " _SHADOWS_SOFT"};

                var mainTexturePath = exportPath + "/Textures/" + h3dMaterial.Texture0Name + "_1.png";
                var mainTexture = (Texture2D) AssetDatabase.LoadAssetAtPath (mainTexturePath, typeof(Texture2D));
                if (mainTexture != null) {
                    var mainTextureRepresentation = new TextureUtils.H3DTextureRepresentation {
                        TextureCoord = h3dMaterial.MaterialParams.TextureCoords[0],
                        TextureMapper = h3dMaterial.TextureMappers[0]
                    };

                    var importer = (TextureImporter) AssetImporter.GetAtPath (mainTexturePath);
                    importer.wrapModeU =
                        TextureUtils.PicaToUnityTextureWrapMode (mainTextureRepresentation.TextureMapper.WrapU);
                    importer.wrapModeV =
                        TextureUtils.PicaToUnityTextureWrapMode (mainTextureRepresentation.TextureMapper.WrapV);
                    importer.maxTextureSize = 256;
                    AssetDatabase.ImportAsset (mainTexturePath, ImportAssetOptions.ForceUpdate);

                    newMaterial.SetVector (Shader.PropertyToID(shaderImportSettings.BaseMapTiling),
                        new Vector4 (mainTextureRepresentation.TextureCoord.Scale.X,
                            mainTextureRepresentation.TextureCoord.Scale.Y, 0, 0));
                    newMaterial.SetVector (Shader.PropertyToID (shaderImportSettings.BaseMapOffset),
                        new Vector4 (0, 0));
                    newMaterial.SetTexture (Shader.PropertyToID (shaderImportSettings.BaseMap),
                        mainTexture);
                    newMaterial.mainTexture = mainTexture;
                }

                var normalMapPath = exportPath +  "/Textures/" + h3dMaterial.Texture2Name + "_1.png";
                var normalTexture = (Texture2D) AssetDatabase.LoadAssetAtPath (normalMapPath, typeof(Texture2D));
                if (normalTexture != null) {
                    var normalTextureRepresentation = new TextureUtils.H3DTextureRepresentation {
                        TextureCoord = h3dMaterial.MaterialParams.TextureCoords[2],
                        TextureMapper = h3dMaterial.TextureMappers[2]
                    };

                    var importer = (TextureImporter) AssetImporter.GetAtPath (normalMapPath);
                    // importer.textureType = TextureImporterType.NormalMap;
                    importer.wrapModeU =
                        TextureUtils.PicaToUnityTextureWrapMode (normalTextureRepresentation.TextureMapper.WrapU);
                    importer.wrapModeV =
                        TextureUtils.PicaToUnityTextureWrapMode (normalTextureRepresentation.TextureMapper.WrapV);
                    importer.maxTextureSize = 256;
                    AssetDatabase.ImportAsset (normalMapPath, ImportAssetOptions.ForceUpdate);

                    newMaterial.SetVector (Shader.PropertyToID(shaderImportSettings.NormalMapTiling),
                        new Vector4 (normalTextureRepresentation.TextureCoord.Scale.X,
                            normalTextureRepresentation.TextureCoord.Scale.Y, 0, 0));
                    newMaterial.SetVector (Shader.PropertyToID (shaderImportSettings.NormalMapOffset),
                        new Vector4 (normalTextureRepresentation.TextureCoord.Translation.X,
                            normalTextureRepresentation.TextureCoord.Translation.Y));
                    newMaterial.SetTexture (Shader.PropertyToID (shaderImportSettings.NormalMap),
                        normalTexture);
                }

                var occlusionMapPath = exportPath +  "/Textures/"  + h3dMaterial.Texture1Name + "_1.png";
                var occlusionTexture = (Texture2D) AssetDatabase.LoadAssetAtPath (occlusionMapPath, typeof(Texture2D));
                if (occlusionTexture != null) {
                    var occlusionMapRepresentation = new TextureUtils.H3DTextureRepresentation {
                        TextureCoord = h3dMaterial.MaterialParams.TextureCoords[1],
                        TextureMapper = h3dMaterial.TextureMappers[1]
                    };

                    var importer = (TextureImporter) AssetImporter.GetAtPath (occlusionMapPath);
                    importer.wrapModeU =
                        TextureUtils.PicaToUnityTextureWrapMode (occlusionMapRepresentation.TextureMapper.WrapU);
                    importer.wrapModeV =
                        TextureUtils.PicaToUnityTextureWrapMode (occlusionMapRepresentation.TextureMapper.WrapV);
                    importer.maxTextureSize = 256;
                    AssetDatabase.ImportAsset (occlusionMapPath, ImportAssetOptions.ForceUpdate);

                    newMaterial.SetVector (Shader.PropertyToID(shaderImportSettings.OcclusionMapTiling),
                        new Vector4 (occlusionMapRepresentation.TextureCoord.Scale.X,
                            occlusionMapRepresentation.TextureCoord.Scale.Y, 0, 0));
                    newMaterial.SetVector (Shader.PropertyToID (shaderImportSettings.OcclusionMapOffset),
                        new Vector4 (occlusionMapRepresentation.TextureCoord.Translation.X,
                            occlusionMapRepresentation.TextureCoord.Translation.Y));
                    newMaterial.SetTexture (Shader.PropertyToID (shaderImportSettings.OcclusionMap),
                        occlusionTexture);
                }

                if (materialType == MaterialType.Core) {
                    newMaterial.SetColor (Shader.PropertyToID (shaderImportSettings.Constant4Color),
                        new Color32 (
                            h3dMaterial.MaterialParams.Constant4Color.R,
                            h3dMaterial.MaterialParams.Constant4Color.G,
                            h3dMaterial.MaterialParams.Constant4Color.B,
                            h3dMaterial.MaterialParams.Constant4Color.A
                        ));
                    // newMaterial.SetVector ("_TestV4", testVector4);
                } else if (materialType == MaterialType.Stencil) {
                    newMaterial.SetColor (Shader.PropertyToID (shaderImportSettings.Constant4Color),
                        new Color32 (
                            h3dMaterial.MaterialParams.Constant4Color.R,
                            h3dMaterial.MaterialParams.Constant4Color.G,
                            h3dMaterial.MaterialParams.Constant4Color.B,
                            h3dMaterial.MaterialParams.Constant4Color.A
                        ));
                    newMaterial.SetColor (Shader.PropertyToID (shaderImportSettings.Constant3Color),
                        new Color32 (
                            h3dMaterial.MaterialParams.Constant3Color.R,
                            h3dMaterial.MaterialParams.Constant3Color.G,
                            h3dMaterial.MaterialParams.Constant3Color.B,
                            h3dMaterial.MaterialParams.Constant3Color.A
                        ));
                }

                if (!File.Exists (filePath)) {
                    AssetDatabase.CreateAsset (newMaterial, currentMaterialExportFolder + h3dMaterial.Name + ".mat");
                }

                matDict.Add (h3dMaterial.Name, newMaterial);
            }

            AssetDatabase.SaveAssets ();
            return matDict;
        }

        private static Dictionary<string, Material> GenerateMaterialFiles (H3D h3DScene, string exportPath,
            P3ds2UShaderProperties shaderImportSettings)
        {
            var currentMaterialExportFolder = exportPath + "/Materials/";
            if (!Directory.Exists (currentMaterialExportFolder)) {
                Directory.CreateDirectory (currentMaterialExportFolder);
            }
            
            var matDict = new Dictionary<string, Material> ();
            foreach (var h3dMaterial in h3DScene.Models[0].Materials) {
                var filePath = currentMaterialExportFolder + h3dMaterial.Name + ".mat";
                Shader shaderToApply;
                MaterialType materialType;
                if ((int) h3dMaterial.MaterialParams.MetaData[0].Values[0] == 2) {
                    shaderToApply = shaderImportSettings.irisShader;
                    materialType = MaterialType.Iris;   
                } else {
                    shaderToApply = shaderImportSettings.bodyShader;
                    materialType = MaterialType.Body;
                }
                if (h3dMaterial.MaterialParams.StencilTest.Enabled) {
                    if (h3dMaterial.MaterialParams.StencilTest.Function == (PICATestFunc) 1) {
                        shaderToApply = shaderImportSettings.fireCoreShader;
                        materialType = MaterialType.Core;
                    }
                    if (h3dMaterial.MaterialParams.StencilTest.Function == (PICATestFunc) 2) {
                        shaderToApply = shaderImportSettings.fireStencilShader;
                        materialType = MaterialType.Stencil;
                    }
                }
                
                Material newMaterial;
                if (File.Exists (filePath)) {
                    newMaterial = AssetDatabase.LoadAssetAtPath<Material> (filePath);
                    newMaterial.shader = shaderToApply;
                } else {
                    newMaterial = new Material (shaderToApply);
                }

                newMaterial.shaderKeywords = new[]
                    {"_MAIN_LIGHT_CALCULATE_SHADOWS", "_MAIN_LIGHT_SHADOW_CASCADE", " _SHADOWS_SOFT"};

                var mainTexturePath = exportPath + "/Textures/" + h3dMaterial.Texture0Name + ".png";
                var mainTexture = (Texture2D) AssetDatabase.LoadAssetAtPath (mainTexturePath, typeof(Texture2D));
                if (mainTexture != null) {
                    var mainTextureRepresentation = new TextureUtils.H3DTextureRepresentation {
                        TextureCoord = h3dMaterial.MaterialParams.TextureCoords[0],
                        TextureMapper = h3dMaterial.TextureMappers[0]
                    };

                    var importer = (TextureImporter) AssetImporter.GetAtPath (mainTexturePath);
                    importer.wrapModeU =
                        TextureUtils.PicaToUnityTextureWrapMode (mainTextureRepresentation.TextureMapper.WrapU);
                    importer.wrapModeV =
                        TextureUtils.PicaToUnityTextureWrapMode (mainTextureRepresentation.TextureMapper.WrapV);
                    importer.maxTextureSize = 256;
                    AssetDatabase.ImportAsset (mainTexturePath, ImportAssetOptions.ForceUpdate);

                    newMaterial.SetVector (Shader.PropertyToID(shaderImportSettings.BaseMapTiling),
                        new Vector4 (mainTextureRepresentation.TextureCoord.Scale.X,
                            mainTextureRepresentation.TextureCoord.Scale.Y, 0, 0));
                    newMaterial.SetVector (Shader.PropertyToID (shaderImportSettings.BaseMapOffset),
                        new Vector4 (0, 0));
                    newMaterial.SetTexture (Shader.PropertyToID (shaderImportSettings.BaseMap),
                        mainTexture);
                    newMaterial.mainTexture = mainTexture;
                }

                var normalMapPath = exportPath +  "/Textures/" + h3dMaterial.Texture2Name + ".png";
                var normalTexture = (Texture2D) AssetDatabase.LoadAssetAtPath (normalMapPath, typeof(Texture2D));
                if (normalTexture != null) {
                    var normalTextureRepresentation = new TextureUtils.H3DTextureRepresentation {
                        TextureCoord = h3dMaterial.MaterialParams.TextureCoords[2],
                        TextureMapper = h3dMaterial.TextureMappers[2]
                    };

                    var importer = (TextureImporter) AssetImporter.GetAtPath (normalMapPath);
                    // importer.textureType = TextureImporterType.NormalMap;
                    importer.wrapModeU =
                        TextureUtils.PicaToUnityTextureWrapMode (normalTextureRepresentation.TextureMapper.WrapU);
                    importer.wrapModeV =
                        TextureUtils.PicaToUnityTextureWrapMode (normalTextureRepresentation.TextureMapper.WrapV);
                    importer.maxTextureSize = 256;
                    AssetDatabase.ImportAsset (normalMapPath, ImportAssetOptions.ForceUpdate);

                    newMaterial.SetVector (Shader.PropertyToID(shaderImportSettings.NormalMapTiling),
                        new Vector4 (normalTextureRepresentation.TextureCoord.Scale.X,
                            normalTextureRepresentation.TextureCoord.Scale.Y, 0, 0));
                    newMaterial.SetVector (Shader.PropertyToID (shaderImportSettings.NormalMapOffset),
                        new Vector4 (normalTextureRepresentation.TextureCoord.Translation.X,
                            normalTextureRepresentation.TextureCoord.Translation.Y));
                    newMaterial.SetTexture (Shader.PropertyToID (shaderImportSettings.NormalMap),
                        normalTexture);
                }

                var occlusionMapPath = exportPath +  "/Textures/"  + h3dMaterial.Texture1Name + ".png";
                var occlusionTexture = (Texture2D) AssetDatabase.LoadAssetAtPath (occlusionMapPath, typeof(Texture2D));
                if (occlusionTexture != null) {
                    var occlusionMapRepresentation = new TextureUtils.H3DTextureRepresentation {
                        TextureCoord = h3dMaterial.MaterialParams.TextureCoords[1],
                        TextureMapper = h3dMaterial.TextureMappers[1]
                    };

                    var importer = (TextureImporter) AssetImporter.GetAtPath (occlusionMapPath);
                    importer.wrapModeU =
                        TextureUtils.PicaToUnityTextureWrapMode (occlusionMapRepresentation.TextureMapper.WrapU);
                    importer.wrapModeV =
                        TextureUtils.PicaToUnityTextureWrapMode (occlusionMapRepresentation.TextureMapper.WrapV);
                    importer.maxTextureSize = 256;
                    AssetDatabase.ImportAsset (occlusionMapPath, ImportAssetOptions.ForceUpdate);

                    newMaterial.SetVector (Shader.PropertyToID(shaderImportSettings.OcclusionMapTiling),
                        new Vector4 (occlusionMapRepresentation.TextureCoord.Scale.X,
                            occlusionMapRepresentation.TextureCoord.Scale.Y, 0, 0));
                    newMaterial.SetVector (Shader.PropertyToID (shaderImportSettings.OcclusionMapOffset),
                        new Vector4 (occlusionMapRepresentation.TextureCoord.Translation.X,
                            occlusionMapRepresentation.TextureCoord.Translation.Y));
                    newMaterial.SetTexture (Shader.PropertyToID (shaderImportSettings.OcclusionMap),
                        occlusionTexture);
                }

                if (materialType == MaterialType.Core) {
                    newMaterial.SetColor (Shader.PropertyToID (shaderImportSettings.Constant4Color),
                        new Color32 (
                            h3dMaterial.MaterialParams.Constant4Color.R,
                            h3dMaterial.MaterialParams.Constant4Color.G,
                            h3dMaterial.MaterialParams.Constant4Color.B,
                            h3dMaterial.MaterialParams.Constant4Color.A
                        ));
                    // newMaterial.SetVector ("_TestV4", testVector4);
                } else if (materialType == MaterialType.Stencil) {
                    newMaterial.SetColor (Shader.PropertyToID (shaderImportSettings.Constant4Color),
                        new Color32 (
                            h3dMaterial.MaterialParams.Constant4Color.R,
                            h3dMaterial.MaterialParams.Constant4Color.G,
                            h3dMaterial.MaterialParams.Constant4Color.B,
                            h3dMaterial.MaterialParams.Constant4Color.A
                        ));
                    newMaterial.SetColor (Shader.PropertyToID (shaderImportSettings.Constant3Color),
                        new Color32 (
                            h3dMaterial.MaterialParams.Constant3Color.R,
                            h3dMaterial.MaterialParams.Constant3Color.G,
                            h3dMaterial.MaterialParams.Constant3Color.B,
                            h3dMaterial.MaterialParams.Constant3Color.A
                        ));
                }

                if (!File.Exists (filePath)) {
                    AssetDatabase.CreateAsset (newMaterial, currentMaterialExportFolder + h3dMaterial.Name + ".mat");
                }

                matDict.Add (h3dMaterial.Name, newMaterial);
            }

            AssetDatabase.SaveAssets ();
            return matDict;
        }


        private static Dictionary<string, SkinnedMeshRenderer> GenerateMeshInUnityScene (H3D h3DScene, string exportPath, P3ds2USettingsScriptableObject importSettings)
        {
            var meshDict = new Dictionary<string, SkinnedMeshRenderer> ();
            var toBeDestroyed = GameObject.Find ("GeneratedUnityObject");
            if (toBeDestroyed != null) DestroyImmediate (toBeDestroyed);

            var h3DModel = h3DScene.Models[0];

            var emptyGo = new GameObject ("EmptyGo");
            var sceneGo = new GameObject ("GeneratedUnityObject");
            
            var skeletonRoot = SkeletonUtils.GenerateSkeletonForModel (h3DModel);
            if (skeletonRoot == null) {
                //Skeleton not present in model
            } else {
                SpawnBones (skeletonRoot, sceneGo, emptyGo);
            }
            
            var currentMeshExportFolder = exportPath + "/Meshes/";
            if (!Directory.Exists (currentMeshExportFolder)) {
                Directory.CreateDirectory (currentMeshExportFolder);
            }

            foreach (var h3DMesh in h3DModel.Meshes) {
                if (h3DMesh.Type == H3DMeshType.Silhouette) continue;

                var picaVertices = MeshTransform.GetWorldSpaceVertices (h3DModel.Skeleton, h3DMesh);
                foreach (var subMesh in h3DMesh.SubMeshes) {
                    var subMeshName = h3DModel.MeshNodesTree.Find (h3DMesh.NodeIndex) + "_" +
                                      h3DModel.Meshes.IndexOf (h3DMesh) + "_" + h3DMesh.SubMeshes.IndexOf (subMesh);
                    var modelGo = Instantiate (emptyGo, sceneGo.transform);
                    modelGo.name = subMeshName;
                    if (importSettings.ImporterSettings.ImportFireMaterials) {
                        if (modelGo.name.Contains ("FireCore")) {
                            try {
                                modelGo.layer = LayerMask.NameToLayer ("FireCore");
                            }
                            catch (Exception) {
                                // 
                            }
                        } else if (modelGo.name.Contains ("FireSten")) {
                            try {
                                modelGo.layer = LayerMask.NameToLayer ("FireMask");
                            }
                            catch (Exception) {
                                //
                            }
                        }
                    }

                    var meshFilter = modelGo.AddComponent<MeshFilter> ();
                    var mesh = new Mesh ();

                    var unityMeshPositions = new List<Vector3> ();
                    var unityMeshTangents = new List<Vector4> ();
                    var unityMeshNormals = new List<Vector3> ();
                    var unityMeshUV = new List<Vector2> ();
                    var unityMeshTriangles = new List<ushort> ();

                    unityMeshPositions.AddRange (MeshUtils.PicaToUnityVertex (picaVertices));
                    unityMeshNormals.AddRange (MeshUtils.PicaToUnityNormals (picaVertices));
                    unityMeshTangents.AddRange (MeshUtils.PicaToUnityTangents (picaVertices));
                    unityMeshUV.AddRange (MeshUtils.PicaToUnityUV (picaVertices));
                    unityMeshTriangles.AddRange (subMesh.Indices);

                    var unityVertexBones = new List<BoneWeight> ();

                    if (subMesh.Skinning == H3DSubMeshSkinning.Smooth)
                        foreach (var picaVertex in picaVertices) {
                            var vertexBoneWeight = new BoneWeight ();
                            for (var boneIndexInVertex = 0; boneIndexInVertex < 4; boneIndexInVertex++) {
                                var bIndex = picaVertex.Indices[boneIndexInVertex];
                                var weight = picaVertex.Weights[boneIndexInVertex];

                                if (weight == 0) break;

                                if (bIndex < subMesh.BoneIndices.Length && bIndex > -1)
                                    bIndex = subMesh.BoneIndices[bIndex];
                                else
                                    bIndex = 0;

                                switch (boneIndexInVertex) {
                                    case 0:
                                        vertexBoneWeight.weight0 = weight;
                                        vertexBoneWeight.boneIndex0 = bIndex;
                                        break;
                                    case 1:
                                        vertexBoneWeight.weight1 = weight;
                                        vertexBoneWeight.boneIndex1 = bIndex;
                                        break;
                                    case 2:
                                        vertexBoneWeight.weight2 = weight;
                                        vertexBoneWeight.boneIndex2 = bIndex;
                                        break;
                                    case 3:
                                        vertexBoneWeight.weight3 = weight;
                                        vertexBoneWeight.boneIndex3 = bIndex;
                                        break;
                                }
                            }

                            unityVertexBones.Add (vertexBoneWeight);
                        }
                    else
                        foreach (var picaVertex in picaVertices) {
                            var bIndex = picaVertex.Indices[0];

                            if (bIndex < subMesh.BoneIndices.Length && bIndex > -1)
                                bIndex = subMesh.BoneIndices[bIndex];
                            else
                                bIndex = 0;

                            var vertexBoneWeight = new BoneWeight {
                                boneIndex0 = bIndex,
                                weight0 = 1
                            };
                            unityVertexBones.Add (vertexBoneWeight);
                        }


                    mesh.subMeshCount = 1;
                    mesh.vertices = unityMeshPositions.ToArray ();
                    mesh.normals = unityMeshNormals.ToArray ();
                    mesh.tangents = unityMeshTangents.ToArray ();
                    mesh.uv = unityMeshUV.ToArray ();
                    unityMeshTriangles.Reverse ();
                    mesh.SetTriangles (unityMeshTriangles, 0);

                    mesh.boneWeights = unityVertexBones.ToArray ();

                    var meshRenderer = modelGo.AddComponent<SkinnedMeshRenderer> ();
                    meshRenderer.quality = SkinQuality.Bone4;
                    meshRenderer.sharedMesh = mesh;
                    var bonesTransform = sceneGo.transform.GetChild (0).GetComponentsInChildren<Transform> ();
                    meshRenderer.rootBone = bonesTransform[0];
                    meshRenderer.bones = bonesTransform;
                    meshRenderer.updateWhenOffscreen = true;
                    mesh.bindposes = bonesTransform
                        .Select (t => t.worldToLocalMatrix * bonesTransform[0].localToWorldMatrix).ToArray ();

                    meshFilter.sharedMesh = mesh;
                    SaveMeshAtPath (mesh, currentMeshExportFolder + subMeshName + ".asset");
                    meshDict.Add (subMeshName, meshRenderer);
                }
            }

            DestroyImmediate (emptyGo);
            return meshDict;
        }


        private static void SpawnBones (SkeletonUtils.SkeletonNode root, GameObject parentGo, GameObject nodeGo)
        {
            var rootGo = Instantiate (nodeGo, parentGo.transform);
            rootGo.transform.localScale = root.Scale;

            var positionAxes = new Vector3 (-1, 1, 1);
            var positionVector = root.Translation;
            rootGo.transform.localPosition = new Vector3 {
                x = positionAxes.x * positionVector.x,
                y = positionAxes.y * positionVector.y,
                z = positionAxes.z * positionVector.z
            };
            foreach (var singleRotation in root.Rotation) {
                var rotationVector = VectorExtensions.GetAxisFromRotation (singleRotation);
                rootGo.transform.Rotate (rotationVector, VectorExtensions.GetScalarFromRotation (singleRotation));
            }

            rootGo.name = root.Name;
            if (root.Nodes == null)
                return;
            foreach (var singleNode in root.Nodes) SpawnBones (singleNode, rootGo, nodeGo);
        }

        private static void SaveMeshAtPath (Mesh mesh, string path)
        {
            if (mesh == null) throw new ArgumentNullException (nameof(mesh));
            if (File.Exists (path)) File.Delete (path);
            AssetDatabase.CreateAsset (mesh, path);
            AssetDatabase.SaveAssets ();
        }
        
        [MenuItem ("3DStoUnity/Build Asset Bundles")]
        private static void BuildPokemonAssetBundles()
        {
            EditorUtility.DisplayDialog ("Placeholder Action",
                "Feature in the pipeline, will consider Addressables in the future too. Going either route way will reduce the final build size significantly and allow for DLC content in your project",
                "ok");
            return;
        }
    }
}
