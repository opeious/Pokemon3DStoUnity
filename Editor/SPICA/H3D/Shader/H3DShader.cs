using System;

namespace P3DS2U.Editor.SPICA.H3D.Shader
{
    public class H3DShader : INamed
    {
        public byte[] Program;

        //Those seems to be always null?
        private uint[] ShaderAllCommands;
        private uint[] ShaderProgramCommands;
        private uint[] ShaderSetupCommands;

        public short VtxShaderIndex;
        public short GeoShaderIndex;

        private uint BindingAddress; //SBZ?

        private string _Name;

        public string Name
        {
            get => _Name;
            set => _Name = value ?? throw new Exception ("null");
        }

        private uint UserDefinedAddress; //SBZ
    }
    
}