using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using P3DS2U.Editor.SPICA.Commands;
using P3DS2U.Editor.SPICA.GFL2.Model.Material;
using P3DS2U.Editor.SPICA.GFL2.Model.Mesh;
using P3DS2U.Editor.SPICA.H3D;
using P3DS2U.Editor.SPICA.H3D.Model;
using P3DS2U.Editor.SPICA.H3D.Model.Material;
using P3DS2U.Editor.SPICA.H3D.Model.Mesh;
using P3DS2U.Editor.SPICA.Math3D;

namespace P3DS2U.Editor.SPICA.GFL2.Model
{
    public class GFModel
    {
        private const uint MagicNum = 0x15122117u;
        private const string MagicStr = "gfmodel";

        internal const string DefaultLUTName = "LookupTableSetContentCtrName";
        public readonly List<GFLUT> LUTs;
        public readonly List<GFMaterial> Materials;
        public readonly List<GFMesh> Meshes;

        public readonly List<GFBone> Skeleton;
        public Vector4 BBoxMaxVector;

        public Vector4 BBoxMinVector;

        public string Name;
        public Matrix4x4 Transform;

        public GFModel ()
        {
            Skeleton = new List<GFBone> ();
            LUTs = new List<GFLUT> ();
            Materials = new List<GFMaterial> ();
            Meshes = new List<GFMesh> ();
        }

        public GFModel (BinaryReader Reader, string ModelName) : this ()
        {
            Name = ModelName;

            var MagicNumber = Reader.ReadUInt32 ();
            var SectionsCount = Reader.ReadUInt32 ();

            GFSection.SkipPadding (Reader.BaseStream);

            var ModelSection = new GFSection (Reader);

            var ShaderNames = ReadHashTable (Reader);
            var TextureNames = ReadHashTable (Reader);
            var MaterialNames = ReadHashTable (Reader);
            var MeshNames = ReadHashTable (Reader);

            BBoxMinVector = Reader.ReadVector4 ();
            BBoxMaxVector = Reader.ReadVector4 ();
            Transform = Reader.ReadMatrix4x4 ();

            //TODO: Investigate what is this (maybe Tile permissions?)
            var UnknownDataLength = Reader.ReadUInt32 ();
            var UnknownDataOffset = Reader.ReadUInt32 ();
            var Padding = Reader.ReadUInt64 ();

            Reader.BaseStream.Seek (UnknownDataOffset + UnknownDataLength, SeekOrigin.Current);

            var BonesCount = Reader.ReadInt32 ();

            Reader.BaseStream.Seek (0xc, SeekOrigin.Current);

            for (var Index = 0; Index < BonesCount; Index++) Skeleton.Add (new GFBone (Reader));

            GFSection.SkipPadding (Reader.BaseStream);

            var LUTsCount = Reader.ReadInt32 ();
            var LUTLength = Reader.ReadInt32 ();

            GFSection.SkipPadding (Reader.BaseStream);

            for (var Index = 0; Index < LUTsCount; Index++) LUTs.Add (new GFLUT (Reader, LUTLength));

            for (var Index = 0; Index < MaterialNames.Length; Index++) Materials.Add (new GFMaterial (Reader));

            for (var Index = 0; Index < MeshNames.Length; Index++) Meshes.Add (new GFMesh (Reader));

            foreach (var LUT in LUTs)
            foreach (var HN in TextureNames) {
                var FNV = new GFNV1 ();

                FNV.Hash (HN.Name);

                if (LUT.HashId == FNV.HashCode) {
                    LUT.Name = HN.Name;

                    break;
                }
            }
        }

        private GFHashName[] ReadHashTable (BinaryReader Reader)
        {
            var Count = Reader.ReadUInt32 ();

            var Values = new GFHashName[Count];

            for (var Index = 0; Index < Count; Index++) {
                Values[Index].Hash = Reader.ReadUInt32 ();
                Values[Index].Name = Reader.ReadPaddedString (0x40);
            }

            return Values;
        }

