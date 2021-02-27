using SPICA.Formats.Common;

namespace SPICA.Formats.CtrH3D.Shader
{
    public class H3DShader : INamed
    {
        private uint BindingAddress; //SBZ?
        public short GeoShaderIndex;
        public byte[] Program;

        //Those seems to be always null?
        private uint[] ShaderAllCommands;
        private uint[] ShaderProgramCommands;
        private uint[] ShaderSetupCommands;

        private uint UserDefinedAddress; //SBZ

        public short VtxShaderIndex;

        public string Name { get; set; }
    }
}