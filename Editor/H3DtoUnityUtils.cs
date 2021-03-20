using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using P3DS2U.Editor.SPICA;
using P3DS2U.Editor.SPICA.Commands;
using P3DS2U.Editor.SPICA.Converters;
using P3DS2U.Editor.SPICA.H3D.Model;
using P3DS2U.Editor.SPICA.H3D.Model.Material;
using UnityEngine;

namespace P3DS2U.Editor
{
    public static class BinaryUtils
    {
        private const uint MODEL = 0x15122117;
        private const uint TEXTURE = 0x15041213;
        private const uint MOTION = 0x00060000;
        
        public enum FileType
        {
            Undefined,
            Model,
            Texture,
            Animation,
        }

        public static FileType GetBinaryFileType (string fileName)
        {
            using (var fs = new FileStream (fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                if (fs.Length > 4) {
                    var reader = new BinaryReader (fs);
                    var magicNum = reader.ReadUInt32 ();
                    fs.Seek (-4, SeekOrigin.Current);
                    var magic = Encoding.ASCII.GetString (reader.ReadBytes (4));
                    fs.Seek (0, SeekOrigin.Begin);
                    if (GFPackageExtensions.IsValidPackage (fs)) {
                        var packHeader = GFPackageExtensions.GetPackageHeader (fs);
                        if (packHeader.Magic == "PC") {
                            fs.Seek (packHeader.Entries[0].Address, SeekOrigin.Begin);

                            try
                            {
                                if (reader == null || reader.PeekChar() == -1)
                                    return FileType.Undefined;

                                var magicNum2 = reader.ReadUInt32();
                                switch (magicNum2)
                                {
                                    case MODEL:
                                        return FileType.Model;
                                    case TEXTURE:
                                        return FileType.Texture;
                                    case MOTION:
                                        return FileType.Animation;

                                }
                            }
                            catch (Exception e)
                            {
                                Debug.LogError("Error: " + e.Message + ": "+e.StackTrace);
                                return FileType.Undefined;
                            }

                        }
                    }
                }
            }   
            return FileType.Undefined;
        }
    }

    public static class AnimationUtils
    {
        public enum MatAnimationModifier
        {
            Skipped,
            Tex0TranslateX,
            Tex1TranslateX,
            Tex2TranslateX,
            Tex0TranslateY,
            Tex1TranslateY,
            Tex2TranslateY,
            Tex0Scale,
            Tex1Scale,
            Tex2Scale,
            Tex0Rot,
            Tex1Rot,
            Tex2Rot,
        }

        public static string MatModifierToShaderProp (MatAnimationModifier matAnimationModifier)
        {
            switch (matAnimationModifier) {
                case MatAnimationModifier.Tex0TranslateX:
                    return P3ds2USettingsScriptableObject.Instance.customShaderSettings.Tex0TranslateX;
                case MatAnimationModifier.Tex1TranslateX:
                    return P3ds2USettingsScriptableObject.Instance.customShaderSettings.Tex1TranslateX;                
                case MatAnimationModifier.Tex2TranslateX:
                    return P3ds2USettingsScriptableObject.Instance.customShaderSettings.Tex2TranslateX;                
                case MatAnimationModifier.Tex0TranslateY:
                    return P3ds2USettingsScriptableObject.Instance.customShaderSettings.Tex0TranslateY;
                case MatAnimationModifier.Tex1TranslateY:
                    return P3ds2USettingsScriptableObject.Instance.customShaderSettings.Tex1TranslateY;
                case MatAnimationModifier.Tex2TranslateY:
                    return P3ds2USettingsScriptableObject.Instance.customShaderSettings.Tex2TranslateY;
                default:
                    throw new ArgumentOutOfRangeException (nameof(matAnimationModifier), matAnimationModifier, null);
            }
        }
        
        public static AnimationCurve GetOrAddCurve(Dictionary<MatAnimationModifier, AnimationCurve> curvesDict, MatAnimationModifier modifier)
        {
            if (modifier == MatAnimationModifier.Skipped) {
                Debug.LogError ("Type of anim not supported!");
                return null;
            }
            
            if (curvesDict.ContainsKey (modifier)) {
                return curvesDict[modifier];
            }
            var newCurve = new AnimationCurve();
            curvesDict.Add (modifier, newCurve);
            return newCurve;
        }

        private const float UnityConversionFactor = 2f;
        public static float GetTimeAtFrame (AnimationClip clip, int frame) => UnityConversionFactor * frame / clip.frameRate;
    }
    
    public static class DirectoryUtils
    {
        public static IEnumerable<string> GetAllFilesRecursive(string path, bool includeMetaFiles = false)
        {
            var retVal = new List<string> ();
            try
            {
                foreach (var d in Directory.GetDirectories(path)) {
                    retVal.AddRange (Directory.GetFiles (d).Where (f => includeMetaFiles || !f.Contains (".meta")));
                    GetAllFilesRecursive(d);
                }
            }
            catch (Exception e)
            {
                Debug.LogError (e.Message);
            }
            return retVal;
        }
    }

    
    public static class H3DMaterialExtensions
    {
        public static int GetTextureIndex (this H3DMaterial h3DMaterial, string name)
        {
            return h3DMaterial.Texture0Name == name ? 0 : h3DMaterial.Texture1Name == name ? 1 : 2;
        }

        public static IEnumerable<string> TextureNames (this H3DMaterial h3DMaterial)
        {
            return new List<string> {
                h3DMaterial.Texture0Name, h3DMaterial.Texture1Name, h3DMaterial.Texture2Name
            };
        }
    }

