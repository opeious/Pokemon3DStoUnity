using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using P3DS2U.Editor.SPICA.Serialization.Attributes;

namespace P3DS2U.Editor.SPICA.H3D
{
    [Inline]
    public class H3DDict<T> : IPatriciaDict<T> where T : INamed
    {
        private readonly List<T> Values;
        private readonly H3DPatriciaTree NameTree;

        public H3DDict ()
        {
            Values = new List<T> ();
            NameTree = new H3DPatriciaTree ();
        }

        public T this [int Index] {
            get => Values[Index];
            set => Values[Index] = value;
        }

        public T this [string Name] {
            get => Values[NameTree.Find (Name)];
            set => Values[NameTree.Find (Name)] = value;
        }

        public bool IsReadOnly => false;

        public int Count => Values.Count;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public IEnumerator<T> GetEnumerator ()
        {
            return Values.GetEnumerator ();
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

        //List management methods
        public void Add (T Value)
        {
            Values.Add (Value);

            NameTree.Add (((INamed) Value).Name);

            OnCollectionChanged (NotifyCollectionChangedAction.Add, Value);
        }

        public void Insert (int Index, T Value)
        {
            Values.Insert (Index, Value);

            NameTree.Insert (Index, ((INamed) Value).Name);

            OnCollectionChanged (NotifyCollectionChangedAction.Replace, Value, Index);
        }

        public bool Remove (T Value)
        {
            var Removed = Values.Remove (Value);

            NameTree.Remove (((INamed) Value).Name);

            OnCollectionChanged (NotifyCollectionChangedAction.Remove, Value);

            return Removed;
        }

        public void Clear ()
        {
            Values.Clear ();

            NameTree.Clear ();

            OnCollectionChanged (NotifyCollectionChangedAction.Reset, default);
        }

        public int Find (string Name)
        {
            return NameTree.Find (Name);
        }

        public bool Contains (string Name)
        {
            return NameTree.Contains (Name);
        }

        public bool Contains (T Value)
        {
            return Values.Contains (Value);
        }

        public void CopyTo (T[] Array, int Index)
        {
            Values.CopyTo (Array, Index);
        }

        private void OnCollectionChanged (NotifyCollectionChangedAction Action, T NewItem, int Index = -1)
        {
            CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (Action, NewItem, Index));
        }

        public void Remove (int Index)
        {
            Remove (this[Index]);
        }

        public void Remove (string Name)
        {
            Remove (this[Name]);
        }
    }
}