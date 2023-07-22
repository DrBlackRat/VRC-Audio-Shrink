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
            if (audioShrink != null && audioShrink.audioLink == null)
            {
                audioShrink.audioLink = Object.FindObjectOfType<AudioLink>();
            }
        }
    }
}

