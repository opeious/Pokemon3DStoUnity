using System.Collections;
using System.Collections.Generic;
using System.IO;
using P3DS2U.Editor.SPICA.GFL2.Motion;

namespace P3DS2U.Editor.SPICA.GFL2
{
    public class GFMotionPack : IEnumerable<GFMotion>
    {
        private readonly List<GFMotion> Animations;

        public GFMotionPack ()
        {
            Animations = new List<GFMotion> ();
        }

        public GFMotionPack (Stream Input) : this (new BinaryReader (Input))
        {
        }

        public GFMotionPack (BinaryReader Reader) : this ()
        {
            var AnimsCount = Reader.ReadUInt32 ();

            var Position = Reader.BaseStream.Position;

            for (var Index = 0; Index < AnimsCount; Index++) {
                Reader.BaseStream.Seek (Position + Index * 4, SeekOrigin.Begin);

                var Address = Reader.ReadUInt32 ();

                if (Address == 0) continue;

                Reader.BaseStream.Seek (Position + Address, SeekOrigin.Begin);

                Animations.Add (new GFMotion (Reader, Index));
            }
        }

        public GFMotion this [int Index] {
            get => Animations[Index];
            set => Animations[Index] = value;
        }

        public int Count => Animations.Count;

        public IEnumerator<GFMotion> GetEnumerator ()
        {
            return Animations.GetEnumerator ();
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        public void Add (GFMotion Mot)
        {
            Animations.Add (Mot);
        }

        public void Insert (int Index, GFMotion Mot)
        {
            Animations.Insert (Index, Mot);
        }

        public void Remove (GFMotion Mot)
        {
            Animations.Remove (Mot);
        }

        public void Clear ()
        {
            Animations.Clear ();
        }
    }
}