using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using P3DS2U.Editor.SPICA.Math3D;
using P3DS2U.Editor.SPICA.Serialization.Attributes;

namespace P3DS2U.Editor.SPICA.Serialization
{
    internal class BinaryDeserializer : BinarySerialization
    {
        public readonly BinaryReader Reader;
        private readonly Dictionary<long, object> ListObjs;

        private readonly Dictionary<long, object> Objects;

        public BinaryDeserializer (Stream BaseStream, SerializationOptions Options) : base (BaseStream, Options)
        {
            Reader = new BinaryReader (BaseStream);

            Objects = new Dictionary<long, object> ();
            ListObjs = new Dictionary<long, object> ();
        }

        public T Deserialize<T> ()
        {
            return (T) ReadValue (typeof(T));
        }

        private object ReadValue (Type Type, bool IsRef = false)
        {
            if (Type.IsPrimitive || Type.IsEnum)
                switch (Type.GetTypeCode (Type)) {
                    case TypeCode.UInt64: return Reader.ReadUInt64 ();
                    case TypeCode.UInt32: return Reader.ReadUInt32 ();
                    case TypeCode.UInt16: return Reader.ReadUInt16 ();
                    case TypeCode.Byte: return Reader.ReadByte ();
                    case TypeCode.Int64: return Reader.ReadInt64 ();
                    case TypeCode.Int32: return Reader.ReadInt32 ();
                    case TypeCode.Int16: return Reader.ReadInt16 ();
                    case TypeCode.SByte: return Reader.ReadSByte ();
                    case TypeCode.Single: return Reader.ReadSingle ();
                    case TypeCode.Double: return Reader.ReadDouble ();
                    case TypeCode.Boolean: return Reader.ReadUInt32 () != 0;

                    default: return null;
                }

            if (IsList (Type))
                return ReadList (Type);
            if (Type == typeof(string))
                return ReadString ();
            if (Type == typeof(Vector2))
                return Reader.ReadVector2 ();
            if (Type == typeof(Vector3))
                return Reader.ReadVector3 ();
            if (Type == typeof(Vector4))
                return Reader.ReadVector4 ();
            if (Type == typeof(Quaternion))
                return Reader.ReadQuaternion ();
            if (Type == typeof(Matrix3x3))
                return Reader.ReadMatrix3x3 ();
            if (Type == typeof(Matrix3x4))
                return Reader.ReadMatrix3x4 ();
            if (Type == typeof(Matrix4x4))
                return Reader.ReadMatrix4x4 ();
            return ReadObject (Type, IsRef);
        }

        private IList ReadList (Type Type)
        {
            return ReadList (Type, false, Reader.ReadInt32 ());
        }

        private IList ReadList (Type Type, FieldInfo Info)
        {
            return ReadList (
                Type,
                Info.IsDefined (typeof(RangeAttribute)),
                Info.GetCustomAttribute<FixedLengthAttribute> ()?.Length ?? Reader.ReadInt32 ());
        }

        private IList ReadList (Type Type, bool Range, int Length)
        {
            IList List;

            if (Type.IsArray) {
                Type = Type.GetElementType ();
                List = Array.CreateInstance (Type, Length);
            } else {
                List = (IList) Activator.CreateInstance (Type);
                Type = Type.GetGenericArguments ()[0];
            }

            var BR = new BitReader (Reader);

            var IsBool = Type == typeof(bool);
            var Inline = Type.IsDefined (typeof(InlineAttribute));
            var IsValue = Type.IsValueType || Type.IsEnum || Inline;

            for (var Index = 0; (Range ? BaseStream.Position : Index) < Length; Index++) {
                var Position = BaseStream.Position;

                object Value;

                if (IsBool)
                    Value = BR.ReadBit ();
                else if (IsValue)
                    Value = ReadValue (Type);
                else
                    Value = ReadReference (Type);

                /*
                 * This is not necessary to make deserialization work, but
                 * is needed because H3D uses range lists for the meshes,
                 * and since meshes are actually classes treated as structs,
                 * we need to use the same reference for meshes on the different layer
                 * lists, otherwise it writes the same mesh more than once (and
                 * this should still work, but the file will be bigger for no
                 * good reason, and also is not what the original tool does).
                 */
                if (Type.IsClass && !IsList (Type)) {
                    if (!ListObjs.TryGetValue (Position, out var Obj))
                        ListObjs.Add (Position, Value);
                    else if (Range) Value = Obj;
                }

                if (List.IsFixedSize)
                    List[Index] = Value;
                else
                    List.Add (Value);
            }

            return List;
        }

        private string ReadString ()
        {
            var SB = new StringBuilder ();

            for (char Chr; (Chr = Reader.ReadChar ()) != '\0';) SB.Append (Chr);

            return SB.ToString ();
        }

