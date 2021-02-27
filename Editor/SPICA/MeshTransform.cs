using System.Collections.Generic;
using System.Numerics;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Formats.CtrH3D.Model.Mesh;
using SPICA.PICA.Converters;

namespace SPICA.Formats.Common
{
    internal static class MeshTransform
    {
        public static List<PICAVertex> GetVerticesList (H3DDict<H3DBone> Skeleton, H3DMesh Mesh)
        {
            var Output = new List<PICAVertex> ();

            var Vertices = Mesh.GetVertices ();

            foreach (var SM in Mesh.SubMeshes)
            foreach (var i in SM.Indices) {
                var v = Vertices[i];

                if (Skeleton != null &&
                    Skeleton.Count > 0 &&
                    SM.Skinning != H3DSubMeshSkinning.Smooth) {
                    int b = SM.BoneIndices[v.Indices[0]];

                    var Transform = Skeleton[b].GetWorldTransform (Skeleton);

                    v.Position = Vector4.Transform (new Vector3 (
                            v.Position.X,
                            v.Position.Y,
                            v.Position.Z),
                        Transform);

                    v.Normal.W = 0;

                    v.Normal = Vector4.Transform (v.Normal, Transform);
                    v.Normal = Vector4.Normalize (v.Normal);
                }

                for (var b = 0; b < 4 && v.Weights[b] > 0; b++) v.Indices[b] = SM.BoneIndices[v.Indices[b]];

                Output.Add (v);
            }

            return Output;
        }

        public static PICAVertex[] GetWorldSpaceVertices (H3DDict<H3DBone> Skeleton, H3DMesh Mesh)
        {
            var Vertices = Mesh.GetVertices ();

            var TransformedVertices = new bool[Vertices.Length];

            //Smooth meshes are already in World Space, so we don't need to do anything.
            if (Mesh.Skinning != H3DMeshSkinning.Smooth)
                foreach (var SM in Mesh.SubMeshes)
                foreach (var i in SM.Indices) {
                    if (TransformedVertices[i]) continue;

                    TransformedVertices[i] = true;

                    var v = Vertices[i];

                    if (Skeleton != null &&
                        Skeleton.Count > 0 &&
                        SM.Skinning != H3DSubMeshSkinning.Smooth) {
                        int b = SM.BoneIndices[v.Indices[0]];

                        var Transform = Skeleton[b].GetWorldTransform (Skeleton);

                        v.Position = Vector4.Transform (new Vector3 (
                                v.Position.X,
                                v.Position.Y,
                                v.Position.Z),
                            Transform);

                        v.Normal.W = 0;

                        v.Normal = Vector4.Transform (v.Normal, Transform);
                        v.Normal = Vector4.Normalize (v.Normal);
                    }

                    Vertices[i] = v;
                }

            return Vertices;
        }
    }
}