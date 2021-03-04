using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CurveExtended;
using P3DS2U.Editor.SPICA;
using P3DS2U.Editor.SPICA.COLLADA;
using P3DS2U.Editor.SPICA.H3D;
using P3DS2U.Editor.SPICA.H3D.Animation;
using P3DS2U.Editor.SPICA.H3D.Model;
using P3DS2U.Editor.SPICA.H3D.Model.Mesh;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

/*
 *TODO: Import folder structure options via popup at start
 *TODO: Shaders in ase format (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null && UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset.GetType().ToString().Contains("UniversalRenderPipelineAsset"))
 *TODO: Duplicate animation content instead of re-saving, to remove read only lock on animations
 *
 *
 * @opeious will you solve problem with repeating skeleton animations? I used this solution in my spica fork (GFPkmnModel):
                        List<uint> sklAdresses = new List<uint>();
                        if (SklAnim != null)
                        {
                            if (!sklAdresses.Contains(Header.Entries[Index].Address))
                            {
                                Output.SkeletalAnimations.Add(SklAnim);
                                sklAdresses.Add(Header.Entries[Index].Address);
                            }
                       }
 */

namespace P3DS2U.Editor
{
    public class PokemonImporter : EditorWindow
    {
        private static readonly int BaseMap = Shader.PropertyToID ("_BaseMap");
        private static readonly int NormalMap = Shader.PropertyToID ("_NormalMap");
        private static readonly int OcclusionMap = Shader.PropertyToID ("_OcclusionMap");
        private static readonly int BaseMapTiling = Shader.PropertyToID ("_BaseMapTiling");
        private static readonly int NormalMapTiling = Shader.PropertyToID ("_NormalMapTiling");
        private static readonly int OcclusionMapTiling = Shader.PropertyToID ("_OcclusionMapTiling");

        private const string ImportPath = "Assets/Bin3DS/";
        private const string ExportPath = "Assets/Exported/";

        private static int _processedCount;

        private delegate void OnAnimationImported (AnimationClip clip);
        private static OnAnimationImported OnAnimationImportedEvent;
        public static void RaiseOnAnimationImportedEvent (AnimationClip clip) => OnAnimationImportedEvent?.Invoke (clip);
        private static H3D h3DScene = null;
        private static int CurrentAnimationIndex = 0;
        private static string CurrentAnimationExportFolder = "";
        
