using System.Collections.Generic;
using System.Collections.Specialized;

namespace P3DS2U.Editor.SPICA
{
    public interface IPatriciaDict<T> : INotifyCollectionChanged, ICollection<T>, INameIndexed
    {
        T this [int Index] { get; set; }
        T this [string Name] { get; set; }

        bool Contains (string Name);

        void Insert (int Index, T Value);
    }
}