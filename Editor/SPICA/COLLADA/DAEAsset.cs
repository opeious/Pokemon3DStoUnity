using System;

namespace P3DS2U.Editor.SPICA.COLLADA
{
    public class DAEAsset
    {
        public DateTime created;
        public DateTime modified;

        public DAEAsset ()
        {
            created = DateTime.Now;
            modified = DateTime.Now;
        }
    }
}