        [MenuItem ("3DStoUnity/Import Pokemon (Bin)")]
        private static void StartImportingBinaries ()
        {
            try {
                if (!Directory.Exists (ImportPath)) {
                    Directory.CreateDirectory (ImportPath);
                    EditorUtility.DisplayDialog ("Created Folder " + ImportPath,
                        "Created Folder" + ImportPath +
                        " \nPlease place .bin files to be imported in that directory or subdirectories, Files with the same name will be merged together",
                        "ok");
                    return;
                }

                var allFiles = DirectoryUtils.GetAllFilesRecursive ("Assets/Bin3DS/");
                var scenesDict = new Dictionary<string, List<string>> ();
                foreach (var singleFile in allFiles) {
                    var trimmedName = Path.GetFileName (singleFile);
                    if (!scenesDict.ContainsKey (trimmedName)) {
                        scenesDict.Add (trimmedName, new List<string> {singleFile});
                    } else {
                        scenesDict[trimmedName].Add (singleFile);
                    }
                }

                _processedCount = 0;
                foreach (var kvp in scenesDict) {
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

                    //Merge animation binaries
                    foreach (var singleFileToBeMerged in kvp.Value) {
                        var fileType = BinaryUtils.GetBinaryFileType (singleFileToBeMerged);
                        if (fileType != BinaryUtils.FileType.Animation) continue;
                        H3DDict<H3DBone> skeleton = null;
                        if (h3DScene.Models.Count > 0) skeleton = h3DScene.Models[0].Skeleton;
                        var data = FormatIdentifier.IdentifyAndOpen (singleFileToBeMerged, skeleton);
                        if (data != null) h3DScene.Merge (data);
                    }

                    var combinedExportFolder = ExportPath + kvp.Key.Replace (".bin", "/Files/");
                    if (!Directory.Exists (combinedExportFolder)) {
                        Directory.CreateDirectory (combinedExportFolder);
                    } else {
                        Directory.Delete (ExportPath + kvp.Key.Replace (".bin", "/"), true);
                        Directory.CreateDirectory (combinedExportFolder);
                    }

                    GenerateTextureFiles (h3DScene, combinedExportFolder);
                    var meshDict = GenerateMeshInUnityScene (h3DScene, combinedExportFolder);
                    var matDict = GenerateMaterialFiles (h3DScene, combinedExportFolder);
                    AddMaterialsToGeneratedMeshes (meshDict, matDict, h3DScene);
                    GenerateSkeletalAnimations (h3DScene, combinedExportFolder);
                    GenerateMaterialAnimations (h3DScene, combinedExportFolder, matDict);

                    var go = GameObject.Find ("GeneratedUnityObject");
                    go.name = kvp.Key.Replace (".bin", "");
                    var prefabPath =
                        AssetDatabase.GenerateUniqueAssetPath (ExportPath + go.name + "/" + go.name + ".prefab");
                    PrefabUtility.SaveAsPrefabAssetAndConnect (go, prefabPath, InteractionMode.UserAction);

                    go.transform.localPosition = new Vector3 {
                        x = Random.Range (-500f, 500f),
                        y = 0,
                        z = Random.Range (-500f, 500f)
                    };
                }
            }
            catch (Exception e) {
                Debug.LogError ("Something went horribly wrong! Hmu, I'll try to fix it.\n" + e.Message + "\n" +
                                e.StackTrace);
            }

            EditorUtility.ClearProgressBar();
        }
        
