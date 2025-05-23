﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using Kirurobo;
using TMPro;

public class AvatarSettingsMenu : MonoBehaviour
{
    public GameObject menuPanel, uniWindowControllerObject, bloomObject, dayNightObject;
    public Button applyButton, resetButton, windowSizeButton;
    public Slider soundThresholdSlider, idleSwitchTimeSlider, idleTransitionTimeSlider,
                  avatarSizeSlider, fpsLimitSlider, petVolumeSlider, effectsVolumeSlider, menuVolumeSlider;
    public Toggle enableDancingToggle, enableMouseTrackingToggle, isTopmostToggle,
                  enableParticlesToggle, bloomToggle, dayNightToggle, enableWindowSittingToggle, enableDiscordRPCToggle;
    public TMP_Dropdown graphicsDropdown;
    public VRMLoader vrmLoader;
    public bool resetAlsoClearsAllowedApps = false;
    public List<AudioSource> petAudioSources = new(), effectsAudioSources = new(), menuAudioSources = new();
    public static bool IsMenuOpen { get; set; }
    public Slider headBlendSlider, spineBlendSlider, eyeBlendSlider;
    public Toggle enableHandHoldingToggle;
    public Slider hueShiftSlider;
    public Slider saturationSlider;

    private UniWindowController uniWindowController;
    private AvatarParticleHandler currentParticleHandler;
    public Button refreshAppsListButton;
    public Toggle ambientOcclusionToggle;
    public GameObject ambientOcclusionObject;
    public Toggle enableIKToggle;

    public Slider bigScreenSaverTimeoutSlider;
    public Toggle bigScreenSaverEnableToggle;
    public TMP_Text bigScreenSaverTimeoutLabel;

    private static readonly int[] TimeoutSteps = { 30, 60, 300, 900, 1800, 2700, 3600, 5400, 7200, 9000, 10800 };
    private static readonly string[] TimeoutLabels = {
    "30s", "1 min", "5 min", "15 min", "30 min", "45 min", "1 h", "1.5 h", "2 h", "2.5 h", "3 h"
};


    [System.Serializable]
    public class AccessoryToggleEntry
    {
        public string ruleName;
        public Toggle toggle;
    }

    public List<AccessoryToggleEntry> accessoryToggleBindings = new List<AccessoryToggleEntry>();

