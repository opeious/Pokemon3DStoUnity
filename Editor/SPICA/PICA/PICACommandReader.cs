using System.Collections.Generic;
using System.Numerics;
using P3DS2U.Editor.SPICA.Commands;

namespace P3DS2U.Editor.SPICA.PICA
{
    internal class PICACommandReader
    {
        private int CmdIndex;

        private readonly List<PICACommand> Commands;
        private readonly UniformManager GeoShader;

        private readonly UniformManager VtxShader;

        public PICACommandReader (uint[] Cmds)
        {
            Commands = new List<PICACommand> ();

            VtxShader = new UniformManager ();
            GeoShader = new UniformManager ();

            var Index = 0;

            while (Index < Cmds.Length) {
                var Parameter = Cmds[Index++];
                var Command = Cmds[Index++];

                var Id = (Command >> 0) & 0xffff;
                var Mask = (Command >> 16) & 0xf;
                var ExtraParams = (Command >> 20) & 0x7ff;
                var Consecutive = Command >> 31 != 0;

                if (Consecutive) {
                    for (var i = 0; i < ExtraParams + 1; i++) {
                        var Cmd = new PICACommand {
                            Register = (PICARegister) Id++,
                            Parameters = new[] {Parameter},
                            Mask = Mask
                        };

                        CheckVtxUniformsCmd (Cmd);
                        CheckGeoUniformsCmd (Cmd);

                        Commands.Add (Cmd);

                        if (i < ExtraParams) Parameter = Cmds[Index++];
                    }
                } else {
                    var Parameters = new List<uint> {Parameter};

                    for (var i = 0; i < ExtraParams; i++) Parameters.Add (Cmds[Index++]);

                    var Cmd = new PICACommand {
                        Register = (PICARegister) Id,
                        Parameters = Parameters.ToArray (),
                        Mask = Mask
                    };

                    CheckVtxUniformsCmd (Cmd);
                    CheckGeoUniformsCmd (Cmd);

                    Commands.Add (Cmd);
                }

                //Commands must be padded in 8 bytes blocks, so Index can't be even!
                if ((Index & 1) != 0) Index++;
            }
        }

        public Vector4[] VtxShaderUniforms => VtxShader.Uniforms;
        public Vector4[] GeoShaderUniforms => GeoShader.Uniforms;

        public bool HasCommand => CmdIndex < Commands.Count;

        private void CheckVtxUniformsCmd (PICACommand Cmd)
        {
            if (Cmd.Register == PICARegister.GPUREG_VSH_FLOATUNIFORM_INDEX)
                VtxShader.SetIndexCommand (Cmd.Parameters[0]);
            else if (
                Cmd.Register >= PICARegister.GPUREG_VSH_FLOATUNIFORM_DATA0 &&
                Cmd.Register <= PICARegister.GPUREG_VSH_FLOATUNIFORM_DATA7)
                VtxShader.SetValueParameters (Cmd.Parameters);
        }

        private void CheckGeoUniformsCmd (PICACommand Cmd)
        {
            if (Cmd.Register == PICARegister.GPUREG_GSH_FLOATUNIFORM_INDEX)
                GeoShader.SetIndexCommand (Cmd.Parameters[0]);
            else if (
                Cmd.Register >= PICARegister.GPUREG_GSH_FLOATUNIFORM_DATA0 &&
                Cmd.Register <= PICARegister.GPUREG_GSH_FLOATUNIFORM_DATA7)
                GeoShader.SetValueParameters (Cmd.Parameters);
        }

        public PICACommand GetCommand ()
        {
            return Commands[CmdIndex++];
        }

        public PICACommand[] GetCommands ()
        {
            return Commands.ToArray ();
        }

        public Dictionary<uint, Vector4> GetAllVertexShaderUniforms ()
        {
            return VtxShader.GetAllUsedUniforms ();
        }

        public Dictionary<uint, Vector4> GetAllGeometryShaderUniforms ()
        {
            return GeoShader.GetAllUsedUniforms ();
        }

        private class UniformManager
        {
            private bool Uniform32Bits;

            private uint UniformIndex;
            public readonly Vector4[] Uniforms;

            private readonly HashSet<uint> UsedUniforms;

            private PICAVectorFloat24 VectorF24;

            public UniformManager ()
            {
                Uniforms = new Vector4[96];

                UsedUniforms = new HashSet<uint> ();
            }

            public void SetIndexCommand (uint Cmd)
            {
                UniformIndex = (Cmd & 0xff) << 2;
                Uniform32Bits = Cmd >> 31 != 0;
            }

            public void SetValueParameters (uint[] Params)
            {
                foreach (var Param in Params) {
                    var UIdx = (UniformIndex >> 2) & 0x5f;

                    if (!UsedUniforms.Contains (UIdx)) UsedUniforms.Add (UIdx);

                    if (Uniform32Bits) {
                        var Value = IOUtils.ToSingle (Param);

                        switch (UniformIndex & 3) {
                            case 0:
                                Uniforms[UIdx].W = Value;
                                break;
                            case 1:
                                Uniforms[UIdx].Z = Value;
                                break;
                            case 2:
                                Uniforms[UIdx].Y = Value;
                                break;
                            case 3:
                                Uniforms[UIdx].X = Value;
                                break;
                        }
                    } else {
                        switch (UniformIndex & 3) {
                            case 0:
                                VectorF24.Word0 = Param;
                                break;
                            case 1:
                                VectorF24.Word1 = Param;
                                break;
                            case 2:
                                VectorF24.Word2 = Param;
                                break;
                        }

                        if ((UniformIndex & 3) == 2) {
                            //The Float 24 Vector only uses 3 Words (24 * 4 = 96 bits = 3 Words)
                            //for all four elements (X/Y/Z/W), so we ignore the fourth Word here
                            UniformIndex++;

                            Uniforms[UIdx] = VectorF24;
                        }
                    }

                    UniformIndex++;
                }
            }

            public Dictionary<uint, Vector4> GetAllUsedUniforms ()
            {
                var Output = new Dictionary<uint, Vector4> ();

                foreach (var UIdx in UsedUniforms) Output.Add (UIdx, Uniforms[UIdx]);

                return Output;
            }
        }
    }
}