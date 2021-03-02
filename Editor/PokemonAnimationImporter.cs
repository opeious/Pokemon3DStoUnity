using UnityEditor;
using UnityEngine;

namespace P3DS2U.Editor
{
    public class PokemonAnimationImporter : AssetPostprocessor
    {
        public static bool IsEnabled = false;
        
        private void OnPostprocessAnimation (GameObject root, AnimationClip clip)
        {
            if (IsEnabled) {
                PokemonImporter.RaiseOnAnimationImportedEvent (clip);   
            }
        }
    }
}
