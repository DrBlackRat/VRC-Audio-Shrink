﻿
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
        [Space(10)]
        [SerializeField][Tooltip("Sync Audio Band, Default Scale and Max Amplitude Scale over the network")]
        private bool syncSettings = false;
        [SerializeField][Tooltip("Disable & Lock AudioShrink for everyone, the master can change it later")]
        private bool masterDisable = false;
        [SerializeField][Tooltip("Only allow the master to change the Settings, will enable Sync Settings")]
        private bool masterControl = false;

        [Header("AudioLink Settings")]
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

        // Audio Band UI stuff
        private bool aLBandMovementCompleted = true;
        private float aLBandMovementElapsedTime = 0f;
        [SerializeField]
        private float aLBandMovementDuration = 0.3f;
        [SerializeField]
        private AnimationCurve aLBandMovementCurve;
        private Vector3 aLBandMovementOldPos;
        private Vector3 alBandSelectorBassPos = new Vector3(-18f, 0f, 0f);
        private Vector3 alBandSelectorLowMidPos = new Vector3(-6f, 0f, 0f);
        private Vector3 alBandSelectorHighMidPos = new Vector3(6f, 0f, 0f);
        private Vector3 alBandSelectorTreblePos = new Vector3(18f, 0f, 0f);

        // Safe Zone stuff
        [SerializeField]
        private GameObject safeZoneMessageObj;
        [SerializeField]
        private TextMeshProUGUI safeZoneText;
        private RectTransform safeZoneMessageTransform;
        private bool safeZoneMessageMovementCompleted = true;
        private float safeZoneMessageElapsedTime = 0f;
        [SerializeField]
        private float safeZoneMessageDuration = 0.3f;
        [SerializeField]
        private AnimationCurve safeZoneMessageCurve;
        private Vector3 safeZoneMessageOffPos = new Vector3(0f, -4.5f, 0f);
        private Vector3 safeZoneMessageOnPos = new Vector3(0f, 4f, 0f);
#if UNITY_ANDROID
        private string safeZoneMessage = "You are currently inside a Safe Zone! \nStep out of it for AudioShrink to take effect.";
#else
        private string safeZoneMessage = "You are currently inside a Safe Zone! \r\nStep out of it for AudioShrink to take effect.";
