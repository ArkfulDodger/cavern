using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Experimental.Rendering.Universal;

public class PlayerLight : MonoBehaviour
{
    [Serializable]
    public struct PointLightSetting
    {
        public float innerRadius;
        public float outerRadius;
        public float intensity;
    }
    [Serializable]
    public struct LightSettingPreset
    {
        public int cellRequirement;
        public PointLightSetting faceLightSettings;
        public PointLightSetting foregroundLightSettings;
        public PointLightSetting backgroundLightSettings;
    }
    public Light2D faceLight;
    public Light2D foregroundLight;
    public Light2D backgroundLight;
    public Light2D finalLight;
    [SerializeField] List<LightSettingPreset> lightPresets = new List<LightSettingPreset>();
    public LightSettingPreset currentLightPreset;
    public LightSettingPreset transformationOut;
    public LightSettingPreset transformationIn;

    public float facePulseIntensity = 2;
    public float facePulseTime = 0.25f;
    public float pauseTime = 0.25f;
    public float glowPulseIntensityMod = 2f;
    public float glowPulseTime = 0.7f;
    AudioSource audioSource;
    [SerializeField] AudioClip standardAbsorbSound;
    [SerializeField] AudioClip upgradeAbsorbSound;

    private void Awake() {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable() {
        EventBroker.CellAbsorbed += CellAbsorbed;
    }

    private void OnDisable() {
        EventBroker.CellAbsorbed -= CellAbsorbed;
    }

    // Start is called before the first frame update
    void Start()
    {
        currentLightPreset = GetCurrentRequiredPreset();
        SetLightsToPreset(currentLightPreset);
        UpdateTransformationInPreset();

        if (GameManager.instance.cellCount == GameManager.instance.maxCells)
            finalLight.gameObject.SetActive(true);
        else
            finalLight.gameObject.SetActive(false);
    }

    void CellAbsorbed()
    {
        if (LightUpdateNeeded(out LightSettingPreset newPreset))
        {
            if (GameManager.instance.cellCount < GameManager.instance.maxCells)
            {
                Notifications.text = "GLOW Increased";
                EventBroker.NoticeCall();
            }
            StartCoroutine(ChangeLightsTo(newPreset));
        }
        else
            StartCoroutine(PulseFaceLight());
    }

    IEnumerator PulseFaceLight()
    {
        audioSource.Stop();
        audioSource.clip = standardAbsorbSound;
        audioSource.Play();

        float startIntensity = faceLight.intensity;

        float timer = 0;
        while (timer < facePulseTime)
        {
            faceLight.intensity = Mathf.Lerp(startIntensity, facePulseIntensity, timer/facePulseTime);
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0;
        while (timer < facePulseTime)
        {
            faceLight.intensity = Mathf.Lerp(facePulseIntensity, startIntensity, timer/facePulseTime);
            timer += Time.deltaTime;
            yield return null;
        }
        faceLight.intensity = startIntensity;
    }

    bool LightUpdateNeeded(out LightSettingPreset newPreset)
    {
        newPreset = GetCurrentRequiredPreset();

        if (currentLightPreset.cellRequirement == newPreset.cellRequirement)
            return false;
        else
            return true;

    }

    LightSettingPreset GetCurrentRequiredPreset()
    {
        int cellCount = GameManager.instance.cellCount;
        LightSettingPreset tempLightPreset = lightPresets[0];

        foreach (LightSettingPreset lightPreset in lightPresets)
        {
            if (cellCount >= lightPreset.cellRequirement)
                tempLightPreset = lightPreset;
            else
                break;
        }

        return tempLightPreset;
    }

    void SetLightsToPreset(LightSettingPreset preset)
    {
        SetLightToSettings(faceLight, preset.faceLightSettings);
        SetLightToSettings(foregroundLight, preset.foregroundLightSettings);
        SetLightToSettings(backgroundLight, preset.backgroundLightSettings);
    }

    public void SetLightToSettings(Light2D light, PointLightSetting setting)
    {
        light.pointLightInnerRadius = setting.innerRadius;
        light.pointLightOuterRadius = setting.outerRadius;
        light.intensity = setting.intensity;
    }

    IEnumerator ChangeLightsTo(LightSettingPreset newPreset)
    {
        audioSource.Stop();
        audioSource.clip = upgradeAbsorbSound;
        audioSource.Play();

        float timer = 0;
        while(timer < glowPulseTime)
        {
            float lerpVal = timer/glowPulseTime;
            LerpLightValues(faceLight, currentLightPreset.faceLightSettings, newPreset.faceLightSettings, lerpVal, true);
            LerpLightValues(foregroundLight, currentLightPreset.foregroundLightSettings, newPreset.foregroundLightSettings, lerpVal, true);
            LerpLightValues(backgroundLight, currentLightPreset.backgroundLightSettings, newPreset.backgroundLightSettings, lerpVal, true);

            timer += Time.deltaTime;
            yield return null;
        }
        faceLight.pointLightInnerRadius = newPreset.faceLightSettings.innerRadius;
        faceLight.pointLightOuterRadius = newPreset.faceLightSettings.outerRadius;
        foregroundLight.pointLightInnerRadius = newPreset.foregroundLightSettings.innerRadius;
        foregroundLight.pointLightOuterRadius = newPreset.foregroundLightSettings.outerRadius;
        backgroundLight.pointLightInnerRadius = newPreset.backgroundLightSettings.innerRadius;
        backgroundLight.pointLightOuterRadius = newPreset.backgroundLightSettings.outerRadius;
        
        if (GameManager.instance.cellCount == GameManager.instance.maxCells)
            finalLight.gameObject.SetActive(true);

        timer = 0;
        while (timer < glowPulseTime)
        {
            float lerpVal = timer/glowPulseTime;
            LerpLightValues(faceLight, currentLightPreset.faceLightSettings, newPreset.faceLightSettings, lerpVal, false);
            LerpLightValues(foregroundLight, currentLightPreset.foregroundLightSettings, newPreset.foregroundLightSettings, lerpVal, false);
            LerpLightValues(backgroundLight, currentLightPreset.backgroundLightSettings, newPreset.backgroundLightSettings, lerpVal, false);

            timer += Time.deltaTime;
            yield return null;
        }
        faceLight.intensity = newPreset.faceLightSettings.intensity;
        foregroundLight.intensity = newPreset.foregroundLightSettings.intensity;
        backgroundLight.intensity = newPreset.backgroundLightSettings.intensity;

        currentLightPreset = newPreset;
        UpdateTransformationInPreset();
        yield return null;
    }

    void LerpLightValues(Light2D light, PointLightSetting currentSetting, PointLightSetting newSetting, float lerpVal, bool pulseUp)
    {
        if (pulseUp)
        {
            light.pointLightInnerRadius = Mathf.Lerp(currentSetting.innerRadius, newSetting.innerRadius, lerpVal);
            light.pointLightOuterRadius = Mathf.Lerp(currentSetting.outerRadius, newSetting.outerRadius, lerpVal);
            light.intensity = Mathf.Lerp(currentSetting.intensity, newSetting.intensity * glowPulseIntensityMod, lerpVal);
        }
        else
        {
            light.intensity = Mathf.Lerp(newSetting.intensity * glowPulseIntensityMod, newSetting.intensity, lerpVal);
        }
    }

    public void StandardLightLerp(Light2D light, PointLightSetting fromSetting, PointLightSetting toSetting, float lerpVal)
    {
        light.pointLightInnerRadius = Mathf.Lerp(fromSetting.innerRadius, toSetting.innerRadius, lerpVal);
        light.pointLightOuterRadius = Mathf.Lerp(fromSetting.outerRadius, toSetting.outerRadius, lerpVal);
        light.intensity = Mathf.Lerp(fromSetting.intensity, toSetting.intensity, lerpVal);
    }

    public void UpdateTransformationInPreset()
    {
        transformationIn.foregroundLightSettings.innerRadius = currentLightPreset.foregroundLightSettings.innerRadius * 2;
        transformationIn.foregroundLightSettings.outerRadius = currentLightPreset.foregroundLightSettings.outerRadius * 2;
        transformationIn.foregroundLightSettings.intensity = currentLightPreset.foregroundLightSettings.intensity * 2;

        transformationIn.backgroundLightSettings.innerRadius = currentLightPreset.backgroundLightSettings.innerRadius * 2;
        transformationIn.backgroundLightSettings.outerRadius = currentLightPreset.backgroundLightSettings.outerRadius * 2;
        transformationIn.backgroundLightSettings.intensity = currentLightPreset.backgroundLightSettings.intensity * 2;

        transformationIn.faceLightSettings.innerRadius = currentLightPreset.faceLightSettings.innerRadius * 2;
        transformationIn.faceLightSettings.outerRadius = currentLightPreset.faceLightSettings.outerRadius * 2;
        transformationIn.faceLightSettings.intensity = currentLightPreset.faceLightSettings.intensity * 2;
    }
}
