using System.Collections.Generic;

namespace P3DS2U.Editor.SPICA.Commands
{
    public struct PICAAttribute
    {
        public PICAAttributeName Name;
        public PICAAttributeFormat Format;
        public int Elements;
        public float Scale;

        public static List<PICAAttribute> GetAttributes (params PICAAttributeName[] Names)
        {
            var Output = new List<PICAAttribute> ();

            foreach (var Name in Names)
                switch (Name) {
                    case PICAAttributeName.Position:
                    case PICAAttributeName.Normal:
                    case PICAAttributeName.Tangent:
                        Output.Add (new PICAAttribute {
                            Name = Name,
                            Format = PICAAttributeFormat.Float,
                            Elements = 3,
                            Scale = 1
                        });
                        break;

                    case PICAAttributeName.TexCoord0:
                    case PICAAttributeName.TexCoord1:
                    case PICAAttributeName.TexCoord2:
                        Output.Add (new PICAAttribute {
                            Name = Name,
                            Format = PICAAttributeFormat.Float,
                            Elements = 2,
                            Scale = 1
                        });
                        break;

                    case PICAAttributeName.Color:
                        Output.Add (new PICAAttribute {
                            Name = PICAAttributeName.Color,
                            Format = PICAAttributeFormat.Ubyte,
                            Elements = 4,
                            Scale = 1f / 255
                        });
                        break;

                    case PICAAttributeName.BoneIndex:
                        Output.Add (new PICAAttribute {
                            Name = PICAAttributeName.BoneIndex,
                            Format = PICAAttributeFormat.Ubyte,
                            Elements = 4,
                            Scale = 1
                        });
                        break;

                    case PICAAttributeName.BoneWeight:
                        Output.Add (new PICAAttribute {
                            Name = PICAAttributeName.BoneWeight,
                            Format = PICAAttributeFormat.Ubyte,
                            Elements = 4,
                            Scale = 0.01f
                        });
                        break;
                }

            return Output;
        }
    }
}