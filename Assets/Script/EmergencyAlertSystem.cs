using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EmergencyAlertSystem : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip customSirenClip;
    [Range(400f, 1500f)]
    public float baseFrequency = 800f;
    public float sirenFrequencySweepRange = 200f;
    public float sweepSpeed = 2.5f;

    [Header("Visual Flashing Settings")]
    public float flashSpeed = 3f;
    public Color alertColor = Color.red;

    [Header("Font Settings")]
    public TMP_FontAsset customFont;

    private AudioSource audioSource;
    private AlertSirenSynth sirenSynth;
    public bool isAlertActive = false; // Accessible to synth
    public bool hasCustomSiren = false; // Cached on main thread

    // --- State variables for Audio Thread ---
    public double phase = 0.0;
    public double sampling_frequency = 48000;
    public float cachedTime = 0f; // Main-thread cached time for safe usage in audio thread!

    // References to lights
    private Light directionalLight;
    private List<Light> pointLights = new List<Light>();
    private List<Color> originalPointLightColors = new List<Color>();
    private List<float> originalPointLightIntensities = new List<float>();
    private Color originalDirLightColor;
    private float originalDirLightIntensity;

    // UI References
    private GameObject alertCanvasParent;
    private RectTransform warningBar;
    private TextMeshProUGUI warningText;
    private float textScrollSpeed = 150f;
    private float originalWidth;

    void Awake()
    {
        GameObject audioObj = new GameObject("AlertSirenAudio");
        audioObj.transform.parent = this.transform;
        
        audioSource = audioObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        sirenSynth = audioObj.AddComponent<AlertSirenSynth>();
        sirenSynth.system = this;
    }

    void Start()
    {
        hasCustomSiren = (customSirenClip != null); // Cache on main thread!
        // Store original light states so we can restore them later
        directionalLight = FindDirectionalLight();
        if (directionalLight != null)
        {
            originalDirLightColor = directionalLight.color;
            originalDirLightIntensity = directionalLight.intensity;
        }

        // Initialize UI
        CreateAlertUI();
    }

    void Update()
    {
        cachedTime = Time.time; // Cache time on main thread
        if (!isAlertActive) return;

        // 1. Flash lights between original/white and Red
        float wave = Mathf.PingPong(Time.time * flashSpeed, 1f);
        
        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(originalDirLightColor, alertColor, wave);
            directionalLight.intensity = Mathf.Lerp(originalDirLightIntensity * 0.2f, originalDirLightIntensity * 0.8f, wave);
        }

        for (int i = 0; i < pointLights.Count; i++)
        {
            if (pointLights[i] != null)
            {
                pointLights[i].color = Color.Lerp(originalPointLightColors[i], alertColor, wave);
                pointLights[i].intensity = Mathf.Lerp(0f, originalPointLightIntensities[i] * 2.5f, wave);
            }
        }

        // 2. Scroll the warning text on screen
        if (warningText != null)
        {
            warningText.rectTransform.anchoredPosition += Vector2.left * textScrollSpeed * Time.deltaTime;
            
            // Loop text when it rolls off screen
            if (warningText.rectTransform.anchoredPosition.x < -originalWidth - 100f)
            {
                warningText.rectTransform.anchoredPosition = new Vector2(Screen.width + 100f, warningText.rectTransform.anchoredPosition.y);
            }
        }
    }

    [ContextMenu("Trigger Alert")]
    public void StartAlert()
    {
        if (isAlertActive) return;
        isAlertActive = true;
        hasCustomSiren = (customSirenClip != null); // Cache on main thread!

        // Gather all generated point lights in the scene
        pointLights.Clear();
        originalPointLightColors.Clear();
        originalPointLightIntensities.Clear();
        
        Light[] allLights = FindObjectsOfType<Light>();
        foreach (Light l in allLights)
        {
            if (l.type == LightType.Point)
            {
                pointLights.Add(l);
                originalPointLightColors.Add(l.color);
                originalPointLightIntensities.Add(l.intensity);
            }
        }

        // Start Siren
        if (customSirenClip != null)
        {
            audioSource.clip = customSirenClip;
            audioSource.loop = true;
            audioSource.Play();
        }
        else
        {
            // Synthesizing using OnAudioFilterRead
            audioSource.Play(); // Play dummy audio stream to trigger OnAudioFilterRead
        }

        // Display Warning UI
        if (alertCanvasParent != null)
        {
            alertCanvasParent.SetActive(true);
        }

        Debug.Log("Emergency Alert Triggered!");
    }

    [ContextMenu("Stop Alert")]
    public void StopAlert()
    {
        if (!isAlertActive) return;
        isAlertActive = false;

        // Stop Audio
        audioSource.Stop();

        // Restore original light settings
        if (directionalLight != null)
        {
            directionalLight.color = originalDirLightColor;
            directionalLight.intensity = originalDirLightIntensity;
        }

        for (int i = 0; i < pointLights.Count; i++)
        {
            if (pointLights[i] != null)
            {
                pointLights[i].color = originalPointLightColors[i];
                pointLights[i].intensity = originalPointLightIntensities[i];
            }
        }

        // Hide Warning UI
        if (alertCanvasParent != null)
        {
            alertCanvasParent.SetActive(false);
        }

        Debug.Log("Emergency Alert Stopped & Lights Restored.");
    }

    private Light FindDirectionalLight()
    {
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light l in lights)
        {
            if (l.type == LightType.Directional) return l;
        }
        return null;
    }

    private void CreateAlertUI()
    {
        // Find existing canvas or create one
        Canvas canvas = FindObjectOfType<Canvas>();
        GameObject canvasObj;
        if (canvas == null)
        {
            canvasObj = new GameObject("EmergencyCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvasObj = canvas.gameObject;
        }

        // Create alert UI parent (so we can enable/disable it easily)
        alertCanvasParent = new GameObject("EmergencyAlertPanel");
        alertCanvasParent.transform.parent = canvasObj.transform;
        
        RectTransform parentRT = alertCanvasParent.AddComponent<RectTransform>();
        parentRT.anchorMin = Vector2.zero;
        parentRT.anchorMax = Vector2.one;
        parentRT.sizeDelta = Vector2.zero;
        parentRT.localPosition = Vector3.zero;

        // 1. Red warning bar at the top of the screen
        GameObject barObj = new GameObject("WarningBar");
        barObj.transform.parent = alertCanvasParent.transform;
        
        warningBar = barObj.AddComponent<RectTransform>();
        warningBar.anchorMin = new Vector2(0f, 0.85f);
        warningBar.anchorMax = new Vector2(1f, 0.95f);
        warningBar.sizeDelta = Vector2.zero;
        warningBar.anchoredPosition = Vector2.zero;

        Image barImage = barObj.AddComponent<Image>();
        // Sleek semi-transparent dark crimson red background for warning text
        barImage.color = new Color(0.6f, 0f, 0f, 0.85f);

        // Add outline strips
        GameObject topStrip = new GameObject("TopStrip");
        topStrip.transform.parent = barObj.transform;
        RectTransform tsRT = topStrip.AddComponent<RectTransform>();
        tsRT.anchorMin = new Vector2(0f, 0.95f);
        tsRT.anchorMax = new Vector2(1f, 1f);
        tsRT.sizeDelta = Vector2.zero;
        topStrip.AddComponent<Image>().color = Color.yellow; // Yellow hazards tape look

        GameObject bottomStrip = new GameObject("BottomStrip");
        bottomStrip.transform.parent = barObj.transform;
        RectTransform bsRT = bottomStrip.AddComponent<RectTransform>();
        bsRT.anchorMin = new Vector2(0f, 0f);
        bsRT.anchorMax = new Vector2(1f, 0.05f);
        bsRT.sizeDelta = Vector2.zero;
        bottomStrip.AddComponent<Image>().color = Color.yellow;

        // 2. Scrolling emergency text
        GameObject textObj = new GameObject("EmergencyScrollText");
        textObj.transform.parent = barObj.transform;
        
        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 0.1f);
        textRT.anchorMax = new Vector2(1f, 0.9f);
        textRT.sizeDelta = Vector2.zero;
        textRT.anchoredPosition = new Vector2(Screen.width, 0f); // start off-screen right

        warningText = textObj.AddComponent<TextMeshProUGUI>();
        
        // --- Bulletproof Font Resolver ---
        if (customFont == null)
        {
#if UNITY_EDITOR
            customFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SU3DJPFont/TextMeshProFont/Dynamic/mplus-1p-medium SDF Dynamic.asset");
#endif
        }
        if (customFont == null)
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var f in fonts)
            {
                if (f != null && f.name != null && (f.name.Contains("mplus") || (f.name.Contains("SDF") && !f.name.Contains("Liberation"))))
                {
                    customFont = f;
                    break;
                }
            }
        }

        if (customFont != null)
        {
            warningText.font = customFont;
            warningText.SetAllDirty();
        }
        warningText.alignment = TextAlignmentOptions.MidlineLeft;
        warningText.fontSize = 24f;
        warningText.fontStyle = FontStyles.Bold;
        warningText.color = Color.white;
        warningText.text = "【警告】校庭にて謎の地球外生命体の襲来を確認！全生徒は直ちに校庭へ避難（移動）しなさい！ EMERGENCY WARNING - INVASION IN PROGRESS! EVACUATE TO THE SCHOOLYARD IMMEDIATELY!";
        
        // Calculate text width dynamically
        warningText.ForceMeshUpdate();
        originalWidth = warningText.preferredWidth;
        warningText.rectTransform.sizeDelta = new Vector2(originalWidth + 50f, warningText.rectTransform.sizeDelta.y);

        // Hide at start
        alertCanvasParent.SetActive(false);
    }

    // --- PROCEDURAL AUDIO SIREN GENERATOR ---
}

public class AlertSirenSynth : MonoBehaviour
{
    public EmergencyAlertSystem system;

    // This is called by Unity's audio system on a background thread when audioSource.Play() is running.
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (system == null || !system.isAlertActive || system.hasCustomSiren) return;

        double doubleSampleRate = system.sampling_frequency;

        for (int i = 0; i < data.Length; i += channels)
        {
            double frequencySweep = Mathf.Sin(system.cachedTime * system.sweepSpeed) * system.sirenFrequencySweepRange;
            double currentFrequency = system.baseFrequency + frequencySweep;

            system.phase += 2.0 * Mathf.PI * currentFrequency / doubleSampleRate;
            float sample = (float)Mathf.Sin((float)system.phase);

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample * 0.25f; 
            }

            if (system.phase > 2.0 * Mathf.PI * 100.0)
            {
                system.phase -= 2.0 * Mathf.PI * 100.0;
            }
        }
    }
}
