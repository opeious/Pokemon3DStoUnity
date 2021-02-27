using System;
using System.Collections;
using System.Collections.Generic;
using SPICA.Formats.Common;
using SPICA.Serialization;
using SPICA.Serialization.Attributes;

namespace SPICA.Formats.CtrH3D
{
    public class H3DPatriciaTree : ICustomSerialization, IEnumerable<string>, INameIndexed
    {
        private const string DuplicateKeysEx = "Tree shouldn't contain duplicate keys!";
        [Ignore] private readonly List<string> Names;
        [Ignore] private readonly List<H3DPatriciaTreeNode> Nodes;

        [Ignore] private bool TreeNeedsRebuild;

        public H3DPatriciaTree ()
        {
            Nodes = new List<H3DPatriciaTreeNode> {new H3DPatriciaTreeNode ()};
            Names = new List<string> ();
        }

        public int Count => Names.Count;

        void ICustomSerialization.Deserialize (BinaryDeserializer Deserializer)
        {
            var MaxIndex = 0;
            var Index = 0;

            Nodes.Clear ();

            while (Index++ <= MaxIndex) {
                var Node = Deserializer.Deserialize<H3DPatriciaTreeNode> ();

                MaxIndex = Math.Max (MaxIndex, Node.LeftNodeIndex);
                MaxIndex = Math.Max (MaxIndex, Node.RightNodeIndex);

                if (Nodes.Count > 0) Names.Add (Node.Name);

                Nodes.Add (Node);
            }
        }

        bool ICustomSerialization.Serialize (BinarySerializer Serializer)
        {
            if (TreeNeedsRebuild) RebuildTree ();

            Serializer.WriteValue (Nodes);

            return true;
        }

        public IEnumerator<string> GetEnumerator ()
        {
            return Names.GetEnumerator ();
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        public int Find (string Name)
        {
            if (Name == null) return -1;

            if (TreeNeedsRebuild) RebuildTree ();

            var Output = 0;

            if (Nodes != null && Nodes.Count > 0) {
                H3DPatriciaTreeNode Root;

                Output = PatriciaTree.Traverse (Name, Nodes, out Root);

                if (Nodes[Output].Name != Name) Output = 0;
            }

            return Output - 1;
        }

        //Implementation
        public bool Contains (string Name)
        {
            return Find (Name) != -1;
        }

        public string Find (int Index)
        {
            return Names[Index];
        }

        public void Add (string Name)
        {
            TreeNeedsRebuild = true;

            Names.Add (Name);
        }

        public void Insert (int Index, string Name)
        {
            TreeNeedsRebuild = true;

            Names.Insert (Index, Name);
        }

        public void Remove (string Name)
        {
            TreeNeedsRebuild = true;

            Names.Remove (Name);
        }

        public void Clear ()
        {
            TreeNeedsRebuild = true;

            Names.Clear ();
        }

        private void RebuildTree ()
        {
            Nodes.Clear ();

            if (Names.Count > 0)
                Nodes.Add (new H3DPatriciaTreeNode {ReferenceBit = uint.MaxValue});
            else
                Nodes.Add (new H3DPatriciaTreeNode ());

            var MaxLength = 0;

            foreach (var Name in Names)
                if (MaxLength < Name.Length)
                    MaxLength = Name.Length;

            foreach (var Name in Names) PatriciaTree.Insert (Nodes, new H3DPatriciaTreeNode {Name = Name}, MaxLength);

            TreeNeedsRebuild = false;
        }
    }
}