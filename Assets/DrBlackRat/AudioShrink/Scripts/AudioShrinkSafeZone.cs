
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace DrBlackRat
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(100)]
    public class AudioShrinkSafeZone : UdonSharpBehaviour
    {
        [HideInInspector]
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

