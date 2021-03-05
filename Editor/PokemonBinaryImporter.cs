using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace P3DS2U.Editor
{
    public class PokemonBinaryImporter : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (importedAssets.Length > 0 || deletedAssets.Length > 0 || movedAssets.Length > 0 ||
                movedFromAssetPaths.Length > 0) {
                var newOrModifiedFiles = new List<string> ();
                newOrModifiedFiles.AddRange (importedAssets);
                newOrModifiedFiles.AddRange (deletedAssets);
                newOrModifiedFiles.AddRange (movedAssets);
                newOrModifiedFiles.AddRange (movedFromAssetPaths);
                OnCheckPostProcessBinary (newOrModifiedFiles);
            }
        }

        private static void OnCheckPostProcessBinary (IEnumerable<string> newOrModifiedFiles)
        {
            if (P3ds2USettingsScriptableObject.ImportInProgress && newOrModifiedFiles.Any (file => file.Contains (".bin") && P3ds2USettingsScriptableObject.Instance != null)) {
                P3ds2USettingsScriptableObject.Instance.RegeneratePreview ();
            }
        }
    }
}
