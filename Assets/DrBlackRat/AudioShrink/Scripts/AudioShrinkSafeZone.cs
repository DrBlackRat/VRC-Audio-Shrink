
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace DrBlackRat
{
    public class AudioShrinkSafeZone : UdonSharpBehaviour
    {
        public AudioShrink audioShrink;
        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!player.isLocal) return;
            audioShrink._AudioShrinkOnPlayerSafeZoneEnter();
        }
        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!player.isLocal) return;
            audioShrink._AudioShrinkOnPlayerSafeZoneExit();
        }
    }
}

