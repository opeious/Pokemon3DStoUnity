using System.IO;
using P3DS2U.Editor.SPICA.GFL2;
using P3DS2U.Editor.SPICA.GFL2.Model;
using P3DS2U.Editor.SPICA.GFL2.Motion;
using P3DS2U.Editor.SPICA.GFL2.Shader;
using P3DS2U.Editor.SPICA.GFL2.Texture;
using P3DS2U.Editor.SPICA.H3D;
using P3DS2U.Editor.SPICA.H3D.Model;

namespace P3DS2U.Editor.SPICA
{
    internal class GFPkmnModel
    {
        private const uint GFModelConstant = 0x15122117;
        private const uint GFTextureConstant = 0x15041213;
        private const uint GFMotionConstant = 0x00060000;

        private const uint BCHConstant = 0x00484342;

        public static H3D.H3D OpenAsH3D (Stream Input, GFPackageExtensions.Header Header, H3DDict<H3DBone> Skeleton = null, int animFilesCount = -1)
        {
            H3D.H3D Output = default;

            var Reader = new BinaryReader (Input);

            Input.Seek (Header.Entries[0].Address, SeekOrigin.Begin);

            if (Reader.PeekChar() != -1)
            {
                var MagicNum = Reader.ReadUInt32();

                switch (MagicNum)
                {
                    case GFModelConstant:
                        var MdlPack = new GFModelPack();

                        //High Poly Pokémon model
                        Input.Seek(Header.Entries[0].Address, SeekOrigin.Begin);

                        MdlPack.Models.Add(new GFModel(Reader, "PM_HighPoly"));

                        //Low Poly Pokémon model
                        Input.Seek(Header.Entries[1].Address, SeekOrigin.Begin);

                        MdlPack.Models.Add(new GFModel(Reader, "PM_LowPoly"));

                        //Pokémon Shader package
                        Input.Seek(Header.Entries[2].Address, SeekOrigin.Begin);

                        var PSHeader = GFPackageExtensions.GetPackageHeader(Input);

                        foreach (var Entry in PSHeader.Entries)
                        {
                            Input.Seek(Entry.Address, SeekOrigin.Begin);

                            MdlPack.Shaders.Add(new GFShader(Reader));
                        }

                        //More shaders
                        Input.Seek(Header.Entries[3].Address, SeekOrigin.Begin);

                        if (GFPackageExtensions.IsValidPackage(Input))
                        {
                            var PCHeader = GFPackageExtensions.GetPackageHeader(Input);

                            foreach (var Entry in PCHeader.Entries)
                            {
                                Input.Seek(Entry.Address, SeekOrigin.Begin);

                                MdlPack.Shaders.Add(new GFShader(Reader));
                            }
                        }

                        Output = MdlPack.ToH3D();

                        break;

                    case GFTextureConstant:
                        Output = new H3D.H3D();

                        foreach (var Entry in Header.Entries)
                        {
                            Input.Seek(Entry.Address, SeekOrigin.Begin);

                            Output.Textures.Add(new GFTexture(Reader).ToH3DTexture());
                        }

                        break;

                    case GFMotionConstant:
                        Output = new H3D.H3D();

                        if (Skeleton == null) break;
                        for (var Index = 0; Index < Header.Entries.Length; Index++)
                        {
                            Input.Seek(Header.Entries[Index].Address, SeekOrigin.Begin);

                            if (Input.Position + 4 > Input.Length) break;
                            if (Reader.ReadUInt32() != GFMotionConstant) continue;

                            Input.Seek(-4, SeekOrigin.Current);

                            var Mot = new GFMotion(Reader, Index);

                            var SklAnim = Mot.ToH3DSkeletalAnimation(Skeleton);
                            var MatAnim = Mot.ToH3DMaterialAnimation();
                            var VisAnim = Mot.ToH3DVisibilityAnimation();

                            if (P3ds2USettingsScriptableObject.Instance.ImporterSettings.RenameGeneratedAnimationFiles)
                            {
                                string motionUsage = "";
                                switch (animFilesCount)
                                {
                                    case 0: motionUsage = "Fight"; break;
                                    case 1: motionUsage = "Pet"; break;
                                    case 2: motionUsage = "Movement"; break;
                                    default: return Output;
                                }

                                string animationName =
                                    $"{motionUsage}_{AnimationNaming.animationNames[motionUsage][Index].ToLower()}";

                                bool needToImport =
                                    PokemonImporter.AnimationImportOptions[animFilesCount][AnimationNaming.animationNames[motionUsage][Index]];
                                if (needToImport)
                                {
                                    if (SklAnim != null)
                                    {
                                        SklAnim.Name = animationName;

                                        if (Header.Entries[Index].Length != 0)
                                        {
                                            Output.SkeletalAnimations.Add(SklAnim);
                                        }
                                    }

                                    if (MatAnim != null)
                                    {
                                        MatAnim.Name = animationName;

                                        if (Header.Entries[Index].Length != 0)
                                        {
                                            Output.MaterialAnimations.Add(MatAnim);
                                        }
                                    }

                                    if (VisAnim != null)
                                    {
                                        VisAnim.Name = animationName;

                                        if (Header.Entries[Index].Length != 0)
                                        {
                                            Output.VisibilityAnimations.Add(VisAnim);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (SklAnim != null)
                                {
                                    SklAnim.Name = $"Motion_{Mot.Index}";

                                    Output.SkeletalAnimations.Add(SklAnim);
                                }

                                if (MatAnim != null)
                                {
                                    MatAnim.Name = $"Motion_{Mot.Index}";

                                    Output.MaterialAnimations.Add(MatAnim);
                                }

                                if (VisAnim != null)
                                {
                                    VisAnim.Name = $"Motion_{Mot.Index}";

                                    Output.VisibilityAnimations.Add(VisAnim);
                                }
                            }
                        }

                        break;

                    case BCHConstant:
                        Output = new H3D.H3D();

                        foreach (var Entry in Header.Entries)
                        {
                            Input.Seek(Entry.Address, SeekOrigin.Begin);

                            MagicNum = Reader.ReadUInt32();

                            if (MagicNum != BCHConstant) continue;

                            Input.Seek(-4, SeekOrigin.Current);

                            var Buffer = Reader.ReadBytes(Entry.Length);

                            Output.Merge(H3D.H3D.Open(Buffer));
                        }

                        break;
                }
            }

            return Output;
        }
    }
}