#endif
        private bool isEnabled = false;
        private bool inSafeZone = false;
        private int safeZoneCount = 0;
        private bool showSafeZonePopup = false;

        // Internal Master Stuff
        private bool isMaster = false;
        private string masterName = "";
        [SerializeField]
        private GameObject toggleLockObj;
        [SerializeField]
        private GameObject settingsLockObj;
        [SerializeField]
        private GameObject masterSettingsCoverObj;
        [SerializeField]
        private TextMeshProUGUI currnetMasterText;
        [SerializeField]
        private TextMeshProUGUI localSyncSettingsText;
        [SerializeField]
        private Toggle masterDisableToggle;
        [SerializeField]
        private Toggle masterControlToggle;
        [SerializeField]
        private Toggle syncSettingsToggle;

        [HideInInspector]
        public AudioLink audioLink;
        private int audioDataindex = 0;
        private bool audioDataEnabledDefault = false;

        // Synced Variables
        [UdonSynced] private bool netSyncSettings;
        [UdonSynced] private bool netMasterDisable;
        [UdonSynced] private bool netMasterControl;
        [UdonSynced] private string netMasterName;
        [UdonSynced] private ALBand netAudioBand;
        [UdonSynced] private float netDefaultScale;
        [UdonSynced] private float netMaxAmplitudeScale;

        private void Start()
        {
            // Grabbing some stuff
            audioDataEnabledDefault = audioLink.audioDataToggle;
            safeZoneMessageTransform = safeZoneMessageObj.GetComponent<RectTransform>();
            // Inistal Synced Variable set
            netSyncSettings = syncSettings;
            netMasterDisable = masterDisable;
            netMasterControl = masterControl;
            netMasterName = masterName;
            netAudioBand = audioBand;
            netDefaultScale = defaultScale;
            netMaxAmplitudeScale = maxAmplitudeScale;
            // Initial Setup
            CheckMaster();
            SetSyncSettings(syncSettings, false);
            SetMasterDisable(masterDisable, false);
            SetMasterControl(masterControl, false);
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
            // safeZone Message Animation
            if (!safeZoneMessageMovementCompleted)
            {
                if (showSafeZonePopup)
                {
                    MoveSafeZoneMessage(safeZoneMessageOffPos, safeZoneMessageOnPos, true);
                }
                else
                {
                    MoveSafeZoneMessage(safeZoneMessageOnPos, safeZoneMessageOffPos, false);
                }
            }

            // Scale Player
            if (isEnabled && !inSafeZone)
            {
                ScalePlayer();
            }
        }
        #region Networking
        private void SendNetworkData()
        {
            if (!syncSettings && !isMaster) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
        }
        public override void OnDeserialization()
        {
            SetMaster(netMasterName);
            SetMasterDisable(netMasterDisable, false);
            SetMasterControl(netMasterControl, false);
            SetSyncSettings(netSyncSettings, false);
            if (!syncSettings) return;
            SetAudioBand(netAudioBand);
            SetDefaultScale(netDefaultScale, false);
            SetMaxAmplitudeScale(netMaxAmplitudeScale, false);
        }
        #endregion
        #region UI Events
        // Enable / Disable button
        public void _AudioShrinkToggle()
        {
            bool enabled = audioShrinkToggle.isOn;
            if (isEnabled == enabled || masterDisable) return;
            SetEnabled(enabled, true);
        }

        // Reset Button
        public void _ResetSettings()
        {
            if (!isMaster && masterControl) return;
            SetAudioBand(ALBand.bass);
            SetDefaultScale(1, false);
            SetMaxAmplitudeScale(2, false);
            SendNetworkData();
        }

        // AudioLink Band buttons
        public void _ALBandBass()
        {
            if (audioBand == ALBand.bass || !isMaster && masterControl) return;
            SetAudioBand(ALBand.bass);
            SendNetworkData();

        }
        public void _ALBandLowMid()
        {
            if (audioBand == ALBand.lowMid || !isMaster && masterControl) return;
            SetAudioBand(ALBand.lowMid);
            SendNetworkData();
        }
        public void _ALBandHighMid()
        {
            if (audioBand == ALBand.highMid || !isMaster && masterControl) return;
            SetAudioBand(ALBand.highMid);
            SendNetworkData();
        }
        public void _ALBandTreble()
        {
            if (audioBand == ALBand.treble || !isMaster && masterControl) return;
            SetAudioBand(ALBand.treble);
            SendNetworkData();
        }

        // Scale Sliders being moved
        public void _DefaultScaleMoved()
        {
            float sliderValue = defaultScaleSlider.value;
            if (defaultScale == sliderValue || !isMaster && masterControl) return;
            SetDefaultScale(sliderValue, true);
            SendNetworkData();
        }
        public void _MaxAmplitudeScaleMoved()
        {
            float sliderValue = maxAmplitudeScaleSlider.value;
            if (maxAmplitudeScale == sliderValue || !isMaster && masterControl) return;
            SetMaxAmplitudeScale(sliderValue, true);
            SendNetworkData();
        }
        // Master Toggles
        public void _MasterDisableToggle()
        {
            bool disable = masterDisableToggle.isOn;
            if (masterDisable == disable || !isMaster) return;
            SetMasterDisable(disable, true);
            SendNetworkData();
        }
        public void _MasterControlToggle()
        {
            bool toggle = masterControlToggle.isOn;
            if (masterControl == toggle || !isMaster) return;
            SetMasterControl(toggle, true);
            SendNetworkData();
        }
        public void _SyncSettingsToggle()
        {
            bool sync = syncSettingsToggle.isOn;
            if (syncSettings == sync || !isMaster) return;
            SetSyncSettings(sync, true);
            // Set Synced Variables
            netAudioBand = audioBand;
            netDefaultScale = defaultScale;
            netMaxAmplitudeScale = maxAmplitudeScale;
            SendNetworkData();
        }
        #endregion
        #region Setting Variables & According Settings
        // Enabling / Disabling AudioShrink
        private void SetEnabled(bool enabled, bool skipToggleAdjustment)
        {
            isEnabled = enabled;
            // AudioLink Setup
            if (!audioDataEnabledDefault)
            {
                audioLink.audioDataToggle = isEnabled;
                if (isEnabled)
                {
                    audioLink.EnableReadback();
                }
                else
                {
                    audioLink.DisableReadback();
                }
            }
            // Enable / Disable Manual Scaling
            SetManualScaling();
            // Turning the toggle on / off
            if (!skipToggleAdjustment) audioShrinkToggle.SetIsOnWithoutNotify(isEnabled);
            // Hide / Show SafeZone Text
            SafeZonePopup();
        }

        // Setting the Audio Band
        private void SetAudioBand(ALBand setAudioBand)
        {
            audioBand = setAudioBand;
            // Setting Synced Variable
            if (syncSettings) netAudioBand = audioBand;
            // Selector Movement Settings
            aLBandMovementElapsedTime = 0f;
            aLBandMovementCompleted = false;
            aLBandMovementOldPos = audioBandSelector.anchoredPosition3D;
            // calculating new auidoDataindex
            audioDataindex = ((int)audioBand * 128) + delay;
        }

        // Setting Scale Values & Slider / Text
        private void SetDefaultScale(float newValue, bool skipSliderAdjustment)
        {
            defaultScale = newValue;
            // Setting Synced Variable
            if (syncSettings) netDefaultScale = defaultScale;

            defaultScaleText.text = defaultScale.ToString("F2", CultureInfo.InvariantCulture);
            if (!skipSliderAdjustment) defaultScaleSlider.value = defaultScale;
        }
        private void SetMaxAmplitudeScale(float newValue, bool skipSliderAdjustment)
        {
            maxAmplitudeScale = newValue;
            // Setting Synced Variable
            if (syncSettings) netMaxAmplitudeScale = maxAmplitudeScale;

            maxAmplitudeScaleText.text = maxAmplitudeScale.ToString("F2", CultureInfo.InvariantCulture);
            if (!skipSliderAdjustment) maxAmplitudeScaleSlider.value = maxAmplitudeScale;
        }

        private void SetManualScaling()
        {
#if !UNITY_EDITOR
            if (isEnabled && !inSafeZone)
            {
                Networking.LocalPlayer.SetManualAvatarScalingAllowed(false);
            }
            else 
            {
               Networking.LocalPlayer.SetAvatarEyeHeightByMultiplier(1);
               Networking.LocalPlayer.SetManualAvatarScalingAllowed(true);
            }
#endif
        }
        #endregion
        #region Master Settings
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            CheckMaster();
        }
        // Check if Master
        private void CheckMaster()
        {
            if (!Networking.IsMaster) return;
            isMaster = true;
            string name = Networking.LocalPlayer.displayName;
            SetMaster(name);
            SendNetworkData();
        }
        // Set Master
        private void SetMaster(string name)
        {
            // Set Master Name
            masterName = name;
            netMasterName = masterName;
            currnetMasterText.text = $"Master: {masterName}";
            // Cover Master Settings
            masterSettingsCoverObj.SetActive(!isMaster);
            // Uncover Normal Settings
            if (masterControl && isMaster) settingsLockObj.SetActive(false);
        }
        // Change Master Settings
        private void SetMasterDisable(bool disable, bool skipToggleAdjustment)
        {
            masterDisable = disable;
            netMasterDisable = masterDisable;
            toggleLockObj.SetActive(masterDisable);
            // Disable AudioShrink
            if (masterDisable) SetEnabled(false, false);
            // Adjust Toggle
            if (!skipToggleAdjustment) masterDisableToggle.SetIsOnWithoutNotify(masterDisable);
        }
        private void SetMasterControl(bool control, bool skipToggleAdjustment)
        {
            masterControl = control;
            netMasterControl = masterControl;
            if (isMaster)
            {
                settingsLockObj.SetActive(false);
            }
            else
            {
                settingsLockObj.SetActive(masterControl);
            }
            // Enabling Sync Settings
            if (masterControl)
            {
                SetSyncSettings(true, false);
                syncSettingsToggle.interactable = false;
            }
            else
            {
                syncSettingsToggle.interactable = true;
            }
            // Adjust Toggle
            if (!skipToggleAdjustment) masterControlToggle.SetIsOnWithoutNotify(masterControl);
        }
        private void SetSyncSettings(bool sync, bool skipToggleAdjustment)
        {
            syncSettings = sync;
            netSyncSettings = syncSettings;
            if (syncSettings)
            {
                localSyncSettingsText.text = "Synced";
            }
            else
            {
                localSyncSettingsText.text = "Local";
            }
            // Adjust Toggle
            if (!skipToggleAdjustment) syncSettingsToggle.SetIsOnWithoutNotify(syncSettings);
        }
        #endregion
        #region Safe Zone
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
                SafeZonePopup();
                // Enable / Disable Manual Scaling
                SetManualScaling();
            }
            else
            {
                if (!inSafeZone) return;
                inSafeZone = false;
                SafeZonePopup();
                // Enable / Disable Manual Scaling
                SetManualScaling();
            }
        }
        private void SafeZonePopup()
        {
            if (inSafeZone && isEnabled)
            {
                // Set Text & Tirgger Animation
                showSafeZonePopup = true;
                safeZoneText.text = safeZoneMessage;
                safeZoneMessageElapsedTime = 0f;
                safeZoneMessageMovementCompleted = false;
            }
            else if (!inSafeZone || !isEnabled && inSafeZone)
            {
                // trigger Animation
                showSafeZonePopup = false;
                safeZoneMessageElapsedTime = 0f;
                safeZoneMessageMovementCompleted = false;
            }
        }

        #endregion
        #region UI Animations
        // Audio Band Selector Animation
        private void MoveALBandSelector(Vector3 startPos, Vector3 endPos)
        {
            if (startPos == endPos)
            {
                aLBandMovementElapsedTime = 0f;
                aLBandMovementCompleted = true;
                return;
            }
            aLBandMovementElapsedTime += Time.deltaTime;
            float percentageComplete = aLBandMovementElapsedTime / aLBandMovementDuration;
            audioBandSelector.anchoredPosition3D = Vector3.Lerp(startPos, endPos, aLBandMovementCurve.Evaluate(percentageComplete));
            if (percentageComplete >= 1f)
            {
                aLBandMovementElapsedTime = 0f;
                aLBandMovementCompleted = true;
            }
        }
        // safeZone Message Animation
        private void MoveSafeZoneMessage(Vector3 startPos, Vector3 endPos, bool showUIPanel)
        {
            if (showUIPanel) safeZoneMessageObj.SetActive(true);
            safeZoneMessageElapsedTime += Time.deltaTime;
            float percentageComplete = safeZoneMessageElapsedTime / safeZoneMessageDuration;
            safeZoneMessageTransform.anchoredPosition3D = Vector3.Lerp(startPos, endPos, safeZoneMessageCurve.Evaluate(percentageComplete));
            if (percentageComplete >= 1)
            {
                safeZoneMessageElapsedTime = 0f;
                safeZoneMessageMovementCompleted = true;
                if (!showUIPanel)
                {
                    safeZoneText.text = "";
                    safeZoneMessageObj.SetActive(false);
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
