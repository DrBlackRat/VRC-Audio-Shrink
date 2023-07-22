
using System.Globalization;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRCAudioLink;

namespace DrBlackRat
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class AudioShrink : UdonSharpBehaviour
    {
        [Header("Audio Shrink Settings")]
        [HideInInspector]
        public AudioLink audioLink;
        [Space(10)]
        [SerializeField][Tooltip("Sync Audio Band, Default Scale and Max Amplitude Scale over the network")]
        private bool syncSettings = true;
        [SerializeField][UdonSynced][Tooltip("AudioLink Band used for AudioShrink")]
        private ALBand audioBand = ALBand.bass;
        [SerializeField][Range(0, 127)][Tooltip("Adds an extra delay to AudioShrink")]
        private int delay = 0;

        [Header("Scale Settings")]
        [SerializeField][UdonSynced][Tooltip("Scale multiplier that is applied when nothing is happening on the set audio band")]
        private float defaultScale = 1.0f;
        [SerializeField][UdonSynced][Tooltip("Scale multiplier that is applied when the set audio band is maxed out ")]
        private float maxAmplitudeScale = 2.0f;

        [Header("Internals")]
        [SerializeField]
        private Toggle audioShrinkToggle;
        [SerializeField]
        private Slider defaultScaleSlider;
        [SerializeField]
        private TextMeshProUGUI defaultScaleText;
        [SerializeField]
        private Slider maxAmplitudeScaleSlider;
        [SerializeField]
        private TextMeshProUGUI maxAmplitudeScaleText;
        [SerializeField]
        private RectTransform audioBandSelector;

        private bool aLBandMovementCompleted = true;
        private float aLBandMovementElapsedTime;
        [SerializeField]
        private float aLBandMovementDuration = 0.3f;
        [SerializeField]
        private AnimationCurve aLBandMovementCurve;
        private Vector3 aLBandMovementOldPos;
        private Vector3 alBandSelectorBassPos = new Vector3(-18f, 0f, 0f);
        private Vector3 alBandSelectorLowMidPos = new Vector3(-6f, 0f, 0f);
        private Vector3 alBandSelectorHighMidPos = new Vector3(6f, 0f, 0f);
        private Vector3 alBandSelectorTreblePos = new Vector3(18f, 0f, 0f);

        [SerializeField]
        private GameObject popupMessage;
        [SerializeField]
        private TextMeshProUGUI popupText;
        private RectTransform popupMessageTransform;
        private bool popupMessageMovementCompleted = true;
        private float popupMessageElapsedTime;
        [SerializeField]
        private float popupMessageDuration = 0.3f;
        [SerializeField]
        private AnimationCurve popupMessageCurve;
        private Vector3 popupMessageOffPos = new Vector3(0f, -4.5f, 0f);
        private Vector3 popupMessageOnPos = new Vector3(0f, 4f, 0f);
#if UNITY_ANDROID
        private string safeZoneMessage = "You are currently inside a Safe Zone! \nStep out of it for AudioShrink to take effect.";
#else
        private string safeZoneMessage = "You are currently inside a Safe Zone! \r\nStep out of it for AudioShrink to take effect.";
#endif

        private bool isEnabled = false;
        private bool inSafeZone = false;

        private int audioBandInt;
        private int audioDataindex;

        private int safeZoneCount;


        private void Start()
        {
            // Grabbing some stuff
            popupMessageTransform = popupMessage.GetComponent<RectTransform>();
            // Initial Setup
            SetEnabled(false, false);
            SetAudioBand(audioBand);
            SetDefaultScale(defaultScale, false);
            SetMaxAmplitudeScale(maxAmplitudeScale, false);

        }
        private void Update()
        {
            // Audio Band Selector Animation
            if (!aLBandMovementCompleted)
            {
                switch (audioBand)
                {
                    case ALBand.bass:
                        MoveALBandSelector(aLBandMovementOldPos, alBandSelectorBassPos);
                        break;
                    case ALBand.lowMid:
                        MoveALBandSelector(aLBandMovementOldPos, alBandSelectorLowMidPos);
                        break;
                    case ALBand.highMid:
                        MoveALBandSelector(aLBandMovementOldPos, alBandSelectorHighMidPos);
                        break;
                    case ALBand.treble:
                        MoveALBandSelector(aLBandMovementOldPos, alBandSelectorTreblePos);
                        break;
                }
            }
            // Popup Message Animation
            if (!popupMessageMovementCompleted)
            {
                if (inSafeZone)
                {
                    MovePopupMessage(popupMessageOffPos, popupMessageOnPos, true);
                }
                else
                {
                    MovePopupMessage(popupMessageOnPos, popupMessageOffPos, false);
                }
            }

            // Scale Player
            if (isEnabled && !inSafeZone)
            {
                ScalePlayer();
            }
            else if (isEnabled && inSafeZone)
            {
#if !UNITY_EDITOR
                Networking.LocalPlayer.SetAvatarEyeHeightByMultiplier(1);
#endif
            }
        }
        // Networking
        public override void OnDeserialization()
        {
            if (!syncSettings) return;
            SetAudioBand(audioBand);
            SetDefaultScale(defaultScale, false);
            SetMaxAmplitudeScale(maxAmplitudeScale, false);
        }
        #region UI Events
        // Enable / Disable button
        public void _AudioShrinkToggle()
        {
            SetEnabled(audioShrinkToggle.isOn, true);
        }

        // Reset Button
        public void _ResetSettings()
        {
            if (audioBand != ALBand.bass) SetAudioBand(ALBand.bass);
            if (defaultScale != 1) SetDefaultScale(1, false);
            if (maxAmplitudeScale != 2) SetMaxAmplitudeScale(2, false);
            // Networking
            if (!syncSettings) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        // AudioLink Band buttons
        public void _ALBandBass()
        {
            if (audioBand == ALBand.bass) return;
            SetAudioBand(ALBand.bass);
            // Networking
            if (!syncSettings) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
        public void _ALBandLowMid()
        {
            if (audioBand == ALBand.lowMid) return;
            SetAudioBand(ALBand.lowMid);
            // Networking
            if (!syncSettings) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
        public void _ALBandHighMid()
        {
            if (audioBand == ALBand.highMid) return;
            SetAudioBand(ALBand.highMid);
            // Networking
            if (!syncSettings) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
        public void _ALBandTreble()
        {
            if (audioBand == ALBand.treble) return;
            SetAudioBand(ALBand.treble);
            // Networking
            if (!syncSettings) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }

        // Scale Sliders being moved
        public void _DefaultScaleMoved()
        {
            float sliderValue = defaultScaleSlider.value;
            if (defaultScale == sliderValue) return;
            SetDefaultScale(sliderValue, true);
            // Networking
            if (!syncSettings) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
        public void _MaxAmplitudeScaleMoved()
        {
            float sliderValue = maxAmplitudeScaleSlider.value;
            if (maxAmplitudeScale == sliderValue) return;
            SetMaxAmplitudeScale(sliderValue, true);
            // Networking
            if (!syncSettings) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
        #endregion
        #region Safe Zone Events
        // Safe Zone Trigger / Respawn etc
        public void _AudioShrinkOnPlayerSafeZoneEnter()
        {
            safeZoneCount += 1;
            UpdateSafeZone();
        }
        public void _AudioShrinkOnPlayerSafeZoneExit()
        {
            safeZoneCount -= 1;
            UpdateSafeZone();
        }
        // Enabling / Disabling SafeZoneMode
        private void UpdateSafeZone()
        {
            if (safeZoneCount > 0)
            {
                if (inSafeZone) return;
                inSafeZone = true;
                // Set Text & Tirgger Animation
                popupText.text = safeZoneMessage;
                popupMessageElapsedTime = 0f;
                popupMessageMovementCompleted = false;
            }
            else
            {
                if (!inSafeZone) return;
                inSafeZone = false;
                // trigger Animation
                popupMessageElapsedTime = 0f;
                popupMessageMovementCompleted = false;
            }

        }
        #endregion
        #region Setting Variables & According Settings
        // Enabling / Disabling AudioShrink
        private void SetEnabled(bool enabled, bool skipToggleAdjustment)
        {
            isEnabled = enabled;
            // AudioLink Setup
            audioLink.audioDataToggle = isEnabled;
            if (isEnabled)
            {
                audioLink.EnableReadback();
            }
            else
            {
                audioLink.DisableReadback();
            }
            // Enabling / Disabling Manual Scalling
#if !UNITY_EDITOR
            Networking.LocalPlayer.SetManualAvatarScalingAllowed(!isEnabled);
#endif
            // Turning the toggle on / off
            if (!skipToggleAdjustment)
            {
                audioShrinkToggle.SetIsOnWithoutNotify(isEnabled);
            }
        }

        // Setting the Audio Band
        private void SetAudioBand(ALBand setAudioBand)
        {
            audioBand = setAudioBand;
            // Selector Movement Settings
            aLBandMovementElapsedTime = 0f;
            aLBandMovementCompleted = false;
            aLBandMovementOldPos = audioBandSelector.anchoredPosition3D;
            // Setting band
            switch (audioBand)
            {
                case ALBand.bass:
                    audioBandInt = 0;
                    break;
                case ALBand.lowMid:
                    audioBandInt = 1;
                    break;
                case ALBand.highMid:
                    audioBandInt = 3;
                    break;
                case ALBand.treble:
                    audioBandInt = 4;
                    break;
            }
            // calculating new auidoDataindex
            audioDataindex = (audioBandInt * 128) + delay;
        }

        // Setting Scale Values & Slider / Text
        private void SetDefaultScale(float newValue, bool skipSliderAdjustment)
        {
            defaultScale = newValue;
            defaultScaleText.text = defaultScale.ToString("F2", CultureInfo.InvariantCulture);
            if (!skipSliderAdjustment)
            {
                defaultScaleSlider.value = defaultScale;
            }

        }
        private void SetMaxAmplitudeScale(float newValue, bool skipSliderAdjustment)
        {
            maxAmplitudeScale = newValue;
            maxAmplitudeScaleText.text = maxAmplitudeScale.ToString("F2", CultureInfo.InvariantCulture);
            if (!skipSliderAdjustment)
            {
                maxAmplitudeScaleSlider.value = maxAmplitudeScale;
            }
        }
        #endregion

        #region UI Animations
        // Audio Band Selector Animation
        private void MoveALBandSelector(Vector3 startPos, Vector3 endPos)
        {
            aLBandMovementElapsedTime += Time.deltaTime;
            float percentageComplete = aLBandMovementElapsedTime / aLBandMovementDuration;
            audioBandSelector.anchoredPosition3D = Vector3.Lerp(startPos, endPos, aLBandMovementCurve.Evaluate(percentageComplete));
            if (percentageComplete >= 1f)
            {
                aLBandMovementElapsedTime = 0f;
                aLBandMovementCompleted = true;
            }
        }
        // Popup Message Animation
        private void MovePopupMessage(Vector3 startPos, Vector3 endPos, bool showUIPanel)
        {
            if (showUIPanel) popupMessage.SetActive(true);
            popupMessageElapsedTime += Time.deltaTime;
            float percentageComplete = popupMessageElapsedTime / popupMessageDuration;
            popupMessageTransform.anchoredPosition3D = Vector3.Lerp(startPos, endPos, popupMessageCurve.Evaluate(percentageComplete));
            if (percentageComplete >= 1)
            {
                popupMessageElapsedTime = 0f;
                popupMessageMovementCompleted = true;
                if (!showUIPanel)
                {
                    popupText.text = "";
                    popupMessage.SetActive(false);
                }
            }
        }
        #endregion
        #region Scaling Player
        private void ScalePlayer()
        {
            Color[] audioData = audioLink.audioData;
            if (audioData.Length == 0) return;
            float amplitude = audioData[audioDataindex].grayscale;
#if !UNITY_EDITOR
            Networking.LocalPlayer.SetAvatarEyeHeightByMultiplier(Mathf.Lerp(defaultScale, maxAmplitudeScale, amplitude));
#endif
        }
        #endregion
    }
    public enum ALBand
    {
        bass,
        lowMid,
        highMid,
        treble,
    }

}
