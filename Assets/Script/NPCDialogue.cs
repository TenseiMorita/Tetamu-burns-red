using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class NPCDialogue : MonoBehaviour
{
    public GameObject dialoguePanel;

    public TextMeshProUGUI dialogueText;

    private bool isPlayerNear;

    public CameraFollow cameraFollow;

    public PlayerMove playerMove;

    public TextMeshProUGUI nameText;

    public string characterName;

    public GameManager gameManager;

    // Custom runtime components
    private TextMeshProUGUI customNameText;
    private TextMeshProUGUI customDialogueText;

    //隍・焚縺ｮ譁・ｫ繧貞・繧後ｋ邂ｱ
    public string[] lines;

    //莉翫←縺ｮ譁・ｫ縺九ｒ謗｢縺・
    private int currentLine = 0;

    private bool isTyping = false;
    private string currentParsedDialogue = "";

    void Start()
    {
        ApplyPremiumHBRStyling();
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    private void ApplyPremiumHBRStyling()
    {
        // 0. Self-Heal missing dialoguePanel reference (including inactive objects!)
        if (dialoguePanel == null)
        {
            var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj != null && (obj.name == "dialoguePanel" || obj.name == "DialoguePanel" || obj.name == "Dialogue Panel"))
                {
#if UNITY_EDITOR
                    if (UnityEditor.EditorUtility.IsPersistent(obj)) continue;
#endif
                    dialoguePanel = obj;
                    break;
                }
            }
            
            if (dialoguePanel == null)
            {
                Debug.LogWarning("NPCDialogue: dialoguePanel could not be resolved in scene (even inactive scan)!");
                return;
            }
        }

        // Self-Heal missing text references (including inactive children!)
        if (dialogueText == null && dialoguePanel != null)
        {
            var t = dialoguePanel.transform.Find("dialogueText");
            if (t == null) t = dialoguePanel.transform.Find("DialogueText");
            if (t != null) dialogueText = t.GetComponent<TextMeshProUGUI>();
            if (dialogueText == null) dialogueText = dialoguePanel.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        if (nameText == null && dialoguePanel != null)
        {
            var t = dialoguePanel.transform.Find("nameText");
            if (t == null) t = dialoguePanel.transform.Find("NameText");
            if (t != null) nameText = t.GetComponent<TextMeshProUGUI>();
            if (nameText == null)
            {
                foreach (Transform child in dialoguePanel.transform)
                {
                    if (child.name == "nameText" || child.name == "NameText")
                    {
                        nameText = child.GetComponent<TextMeshProUGUI>();
                        break;
                    }
                }
            }
        }

        // Automatically fetch gameManager if null
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        // --- ULTRA-ROBUST JAPANESE FONT RESOLVER (Self-Healing & Editor Force Load) ---
        TMP_FontAsset jpnFont = null;

        // 1. Check GameManager customFont
        if (gameManager != null && gameManager.customFont != null)
        {
            jpnFont = gameManager.customFont;
        }

        // 2. Editor-Time Database Force Load (100% Guaranteed to work in Editor Play Mode!)
#if UNITY_EDITOR
        if (jpnFont == null)
        {
            // Try loading the dynamic Japanese SDF font first (contains complete hiragana/katakana and kanji rendering)
            jpnFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SU3DJPFont/TextMeshProFont/Dynamic/mplus-1p-medium SDF Dynamic.asset");
            if (jpnFont != null) Debug.Log("NPCDialogue: Loaded dynamic Japanese SDF font from direct path.");
        }
        if (jpnFont == null)
        {
            jpnFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SU3DJPFont/TextMeshProFont/Selected/mplus-1p-medium SDF.asset");
            if (jpnFont != null) Debug.Log("NPCDialogue: Loaded pre-baked static Japanese SDF font fallback from direct path.");
        }
        if (jpnFont == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("mplus-1p-regular SDF");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                jpnFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                Debug.Log("NPCDialogue: Force-loaded jpnFont from AssetDatabase: " + path);
            }
        }
        if (jpnFont == null)
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (var guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("mplus") || (path.Contains("SDF") && !path.Contains("Liberation")))
                {
                    jpnFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                    if (jpnFont != null)
                    {
                        Debug.Log("NPCDialogue: Fallback force-loaded jpnFont from AssetDatabase: " + path);
                        break;
                    }
                }
            }
        }
