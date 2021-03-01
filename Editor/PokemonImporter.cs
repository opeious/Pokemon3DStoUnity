using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using P3DS2U.Editor.SPICA;
using P3DS2U.Editor.SPICA.H3D;
using P3DS2U.Editor.SPICA.H3D.Animation;
using P3DS2U.Editor.SPICA.H3D.Model;
using P3DS2U.Editor.SPICA.H3D.Model.Mesh;
using P3DS2U.Editor.SPICA.Math3D;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

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

        [MenuItem ("3DStoUnity/Import Pokemon (Bin)")]
        private static void StartImportingBinaries ()
        {
            if (!Directory.Exists (ImportPath)) {
                Directory.CreateDirectory (ImportPath);
                EditorUtility.DisplayDialog ("Created Folder " + ImportPath,
                    "Created Folder" + ImportPath +" \nPlease place .bin files to be imported in that directory or subdirectories, Files with the same name will be merged together",
                    "ok");
                return;
            }

            var allFiles = DirectoryUtils.GetAllFilesRecursive ("Assets/Bin3DS/");
            var scenesDict = new Dictionary<string, List<string>> ();
            foreach (var singleFile in allFiles) {
                var trimmedName = Path.GetFileName (singleFile);
                if (!scenesDict.ContainsKey (trimmedName)) {
                    scenesDict.Add(trimmedName, new List<string> {singleFile});
                } else {
                    scenesDict[trimmedName].Add (singleFile);
                }
            }

            _processedCount = 0;
            foreach (var kvp in scenesDict) {
                EditorUtility.DisplayProgressBar ("Importing", kvp.Key.Replace (".bin", ""),
                    (float) _processedCount / scenesDict.Count);

                var h3DScene = new H3D ();
                
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

                var combinedExportFolder = ExportPath + kvp.Key.Replace (".bin","/Files/");
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
                // GenerateSkeletalAnimations (h3DScene, combinedExportFolder);

                var go = GameObject.Find ("GeneratedUnityObject");
                go.name = kvp.Key.Replace (".bin", "");
                var prefabPath = AssetDatabase.GenerateUniqueAssetPath (ExportPath + go.name +"/" + go.name + ".prefab");
                PrefabUtility.SaveAsPrefabAssetAndConnect (go, prefabPath, InteractionMode.UserAction);
                
                go.transform.localPosition = new Vector3 {
                    x = Random.Range (-500f, 500f),
                    y = 0,
                    z = Random.Range (-500f, 500f)
                };
            }
            EditorUtility.ClearProgressBar();
        }

        private static void GenerateSkeletalAnimations (H3D h3DScene, string combinedExportFolder)
        {
            var go = GameObject.Find ("GeneratedUnityObject");
            var skeletonTransform = go.transform.GetChild (0);
            var boneTransformDict = skeletonTransform.GetComponentsInChildren<Transform> ()
                .ToDictionary (boneTransform => boneTransform.name);
            var animation = skeletonTransform.gameObject.AddComponent<Animation> ();
            foreach (var h3dSkeletalAnimation in h3DScene.SkeletalAnimations) {
                var unityClip = new AnimationClip {name = h3dSkeletalAnimation.Name};

                var clipSettings = AnimationUtility.GetAnimationClipSettings (unityClip);
                clipSettings.loopTime = h3dSkeletalAnimation.AnimationFlags == H3DAnimationFlags.IsLooping;
                
                AnimationUtility.SetAnimationClipSettings(unityClip, clipSettings);
            
                
                
                
                var FramesCount = (int) h3dSkeletalAnimation.FramesCount + 1;

                foreach (var Elem in h3dSkeletalAnimation.Elements) {
                    // if (Elem.PrimitiveType != H3DPrimitiveType.Transform &&
                    //     Elem.PrimitiveType != H3DPrimitiveType.QuatTransform) continue;
                    //
                    // for (int i = 0; i < 5; i++) {
                    //     var AnimTimes = new string[FramesCount];
                    //     var AnimPoses = new string[FramesCount];
                    //     var AnimLerps = new string[FramesCount];
                    //
                    //     var IsRotation = i > 0 && i < 4; //1,2,3
                    //     bool Skip;
                    //     switch (Elem.Content) {
                    //         case H3DAnimTransform h3DAnimTransform:
                    //             switch (i) {
                    //                 case 0:
                    //                     Skip = !h3DAnimTransform.TranslationExists;
                    //                     break;
                    //                 case 1:
                    //                     Skip = !h3DAnimTransform.RotationX.Exists;
                    //                     break;
                    //                 case 2:
                    //                     Skip = !h3DAnimTransform.RotationY.Exists;
                    //                     break;
                    //                 case 3:
                    //                     Skip = !h3DAnimTransform.RotationZ.Exists;
                    //                     break;
                    //                 case 4:
                    //                     Skip = !h3DAnimTransform.ScaleExists;
                    //                     break;
                    //             }
                    //
                    //             break;
                    //         case H3DAnimQuatTransform h3DAnimQuatTransform:
                    //             switch (i) {
                    //                 case 0:
                    //                     Skip = !h3DAnimQuatTransform.HasTranslation;
                    //                     break;
                    //                 case 1:
                    //                     Skip = !h3DAnimQuatTransform.HasRotation;
                    //                     break;
                    //                 case 2:
                    //                     Skip = !h3DAnimQuatTransform.HasRotation;
                    //                     break;
                    //                 case 3:
                    //                     Skip = !h3DAnimQuatTransform.HasRotation;
                    //                     break;
                    //                 case 4:
                    //                     Skip = !h3DAnimQuatTransform.HasScale;
                    //                     break;
                    //             }
                    //
                    //             break;
                    //     }
                    //     if (Skip) continue;
                    //     
                    //     for (var Frame = 0; Frame < FramesCount; Frame++) {
                    //         var StrTrans = string.Empty;
                    //
                    //         var PElem = SklAnim.Elements.FirstOrDefault (x => x.Name == Parent?.Name);
                    //
                    //         var InvScale = Vector3.one;
                    //
                    //         if (Elem.Content is H3DAnimTransform h3DAnimTransform) {
                    //             //Compensate parent bone scale (basically, don't inherit scales)
                    //             if (Parent != null && (SklBone.Flags & H3DBoneFlags.IsSegmentScaleCompensate) != 0) {
                    //                 if (PElem != null) {
                    //                     var PTrans = (H3DAnimTransform) PElem.Content;
                    //
                    //                     InvScale /= new Vector3 (
                    //                         PTrans.ScaleX.Exists ? PTrans.ScaleX.GetFrameValue (Frame) : Parent.Scale.X,
                    //                         PTrans.ScaleY.Exists ? PTrans.ScaleY.GetFrameValue (Frame) : Parent.Scale.Y,
                    //                         PTrans.ScaleZ.Exists
                    //                             ? PTrans.ScaleZ.GetFrameValue (Frame)
                    //                             : Parent.Scale.Z);
                    //                 } else {
                    //                     InvScale /= Parent.Scale;
                    //                 }
                    //             }
                    //
                    //             switch (i) {
                    //                 //Translation
                    //                 case 0:
                    //                     StrTrans = DAEUtils.VectorStr (new Vector3 (
                    //                         h3DAnimTransform.TranslationX.Exists //X
                    //                             ? h3DAnimTransform.TranslationX.GetFrameValue (Frame)
                    //                             : SklBone.Translation.X,
                    //                         h3DAnimTransform.TranslationY.Exists //Y
                    //                             ? h3DAnimTransform.TranslationY.GetFrameValue (Frame)
                    //                             : SklBone.Translation.Y,
                    //                         h3DAnimTransform.TranslationZ.Exists //Z
                    //                             ? h3DAnimTransform.TranslationZ.GetFrameValue (Frame)
                    //                             : SklBone.Translation.Z));
                    //                     break;
                    //
                    //                 //Scale
                    //                 case 4:
                    //                     StrTrans = DAEUtils.VectorStr (InvScale * new Vector3 (
                    //                         h3DAnimTransform.ScaleX.Exists //X
                    //                             ? h3DAnimTransform.ScaleX.GetFrameValue (Frame)
                    //                             : SklBone.Scale.X,
                    //                         h3DAnimTransform.ScaleY.Exists //Y
                    //                             ? h3DAnimTransform.ScaleY.GetFrameValue (Frame)
                    //                             : SklBone.Scale.Y,
                    //                         h3DAnimTransform.ScaleZ.Exists //Z
                    //                             ? h3DAnimTransform.ScaleZ.GetFrameValue (Frame)
                    //                             : SklBone.Scale.Z));
                    //                     break;
                    //
                    //                 //Rotation
                    //                 case 1:
                    //                     StrTrans = DAEUtils.RadToDegStr (h3DAnimTransform.RotationX.GetFrameValue (Frame));
                    //                     break;
                    //                 case 2:
                    //                     StrTrans = DAEUtils.RadToDegStr (h3DAnimTransform.RotationY.GetFrameValue (Frame));
                    //                     break;
                    //                 case 3:
                    //                     StrTrans = DAEUtils.RadToDegStr (h3DAnimTransform.RotationZ.GetFrameValue (Frame));
                    //                     break;
                    //             }
                    //         } else if (Elem.Content is H3DAnimQuatTransform QuatTransform) {
                    //             //Compensate parent bone scale (basically, don't inherit scales)
                    //             if (Parent != null && (SklBone.Flags & H3DBoneFlags.IsSegmentScaleCompensate) != 0) {
                    //                 if (PElem != null)
                    //                     InvScale /= ((H3DAnimQuatTransform) PElem.Content).GetScaleValue (Frame);
                    //                 else
                    //                     InvScale /= Parent.Scale;
                    //             }
                    //
                    //             switch (i) {
                    //                 case 0:
                    //                     StrTrans = DAEUtils.VectorStr (QuatTransform.GetTranslationValue (Frame));
                    //                     break;
                    //                 case 1:
                    //                     StrTrans = DAEUtils.RadToDegStr (QuatTransform.GetRotationValue (Frame)
                    //                         .ToEuler ().X);
                    //                     break;
                    //                 case 2:
                    //                     StrTrans = DAEUtils.RadToDegStr (QuatTransform.GetRotationValue (Frame)
                    //                         .ToEuler ().Y);
                    //                     break;
                    //                 case 3:
                    //                     StrTrans = DAEUtils.RadToDegStr (QuatTransform.GetRotationValue (Frame)
                    //                         .ToEuler ().Z);
                    //                     break;
                    //                 case 4:
                    //                     StrTrans = DAEUtils.VectorStr (InvScale * QuatTransform.GetScaleValue (Frame));
                    //                     break;
                    //             }
                    //         }
                    //
                    //         //This is the Time in seconds, so we divide by the target FPS
                    //         AnimTimes[Frame] = (Frame / 30f).ToString (CultureInfo.InvariantCulture);
                    //         AnimPoses[Frame] = StrTrans;
                    //         AnimLerps[Frame] = "LINEAR";
                    //     }
                    //
                    // }
                }





                foreach (var animationElement in h3dSkeletalAnimation.Elements) {
                    var boneTransform = boneTransformDict[animationElement.Name];
                    var curveBindingPath = AnimationUtility.CalculateTransformPath (boneTransform, animation.transform);
                    if (animationElement.Content is H3DAnimQuatTransform quatTransform) {
                        var cx = new AnimationCurve();
                        var cy = new AnimationCurve();
                        var cz = new AnimationCurve();
                        var crx = new AnimationCurve();
                        var cry = new AnimationCurve();
                        var crz = new AnimationCurve();   
                        var crw = new AnimationCurve();      
                        var csx = new AnimationCurve();
                        var csy = new AnimationCurve();
                        var csz = new AnimationCurve();
                        
                        for (var i = 0; i < h3dSkeletalAnimation.FramesCount; i++) {
                            cx.AddKey (i, quatTransform.Translations[i].X);
                            cy.AddKey (i, quatTransform.Translations[i].Y);
                            cz.AddKey (i, quatTransform.Translations[i].Z);
                            var rot = new Quaternion {eulerAngles = new Vector3 {
                                x = SkeletonUtils.RadToDeg (quatTransform.GetRotationValue (i).ToEuler ().X),
                                y = SkeletonUtils.RadToDeg (quatTransform.GetRotationValue (i).ToEuler ().Y),
                                z = SkeletonUtils.RadToDeg (quatTransform.GetRotationValue (i).ToEuler ().Z)
                            }};
                            crx.AddKey (i, rot.x);
                            cry.AddKey (i, rot.y);
                            crz.AddKey (i, rot.z);
                            crw.AddKey (i, rot.w);
                            csx.AddKey (i, quatTransform.Scales[i].X);
                            csy.AddKey (i, quatTransform.Scales[i].Y);
                            csz.AddKey (i, quatTransform.Scales[i].Z);
                        }
                        unityClip.SetCurve (curveBindingPath, typeof(Transform), "localPosition.x", cx);
                        unityClip.SetCurve(curveBindingPath, typeof(Transform), "localPosition.y", cy);
                        unityClip.SetCurve(curveBindingPath, typeof(Transform), "localPosition.z", cz);
                        unityClip.SetCurve(curveBindingPath, typeof(Transform), "localRotation.x", crx);
                        unityClip.SetCurve(curveBindingPath, typeof(Transform), "localRotation.y", cry);
                        unityClip.SetCurve(curveBindingPath, typeof(Transform), "localRotation.z", crz);
                        unityClip.SetCurve(curveBindingPath, typeof(Transform), "localRotation.w", crw);
                        unityClip.SetCurve(curveBindingPath, typeof(Transform), "localScale.x", csx);
                        unityClip.SetCurve(curveBindingPath, typeof(Transform), "localScale.y", csy);
                        unityClip.SetCurve(curveBindingPath, typeof(Transform), "localScale.z", csz);
                    }               
                }

                AssetDatabase.CreateAsset (unityClip, combinedExportFolder + unityClip.name + ".anim");
                // animation.AddClip (unityClip, unityClip.name);
                animation.clip = unityClip;
            }
            
            
            
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

            foreach (var kvp in textureDict)
                File.WriteAllBytes (exportPath + kvp.Key + ".png", kvp.Value.EncodeToPNG ());

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

            
            var matDict = new Dictionary<string, Material> ();
            foreach (var h3dMaterial in h3DScene.Models[0].Materials) {
                var newMaterial = new Material (Shader.Find ("Shader Graphs/LitPokemonShader"));

                var mainTexturePath = exportPath + h3dMaterial.Texture0Name + ".png";
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

                var normalMapPath = exportPath + h3dMaterial.Texture2Name + ".png";
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

                var occlusionMapPath = exportPath + h3dMaterial.Texture1Name + ".png";
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

                AssetDatabase.CreateAsset (newMaterial, exportPath + h3dMaterial.Name + ".mat");
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
                    SaveMeshAtPath (mesh, exportPath + subMeshName + ".asset");
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
    }
}