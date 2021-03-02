using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Xml.Serialization;
using P3DS2U.Editor.SPICA.Commands;
using P3DS2U.Editor.SPICA.H3D.Animation;
using P3DS2U.Editor.SPICA.H3D.Model;
using P3DS2U.Editor.SPICA.H3D.Model.Mesh;
using P3DS2U.Editor.SPICA.Math3D;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    [XmlRoot ("COLLADA", Namespace = "http://www.collada.org/2005/11/COLLADASchema")]
    public class DaeAnimations
    {
        public DAEAsset asset = new DAEAsset ();

        [XmlArrayItem ("animation")] public List<DAEAnimation> library_animations;
        [XmlArrayItem ("controller")] public List<DAEController> library_controllers;
        [XmlArrayItem ("effect")] public List<DAEEffect> library_effects;
        [XmlArrayItem ("geometry")] public List<DAEGeometry> library_geometries;
        [XmlArrayItem ("image")] public List<DAEImage> library_images;
        [XmlArrayItem ("material")] public List<DAEMaterial> library_materials;
        [XmlArrayItem ("visual_scene")] public List<DAEVisualScene> library_visual_scenes;

        public DAEScene scene = new DAEScene ();
        [XmlAttribute] public string version = "1.4.1";

        public DaeAnimations ()
        {
        }

        public DaeAnimations (H3D.H3D Scene, int MdlIndex, int AnimIndex = -1)
        {
            if (MdlIndex != -1) {
                library_visual_scenes = new List<DAEVisualScene> ();

                var Mdl = Scene.Models[MdlIndex];

                var VN = new DAEVisualScene ();

                VN.name = $"{Mdl.Name}_{MdlIndex.ToString ("D2")}";
                VN.id = $"{VN.name}_id";
                var RootBoneId = string.Empty;

                if ((Mdl.Skeleton?.Count ?? 0) > 0) {
                    var ChildBones = new Queue<Tuple<H3DBone, DAENode>> ();

                    var RootNode = new DAENode ();

                    ChildBones.Enqueue (Tuple.Create (Mdl.Skeleton[0], RootNode));

                    RootBoneId = $"#{Mdl.Skeleton[0].Name}_bone_id";

                    while (ChildBones.Count > 0) {
                        var Bone_Node = ChildBones.Dequeue ();

                        var Bone = Bone_Node.Item1;

                        Bone_Node.Item2.id = $"{Bone.Name}_bone_id";
                        Bone_Node.Item2.name = Bone.Name;
                        Bone_Node.Item2.sid = Bone.Name;
                        Bone_Node.Item2.type = DAENodeType.JOINT;
                        Bone_Node.Item2.SetBoneEuler (Bone.Translation, Bone.Rotation, Bone.Scale);

                        foreach (var B in Mdl.Skeleton) {
                            if (B.ParentIndex == -1) continue;

                            var ParentBone = Mdl.Skeleton[B.ParentIndex];

                            if (ParentBone == Bone) {
                                var Node = new DAENode ();

                                ChildBones.Enqueue (Tuple.Create (B, Node));

                                if (Bone_Node.Item2.Nodes == null) Bone_Node.Item2.Nodes = new List<DAENode> ();

                                Bone_Node.Item2.Nodes.Add (Node);
                            }
                        }
                    }

                    VN.node.Add (RootNode);
                }


                library_visual_scenes.Add (VN);

                if (library_visual_scenes.Count > 0)
                    scene.instance_visual_scene.url = $"#{library_visual_scenes[0].id}";

            } //MdlIndex != -1

            if (AnimIndex != -1) {
                library_animations = new List<DAEAnimation> ();

                string[] AnimElemNames = {"translate", "rotateX", "rotateY", "rotateZ", "scale"};

                var SklAnim = Scene.SkeletalAnimations[AnimIndex];

                var Skeleton = Scene.Models[0].Skeleton;

                var FramesCount = (int) SklAnim.FramesCount + 1;

                foreach (var Elem in SklAnim.Elements) {
                    if (Elem.PrimitiveType != H3DPrimitiveType.Transform &&
                        Elem.PrimitiveType != H3DPrimitiveType.QuatTransform) continue;

                    var SklBone = Skeleton.FirstOrDefault (x => x.Name == Elem.Name);
                    H3DBone Parent = null;

                    if (SklBone != null && SklBone.ParentIndex != -1) Parent = Skeleton[SklBone.ParentIndex];

                    for (var i = 0; i < 5; i++) {
                        var AnimTimes = new string[FramesCount];
                        var AnimPoses = new string[FramesCount];
                        var AnimLerps = new string[FramesCount];

                        var IsRotation = i > 0 && i < 4; //1, 2, 3

                        var Skip =
                            Elem.PrimitiveType != H3DPrimitiveType.Transform &&
                            Elem.PrimitiveType != H3DPrimitiveType.QuatTransform;

                        if (!Skip) {
                            if (Elem.Content is H3DAnimTransform Transform)
                                switch (i) {
                                    case 0:
                                        Skip = !Transform.TranslationExists;
                                        break;
                                    case 1:
                                        Skip = !Transform.RotationX.Exists;
                                        break;
                                    case 2:
                                        Skip = !Transform.RotationY.Exists;
                                        break;
                                    case 3:
                                        Skip = !Transform.RotationZ.Exists;
                                        break;
                                    case 4:
                                        Skip = !Transform.ScaleExists;
                                        break;
                                }
                            else if (Elem.Content is H3DAnimQuatTransform QuatTransform)
                                switch (i) {
                                    case 0:
                                        Skip = !QuatTransform.HasTranslation;
                                        break;
                                    case 1:
                                        Skip = !QuatTransform.HasRotation;
                                        break;
                                    case 2:
                                        Skip = !QuatTransform.HasRotation;
                                        break;
                                    case 3:
                                        Skip = !QuatTransform.HasRotation;
                                        break;
                                    case 4:
                                        Skip = !QuatTransform.HasScale;
                                        break;
                                }
                        }

                        if (Skip) continue;

                        for (var Frame = 0; Frame < FramesCount; Frame++) {
                            var StrTrans = string.Empty;

                            var PElem = SklAnim.Elements.FirstOrDefault (x => x.Name == Parent?.Name);

                            var InvScale = Vector3.One;

                            if (Elem.Content is H3DAnimTransform Transform) {
                                //Compensate parent bone scale (basically, don't inherit scales)
                                if (Parent != null && (SklBone.Flags & H3DBoneFlags.IsSegmentScaleCompensate) != 0) {
                                    if (PElem != null) {
                                        var PTrans = (H3DAnimTransform) PElem.Content;

                                        InvScale /= new Vector3 (
                                            PTrans.ScaleX.Exists ? PTrans.ScaleX.GetFrameValue (Frame) : Parent.Scale.X,
                                            PTrans.ScaleY.Exists ? PTrans.ScaleY.GetFrameValue (Frame) : Parent.Scale.Y,
                                            PTrans.ScaleZ.Exists
                                                ? PTrans.ScaleZ.GetFrameValue (Frame)
                                                : Parent.Scale.Z);
                                    } else {
                                        InvScale /= Parent.Scale;
                                    }
                                }

                                switch (i) {
                                    //Translation
                                    case 0:
                                        StrTrans = DAEUtils.VectorStr (new Vector3 (
                                            Transform.TranslationX.Exists //X
                                                ? Transform.TranslationX.GetFrameValue (Frame)
                                                : SklBone.Translation.X,
                                            Transform.TranslationY.Exists //Y
                                                ? Transform.TranslationY.GetFrameValue (Frame)
                                                : SklBone.Translation.Y,
                                            Transform.TranslationZ.Exists //Z
                                                ? Transform.TranslationZ.GetFrameValue (Frame)
                                                : SklBone.Translation.Z));
                                        break;

                                    //Scale
                                    case 4:
                                        StrTrans = DAEUtils.VectorStr (InvScale * new Vector3 (
                                            Transform.ScaleX.Exists //X
                                                ? Transform.ScaleX.GetFrameValue (Frame)
                                                : SklBone.Scale.X,
                                            Transform.ScaleY.Exists //Y
                                                ? Transform.ScaleY.GetFrameValue (Frame)
                                                : SklBone.Scale.Y,
                                            Transform.ScaleZ.Exists //Z
                                                ? Transform.ScaleZ.GetFrameValue (Frame)
                                                : SklBone.Scale.Z));
                                        break;

                                    //Rotation
                                    case 1:
                                        StrTrans = DAEUtils.RadToDegStr (Transform.RotationX.GetFrameValue (Frame));
                                        break;
                                    case 2:
                                        StrTrans = DAEUtils.RadToDegStr (Transform.RotationY.GetFrameValue (Frame));
                                        break;
                                    case 3:
                                        StrTrans = DAEUtils.RadToDegStr (Transform.RotationZ.GetFrameValue (Frame));
                                        break;
                                }
                            } else if (Elem.Content is H3DAnimQuatTransform QuatTransform) {
                                //Compensate parent bone scale (basically, don't inherit scales)
                                if (Parent != null && (SklBone.Flags & H3DBoneFlags.IsSegmentScaleCompensate) != 0) {
                                    if (PElem != null)
                                        InvScale /= ((H3DAnimQuatTransform) PElem.Content).GetScaleValue (Frame);
                                    else
                                        InvScale /= Parent.Scale;
                                }

                                switch (i) {
                                    case 0:
                                        StrTrans = DAEUtils.VectorStr (QuatTransform.GetTranslationValue (Frame));
                                        break;
                                    case 1:
                                        StrTrans = DAEUtils.RadToDegStr (QuatTransform.GetRotationValue (Frame)
                                            .ToEuler ().X);
                                        break;
                                    case 2:
                                        StrTrans = DAEUtils.RadToDegStr (QuatTransform.GetRotationValue (Frame)
                                            .ToEuler ().Y);
                                        break;
                                    case 3:
                                        StrTrans = DAEUtils.RadToDegStr (QuatTransform.GetRotationValue (Frame)
                                            .ToEuler ().Z);
                                        break;
                                    case 4:
                                        StrTrans = DAEUtils.VectorStr (InvScale * QuatTransform.GetScaleValue (Frame));
                                        break;
                                }
                            }

                            //This is the Time in seconds, so we divide by the target FPS
                            AnimTimes[Frame] = (Frame / 30f).ToString (CultureInfo.InvariantCulture);
                            AnimPoses[Frame] = StrTrans;
                            AnimLerps[Frame] = "LINEAR";
                        }

                        var Anim = new DAEAnimation ();

                        Anim.name = $"{SklAnim.Name}_{Elem.Name}_{AnimElemNames[i]}";
                        Anim.id = $"{Anim.name}_id";

                        Anim.src.Add (new DAESource ($"{Anim.name}_frame", 1, AnimTimes, "TIME", "float"));
                        Anim.src.Add (new DAESource ($"{Anim.name}_interp", 1, AnimLerps, "INTERPOLATION", "Name"));

                        Anim.src.Add (IsRotation
                            ? new DAESource ($"{Anim.name}_pose", 1, AnimPoses, "ANGLE", "float")
                            : new DAESource ($"{Anim.name}_pose", 3, AnimPoses,
                                "X", "float",
                                "Y", "float",
                                "Z", "float"));

                        Anim.sampler.AddInput ("INPUT", $"#{Anim.src[0].id}");
                        Anim.sampler.AddInput ("INTERPOLATION", $"#{Anim.src[1].id}");
                        Anim.sampler.AddInput ("OUTPUT", $"#{Anim.src[2].id}");

                        Anim.sampler.id = $"{Anim.name}_samp_id";
                        Anim.channel.source = $"#{Anim.sampler.id}";
                        Anim.channel.target = $"{Elem.Name}_bone_id/{AnimElemNames[i]}";

                        if (IsRotation) Anim.channel.target += ".ANGLE";

                        library_animations.Add (Anim);
                    } //Axis 0-5
                } //SklAnim.Elements
            } //AnimIndex != -1
        }

        public void Save (string FileName)
        {
            using (var FS = new FileStream (FileName, FileMode.Create)) {
                var Serializer = new XmlSerializer (typeof(DaeAnimations));

                Serializer.Serialize (FS, this);
            }
        }
    }
}