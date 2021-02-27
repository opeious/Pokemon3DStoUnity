using System.Collections.Generic;
using System.Xml.Serialization;
using P3DS2U.Editor.SPICA.Commands;
using P3DS2U.Editor.SPICA.H3D.Model.Material;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAEEffect
    {
        [XmlAttribute] public string id;
        [XmlAttribute] public string name;

        public DAEProfileCOMMON profile_COMMON = new DAEProfileCOMMON ();
    }

    public class DAEProfileCOMMON
    {
        [XmlElement ("newparam")] public List<DAEEffectParam> newparam = new List<DAEEffectParam> ();
        [XmlAttribute] public string sid;

        public DAEEffectProfileCOMMONTechnique technique = new DAEEffectProfileCOMMONTechnique ();
    }

    public class DAEEffectParam
    {
        [XmlElement (IsNullable = false)] public DAEEffectParamSurfaceElement surface;
        [XmlElement (IsNullable = false)] public DAEEffectParamSampler2DElement sampler2D;
        [XmlAttribute] public string sid;
    }

    public class DAEEffectParamSurfaceElement
    {
        public string format;

        public string init_from;
        [XmlAttribute] public string type;
    }

    public class DAEEffectParamSampler2DElement
    {
        public DAEFilter magfilter;
        public DAEFilter minfilter;
        public DAEFilter mipfilter;
        public string source;
        public DAEWrap wrap_s;
        public DAEWrap wrap_t;
    }

    public enum DAEWrap
    {
        NONE,
        WRAP,
        MIRROR,
        CLAMP,
        BORDER
    }

    public enum DAEFilter
    {
        NONE,
        NEAREST,
        LINEAR,
        NEAREST_MIPMAP_NEAREST,
        LINEAR_MIPMAP_NEAREST,
        NEAREST_MIPMAP_LINEAR,
        LINEAR_MIPMAP_LINEAR
    }

    public static class DAEH3DTextureWrapExtensions
    {
        public static DAEWrap ToDAEWrap (this PICATextureWrap Wrap)
        {
            switch (Wrap) {
                default:
                case PICATextureWrap.ClampToEdge: return DAEWrap.CLAMP;
                case PICATextureWrap.ClampToBorder: return DAEWrap.BORDER;
                case PICATextureWrap.Repeat: return DAEWrap.WRAP;
                case PICATextureWrap.Mirror: return DAEWrap.MIRROR;
            }
        }
    }

    public static class DAEH3DTextureFilterExtensions
    {
        public static DAEFilter ToDAEFilter (this H3DTextureMinFilter Filter)
        {
            switch (Filter) {
                default:
                case H3DTextureMinFilter.Nearest: return DAEFilter.NEAREST;
                case H3DTextureMinFilter.NearestMipmapNearest: return DAEFilter.NEAREST_MIPMAP_NEAREST;
                case H3DTextureMinFilter.NearestMipmapLinear: return DAEFilter.NEAREST_MIPMAP_LINEAR;
                case H3DTextureMinFilter.Linear: return DAEFilter.LINEAR;
                case H3DTextureMinFilter.LinearMipmapNearest: return DAEFilter.LINEAR_MIPMAP_NEAREST;
                case H3DTextureMinFilter.LinearMipmapLinear: return DAEFilter.LINEAR_MIPMAP_LINEAR;
            }
        }

        public static DAEFilter ToDAEFilter (this H3DTextureMagFilter Filter)
        {
            switch (Filter) {
                default:
                case H3DTextureMagFilter.Nearest: return DAEFilter.NEAREST;
                case H3DTextureMagFilter.Linear: return DAEFilter.LINEAR;
            }
        }
    }

    public class DAEEffectProfileCOMMONTechnique
    {
        public DAEPhong phong = new DAEPhong ();
        [XmlAttribute] public string sid;
    }

    public class DAEPhong
    {
        public DAEPhongDiffuse diffuse = new DAEPhongDiffuse ();
    }

    public class DAEPhongDiffuse
    {
        public DAEPhongDiffuseTexture texture = new DAEPhongDiffuseTexture ();
    }

    public class DAEPhongDiffuseTexture
    {
        [XmlAttribute] public string texcoord = "uv";
        [XmlAttribute] public string texture;
    }
}