#endif

        // 3. Extract from original dialogueText or nameText if they have a custom font assigned in inspector
        if (jpnFont == null && dialogueText != null && dialogueText.font != null && !dialogueText.font.name.Contains("LiberationSans"))
        {
            jpnFont = dialogueText.font;
        }
        if (jpnFont == null && nameText != null && nameText.font != null && !nameText.font.name.Contains("LiberationSans"))
        {
            jpnFont = nameText.font;
        }

        // 4. Scan active TextMeshProUGUI components in the scene to steal a valid Japanese SDF font
        if (jpnFont == null)
        {
            var activeTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
            foreach (var txt in activeTexts)
            {
                if (txt != null && txt.font != null && txt.font.name != null && !txt.font.name.Contains("LiberationSans") && txt.font.name.Contains("SDF"))
                {
                    jpnFont = txt.font;
                    break;
                }
            }
        }

        // 5. Fallback: Search all loaded TMP_FontAsset in memory for a Japanese SDF font
        if (jpnFont == null)
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var f in fonts)
            {
                if (f != null && f.name != null && (f.name.Contains("mplus") || (f.name.Contains("SDF") && !f.name.Contains("LiberationSans"))))
                {
                    jpnFont = f;
                    break;
                }
            }
        }

        // 6. Cache resolving back to GameManager to maintain global typography consistency
        if (gameManager != null && jpnFont != null)
        {
            gameManager.customFont = jpnFont;
        }

        // Ensure dialoguePanel is positioned beautifully (slightly taller to perfectly avoid large font cutoff)
        RectTransform panelRT = dialoguePanel.GetComponent<RectTransform>();
        if (panelRT != null)
        {
            panelRT.anchorMin = new Vector2(0f, 0f);
            panelRT.anchorMax = new Vector2(1f, 0.32f); // covers lower 32% of the screen for absolute text safety
            panelRT.sizeDelta = new Vector2(-120f, -30f); // 60px side margins, 15px bottom margin
            panelRT.anchoredPosition = new Vector2(0f, 15f);
            panelRT.pivot = new Vector2(0.5f, 0f);
        }

        // Hide original panel visual elements
        Image panelImg = dialoguePanel.GetComponent<Image>();
        if (panelImg != null)
        {
            panelImg.color = Color.clear; // Make base panel fully transparent
        }
        Outline panelOutline = dialoguePanel.GetComponent<Outline>();
        if (panelOutline != null)
        {
            panelOutline.enabled = false; // Disable original outline
        }

        // Disable and Deactivate original text renderers to prevent overlapping/clipping
        if (dialogueText != null)
        {
            dialogueText.enabled = false;
            dialogueText.gameObject.SetActive(false); // Completely deactivate to prevent overlap
        }
        if (nameText != null)
        {
            nameText.enabled = false;
            nameText.gameObject.SetActive(false); // Completely deactivate to prevent overlap
        }

        // Find or create the master procedural HBR container
        Transform existingContainer = dialoguePanel.transform.Find("HBR_DialogueContainer");
        GameObject containerObj = existingContainer != null ? existingContainer.gameObject : null;

        if (containerObj == null)
        {
            containerObj = new GameObject("HBR_DialogueContainer");
            containerObj.transform.SetParent(dialoguePanel.transform, false);
            RectTransform containerRT = containerObj.AddComponent<RectTransform>();
            containerRT.anchorMin = Vector2.zero;
            containerRT.anchorMax = Vector2.one;
            containerRT.sizeDelta = Vector2.zero;
            containerRT.anchoredPosition = Vector2.zero;
        }

        // 1. Rebuild Dialogue Background
        Transform existingBg = containerObj.transform.Find("HBR_DialogueBg");
        GameObject bgObj = existingBg != null ? existingBg.gameObject : null;
        if (bgObj == null)
        {
            bgObj = new GameObject("HBR_DialogueBg");
            bgObj.transform.SetParent(containerObj.transform, false);
            RectTransform bgRT = bgObj.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.sizeDelta = Vector2.zero;
            bgRT.anchoredPosition = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.06f, 0.1f, 0.92f); // Sleek glassmorphic dark-navy HBR background

            Outline bgOutline = bgObj.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0f, 0.9f, 1f, 0.6f); // Glowing cyan border
            bgOutline.effectDistance = new Vector2(1.5f, 1.5f);
        }

        // 2. Rebuild Nameplate Background (positioned tabbed above top-left edge)
        Transform existingNameBg = containerObj.transform.Find("HBR_NamePlateBg");
        GameObject nameBgObj = existingNameBg != null ? existingNameBg.gameObject : null;
        if (nameBgObj == null)
        {
            nameBgObj = new GameObject("HBR_NamePlateBg");
            nameBgObj.transform.SetParent(containerObj.transform, false);
            
            RectTransform nbgRT = nameBgObj.AddComponent<RectTransform>();
            nbgRT.anchorMin = new Vector2(0.05f, 0.98f);
            nbgRT.anchorMax = new Vector2(0.22f, 1.25f);
            nbgRT.sizeDelta = Vector2.zero;
            nbgRT.pivot = new Vector2(0.5f, 0.5f);

            Image nbgImg = nameBgObj.AddComponent<Image>();
            nbgImg.color = new Color(0.02f, 0.03f, 0.05f, 0.95f); // Contrast dark-navy nameplate background

            Outline nbgOutline = nameBgObj.AddComponent<Outline>();
            nbgOutline.effectColor = new Color(0f, 0.9f, 1f, 0.6f); // Matching glowing cyan outline
            nbgOutline.effectDistance = new Vector2(1.2f, 1.2f);
        }

        // 3. Rebuild Custom Name Text (child of Nameplate background)
        Transform existingCustomName = nameBgObj.transform.Find("HBR_CustomNameText");
        GameObject customNameObj = existingCustomName != null ? existingCustomName.gameObject : null;
        if (customNameObj == null)
        {
            customNameObj = new GameObject("HBR_CustomNameText");
            customNameObj.transform.SetParent(nameBgObj.transform, false);

            RectTransform cntRT = customNameObj.AddComponent<RectTransform>();
            cntRT.anchorMin = Vector2.zero;
            cntRT.anchorMax = Vector2.one;
            cntRT.sizeDelta = Vector2.zero;
            cntRT.anchoredPosition = Vector2.zero;

            customNameText = customNameObj.AddComponent<TextMeshProUGUI>();
            customNameText.fontStyle = FontStyles.Bold;
            customNameText.color = Color.white;
            customNameText.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            customNameText = customNameObj.GetComponent<TextMeshProUGUI>();
        }

        if (customNameText != null && jpnFont != null)
        {
            customNameText.font = jpnFont;
            customNameText.fontSize = 26f; // Premium size for nameplate
            customNameText.overflowMode = TextOverflowModes.Overflow; // Prevent clipping
            customNameText.SetAllDirty();
        }

        // 4. Rebuild Custom Dialogue Text (child of master container, aligned below nameplate)
        Transform existingCustomDialogue = containerObj.transform.Find("HBR_CustomDialogueText");
        GameObject customDialogueObj = existingCustomDialogue != null ? existingCustomDialogue.gameObject : null;
        if (customDialogueObj == null)
        {
            customDialogueObj = new GameObject("HBR_CustomDialogueText");
            customDialogueObj.transform.SetParent(containerObj.transform, false);

            RectTransform cdtRT = customDialogueObj.AddComponent<RectTransform>();
            cdtRT.anchorMin = new Vector2(0.04f, 0.08f); // Maximum physical height area
            cdtRT.anchorMax = new Vector2(0.96f, 0.88f); // Perfect breathing room below the floating name plate
            cdtRT.sizeDelta = Vector2.zero;
            cdtRT.anchoredPosition = Vector2.zero;

            customDialogueText = customDialogueObj.AddComponent<TextMeshProUGUI>();
            customDialogueText.fontStyle = FontStyles.Normal;
            customDialogueText.color = new Color(0.95f, 0.97f, 1f, 1f); // Elegant off-white
            customDialogueText.alignment = TextAlignmentOptions.TopLeft;
        }
        else
        {
            customDialogueText = customDialogueObj.GetComponent<TextMeshProUGUI>();
        }

        // Always apply font and size overrides
        if (customDialogueText != null)
        {
            customDialogueText.fontSize = 28f; // Readable dialogue size
            customDialogueText.fontStyle = FontStyles.Normal;
            customDialogueText.lineSpacing = 12f; // Comfortable line separation
            customDialogueText.overflowMode = TextOverflowModes.Overflow; // Prevent clipping
            customDialogueText.enableWordWrapping = true;
            if (jpnFont != null)
            {
                customDialogueText.font = jpnFont;
                // Let TMP handle material instantiation automatically - do not assign manually
                customDialogueText.enableAutoSizing = false;
            }

            customDialogueText.SetAllDirty();
        }
    }

    void Update()
    {
        bool canAdvance = isPlayerNear;
        
        // Allow global advance during emergency alerts or cutscene states
        if (gameManager != null && (gameManager.currentState == GameState.Alert || gameManager.currentState == GameState.Cutscene))
        {
            canAdvance = true;
        }

        if (canAdvance && Input.GetKeyDown(KeyCode.E))
        {
            if (gameManager != null && gameManager.currentState == GameState.Explore && !dialoguePanel.activeSelf)
            {
                dialoguePanel.SetActive(true);
                ApplyPremiumHBRStyling();
                if (nameText != null) nameText.text = characterName;
                if (customNameText != null) customNameText.text = characterName;
                currentLine = 0;
                AdvanceDialogue();
            }
            else
            {
                AdvanceDialogue();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (gameManager == null || gameManager.currentState == GameState.Explore)
            {
                if (playerMove != null) playerMove.canMove = true;
                if (cameraFollow != null) cameraFollow.isTalking = false;
            }
        }
    }

    public void StartDialogueSequence(string name, string[] newLines)
    {
        characterName = name;
        lines = newLines;
        currentLine = 0;
        isTyping = false;
        
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        ApplyPremiumHBRStyling();
        if (nameText != null) nameText.text = characterName;
        if (customNameText != null) customNameText.text = characterName;
        
        // Freeze player during dynamic cutscene dialogue
        if (playerMove != null) playerMove.canMove = false;
        if (cameraFollow != null) cameraFollow.isTalking = true;
        
        AdvanceDialogue();
    }

    public void AdvanceDialogue()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            if (dialogueText != null) dialogueText.text = currentParsedDialogue;
            if (customDialogueText != null) customDialogueText.text = currentParsedDialogue;
            isTyping = false;
            return;
        }

        if (lines != null && currentLine < lines.Length)
        {
            if (playerMove != null) playerMove.canMove = false;
            if (cameraFollow != null) cameraFollow.isTalking = true;

            string rawLine = lines[currentLine];
            string speakerName = characterName;
            string parsedDialogue = rawLine;

            // Quote Parser: Check for Name「text」 pattern (Japanese brackets)
            int startQuote = rawLine.IndexOf('「');
            int endQuote = rawLine.LastIndexOf('」');
            if (startQuote >= 0 && endQuote > startQuote)
            {
                speakerName = rawLine.Substring(0, startQuote).Trim();
                parsedDialogue = rawLine.Substring(startQuote + 1, endQuote - startQuote - 1);
            }

            if (nameText != null)
            {
                nameText.text = speakerName;
            }
            if (customNameText != null)
            {
                customNameText.text = speakerName;
            }

            currentParsedDialogue = parsedDialogue;
            StartCoroutine(ShowText(parsedDialogue));
            currentLine++;
        }
        else
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            
            // Only restore controls if in Explore state
            if (gameManager == null || gameManager.currentState == GameState.Explore)
            {
                if (playerMove != null) playerMove.canMove = true;
                if (cameraFollow != null) cameraFollow.isTalking = false;
            }
            
            currentLine = 0;

            if (gameManager != null)
            {
                gameManager.OnDialogueComplete(this);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            
            if (gameManager == null || gameManager.currentState == GameState.Explore)
            {
                if (playerMove != null) playerMove.canMove = true;
            }
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }
    }

    IEnumerator ShowText(string text)
    {
        isTyping = true;
        if (dialogueText != null) dialogueText.text = "";
        if (customDialogueText != null) customDialogueText.text = "";

        foreach (char c in text)
        {
            if (dialogueText != null) dialogueText.text += c;
            if (customDialogueText != null) customDialogueText.text += c;
            yield return new WaitForSeconds(0.04f); // Slightly faster elegant typing speed
        }

        isTyping = false;
    }
}