    private void Start()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
            IsMenuOpen = false;
        }

        windowSizeButton?.onClick.AddListener(CycleWindowSize);

        if (uniWindowControllerObject != null)
            uniWindowController = uniWindowControllerObject.GetComponent<UniWindowController>();
        else
            uniWindowController = FindFirstObjectByType<UniWindowController>();

        refreshAppsListButton?.onClick.AddListener(() =>
        {
            var appManager = FindFirstObjectByType<AllowedAppsManager>();
            if (appManager != null) appManager.RefreshUI();
        });

        var particleHandlers = FindObjectsByType<AvatarParticleHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        currentParticleHandler = particleHandlers.Length > 0 ? particleHandlers[0] : null;
        applyButton?.onClick.AddListener(ApplySettings);
        resetButton?.onClick.AddListener(ResetToDefaults);

        soundThresholdSlider?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.soundThreshold = v; SaveAll(); });
        idleSwitchTimeSlider?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.idleSwitchTime = v; SaveAll(); });
        idleTransitionTimeSlider?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.idleTransitionTime = v; SaveAll(); });
        avatarSizeSlider?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.avatarSize = v; SaveAll(); });
        fpsLimitSlider?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.fpsLimit = (int)v; ApplySettings(); SaveAll(); });
        petVolumeSlider?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.petVolume = v; UpdateAllCategoryVolumes(); SaveAll(); });
        effectsVolumeSlider?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.effectsVolume = v; UpdateAllCategoryVolumes(); SaveAll(); });
        menuVolumeSlider?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.menuVolume = v; UpdateAllCategoryVolumes(); SaveAll(); });

        enableDancingToggle?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.enableDancing = v; SaveAll(); });
        enableMouseTrackingToggle?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.enableMouseTracking = v; SaveAll(); });
        isTopmostToggle?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.isTopmost = v; ApplySettings(); SaveAll(); });
        enableParticlesToggle?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.enableParticles = v; ApplySettings(); SaveAll(); });
        bloomToggle?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.bloom = v; ApplySettings(); SaveAll(); });
        dayNightToggle?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.dayNight = v; ApplySettings(); SaveAll(); });
        enableWindowSittingToggle?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.enableWindowSitting = v; SaveAll(); });
        enableDiscordRPCToggle?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.enableDiscordRPC = v; SaveAll(); });

        headBlendSlider?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.headBlend = v; SaveAll(); });
        spineBlendSlider?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.spineBlend = v; SaveAll(); });
        enableHandHoldingToggle?.onValueChanged.AddListener(v => { SaveLoadHandler.Instance.data.enableHandHolding = v; SaveAll(); });

        bigScreenSaverTimeoutSlider?.onValueChanged.AddListener(OnBigScreenSaverTimeoutSliderChanged);
        bigScreenSaverEnableToggle?.onValueChanged.AddListener(OnBigScreenSaverEnableToggleChanged);


        hueShiftSlider?.onValueChanged.AddListener(v => {
            SaveLoadHandler.Instance.data.uiHueShift = v;
            var shifter = FindFirstObjectByType<MenuHueShift>();
            if (shifter != null) shifter.hueShift = v;
            SaveAll();
        });
        saturationSlider?.onValueChanged.AddListener(v => {
            SaveLoadHandler.Instance.data.uiSaturation = v;
            var shifter = FindFirstObjectByType<MenuHueShift>();
            if (shifter != null) shifter.saturation = v;
            SaveAll();
        });
        ambientOcclusionToggle?.onValueChanged.AddListener(v => {
            SaveLoadHandler.Instance.data.ambientOcclusion = v;
            ApplySettings();
            SaveAll();
        });
        graphicsDropdown?.onValueChanged.AddListener(i => {
            SaveLoadHandler.Instance.data.graphicsQualityLevel = i;
            QualitySettings.SetQualityLevel(i, true);
            SaveAll();
        });
        eyeBlendSlider?.onValueChanged.AddListener(v => {
            SaveLoadHandler.Instance.data.eyeBlend = v;
            SaveAll();
        });
        enableIKToggle?.onValueChanged.AddListener(v => {
            SaveLoadHandler.Instance.data.enableIK = v;
            SaveAll();
        });
        foreach (var entry in accessoryToggleBindings)
        {
            if (!string.IsNullOrEmpty(entry.ruleName) && entry.toggle != null)
            {
                string key = entry.ruleName;
                entry.toggle.onValueChanged.AddListener(v =>
                {
                    SaveLoadHandler.Instance.data.accessoryStates[key] = v;
                    foreach (var handler in AccessoiresHandler.ActiveHandlers)
                        foreach (var rule in handler.rules)
                            if (rule.ruleName == key) { rule.isEnabled = v; break; }
                    SaveAll();
                });
            }
        }
        if (graphicsDropdown != null)
        {
            graphicsDropdown.ClearOptions();
            graphicsDropdown.AddOptions(new List<string> {
            "ULTRA", "VERY HIGH", "HIGH", "NORMAL", "LOW"
        });

            graphicsDropdown.onValueChanged.AddListener((index) =>
            {
                QualitySettings.SetQualityLevel(index, true);
                SaveLoadHandler.Instance.data.graphicsQualityLevel = index;
                SaveLoadHandler.Instance.SaveToDisk();
            });

            graphicsDropdown.SetValueWithoutNotify(SaveLoadHandler.Instance.data.graphicsQualityLevel);
            QualitySettings.SetQualityLevel(SaveLoadHandler.Instance.data.graphicsQualityLevel, true);
        }

        LoadSettings(); ApplySettings(); RestoreWindowSize();

        var shifter = FindFirstObjectByType<MenuHueShift>();
        if (shifter != null)
        {
            shifter.hueShift = SaveLoadHandler.Instance.data.uiHueShift;
            shifter.saturation = SaveLoadHandler.Instance.data.uiSaturation;
        }

    }

    private void OnBigScreenSaverTimeoutSliderChanged(float v)
    {
        int idx = Mathf.Clamp(Mathf.RoundToInt(v), 0, TimeoutSteps.Length - 1);
        SaveLoadHandler.Instance.data.bigScreenScreenSaverTimeoutIndex = idx;
        if (bigScreenSaverTimeoutLabel != null)
            bigScreenSaverTimeoutLabel.text = TimeoutLabels[idx];
        SaveAll();
    }

    private void OnBigScreenSaverEnableToggleChanged(bool v)
    {
        SaveLoadHandler.Instance.data.bigScreenScreenSaverEnabled = v;
        SaveAll();
    }


    private void CycleWindowSize()
    {
        var data = SaveLoadHandler.Instance.data;
        var controller = uniWindowController ?? UniWindowController.current;

        switch (data.windowSizeState)
        {
            case SaveLoadHandler.SettingsData.WindowSizeState.Normal:
                data.windowSizeState = SaveLoadHandler.SettingsData.WindowSizeState.Big;
                controller.windowSize = new Vector2(2048, 1536); break;
            case SaveLoadHandler.SettingsData.WindowSizeState.Big:
                data.windowSizeState = SaveLoadHandler.SettingsData.WindowSizeState.Small;
                controller.windowSize = new Vector2(768, 512); break;
            case SaveLoadHandler.SettingsData.WindowSizeState.Small:
                data.windowSizeState = SaveLoadHandler.SettingsData.WindowSizeState.Normal;
                controller.windowSize = new Vector2(1536, 1024); break;
        }
        SaveLoadHandler.Instance.SaveToDisk();
    }

    private void SaveAll()
    {
        SaveLoadHandler.Instance.SaveToDisk();
        SaveLoadHandler.ApplyAllSettingsToAllAvatars();
    }

    public void LoadSettings()
    {
        foreach (var entry in accessoryToggleBindings)
            if (!string.IsNullOrEmpty(entry.ruleName) && entry.toggle != null &&
                SaveLoadHandler.Instance.data.accessoryStates.TryGetValue(entry.ruleName, out bool state))
                entry.toggle.SetIsOnWithoutNotify(state);

        var data = SaveLoadHandler.Instance.data;
        soundThresholdSlider?.SetValueWithoutNotify(data.soundThreshold);
        idleSwitchTimeSlider?.SetValueWithoutNotify(data.idleSwitchTime);
        idleTransitionTimeSlider?.SetValueWithoutNotify(data.idleTransitionTime);
        avatarSizeSlider?.SetValueWithoutNotify(data.avatarSize);
        fpsLimitSlider?.SetValueWithoutNotify(data.fpsLimit);
        enableDancingToggle?.SetIsOnWithoutNotify(data.enableDancing);
        enableMouseTrackingToggle?.SetIsOnWithoutNotify(data.enableMouseTracking);
        isTopmostToggle?.SetIsOnWithoutNotify(data.isTopmost);
        enableParticlesToggle?.SetIsOnWithoutNotify(data.enableParticles);
        bloomToggle?.SetIsOnWithoutNotify(data.bloom);
        dayNightToggle?.SetIsOnWithoutNotify(data.dayNight);
        petVolumeSlider?.SetValueWithoutNotify(data.petVolume);
        effectsVolumeSlider?.SetValueWithoutNotify(data.effectsVolume);
        menuVolumeSlider?.SetValueWithoutNotify(data.menuVolume);
        enableWindowSittingToggle?.SetIsOnWithoutNotify(data.enableWindowSitting);
        enableDiscordRPCToggle?.SetIsOnWithoutNotify(data.enableDiscordRPC);
        headBlendSlider?.SetValueWithoutNotify(data.headBlend);
        spineBlendSlider?.SetValueWithoutNotify(data.spineBlend);
        enableHandHoldingToggle?.SetIsOnWithoutNotify(data.enableHandHolding);
        hueShiftSlider?.SetValueWithoutNotify(SaveLoadHandler.Instance.data.uiHueShift);
        saturationSlider?.SetValueWithoutNotify(SaveLoadHandler.Instance.data.uiSaturation);
        ambientOcclusionToggle?.SetIsOnWithoutNotify(data.ambientOcclusion);
        eyeBlendSlider?.SetValueWithoutNotify(data.eyeBlend);
        enableIKToggle?.SetIsOnWithoutNotify(SaveLoadHandler.Instance.data.enableIK);

        if (graphicsDropdown != null)
        {
            graphicsDropdown.SetValueWithoutNotify(data.graphicsQualityLevel);
            QualitySettings.SetQualityLevel(data.graphicsQualityLevel, true);
        }

        if (bigScreenSaverTimeoutSlider != null)
        {
            bigScreenSaverTimeoutSlider.SetValueWithoutNotify(SaveLoadHandler.Instance.data.bigScreenScreenSaverTimeoutIndex);
            if (bigScreenSaverTimeoutLabel != null)
                bigScreenSaverTimeoutLabel.text = TimeoutLabels[SaveLoadHandler.Instance.data.bigScreenScreenSaverTimeoutIndex];
        }
        if (bigScreenSaverEnableToggle != null)
            bigScreenSaverEnableToggle.SetIsOnWithoutNotify(SaveLoadHandler.Instance.data.bigScreenScreenSaverEnabled);

        RestoreWindowSize();
    }
    public void ApplySettings()
    {
        var data = SaveLoadHandler.Instance.data;
        data.soundThreshold = soundThresholdSlider?.value ?? 0.2f;
        data.idleSwitchTime = idleSwitchTimeSlider?.value ?? 10f;
        data.idleTransitionTime = idleTransitionTimeSlider?.value ?? 1f;
        data.avatarSize = avatarSizeSlider?.value ?? 1.0f;
        data.fpsLimit = (int)(fpsLimitSlider?.value ?? 90);
        data.enableDancing = enableDancingToggle?.isOn ?? true;
        data.enableMouseTracking = enableMouseTrackingToggle?.isOn ?? true;
        data.isTopmost = isTopmostToggle?.isOn ?? true;
        data.enableParticles = enableParticlesToggle?.isOn ?? true;
        data.bloom = bloomToggle?.isOn ?? true;
        data.dayNight = dayNightToggle?.isOn ?? true;
        data.petVolume = petVolumeSlider?.value ?? 1f;
        data.effectsVolume = effectsVolumeSlider?.value ?? 1f;
        data.menuVolume = menuVolumeSlider?.value ?? 1f;
        data.enableWindowSitting = enableWindowSittingToggle?.isOn ?? false;
        data.enableDiscordRPC = enableDiscordRPCToggle?.isOn ?? true;
        data.headBlend = headBlendSlider?.value ?? 0.7f;
        data.spineBlend = spineBlendSlider?.value ?? 0.5f;
        data.enableHandHolding = enableHandHoldingToggle?.isOn ?? true;
        data.ambientOcclusion = ambientOcclusionToggle?.isOn ?? false;
        data.eyeBlend = eyeBlendSlider?.value ?? 1f;
        data.enableIK = enableIKToggle?.isOn ?? true;

        foreach (var entry in accessoryToggleBindings)
        {
            if (string.IsNullOrEmpty(entry.ruleName) || entry.toggle == null) continue;
            bool isOn = entry.toggle.isOn;
            SaveLoadHandler.Instance.data.accessoryStates[entry.ruleName] = isOn;
            foreach (var handler in AccessoiresHandler.ActiveHandlers)
                foreach (var rule in handler.rules)
                    if (rule.ruleName == entry.ruleName) { rule.isEnabled = isOn; break; }
        }

        if (graphicsDropdown != null)
        {
            data.graphicsQualityLevel = graphicsDropdown.value;
            QualitySettings.SetQualityLevel(graphicsDropdown.value, true);
        }

        if (bloomObject != null) bloomObject.SetActive(data.bloom);
        if (ambientOcclusionObject != null) ambientOcclusionObject.SetActive(data.ambientOcclusion);
        if (dayNightObject != null) dayNightObject.SetActive(data.dayNight);

        if (currentParticleHandler == null)
        {
            var particleHandlers = FindObjectsByType<AvatarParticleHandler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            currentParticleHandler = particleHandlers.Length > 0 ? particleHandlers[0] : null;
        }

        if (currentParticleHandler != null)
        {
            currentParticleHandler.featureEnabled = data.enableParticles;
            currentParticleHandler.enabled = data.enableParticles;
        }

        PetVoiceReactionHandler.GlobalHoverObjectsEnabled = data.enableParticles;


        if (uniWindowController != null) uniWindowController.isTopmost = data.isTopmost;

        foreach (var limiter in FindObjectsByType<FPSLimiter>(FindObjectsSortMode.None))
            limiter.SetFPSLimit(data.fpsLimit);

        UpdateAllCategoryVolumes();
        SaveLoadHandler.Instance.SaveToDisk();
        SaveLoadHandler.ApplyAllSettingsToAllAvatars();
        RestoreWindowSize();
    }
    private void RestoreWindowSize()
    {
        var data = SaveLoadHandler.Instance.data;
        var controller = uniWindowController ?? UniWindowController.current;
        switch (data.windowSizeState)
        {
            case SaveLoadHandler.SettingsData.WindowSizeState.Small: controller.windowSize = new Vector2(768, 512); break;
            case SaveLoadHandler.SettingsData.WindowSizeState.Big: controller.windowSize = new Vector2(2048, 1536); break;
            default: controller.windowSize = new Vector2(1536, 1024); break;
        }
    }
    public void ResetToDefaults()
    {
        var oldData = SaveLoadHandler.Instance.data;
        var newData = new SaveLoadHandler.SettingsData
        {
            windowSizeState = oldData.windowSizeState,
            modStates = new Dictionary<string, bool>(oldData.modStates),
            petVolume = 1f,
            effectsVolume = 1f,
            menuVolume = 1f,
            graphicsQualityLevel = 1,
            enableWindowSitting = false,
            accessoryStates = new Dictionary<string, bool>(),
            enableDiscordRPC = true,
            tutorialDone = oldData.tutorialDone,
            uiHueShift = 0f,
            uiSaturation = 0.5f
        };

        newData.ambientOcclusion = false;
        ambientOcclusionToggle?.SetIsOnWithoutNotify(false);
        enableDiscordRPCToggle?.SetIsOnWithoutNotify(true);

        if (!resetAlsoClearsAllowedApps)
            newData.allowedApps = new List<string>(oldData.allowedApps);

        foreach (var entry in accessoryToggleBindings)
            if (!string.IsNullOrEmpty(entry.ruleName))
                newData.accessoryStates[entry.ruleName] = false;

        SaveLoadHandler.Instance.data.bigScreenScreenSaverTimeoutIndex = 0; // 30s
        SaveLoadHandler.Instance.data.bigScreenScreenSaverEnabled = false;
        if (bigScreenSaverTimeoutSlider != null)
            bigScreenSaverTimeoutSlider.SetValueWithoutNotify(0);
        if (bigScreenSaverEnableToggle != null)
            bigScreenSaverEnableToggle.SetIsOnWithoutNotify(false);
        if (bigScreenSaverTimeoutLabel != null)
            bigScreenSaverTimeoutLabel.text = TimeoutLabels[0];


        SaveLoadHandler.Instance.data = newData;

        foreach (var handler in AccessoiresHandler.ActiveHandlers)
        {
            handler.ResetAccessoryStatesToDefault();
            handler.ClearAccessoryStatesFromSave();
        }

        SaveLoadHandler.Instance.SaveToDisk();
        LoadSettings();

        headBlendSlider?.SetValueWithoutNotify(0.7f);
        spineBlendSlider?.SetValueWithoutNotify(0.5f);
        eyeBlendSlider?.SetValueWithoutNotify(1.0f);
        newData.eyeBlend = 1.0f;
        newData.enableHandHolding = true;
        enableHandHoldingToggle?.SetIsOnWithoutNotify(true);
        newData.enableIK = true;
        enableIKToggle?.SetIsOnWithoutNotify(true);

        FindFirstObjectByType<AvatarScaleController>()?.SyncWithSlider();

        ApplySettings();

        var shifter = FindFirstObjectByType<MenuHueShift>();
        if (shifter != null)
        {
            shifter.hueShift = 0f;
            shifter.saturation = 0.5f;
        }


        if (vrmLoader != null) vrmLoader.ResetModel();
    }

    private void UpdateAllCategoryVolumes()
    {
        float petVolume = petVolumeSlider?.value ?? 1f, effectsVolume = effectsVolumeSlider?.value ?? 1f, menuVolume = menuVolumeSlider?.value ?? 1f;
        foreach (var src in petAudioSources) if (src != null) src.volume = GetBaseVolume(src) * petVolume;
        foreach (var src in effectsAudioSources) if (src != null) src.volume = GetBaseVolume(src) * effectsVolume;
        foreach (var src in menuAudioSources) if (src != null) src.volume = GetBaseVolume(src) * menuVolume;
    }

    private Dictionary<AudioSource, float> baseVolumes = new Dictionary<AudioSource, float>();

    private float GetBaseVolume(AudioSource src)
    {
        if (src == null) return 1f;
        if (!baseVolumes.TryGetValue(src, out float baseVol))
        {
            baseVol = src.volume;
            baseVolumes[src] = baseVol;
        }
        return baseVol;
    }
}