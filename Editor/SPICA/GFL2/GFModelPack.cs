using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using P3DS2U.Editor.SPICA.GFL2.Model;
using P3DS2U.Editor.SPICA.GFL2.Shader;
using P3DS2U.Editor.SPICA.GFL2.Texture;
using P3DS2U.Editor.SPICA.H3D.LUT;

namespace P3DS2U.Editor.SPICA.GFL2
{
    public class GFModelPack
    {
        public readonly List<GFModel> Models;
        public readonly List<GFShader> Shaders;
        public readonly List<GFTexture> Textures;

        public GFModelPack ()
        {
            Models = new List<GFModel> ();
            Textures = new List<GFTexture> ();
            Shaders = new List<GFShader> ();
        }

        public GFModelPack (Stream Input) : this (new BinaryReader (Input))
        {
        }

        public GFModelPack (BinaryReader Reader) : this ()
        {
            var Position = Reader.BaseStream.Position;

            var MagicNumber = Reader.ReadUInt32 ();

            var Counts = new uint[5];

            for (var Index = 0; Index < Counts.Length; Index++) Counts[Index] = Reader.ReadUInt32 ();

            var PointersAddr = Reader.BaseStream.Position;

            for (var Sect = 0; Sect < Counts.Length; Sect++) {
                for (var Entry = 0; Entry < Counts[Sect]; Entry++) {
                    Reader.BaseStream.Seek (PointersAddr + Entry * 4, SeekOrigin.Begin);
                    Reader.BaseStream.Seek (Position + Reader.ReadUInt32 (), SeekOrigin.Begin);

                    var Name = Reader.ReadString ();
                    // string Name = Reader.ReadByteLengthString();
                    var Address = Reader.ReadUInt32 ();

                    Reader.BaseStream.Seek (Position + Address, SeekOrigin.Begin);

                    switch ((Section) Sect) {
                        case Section.Model:
                            Models.Add (new GFModel (Reader, Name));
                            break;
                        case Section.Texture:
                            Textures.Add (new GFTexture (Reader));
                            break;
                        case Section.Shader:
                            Shaders.Add (new GFShader (Reader));
                            break;
                    }
                }

                PointersAddr += Counts[Sect] * 4;
            }
        }

        public H3D.H3D ToH3D ()
        {
            var Output = new H3D.H3D ();

            var L = new H3DLUT ();

            L.Name = GFModel.DefaultLUTName;

            for (var MdlIndex = 0; MdlIndex < Models.Count; MdlIndex++) {
                var Model = Models[MdlIndex];
                var Mdl = Model.ToH3DModel ();

                for (var MatIndex = 0; MatIndex < Model.Materials.Count; MatIndex++) {
                    var Params = Mdl.Materials[MatIndex].MaterialParams;

                    var FragShaderName = Model.Materials[MatIndex].FragShaderName;
                    var VtxShaderName = Model.Materials[MatIndex].VtxShaderName;

                    var FragShader = Shaders.FirstOrDefault (x => x.Name == FragShaderName);
                    var VtxShader = Shaders.FirstOrDefault (x => x.Name == VtxShaderName);

                    if (FragShader != null) {
                        Params.TexEnvBufferColor = FragShader.TexEnvBufferColor;

                        Array.Copy (FragShader.TexEnvStages, Params.TexEnvStages, 6);
                    }

                    if (VtxShader != null) {
                        // foreach (KeyValuePair<uint, Vector4> KV in VtxShader.VtxShaderUniforms)
                        // {
                        //     Params.VtxShaderUniforms.Add(KV.Key, KV.Value);
                        // }
                        //
                        // foreach (KeyValuePair<uint, Vector4> KV in VtxShader.GeoShaderUniforms)
                        // {
                        //     Params.GeoShaderUniforms.Add(KV.Key, KV.Value);
                        // }
                    }
                }

                foreach (var LUT in Model.LUTs)
                    L.Samplers.Add (new H3DLUTSampler {
                        Name = LUT.Name,
                        Table = LUT.Table
                    });

                Output.Models.Add (Mdl);
            }

            Output.LUTs.Add (L);

            Output.CopyMaterials ();

            foreach (var Texture in Textures) Output.Textures.Add (Texture.ToH3DTexture ());

            return Output;
        }

        private enum Section
        {
            Model,
            Texture,
            Unknown2,
            Unknown3,
            Shader
        }
    }
}