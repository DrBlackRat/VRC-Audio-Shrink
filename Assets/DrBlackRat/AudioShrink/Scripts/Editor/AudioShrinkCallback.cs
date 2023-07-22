using System.Collections;
using System.Collections.Generic;
using UnityEditor.Callbacks;
using UnityEngine;
using VRCAudioLink;

namespace DrBlackRat
{
    public static class AudioShrinkCallback
    {
        [PostProcessScene(-100)]
        public static void OnPostProcessScene()
        {
            AudioShrink audioShrink = Object.FindObjectOfType<AudioShrink>();
            if (audioShrink != null)
            {
                audioShrink.audioLink = Object.FindObjectOfType<AudioLink>();
                AudioShrinkSafeZone[] audioShrinkSafeZones = Object.FindObjectsOfType<AudioShrinkSafeZone>();
                foreach (AudioShrinkSafeZone audioShrinkSafeZone in audioShrinkSafeZones) 
                {
                    if (audioShrinkSafeZone.audioShrink != null) return;
                    audioShrinkSafeZone.audioShrink = audioShrink;
                }
            }

        }
    }
}