    public static class TextureUtils
    {
        public static Texture2D FlipTexture (Texture2D original)
        {
            var flipped = new Texture2D (original.width, original.height);
            var xN = original.width;
            var yN = original.height;

            for (var i = 0; i < xN; i++)
            for (var j = 0; j < yN; j++)
                flipped.SetPixel (xN - i - 1, j, original.GetPixel (i, j));
            flipped.Apply ();
            return flipped;
        }

        public static TextureWrapMode PicaToUnityTextureWrapMode (PICATextureWrap picaTextureWrap)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (picaTextureWrap) {
                case PICATextureWrap.Repeat:
                    return TextureWrapMode.Repeat;
                case PICATextureWrap.Mirror:
                    return TextureWrapMode.Mirror;
                case PICATextureWrap.ClampToEdge:
                    return TextureWrapMode.Clamp;
                default:
                    return TextureWrapMode.Mirror;
            }
        }

        public class H3DTextureRepresentation
        {
            public H3DTextureCoord TextureCoord;
            public H3DTextureMapper TextureMapper;
        }
    }

    public static class MeshUtils
    {
        public static IEnumerable<Vector3> PicaToUnityVertex (IEnumerable<PICAVertex> picaVertices)
        {
            return picaVertices.Select (picaVertex =>
                new Vector3 (picaVertex.Position.X * -1, picaVertex.Position.Y, picaVertex.Position.Z)).ToList ();
        }

        public static IEnumerable<Vector4> PicaToUnityTangents (IEnumerable<PICAVertex> picaVertices)
        {
            return picaVertices.Select (picaVertex =>
                    new Vector4 (picaVertex.Tangent.X * -1, picaVertex.Tangent.Y, picaVertex.Tangent.Z, picaVertex.Tangent.W))
                .ToList ();
        }

        public static IEnumerable<Vector2> PicaToUnityUV (IEnumerable<PICAVertex> picaVertices)
        {
            return picaVertices.Select (picaVertex => new Vector2 (picaVertex.TexCoord0.X, picaVertex.TexCoord0.Y))
                .ToList ();
        }

        public static IEnumerable<Vector3> PicaToUnityNormals (IEnumerable<PICAVertex> picaVertices)
        {
            return picaVertices
                .Select (picaVertex => new Vector3 (picaVertex.Normal.X * -1, picaVertex.Normal.Y, picaVertex.Normal.Z))
                .ToList ();
        }
    }

    public static class VectorExtensions
    {
        public static Vector3 CastNumericsVector3 (System.Numerics.Vector3 newValues)
        {
            var vector3 = new Vector3 {x = newValues.X, y = newValues.Y, z = newValues.Z};
            return vector3;
        }

        public static Vector3 CastNumericsVector3 (System.Numerics.Vector4 newValues)
        {
            var vector3 = new Vector3 {x = newValues.X, y = newValues.Y, z = newValues.Z};
            return vector3;
        }

        public static Vector4 CastNumericsVector4 (System.Numerics.Vector4 newValues)
        {
            var vector4 = new Vector4 {x = newValues.X, y = newValues.Y, z = newValues.Z, w = newValues.W};
            return vector4;
        }

        public static Vector3 GetAxisFromRotation (Vector4 vector4)
        {
            return new Vector3 (vector4.x * -1, vector4.y * -1, vector4.z * -1);
        }

        public static float GetScalarFromRotation (Vector4 vector4)
        {
            return vector4.w;
        }
    }

    public static class SkeletonUtils
    {
        private const float RadToDegConstant = (float) (1 / Math.PI * 180);

        public static SkeletonNode GenerateSkeletonForModel (H3DModel mdl)
        {
            if ((mdl.Skeleton?.Count ?? 0) <= 0) return null;
            var rootNode = new SkeletonNode ();
            var childBones = new Queue<Tuple<H3DBone, SkeletonNode>> ();

            childBones.Enqueue (Tuple.Create (mdl.Skeleton[0], rootNode));

            while (childBones.Count > 0) {
                var (item1, item2) = childBones.Dequeue ();

                var bone = item1;

                item2.Name = bone.Name;
                item2.SetBoneEuler (
                    VectorExtensions.CastNumericsVector3 (bone.Translation),
                    VectorExtensions.CastNumericsVector3 (bone.Rotation),
                    VectorExtensions.CastNumericsVector3 (bone.Scale)
                );

                foreach (var b in mdl.Skeleton) {
                    if (b.ParentIndex == -1) continue;

                    var parentBone = mdl.Skeleton[b.ParentIndex];

                    if (parentBone != bone) continue;

                    var node = new SkeletonNode ();

                    childBones.Enqueue (Tuple.Create (b, node));

                    if (item2.Nodes == null) item2.Nodes = new List<SkeletonNode> ();

                    item2.Nodes.Add (node);
                }
            }

            return rootNode;
        }

        public static float RadToDeg (float radians)
        {
            return radians * RadToDegConstant;
        }

        public class SkeletonNode
        {
            public string Name;

            public List<SkeletonNode> Nodes;
            public Vector4[] Rotation;
            public Vector3 Scale;

            public Vector3 Translation;

            public void SetBoneEuler (Vector3 t, Vector3 r, Vector3 s)
            {
                Rotation = new Vector4[3];
                Translation = t;
                Rotation[0] = new Vector4 (0, 0, 1, RadToDeg (r.z));
                Rotation[1] = new Vector4 (0, 1, 0, RadToDeg (r.y));
                Rotation[2] = new Vector4 (-1, 0, 0, RadToDeg (r.x));
                Scale = s;
            }
        }
    }
}