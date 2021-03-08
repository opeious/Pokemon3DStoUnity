using System;
using System.IO;
using System.Numerics;

namespace P3DS2U.Editor.SPICA.Math3D
{
    internal static class VectorExtensions
    {
        public static Vector2 ReadVector2 (this BinaryReader Reader)
        {
            return new Vector2 (
                Reader.ReadSingle (),
                Reader.ReadSingle ());
        }

        public static Vector3 ReadVector3 (this BinaryReader Reader)
        {
            return new Vector3 (
                Reader.ReadSingle (),
                Reader.ReadSingle (),
                Reader.ReadSingle ());
        }

        public static Vector4 ReadVector4 (this BinaryReader Reader)
        {
            return new Vector4 (
                Reader.ReadSingle (),
                Reader.ReadSingle (),
                Reader.ReadSingle (),
                Reader.ReadSingle ());
        }

        public static Quaternion ReadQuaternion (this BinaryReader Reader)
        {
            return new Quaternion (
                Reader.ReadSingle (),
                Reader.ReadSingle (),
                Reader.ReadSingle (),
                Reader.ReadSingle ());
        }

        public static void Write (this BinaryWriter Writer, Vector2 v)
        {
            Writer.Write (v.X);
            Writer.Write (v.Y);
        }

        public static void Write (this BinaryWriter Writer, Vector3 v)
        {
            Writer.Write (v.X);
            Writer.Write (v.Y);
            Writer.Write (v.Z);
        }

        public static void Write (this BinaryWriter Writer, Vector4 v)
        {
            Writer.Write (v.X);
            Writer.Write (v.Y);
            Writer.Write (v.Z);
            Writer.Write (v.W);
        }

        public static void Write (this BinaryWriter Writer, Quaternion q)
        {
            Writer.Write (q.X);
            Writer.Write (q.Y);
            Writer.Write (q.Z);
            Writer.Write (q.W);
        }

        public static Quaternion CreateRotationBetweenVectors (Vector3 a, Vector3 b)
        {
            var qw = Vector3.Dot (a, b) + (float) Math.Sqrt (a.LengthSquared () * b.LengthSquared ());

            var Rotation = new Quaternion (Vector3.Cross (a, b), qw);

            return Quaternion.Normalize (Rotation);
        }

        public static Vector3 ToEuler (this Quaternion q) => q.ComputeAngles ();
    }
}

public static class QuaternionExtensions
{
    public static float ComputeXAngle(this Quaternion q)
    {
        float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        return (float) Math.Atan2(sinr_cosp, cosr_cosp);
    }

    public static float ComputeYAngle(this Quaternion q)
    {
        float sinp = 2 * (q.W * q.Y - q.Z * q.X);
        if (Math.Abs(sinp) >= 1)
            return (float) (Math.PI / 2 * Math.Sign(sinp)); // use 90 degrees if out of range
        else
            return (float) Math.Asin(sinp);
    }

    public static float ComputeZAngle(this Quaternion q)
    {
        float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        return (float) Math.Atan2(siny_cosp, cosy_cosp);
    }

    public static Vector3 ComputeAngles(this Quaternion q)
    {
        return new Vector3(ComputeXAngle(q), ComputeYAngle(q), ComputeZAngle(q));
    }
}