        private static void GenerateMaterialAnimations (H3D h3DScene, string combinedExportFolder,
            Dictionary<string, Material> materialMappingDictionary)
        {
            
            //TODO: rewrite this whole thing
            
            Transform testTransform = GameObject.Find ("0999 - Sylveon").transform;

            for (int i = 0; i < h3DScene.MaterialAnimations.Count; i++) {
                var currentAnim = h3DScene.MaterialAnimations[i];
                var animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip> (combinedExportFolder + "/Animations/" + "anim" + i + ".anim");
                if (animationClip == null) {
                    animationClip = new AnimationClip ();
                    var clipSettings = AnimationUtility.GetAnimationClipSettings (animationClip);
                    clipSettings.loopTime = currentAnim.AnimationFlags == H3DAnimationFlags.IsLooping;
                    AnimationUtility.SetAnimationClipSettings (animationClip, clipSettings);
                }

                var newCurvesDict = new Dictionary<string, AnimationCurve> ();
                
                foreach (var element in currentAnim.Elements) {
                    var didntHandle = false;
                    //TODO: get all the keyframes first then make curves, to support custom shaders in the future
                    switch (element.PrimitiveType) {
                        case H3DPrimitiveType.Boolean:
                            didntHandle = true;
                            break;
                        case H3DPrimitiveType.Float:
                            didntHandle = true;
                            break;
                        case H3DPrimitiveType.Integer:
                            didntHandle = true;
                            break;
                        case H3DPrimitiveType.Vector2D:

                            switch (element.TargetType) {
                                case H3DTargetType.MaterialTexCoord0Scale:
                                    var curveBs = AnimationUtils.GetOrAddCurve (newCurvesDict, "bs");
                                    // SetVector2(Vector, ref TC[0].Scale);       
                                    break;
                                case H3DTargetType.MaterialTexCoord1Scale:
                                    // SetVector2(Vector, ref TC[1].Scale);       
                                    break;
                                case H3DTargetType.MaterialTexCoord2Scale:
                                    // SetVector2(Vector, ref TC[2].Scale);       
                                    break;
                                case H3DTargetType.MaterialTexCoord0Trans:
                                    var curveBt = AnimationUtils.GetOrAddCurve (newCurvesDict, "bty");
                                    if (element.Content is H3DAnimVector2D h3DAnimVector2DBt) {
                                        foreach (var singleYFrame in h3DAnimVector2DBt.Y.KeyFrames) {
                                            var lhs = singleYFrame.InSlope;
                                            var rhs = singleYFrame.OutSlope;
                                            TangentMode tangentMode;
                                            if (lhs == 0 && rhs == 0) {
                                                tangentMode = TangentMode.Stepped;
                                            } else {
                                                tangentMode = TangentMode.Linear;
                                            }

                                            curveBt.AddKey (KeyframeUtil.GetNew (
                                                AnimationUtils.GetTimeAtFrame (animationClip,
                                                    (int) singleYFrame.Frame, currentAnim),
                                                singleYFrame.Value,
                                                tangentMode));
                                            if (tangentMode == TangentMode.Linear) {
                                                curveBt.UpdateAllLinearTangents ();
                                            }
                                        }
                                    }
                                    var skms = testTransform.GetComponentsInChildren<SkinnedMeshRenderer> ();
                                    foreach (var skm in skms) {
                                        if (skm.material.name.Replace (" (Instance)", "") == element.Name) {
                                            var cbp = AnimationUtility.CalculateTransformPath (skm.transform,
                                                testTransform);
                                            animationClip.SetCurve (cbp, typeof(SkinnedMeshRenderer),
                                                "material._BaseMapOffset.y", newCurvesDict["bty"]);
                                        }
                                    }
                                    break;
                                case H3DTargetType.MaterialTexCoord1Trans:
                                    // SetVector2(Vector, ref TC[1].Translation);
                                    break;
                                case H3DTargetType.MaterialTexCoord2Trans:
                                    // var curveNt = AnimationUtils.GetOrAddCurve (newCurvesDict, "ntx");
                                    // if (element.Content is H3DAnimVector2D h3DAnimVector2DNt) {
                                    //     foreach (var singleXFrame in h3DAnimVector2DNt.X.KeyFrames) {
                                    //         var lhs = singleXFrame.InSlope;
                                    //         var rhs = singleXFrame.OutSlope;
                                    //         TangentMode tangentMode;
                                    //         if (lhs == 0 && rhs == 0) {
                                    //             tangentMode = TangentMode.Stepped;
                                    //         } else {
                                    //             tangentMode = TangentMode.Linear;
                                    //         }
                                    //
                                    //         curveNt.AddKey (KeyframeUtil.GetNew (
                                    //             AnimationUtils.GetTimeAtFrame (animationClip,
                                    //                 (int) singleXFrame.Frame, currentAnim),
                                    //             singleXFrame.Value,
                                    //             tangentMode));
                                    //         if (tangentMode == TangentMode.Linear) {
                                    //             curveNt.UpdateAllLinearTangents ();
                                    //         }
                                    //     }
                                    // }

                                    break;
                            }

                            // if (newCurvesDict.ContainsKey ("ntx")) {
                            //     var skms = testTransform.GetComponentsInChildren<SkinnedMeshRenderer> ();
                            //     foreach (var skm in skms) {
                            //         if (skm.material.name.Replace (" (Instance)", "") == element.Name) {
                            //             var cbp = AnimationUtility.CalculateTransformPath (skm.transform,
                            //                 testTransform);
                            //             animationClip.SetCurve (cbp, typeof(SkinnedMeshRenderer),
                            //                 "material._NormalMapOffset.x", newCurvesDict["ntx"]);
                            //         }
                            //     }
                            // }
                            
                            if (newCurvesDict.ContainsKey ("bty")) {

                            }
                            // var x = element.TargetType;
                            // Debug.LogError (x);
                            // var newCurve = new AnimationCurve();
                            // var y = element.Content.GetType ();
                            //
                            
                            break;
                        case H3DPrimitiveType.Vector3D:
                            didntHandle = true;
                            break;
                        case H3DPrimitiveType.Transform:
                            didntHandle = true;
                            break;
                        case H3DPrimitiveType.RGBA:
                            didntHandle = true;
                            break;
                        case H3DPrimitiveType.Texture:
                            didntHandle = true;
                            break;
                        case H3DPrimitiveType.QuatTransform:
                            didntHandle = true;
                            break;
                        case H3DPrimitiveType.MtxTransform:
                            didntHandle = true;
                            break;
                        default:
                            didntHandle = true;
                            break;
                    }

                    if (didntHandle) {
                        //TODO: Handle all the didnt handles
                        Debug.LogError ("Didn't handle for this type: " + element.PrimitiveType);
                    }
                }
                
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
                AssetDatabase.CreateAsset (newClip, CurrentAnimationExportFolder + "anim" + CurrentAnimationIndex++ +".anim");
                AssetDatabase.Refresh ();
            }
        }

        
        //NOTE: doing it this way because unity does keyframe compression on asset import.
        //which won't happen if I generate the animation inside of Unity, also generating animations is hard =/
        //Hit me up, if you know a better way of loading the animations from the binaries
        private static void GenerateSkeletalAnimations (H3D h3DScene, string combinedExportFolder)
        {
            var tempFilePath = Application.dataPath + "/BinInterimAnimation.dae";
            CurrentAnimationExportFolder = combinedExportFolder + "/Animations/";
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

            var currentTextureExportFolder = exportPath + "/Textures/";
            if (!Directory.Exists (currentTextureExportFolder)) {
                Directory.CreateDirectory (currentTextureExportFolder);
            }
            foreach (var kvp in textureDict)
                File.WriteAllBytes (currentTextureExportFolder + kvp.Key + ".png", kvp.Value.EncodeToPNG ());

            AssetDatabase.Refresh ();
        }

