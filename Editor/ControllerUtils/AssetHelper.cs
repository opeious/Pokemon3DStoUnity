using UnityEditor;
using UnityEngine;

namespace P3DS2U
{
    /// <summary>
    /// Asset helper. By giacomelli.
    /// </summary>
    public static class AssetHelper
    {
        /// <summary>
        /// Gets the asset with the specified name from the given path.
        /// </summary>
        /// <returns>The asset.</returns>
        /// <param name="name">Name.</param>
        /// <typeparam name="TAsset">The 1st type parameter.</typeparam>
        public static TAsset GetAsset<TAsset>(string name, string path)
        where TAsset : Object, new()
        {
            return AssetDatabase.LoadAssetAtPath<TAsset>(path) ?? new TAsset { name = name };
        }

        /// <summary>
        /// Saves the asset to the given path.
        /// </summary>
        /// <param name="asset">Asset.</param>
        public static void SaveAsset(this Object asset, string path)
        {
            var existingAsset = AssetDatabase.LoadAssetAtPath(path, asset.GetType());

            if (existingAsset == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                Debug.Log($"'{path}' created.");
            }
            else
                Debug.Log($"'{path}' updated.");
        }

        private static string GetAssetExtension(System.Type assetType)
        {
            if (assetType == typeof(AnimationClip))
                return ".anim";

            if (assetType == typeof(AnimatorOverrideController))
                return ".overrideController";

            Debug.LogWarning($"Returning .asset has extension for {assetType}.");

            return ".asset";
        }
    }
}