        private object ReadObject (Type ObjectType, bool IsRef = false)
        {
            var Position = BaseStream.Position;

            if (ObjectType.IsDefined (typeof(TypeChoiceAttribute))) {
                var TypeId = Reader.ReadUInt32 ();

                var Type = GetMatchingType (ObjectType, TypeId);

                if (Type != null)
                    ObjectType = Type;
                else
                    Debug.WriteLine (
                        "[SPICA|BinaryDeserializer] Unknown Type Id 0x{0:x8} at address {1:x8} and class {2}!", TypeId,
                        Position, ObjectType.FullName);
            }

            var Value = Activator.CreateInstance (ObjectType);

            if (IsRef) Objects.Add (Position, Value);

            var FieldsCount = 0;

            foreach (var Info in GetFieldsSorted (ObjectType)) {
                FieldsCount++;

                if (!Info.GetCustomAttribute<IfVersionAttribute> ()?.Compare (FileVersion) ?? false) continue;

                if (!(
                    Info.IsDefined (typeof(IgnoreAttribute)) ||
                    Info.IsDefined (typeof(CompilerGeneratedAttribute)))) {
                    var Type = Info.FieldType;

                    var TCName = Info.GetCustomAttribute<TypeChoiceNameAttribute> ()?.FieldName;

                    if (TCName != null && Info.IsDefined (typeof(TypeChoiceAttribute))) {
                        var TCInfo = ObjectType.GetField (TCName);

                        var TypeId = Convert.ToUInt32 (TCInfo.GetValue (Value));

                        Type = GetMatchingType (Info, TypeId) ?? Type;
                    }

                    bool Inline;

                    Inline = Info.IsDefined (typeof(InlineAttribute));
                    Inline |= Type.IsDefined (typeof(InlineAttribute));

                    object FieldValue;

                    if (Type.IsValueType || Type.IsEnum || Inline) {
                        FieldValue = IsList (Type)
                            ? ReadList (Type, Info)
                            : ReadValue (Type);

                        if (Type.IsPrimitive && Info.IsDefined (typeof(VersionAttribute)))
                            FileVersion = Convert.ToInt32 (FieldValue);
                    } else {
                        FieldValue = ReadReference (Type, Info);
                    }

                    if (FieldValue != null) Info.SetValue (Value, FieldValue);

                    Align (Info.GetCustomAttribute<PaddingAttribute> ()?.Size ?? 1);
                }
            }

            if (FieldsCount == 0)
                Debug.WriteLine ($"[SPICA|BinaryDeserializer] Class {ObjectType.FullName} has no accessible fields!");

            if (Value is ICustomSerialization) ((ICustomSerialization) Value).Deserialize (this);

            return Value;
        }

        private Type GetMatchingType (MemberInfo Info, uint TypeId)
        {
            foreach (var Attr in Info.GetCustomAttributes<TypeChoiceAttribute> ())
                if (Attr.TypeVal == TypeId)
                    return Attr.Type;

            return null;
        }

        private object ReadReference (Type Type, FieldInfo Info = null)
        {
            uint Address;
            int Length;

            if (GetLengthPos (Info) == LengthPos.AfterPtr) {
                Address = ReadPointer ();
                Length = ReadLength (Type, Info);
            } else {
                Length = ReadLength (Type, Info);
                Address = ReadPointer ();
            }

            var Range = Info?.IsDefined (typeof(RangeAttribute)) ?? false;
            var Repeat = Info?.IsDefined (typeof(RepeatPointerAttribute)) ?? false;

            if (Repeat) BaseStream.Seek (4, SeekOrigin.Current);

            object Value = null;

            if (Address != 0 && (!IsList (Type) || IsList (Type) && Length > 0))
                if (!Objects.TryGetValue (Address, out Value)) {
                    var Position = BaseStream.Position;

                    BaseStream.Seek (Address, SeekOrigin.Begin);

                    Value = IsList (Type)
                        ? ReadList (Type, Range, Length)
                        : ReadValue (Type, true);

                    BaseStream.Seek (Position, SeekOrigin.Begin);
                }

            return Value;
        }

        private int ReadLength (Type Type, FieldInfo Info = null)
        {
            if (IsList (Type)) {
                if (Info?.IsDefined (typeof(FixedLengthAttribute)) ?? false)
                    return Info.GetCustomAttribute<FixedLengthAttribute> ().Length;
                if (GetLengthSize (Info) == LengthSize.Short)
                    return Reader.ReadUInt16 ();
                return Reader.ReadInt32 ();
            }

            return 0;
        }

        public uint ReadPointer ()
        {
            var Address = Reader.ReadUInt32 ();

            if (Options.PtrType == PointerType.SelfRelative && Address != 0) Address += (uint) BaseStream.Position - 4;

            return Address;
        }
    }
}