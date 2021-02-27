using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExtensionMethods;
using JetBrains.Annotations;
using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.WinForms.Formats;
using UnityEditor;
using UnityEngine;

namespace _3DStoUnity
{
    public class PokemonImporter : MonoBehaviour
    {
        private static readonly int BaseMap = Shader.PropertyToID ("_BaseMap");
        private static readonly int NormalMap = Shader.PropertyToID ("_NormalMap");
        private static readonly int OcclusionMap = Shader.PropertyToID ("_OcclusionMap");
        private static readonly int BaseMapTiling = Shader.PropertyToID ("_BaseMapTiling");
        private static readonly int NormalMapTiling = Shader.PropertyToID ("_NormalMapTiling");
        private static readonly int OcclusionMapTiling = Shader.PropertyToID ("_OcclusionMapTiling");

        [MenuItem ("3DStoUnity/Import Pokemon (Bin)")]
        private static void TestImportRaw ()
        {
            var h3DScene = new H3D ();

            // var fileNames = new []{"Assets/Raw/Textures/0195 - Flareon.bin","Assets/Raw/Models/0195 - Flareon.bin"};
            var fileNames = new[] {"Assets/Raw/Textures/0008 - Charizard.bin", "Assets/Raw/Models/0008 - Charizard.bin"};
            foreach (var fileName in fileNames) {
                H3DDict<H3DBone> skeleton = null;

                if (h3DScene.Models.Count > 0) skeleton = h3DScene.Models[0].Skeleton;

                var data = FormatIdentifier.IdentifyAndOpen (fileName, skeleton);

                if (data != null) h3DScene.Merge (data);
            }

            GenerateTextureFiles (h3DScene);
            var meshDict = GenerateMeshInUnityScene (h3DScene);
            var matDict = GenerateMaterialFiles (h3DScene);
            AddMaterialsToGeneratedMeshes (meshDict, matDict, h3DScene);
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

        private static void GenerateTextureFiles (H3D h3DScene)
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
                File.WriteAllBytes ("Assets/Raw/test/" + kvp.Key + ".png", kvp.Value.EncodeToPNG ());

            AssetDatabase.Refresh ();
        }

        private static Dictionary<string, Material> GenerateMaterialFiles (H3D h3DScene)
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

            var PATH = "Assets/Raw/test/";
            var matDict = new Dictionary<string, Material> ();
            foreach (var h3dMaterial in h3DScene.Models[0].Materials) {
                var newMaterial = new Material (Shader.Find ("Shader Graphs/LitPokemonShader"));

                var mainTexturePath = PATH + h3dMaterial.Texture0Name + ".png";
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

                var normalMapPath = PATH + h3dMaterial.Texture2Name + ".png";
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

                var occlusionMapPath = PATH + h3dMaterial.Texture1Name + ".png";
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

                AssetDatabase.CreateAsset (newMaterial, PATH + h3dMaterial.Name + ".mat");
                matDict.Add (h3dMaterial.Name, newMaterial);
            }

            AssetDatabase.SaveAssets ();
            return matDict;
        }


        private static Dictionary<string, SkinnedMeshRenderer> GenerateMeshInUnityScene (H3D h3DScene)
        {
            var meshDict = new Dictionary<string, SkinnedMeshRenderer> ();
            //To be removed after testing
            var toBeDestroyed = GameObject.Find ("Test");
            if (toBeDestroyed != null) DestroyImmediate (toBeDestroyed);

            var h3DModel = h3DScene.Models[0];

            var emptyGo = new GameObject ("EmptyGo");
            var sceneGo = new GameObject ("Test");

            var whiteMat = AssetDatabase.LoadAssetAtPath<Material> ("Assets/Scripts/RawImportScripts/TestMat.mat");

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
                    mesh.SetTriangles (unityMeshTriangles, 0);

                    mesh.boneWeights = unityVertexBones.ToArray ();

                    var meshRenderer = modelGo.AddComponent<SkinnedMeshRenderer> ();
                    meshRenderer.quality = SkinQuality.Bone4;
                    meshRenderer.material = whiteMat;
                    meshRenderer.sharedMesh = mesh;
                    var bonesTransform = sceneGo.transform.GetChild (0).GetComponentsInChildren<Transform> ();
                    meshRenderer.rootBone = bonesTransform[0];
                    meshRenderer.bones = bonesTransform;
                    meshRenderer.updateWhenOffscreen = true;
                    mesh.bindposes = bonesTransform
                        .Select (t => t.worldToLocalMatrix * bonesTransform[0].localToWorldMatrix).ToArray ();

                    meshFilter.sharedMesh = mesh;
                    SaveMeshAtPath (mesh, "Assets/Raw/test/" + subMeshName + ".asset");
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

        private static void SaveMeshAtPath ([NotNull] Mesh mesh, string path)
        {
            if (mesh == null) throw new ArgumentNullException (nameof(mesh));
            if (File.Exists (path)) File.Delete (path);
            AssetDatabase.CreateAsset (mesh, path);
            AssetDatabase.SaveAssets ();
        }
    }
}