using UnityEngine;
using System.Collections.Generic;

namespace P3DS2U
{
    /// <summary>
    /// Animator override controller extensions. By giacomelli.
    /// </summary>
    public static class AnimatorOverrideControllerExtensions
    {
        /// <summary>
        /// Apply the animation clip overrides betweem fromClips and toClips.
        /// </summary>
        /// <param name="animatorOverride">Animator override.</param>
        /// <param name="fromClips">From clips.</param>
        /// <param name="toClips">To clips.</param>
        public static void ApplyOverrides(this AnimatorOverrideController animatorOverride, AnimationClip[] fromClips, AnimationClip[] toClips)
        {
            if (fromClips.Length != toClips.Length)
                Debug.LogError($"fromClips and toClips should have the same number of elements");

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

            for (int i = 0; i < toClips.Length; i++)
            {
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(fromClips[i], toClips[i]));
            }

            animatorOverride.ApplyOverrides(overrides);
        }

        public static void ApplyOverride(this AnimatorOverrideController animatorOverride, AnimationClip fromClip, AnimationClip toClip)
        {
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(fromClip, toClip));

            animatorOverride.ApplyOverrides(overrides);
        }
    }
}