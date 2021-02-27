namespace P3DS2U.Editor.SPICA
{
    internal interface IPatriciaTreeNode
    {
        uint ReferenceBit { get; set; }
        ushort LeftNodeIndex { get; set; }
        ushort RightNodeIndex { get; set; }
        string Name { get; set; }
    }
}