        private int FindName (GFHashName[] Table, string Name)
        {
            for (var Index = 0; Index < Table.Length; Index++)
                if (Table[Index].Name == Name)
                    return Index;

            return -1;
        }

        public void Write (BinaryWriter Writer)
        {
            var SectionsCount = (uint) (Materials.Count + Meshes.Count + 1);

            Writer.Write (MagicNum);
            Writer.Write (SectionsCount);

            GFSection.SkipPadding (Writer.BaseStream);

            var StartPosition = Writer.BaseStream.Position;

            new GFSection (MagicStr).Write (Writer);

            var ShaderNames = new List<string> ();
            var TextureNames = new List<string> ();
            var MaterialNames = new List<string> ();
            var MeshNames = new List<string> ();

            foreach (var Mat in Materials) {
                ShaderNames.Add (Mat.ShaderName);
                TextureNames.Add (Mat.TextureCoords[0].Name);
                TextureNames.Add (Mat.TextureCoords[1].Name);
                TextureNames.Add (Mat.TextureCoords[2].Name);
                TextureNames.Add (GetLUTName (Mat.LUT0HashId));
                TextureNames.Add (GetLUTName (Mat.LUT1HashId));
                TextureNames.Add (GetLUTName (Mat.LUT2HashId));
                MaterialNames.Add (Mat.MaterialName);
            }

            foreach (var Mesh in Meshes) MeshNames.Add (Mesh.Name);

            WriteHashTable (Writer, ShaderNames);
            WriteHashTable (Writer, TextureNames);
            WriteHashTable (Writer, MaterialNames);
            WriteHashTable (Writer, MeshNames);

            Writer.Write (BBoxMinVector);
            Writer.Write (BBoxMaxVector);
            Writer.Write (Transform);

            //TODO: Figure out what is this.
            Writer.Write (0);
            Writer.Write (0x10);
            Writer.Write (0ul);
            Writer.Write (0ul);
            Writer.Write (0ul);

            Writer.Write ((uint) Skeleton.Count);

            Writer.BaseStream.Seek (0xc, SeekOrigin.Current);

            foreach (var Bone in Skeleton) Bone.Write (Writer);

            GFSection.SkipPadding (Writer.BaseStream);

            Writer.Write (LUTs.Count);
            Writer.Write (0x420);

            GFSection.SkipPadding (Writer.BaseStream);

            foreach (var LUT in LUTs) LUT.Write (Writer);

            var EndPosition = Writer.BaseStream.Position;

            Writer.BaseStream.Seek (StartPosition + 8, SeekOrigin.Begin);

            Writer.Write ((uint) (EndPosition - StartPosition - 0x10));

            Writer.BaseStream.Seek (EndPosition, SeekOrigin.Begin);

            foreach (var Mat in Materials) Mat.Write (Writer);

            foreach (var Mesh in Meshes) Mesh.Write (Writer);
        }

        private void WriteHashTable (BinaryWriter Writer, List<string> Elems)
        {
            Elems = Elems.Where (x => x != null).Distinct ().ToList ();

            Writer.Write ((uint) Elems.Count);

            foreach (var Elem in Elems) {
                var FNV = new GFNV1 ();

                var Buffer = Encoding.ASCII.GetBytes (Elem);

                Array.Resize (ref Buffer, 0x40);

                foreach (var b in Buffer) {
                    if (b == 0) break;

                    FNV.Hash (b);
                }

                Writer.Write (FNV.HashCode);
                Writer.Write (Buffer);
            }
        }

