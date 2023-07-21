
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
        [SerializeField][Tooltip("AudioLink Prefab in your scene")]
        private AudioLink audioLink;
        [SerializeField][Tooltip("AudioLink Band used for AudioShrink")]
        private ALBand audioBand = ALBand.bass;
        [SerializeField][Range(0, 127)][Tooltip("Adds an extra delay to AudioShrink")]
        private int delay = 0;

        [Header("Scale Settings")]
        [SerializeField][Tooltip("Scale multiplier that is applied when nothing is happening on the set audio band")]
        private float defaultScale = 1.0f;
        [SerializeField][Tooltip("Scale multiplier that is applied when the set audio band is maxed out ")]
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

        private bool isEnabled = false;

        private bool aLBandMovementCompleted = true;
        private float aLBandMovementElapsedTime;
        [SerializeField]
        private float aLBandMovementDuration = 0.3f;
        private Vector3 aLBandMovementOldPos;
        private Vector3 alBandSelectorBassPos = new Vector3(-18f, 0f, 0f);
        private Vector3 alBandSelectorLowMidPos = new Vector3(-6f, 0f, 0f);
        private Vector3 alBandSelectorHighMidPos = new Vector3(6f, 0f, 0f);
        private Vector3 alBandSelectorTreblePos = new Vector3(18f, 0f, 0f);


        private void Start()
        {
            // Initial Setup
            SetEnabled(audioShrinkToggle.isOn, true);
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
        }
        // Enable / Disable button
        public void _AudioShrinkToggle()
        {
            SetEnabled(audioShrinkToggle.isOn, true);
        }

        // AudioLink Band buttons
        public void _ALBandBass()
        {
            SetAudioBand(ALBand.bass);
        }
        public void _ALBandLowMid()
        {
            SetAudioBand(ALBand.lowMid);
        }
        public void _ALBandHighMid()
        {
            SetAudioBand(ALBand.highMid);
        }
        public void _ALBandTreble()
        {
            SetAudioBand(ALBand.treble);
        }

        // Scale Sliders being moved
        public void _DefaultScaleMoved()
        {
            SetDefaultScale(defaultScaleSlider.value, true);
        }
        public void _MaxAmplitudeScaleMoved()
        {
            SetMaxAmplitudeScale(maxAmplitudeScaleSlider.value, true);
        }

        // Enabling / Disabling AudioShrink
        private void SetEnabled(bool enabled, bool skipToggleAdjustment)
        {
            isEnabled = enabled;
            audioLink.audioDataToggle = isEnabled;
            if (!skipToggleAdjustment)
            {
                audioShrinkToggle.enabled = isEnabled;
            }

        }

        // Setting the Audio Band
        private void SetAudioBand(ALBand setAudioBand)
        {

            if (audioBand != setAudioBand)
            {
                aLBandMovementCompleted = false;
                aLBandMovementOldPos = audioBandSelector.anchoredPosition3D;
            }
            audioBand = setAudioBand;
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
        private void MoveALBandSelector(Vector3 startPos, Vector3 endPos)
        {
            aLBandMovementElapsedTime += Time.deltaTime;
            float percentageComplete = aLBandMovementElapsedTime / aLBandMovementDuration;
            audioBandSelector.anchoredPosition3D = Vector3.Lerp(startPos, endPos, Mathf.SmoothStep(0, 1, percentageComplete));
            if (percentageComplete >= 1f)
            {
                aLBandMovementElapsedTime = 0f;
                aLBandMovementCompleted = true;
            }
        }
    }
    public enum ALBand
    {
        bass,
        lowMid,
        highMid,
        treble,
    }

}