        private static Dictionary<string, Material> GenerateMaterialFiles (H3D h3DScene, string exportPath)
        {
            var textureDict = new Dictionary<string, TextureUtils.H3DTextureRepresentation> ();
            foreach (var h3DMaterial in h3DScene.Models[0].Materials) {
                foreach (var textureName in h3DMaterial.TextureNames ())
                    if (!textureDict.ContainsKey (textureName))
                        textureDict.Add (textureName, new TextureUtils.H3DTextureRepresentation ());

                if (textureDict.Count == 0) break;

                var textureNames = h3DMaterial.TextureNames ();

                foreach (var h3DTextureName in textureNames) {
                    var textureIndex = h3DMaterial.GetTextureIndex (h3DTextureName);

                    textureDict[h3DTextureName].TextureCoord = h3DMaterial.MaterialParams.TextureCoords[textureIndex];
                    textureDict[h3DTextureName].TextureMapper = h3DMaterial.TextureMappers[textureIndex];
                }
            }
            
            var currentMaterialExportFolder = exportPath + "/Materials/";
            if (!Directory.Exists (currentMaterialExportFolder)) {
                Directory.CreateDirectory (currentMaterialExportFolder);
            }
            
            var matDict = new Dictionary<string, Material> ();
            foreach (var h3dMaterial in h3DScene.Models[0].Materials) {
                var newMaterial = new Material (Shader.Find ("Shader Graphs/LitPokemonShader"));

                var mainTexturePath = exportPath + "/Textures/" + h3dMaterial.Texture0Name + ".png";
                var mainTexture = (Texture2D) AssetDatabase.LoadAssetAtPath (mainTexturePath, typeof(Texture2D));
                if (mainTexture != null) {
                    var mainTextureRepresentation = textureDict[h3dMaterial.Texture0Name];

                    var importer = (TextureImporter) AssetImporter.GetAtPath (mainTexturePath);
                    importer.wrapModeU =
                        TextureUtils.PicaToUnityTextureWrapMode (mainTextureRepresentation.TextureMapper.WrapU);
                    importer.wrapModeV =
                        TextureUtils.PicaToUnityTextureWrapMode (mainTextureRepresentation.TextureMapper.WrapV);
                    importer.maxTextureSize = 256;
                    AssetDatabase.ImportAsset (mainTexturePath, ImportAssetOptions.ForceUpdate);

                    newMaterial.SetVector (BaseMapTiling,
                        new Vector4 (mainTextureRepresentation.TextureCoord.Scale.X,
                            mainTextureRepresentation.TextureCoord.Scale.Y, 0, 0));
                    newMaterial.SetTexture (BaseMap, mainTexture);
                    newMaterial.mainTexture = mainTexture;
                }

                var normalMapPath = exportPath +  "/Textures/" + h3dMaterial.Texture2Name + ".png";
                var normalTexture = (Texture2D) AssetDatabase.LoadAssetAtPath (normalMapPath, typeof(Texture2D));
                if (normalTexture != null) {
                    var normalTextureRepresentation = textureDict[h3dMaterial.Texture2Name];

                    var importer = (TextureImporter) AssetImporter.GetAtPath (normalMapPath);
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.wrapModeU =
                        TextureUtils.PicaToUnityTextureWrapMode (normalTextureRepresentation.TextureMapper.WrapU);
                    importer.wrapModeV =
                        TextureUtils.PicaToUnityTextureWrapMode (normalTextureRepresentation.TextureMapper.WrapV);
                    importer.maxTextureSize = 256;
                    AssetDatabase.ImportAsset (normalMapPath, ImportAssetOptions.ForceUpdate);

                    newMaterial.SetVector (NormalMapTiling,
                        new Vector4 (normalTextureRepresentation.TextureCoord.Scale.X,
                            normalTextureRepresentation.TextureCoord.Scale.Y, 0, 0));
                    newMaterial.SetTexture (NormalMap, normalTexture);
                }

                var occlusionMapPath = exportPath +  "/Textures/"  + h3dMaterial.Texture1Name + ".png";
                var occlusionTexture = (Texture2D) AssetDatabase.LoadAssetAtPath (occlusionMapPath, typeof(Texture2D));
                if (occlusionTexture != null) {
                    var occlusionMapRepresentation = textureDict[h3dMaterial.Texture2Name];

                    var importer = (TextureImporter) AssetImporter.GetAtPath (occlusionMapPath);
                    importer.wrapModeU =
                        TextureUtils.PicaToUnityTextureWrapMode (occlusionMapRepresentation.TextureMapper.WrapU);
                    importer.wrapModeV =
                        TextureUtils.PicaToUnityTextureWrapMode (occlusionMapRepresentation.TextureMapper.WrapV);
                    importer.maxTextureSize = 256;
                    AssetDatabase.ImportAsset (occlusionMapPath, ImportAssetOptions.ForceUpdate);

                    newMaterial.SetVector (OcclusionMapTiling,
                        new Vector4 (occlusionMapRepresentation.TextureCoord.Scale.X,
                            occlusionMapRepresentation.TextureCoord.Scale.Y, 0, 0));
                    newMaterial.SetTexture (OcclusionMap, occlusionTexture);
                }
                
                AssetDatabase.CreateAsset (newMaterial, currentMaterialExportFolder + h3dMaterial.Name + ".mat");
                matDict.Add (h3dMaterial.Name, newMaterial);
            }

            AssetDatabase.SaveAssets ();
            return matDict;
        }


        private static Dictionary<string, SkinnedMeshRenderer> GenerateMeshInUnityScene (H3D h3DScene, string exportPath)
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
            if (!Directory.Exists ("Assets/TestPath")) {
                Directory.CreateDirectory ("Assets/TestPath");
            }
            BuildPipeline.BuildAssetBundles ("Assets/TestPath", BuildAssetBundleOptions.None, BuildTarget.Android);
        }
    }
}