        public H3DModel ToH3DModel ()
        {
            var Output = new H3DModel {
                Name = Name
            };

            //Skeleton
            foreach (var Bone in Skeleton)
                Output.Skeleton.Add (new H3DBone {
                    ParentIndex = (short) Skeleton.FindIndex (x => x.Name == Bone.Parent),

                    Name = Bone.Name,
                    Scale = Bone.Scale,
                    Rotation = Bone.Rotation,
                    Translation = Bone.Translation
                });

            foreach (var Bone in Output.Skeleton) {
                Bone.CalculateTransform (Output.Skeleton);

                Bone.Flags |= H3DBoneFlags.IsSegmentScaleCompensate;
            }

            if (Output.Skeleton.Count > 0) Output.Flags = H3DModelFlags.HasSkeleton;

            //Materials
            foreach (var Material in Materials) {
                var Mat = new H3DMaterial ();

                var Params = Mat.MaterialParams;

                Mat.Name = Material.MaterialName;

                Params.FragmentFlags = H3DFragmentFlags.IsLUTReflectionEnabled;

                Array.Copy (Material.TextureSources, Params.TextureSources, 4);

                for (var Unit = 0; Unit < Material.TextureCoords.Length; Unit++) {
                    var TextureName = Material.TextureCoords[Unit].Name;

                    Mat.EnabledTextures[Unit] = TextureName != null;

                    switch (Unit) {
                        case 0:
                            Mat.Texture0Name = TextureName;
                            break;
                        case 1:
                            Mat.Texture1Name = TextureName;
                            break;
                        case 2:
                            Mat.Texture2Name = TextureName;
                            break;
                    }

                    //Texture Coords
                    var MappingType = Material.TextureCoords[Unit].MappingType;

                    Params.TextureCoords[Unit].MappingType = (H3DTextureMappingType) MappingType;

                    Params.TextureCoords[Unit].Scale = Material.TextureCoords[Unit].Scale;
                    Params.TextureCoords[Unit].Rotation = Material.TextureCoords[Unit].Rotation;
                    Params.TextureCoords[Unit].Translation = Material.TextureCoords[Unit].Translation;

                    //Texture Mapper
                    Mat.TextureMappers[Unit].WrapU = (PICATextureWrap) Material.TextureCoords[Unit].WrapU;
                    Mat.TextureMappers[Unit].WrapV = (PICATextureWrap) Material.TextureCoords[Unit].WrapV;

                    Mat.TextureMappers[Unit].MagFilter = (H3DTextureMagFilter) Material.TextureCoords[Unit].MagFilter;
                    Mat.TextureMappers[Unit].MinFilter = (H3DTextureMinFilter) Material.TextureCoords[Unit].MinFilter;

                    Mat.TextureMappers[Unit].MinLOD = (byte) Material.TextureCoords[Unit].MinLOD;

                    Mat.TextureMappers[Unit].BorderColor = Material.BorderColor[Unit];
                }

                Params.EmissionColor = Material.EmissionColor;
                Params.AmbientColor = Material.AmbientColor;
                Params.DiffuseColor = Material.DiffuseColor;
                Params.Specular0Color = Material.Specular0Color;
                Params.Specular1Color = Material.Specular1Color;
                Params.Constant0Color = Material.Constant0Color;
                Params.Constant1Color = Material.Constant1Color;
                Params.Constant2Color = Material.Constant2Color;
                Params.Constant3Color = Material.Constant3Color;
                Params.Constant4Color = Material.Constant4Color;
                Params.Constant5Color = Material.Constant5Color;
                Params.BlendColor = Material.BlendColor;

                //HACK: It's usually 0 on Sun/Moon, this causes issues on some
                //models being rendered transparent (Shader differences).
                Params.DiffuseColor.A = 0xff;

                Params.ColorScale = 1f;

                Params.LUTInputAbsolute = Material.LUTInputAbsolute;
                Params.LUTInputSelection = Material.LUTInputSelection;
                Params.LUTInputScale = Material.LUTInputScale;

                Params.ColorOperation = Material.ColorOperation;
                Params.BlendFunction = Material.BlendFunction;
                Params.LogicalOperation = Material.LogicalOperation;
                Params.AlphaTest = Material.AlphaTest;
                Params.StencilTest = Material.StencilTest;
                Params.StencilOperation = Material.StencilOperation;
                Params.DepthColorMask = Material.DepthColorMask;
                Params.FaceCulling = Material.FaceCulling;

                Params.ColorBufferRead = Material.ColorBufferRead;
                Params.ColorBufferWrite = Material.ColorBufferWrite;

                Params.StencilBufferRead = Material.StencilBufferRead;
                Params.StencilBufferWrite = Material.StencilBufferWrite;

                Params.DepthBufferRead = Material.DepthBufferRead;
                Params.DepthBufferWrite = Material.DepthBufferWrite;

                if (Material.LUT0HashId != 0) {
                    Params.LUTReflecRTableName = DefaultLUTName;
                    Params.LUTReflecRSamplerName = GetLUTName (Material.LUT0HashId);
                }

                if (Material.LUT1HashId != 0) {
                    Params.LUTReflecGTableName = DefaultLUTName;
                    Params.LUTReflecGSamplerName = GetLUTName (Material.LUT1HashId);
                }

                if (Material.LUT2HashId != 0) {
                    Params.LUTReflecBTableName = DefaultLUTName;
                    Params.LUTReflecBSamplerName = GetLUTName (Material.LUT2HashId);
                }

                if (Material.BumpTexture != -1) {
                    Params.BumpTexture = (byte) Material.BumpTexture;
                    Params.BumpMode = H3DBumpMode.AsBump;
                }

                Params.Constant0Assignment = Material.Constant0Assignment;
                Params.Constant1Assignment = Material.Constant1Assignment;
                Params.Constant2Assignment = Material.Constant2Assignment;
                Params.Constant3Assignment = Material.Constant3Assignment;
                Params.Constant4Assignment = Material.Constant4Assignment;
                Params.Constant5Assignment = Material.Constant5Assignment;

                var VtxShaderName = Material.VtxShaderName;

                //Make shader names match X/Y/OR/AS shader names.
                if (VtxShaderName == "Poke" ||
                    VtxShaderName == "PokeNormal")
                    VtxShaderName = "PokePack";

                Params.ShaderReference = $"0@{VtxShaderName}";
                Params.ModelReference = $"{Mat.Name}@{Name}";

                /*
                 * Add those for compatibility with the older BCH models.
                 * It's worth noting that ShaderParam0 is usually used as "UVScale" on model that uses
                 * geometry shader to make billboarded point sprites. On the new shader it have a
                 * multiplication of the Color by 3, while the older one doesn't have such multiplication,
                 * so for compatibility with the older shader, the easiest thing to do is just multiply the
                 * scale by 3 to give the same results on the old shader.
                 */
                Params.MetaData = new H3DMetaData ();

                Params.MetaData.Add (new H3DMetaDataValue ("EdgeType", Material.EdgeType));
                Params.MetaData.Add (new H3DMetaDataValue ("IDEdgeEnable", Material.IDEdgeEnable));
                Params.MetaData.Add (new H3DMetaDataValue ("EdgeID", Material.EdgeID));

                Params.MetaData.Add (new H3DMetaDataValue ("ProjectionType", Material.ProjectionType));

                Params.MetaData.Add (new H3DMetaDataValue ("RimPow", Material.RimPower));
                Params.MetaData.Add (new H3DMetaDataValue ("RimScale", Material.RimScale));
                Params.MetaData.Add (new H3DMetaDataValue ("PhongPow", Material.PhongPower));
                Params.MetaData.Add (new H3DMetaDataValue ("PhongScale", Material.PhongScale));

                Params.MetaData.Add (new H3DMetaDataValue ("IDEdgeOffsetEnable", Material.IDEdgeOffsetEnable));

                Params.MetaData.Add (new H3DMetaDataValue ("EdgeMapAlphaMask", Material.EdgeMapAlphaMask));

                Params.MetaData.Add (new H3DMetaDataValue ("BakeTexture0", Material.BakeTexture0));
                Params.MetaData.Add (new H3DMetaDataValue ("BakeTexture1", Material.BakeTexture1));
                Params.MetaData.Add (new H3DMetaDataValue ("BakeTexture2", Material.BakeTexture2));
                Params.MetaData.Add (new H3DMetaDataValue ("BakeConstant0", Material.BakeConstant0));
                Params.MetaData.Add (new H3DMetaDataValue ("BakeConstant1", Material.BakeConstant1));
                Params.MetaData.Add (new H3DMetaDataValue ("BakeConstant2", Material.BakeConstant2));
                Params.MetaData.Add (new H3DMetaDataValue ("BakeConstant3", Material.BakeConstant3));
                Params.MetaData.Add (new H3DMetaDataValue ("BakeConstant4", Material.BakeConstant4));
                Params.MetaData.Add (new H3DMetaDataValue ("BakeConstant5", Material.BakeConstant5));

                Params.MetaData.Add (new H3DMetaDataValue ("VertexShaderType", Material.VertexShaderType));

                Params.MetaData.Add (new H3DMetaDataValue ("ShaderParam0", Material.ShaderParam0 * 3));
                Params.MetaData.Add (new H3DMetaDataValue ("ShaderParam1", Material.ShaderParam1));
                Params.MetaData.Add (new H3DMetaDataValue ("ShaderParam2", Material.ShaderParam2));
                Params.MetaData.Add (new H3DMetaDataValue ("ShaderParam3", Material.ShaderParam3));

                Output.Materials.Add (Mat);
            }

            //Meshes
            Output.MeshNodesTree = new H3DPatriciaTree ();

            foreach (var Mesh in Meshes)
                //Note: GFModel have one Vertex Buffer for each SubMesh,
                //while on H3D all SubMeshes shares the same Vertex Buffer.
                //For this reason we need to store SubMeshes as Meshes on H3D.
            foreach (var SubMesh in Mesh.SubMeshes) {
                var NodeIndex = Output.MeshNodesTree.Find (Mesh.Name);

                if (NodeIndex == -1) {
                    Output.MeshNodesTree.Add (Mesh.Name);
                    Output.MeshNodesVisibility.Add (true);

                    NodeIndex = Output.MeshNodesCount++;
                }

                var SubMeshes = new List<H3DSubMesh> ();

                var BoneIndices = new ushort[SubMesh.BoneIndicesCount];

                for (var Index = 0; Index < BoneIndices.Length; Index++)
                    BoneIndices[Index] = SubMesh.BoneIndices[Index];

                var SMSk = Output.Skeleton.Count > 0
                    ? H3DSubMeshSkinning.Smooth
                    : H3DSubMeshSkinning.None;

                SubMeshes.Add (new H3DSubMesh {
                    Skinning = SMSk,
                    BoneIndicesCount = SubMesh.BoneIndicesCount,
                    BoneIndices = BoneIndices,
                    Indices = SubMesh.Indices
                });

                var M = new H3DMesh (
                    SubMesh.RawBuffer,
                    SubMesh.VertexStride,
                    SubMesh.Attributes,
                    SubMesh.FixedAttributes,
                    SubMeshes);

                M.Skinning = H3DMeshSkinning.Smooth;

                var MatIndex = Materials.FindIndex (x => x.MaterialName == SubMesh.Name);

                var Mat = Materials[MatIndex];

                M.MaterialIndex = (ushort) MatIndex;
                M.NodeIndex = (ushort) NodeIndex;
                M.Layer = Mat.RenderLayer;
                M.Priority = Mat.RenderPriority;

                M.UpdateBoolUniforms (Output.Materials[MatIndex]);

                Output.AddMesh (M);
            }

            return Output;
        }

        private string GetLUTName (uint Hash)
        {
            return LUTs.FirstOrDefault (x => x.HashId == Hash)?.Name;
        }
    }
}