using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.TextCore.LowLevel;

public enum GameState { Explore, Alert, Cutscene, Battle, Victory, GameOver }

public class GameManager : MonoBehaviour
{
    // ===== Singleton =====
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    public GameState currentState = GameState.Explore;

    [Header("References")]
    public PlayerMove playerMove;
    public CameraFollow cameraFollow;
    public NPCDialogue npcDialogue;

    [Header("Generators")]
    public CorridorGenerator corridorGenerator;
    public SchoolyardGenerator schoolyardGenerator;

    [Header("Alert & Summon Systems")]
    public EmergencyAlertSystem alertSystem;
    public WeaponSummoner weaponSummoner;
    public BattleManager battleManager;
    public BattleUI battleUI;

    [Header("Custom Font")]
    public TMPro.TMP_FontAsset customFont;

    private GameObject corridorObj;
    private GameObject schoolyardObj;

    public List<Transform> npcTransforms = new List<Transform>();

    private GameObject cancerBoss1;
    private GameObject cancerBoss2;

    // Dialogue sequences
    private int dialogueSequence = 0;
    private GameObject globalFadePanel;
    private TextMeshProUGUI chantCutInText;
    private GameObject cutInCanvasParent;

    private GameObject globalLoadingPanel;
    private Image loadingProgressBarFill;
    private TextMeshProUGUI loadingStatusText;

    // ===== Singleton + DontDestroyOnLoad =====
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to scene loaded callback
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("GameManager: Scene loaded -> " + scene.name);

        if (scene.name == "SampleScene")
        {
            StartCoroutine(InitSampleScene());
        }
        else if (scene.name == "Battle")
        {
            StartCoroutine(InitBattleScene());
        }
    }

    // ============================================================
    // SampleScene initialisation (Explore → Alert → Transition)
    // ============================================================
    void Start()
    {
        // Only run initial setup when we are actually in SampleScene
        if (SceneManager.GetActiveScene().name == "SampleScene")
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            SetupEnvironments();
            SetupCharacters();
            CreateGlobalUI();
            StartExplorePhase();
            StartCoroutine(SafeGroundingRoutine());
        }
    }

    private IEnumerator InitSampleScene()
    {
        yield return null; // wait one frame for scene objects to be ready

        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // Re-find or recreate references that belong to SampleScene
        playerMove = FindObjectOfType<PlayerMove>();
        cameraFollow = FindObjectOfType<CameraFollow>();
        if (cameraFollow != null) cameraFollow.enabled = true;
        npcDialogue = FindObjectOfType<NPCDialogue>();

        corridorGenerator = FindObjectOfType<CorridorGenerator>();
        schoolyardGenerator = FindObjectOfType<SchoolyardGenerator>();

        alertSystem = FindObjectOfType<EmergencyAlertSystem>();
        if (alertSystem == null)
        {
            GameObject sys = new GameObject("EmergencyAlertSystem");
            alertSystem = sys.AddComponent<EmergencyAlertSystem>();
        }
        if (alertSystem != null) alertSystem.customFont = customFont;

        weaponSummoner = FindObjectOfType<WeaponSummoner>();
        if (weaponSummoner == null)
        {
            GameObject sys = new GameObject("WeaponSummoner");
            weaponSummoner = sys.AddComponent<WeaponSummoner>();
        }

        // BattleManager and BattleUI live in Battle scene; nullify here
        battleManager = null;
        battleUI = null;

        SetupEnvironments();
        SetupCharacters();
        CreateGlobalUI();
        ApplyGlobalTMPFontFix();
        StartExplorePhase();
        StartCoroutine(SafeGroundingRoutine());
    }

    // ============================================================
    // Battle scene initialisation
    // ============================================================
    private IEnumerator InitBattleScene()
    {
        yield return null; // wait one frame

        Debug.Log("GameManager: InitBattleScene starting...");

        // ── Destroy leftover Talk scene environments that persist via DontDestroyOnLoad ──
        string[] talkSceneObjects = {
            "Generated_Procedural_Corridor",
            "Generated_Procedural_Schoolyard",
            "CorridorGenerator",
            "SchoolyardGenerator",
            "Ground",
            "Classroom corridor wall Right",
            "Classroom corridor wall Left",
            "Classroom corridor doar"
        };
        foreach (string objName in talkSceneObjects)
        {
            GameObject obj = GameObject.Find(objName);
            if (obj != null)
            {
                Debug.Log("GameManager: Destroying Talk scene object -> " + objName);
                Destroy(obj);
            }
        }
        // Also destroy the stored references
        if (corridorObj != null) { Destroy(corridorObj); corridorObj = null; }
        if (schoolyardObj != null) { Destroy(schoolyardObj); schoolyardObj = null; }
        corridorGenerator = null;
        schoolyardGenerator = null;

        // ── Ensure Talk scene actors are not visible in Battle scene ──
        CleanupTalkActorsForBattle();

        // ── Enable kandaiComplete school stage in the Battle scene ──
        GameObject kandaiStage = GameObject.Find("kandaiComplete");
        if (kandaiStage != null)
        {
            kandaiStage.SetActive(true);
            Debug.Log("GameManager: kandaiComplete stage enabled (keeping scene transform).");
        }
        else
        {
            Debug.LogWarning("GameManager: kandaiComplete not found in Battle scene!");
        }

        // Resolve BattleManager & BattleUI from the Battle scene
        battleManager = FindObjectOfType<BattleManager>();
        battleUI = FindObjectOfType<BattleUI>();

        if (battleManager == null)
        {
            GameObject sys = new GameObject("BattleManager");
            battleManager = sys.AddComponent<BattleManager>();
        }
        if (battleUI == null)
        {
            GameObject sys = new GameObject("BattleUI");
            battleUI = sys.AddComponent<BattleUI>();
        }

        weaponSummoner = FindObjectOfType<WeaponSummoner>();
        if (weaponSummoner == null)
        {
            GameObject sys = new GameObject("WeaponSummoner");
            weaponSummoner = sys.AddComponent<WeaponSummoner>();
        }


        // Re-find camera in Battle scene
        cameraFollow = FindObjectOfType<CameraFollow>();
        if (cameraFollow == null)
        {
            Camera cam = Camera.main;
            if (cam != null) cameraFollow = cam.gameObject.AddComponent<CameraFollow>();
        }
        if (cameraFollow != null)
        {
            cameraFollow.target = null;
            cameraFollow.isTalking = false;
            cameraFollow.enabled = false; // Keep camera fixed in Battle scene (except shake effects)
        }

        if (battleManager != null) battleManager.Initialize(this, battleUI);
        if (battleManager != null && cameraFollow != null) battleManager.cameraFollow = cameraFollow;

        // Recreate global loading/fade UI inside Battle scene (new canvas)
        CreateGlobalUI();
        ApplyGlobalTMPFontFix();

        // Start the schoolyard summoning cutscene → then battle
        StartCoroutine(BattleSceneOpening());
    }

    private void CleanupTalkActorsForBattle()
    {
        // Any lingering Player from Talk scene must never appear in Battle scene.
        PlayerMove[] talkPlayers = FindObjectsOfType<PlayerMove>(true);
        foreach (PlayerMove pm in talkPlayers)
        {
            if (pm == null) continue;
            Debug.Log("GameManager: Removing Talk Player for Battle -> " + pm.gameObject.name);
            Destroy(pm.gameObject);
        }
        playerMove = null;

        // Remove dialogue NPCs that can accidentally survive or be pre-placed.
        NPCDialogue[] talkNpcs = FindObjectsOfType<NPCDialogue>(true);
        foreach (NPCDialogue npc in talkNpcs)
        {
            if (npc == null) continue;
            Debug.Log("GameManager: Removing Talk NPC for Battle -> " + npc.gameObject.name);
            Destroy(npc.gameObject);
        }
        npcDialogue = null;

        // Extra safety: remove explicitly named talk actors/clones.
        string[] talkActorNames = { "Player", "NPC", "NPC_1", "NPC_2", "NPC_3", "NPC_4" };
        foreach (string actorName in talkActorNames)
        {
            GameObject actor = GameObject.Find(actorName);
            if (actor != null)
            {
                Debug.Log("GameManager: Destroying talk actor by name -> " + actorName);
                Destroy(actor);
            }
        }
    }

    private IEnumerator BattleSceneOpening()
    {
        yield return new WaitForSeconds(0.3f);

        currentState = GameState.Cutscene;
        dialogueSequence = 2;

        // Destroy the accidental NPC (red capsule) that might have been left in the Battle scene
        GameObject accidentalNPC = GameObject.Find("NPC");
        if (accidentalNPC != null)
        {
            Destroy(accidentalNPC);
        }

        // No dialogue panel in Battle scene - skip straight to weapon summon
        StartCoroutine(SummonRitualSequence());
    }

    // ============================================================
    // Safe grounding
    // ============================================================
    private IEnumerator SafeGroundingRoutine()
    {
        Debug.Log("Safe Grounding Sequence Started...");

        if (playerMove != null)
        {
            CharacterController cc = playerMove.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerMove.transform.position = new Vector3(0f, 1.0f, 2.0f);
            playerMove.canMove = false;
        }

        GameObject npcObj = GameObject.Find("NPC");
        if (npcObj != null)
        {
            CharacterController cc = npcObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            npcObj.transform.position = new Vector3(3f, 1.0f, 2.0f);
        }

        yield return new WaitForSeconds(0.2f);
        yield return new WaitForFixedUpdate();
        Physics.SyncTransforms();

        if (playerMove != null)
        {
            CharacterController cc = playerMove.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;
            playerMove.canMove = true;
        }

        if (npcObj != null)
        {
            CharacterController cc = npcObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;
        }

        Debug.Log("Safe Grounding completed.");
    }

    // ============================================================
    // SampleScene – environments & characters
    // ============================================================
    private void SetupEnvironments()
    {
        try
        {
            if (corridorGenerator == null || corridorGenerator.gameObject == this.gameObject)
            {
                GameObject corrObj = GameObject.Find("CorridorGenerator");
                if (corrObj == null) corrObj = new GameObject("CorridorGenerator");

                CorridorGenerator newGen = corrObj.GetComponent<CorridorGenerator>();
                if (newGen == null) newGen = corrObj.AddComponent<CorridorGenerator>();

                if (corridorGenerator != null && corridorGenerator.gameObject == this.gameObject)
                {
                    newGen.segmentCount = corridorGenerator.segmentCount;
                    newGen.segmentWidth = corridorGenerator.segmentWidth;
                    newGen.corridorHeight = corridorGenerator.corridorHeight;
                    newGen.corridorDepth = corridorGenerator.corridorDepth;
                    newGen.customFont = corridorGenerator.customFont;
                    newGen.floorMaterial = corridorGenerator.floorMaterial;
                    newGen.wallMaterial = corridorGenerator.wallMaterial;
                    newGen.pillarMaterial = corridorGenerator.pillarMaterial;
                    newGen.doorMaterial = corridorGenerator.doorMaterial;
                    newGen.windowFrameMaterial = corridorGenerator.windowFrameMaterial;
                    newGen.glassMaterial = corridorGenerator.glassMaterial;
                    newGen.ceilingMaterial = corridorGenerator.ceilingMaterial;
                    newGen.lightFixtureMaterial = corridorGenerator.lightFixtureMaterial;
                    newGen.noticeBoardMaterial = corridorGenerator.noticeBoardMaterial;
                    newGen.doorPrefab = corridorGenerator.doorPrefab;
                }

                corridorGenerator = newGen;
            }

            corridorGenerator.transform.position = Vector3.zero;
            corridorGenerator.transform.rotation = Quaternion.identity;
            corridorGenerator.transform.localScale = Vector3.one;

            corridorObj = GameObject.Find("Generated_Procedural_Corridor");
            if (corridorObj == null)
            {
                if (customFont != null) corridorGenerator.customFont = customFont;
                corridorGenerator.Generate();
                corridorObj = GameObject.Find("Generated_Procedural_Corridor");
            }

            if (schoolyardGenerator == null || schoolyardGenerator.gameObject == this.gameObject)
            {
                GameObject yardObj = GameObject.Find("SchoolyardGenerator");
                if (yardObj == null) yardObj = new GameObject("SchoolyardGenerator");

                SchoolyardGenerator newGen = yardObj.GetComponent<SchoolyardGenerator>();
                if (newGen == null) newGen = yardObj.AddComponent<SchoolyardGenerator>();

                if (schoolyardGenerator != null && schoolyardGenerator.gameObject == this.gameObject)
                {
                    newGen.yardWidth = schoolyardGenerator.yardWidth;
                    newGen.yardDepth = schoolyardGenerator.yardDepth;
                    newGen.groundMaterial = schoolyardGenerator.groundMaterial;
                    newGen.buildingMaterial = schoolyardGenerator.buildingMaterial;
                    newGen.fenceMaterial = schoolyardGenerator.fenceMaterial;
                    newGen.enemyCoreMaterial = schoolyardGenerator.enemyCoreMaterial;
                    newGen.enemyMetalMaterial = schoolyardGenerator.enemyMetalMaterial;
                    newGen.commanderMaterial = schoolyardGenerator.commanderMaterial;
                }

                schoolyardGenerator = newGen;
            }

            schoolyardGenerator.transform.position = new Vector3(0f, 0f, -150f);
            schoolyardGenerator.transform.rotation = Quaternion.identity;
            schoolyardGenerator.transform.localScale = Vector3.one;

            schoolyardObj = GameObject.Find("Generated_Procedural_Schoolyard");
            if (schoolyardObj == null)
            {
                schoolyardGenerator.Generate();
                schoolyardObj = GameObject.Find("Generated_Procedural_Schoolyard");
            }

            if (schoolyardObj != null) schoolyardObj.SetActive(false);

            Physics.SyncTransforms();

            string[] originalObjNames = {
                "Ground",
                "Classroom corridor wall Right",
                "Classroom corridor wall Left",
                "Classroom corridor doar"
            };
            foreach (string name in originalObjNames)
            {
                GameObject obj = GameObject.Find(name);
                if (obj != null) obj.SetActive(false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error during SetupEnvironments: " + e.Message + "\n" + e.StackTrace);
        }
    }

    private void SetupCharacters()
    {
        if (playerMove == null) playerMove = FindObjectOfType<PlayerMove>();
        if (cameraFollow == null) cameraFollow = FindObjectOfType<CameraFollow>();
        if (npcDialogue == null) npcDialogue = FindObjectOfType<NPCDialogue>();

        if (playerMove != null)
        {
            GameObject playerObj = playerMove.gameObject;
            playerObj.name = "Player";
            playerObj.tag = "Player";

            CharacterController charCtrl = playerObj.GetComponent<CharacterController>();
            if (charCtrl == null) charCtrl = playerObj.AddComponent<CharacterController>();
            charCtrl.height = 2.0f;
            charCtrl.radius = 0.5f;
            charCtrl.center = new Vector3(0f, 1.0f, 0f);

            foreach (var c in playerObj.GetComponents<Collider>())
            {
                if (c != charCtrl) Destroy(c);
            }

            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
        }

        GameObject npcObj = GameObject.Find("NPC");
        if (npcObj != null)
        {
            if (npcDialogue == null) npcDialogue = npcObj.GetComponent<NPCDialogue>();
            if (npcDialogue == null) npcDialogue = npcObj.AddComponent<NPCDialogue>();

            SphereCollider npcTrigger = npcObj.GetComponent<SphereCollider>();
            if (npcTrigger == null) npcTrigger = npcObj.AddComponent<SphereCollider>();
            npcTrigger.isTrigger = true;
            npcTrigger.radius = 2.5f;
            npcTrigger.center = new Vector3(0f, 1.0f, 0f);

            foreach (var c in npcObj.GetComponents<Collider>())
            {
                if (c != npcTrigger) Destroy(c);
            }

            Rigidbody npcRb = npcObj.GetComponent<Rigidbody>();
            if (npcRb != null) Destroy(npcRb);

            npcTransforms.Clear();
            npcTransforms.Add(npcObj.transform);
            for (int i = 1; i <= 4; i++)
            {
                GameObject clone = Instantiate(npcObj);
                clone.name = "NPC_" + i;
                clone.transform.position = npcObj.transform.position + new Vector3(i * 1.5f, 0, 0);

                var diag = clone.GetComponent<NPCDialogue>();
                if (diag != null) Destroy(diag);

                npcTransforms.Add(clone.transform);
            }
        }

        if (alertSystem == null || alertSystem.gameObject == this.gameObject)
        {
            if (alertSystem != null) Destroy(alertSystem);
            GameObject sys = new GameObject("EmergencyAlertSystem");
            alertSystem = sys.AddComponent<EmergencyAlertSystem>();
        }
        if (alertSystem != null) alertSystem.customFont = customFont;

        if (weaponSummoner == null)
        {
            GameObject sys = new GameObject("WeaponSummoner");
            weaponSummoner = sys.AddComponent<WeaponSummoner>();
        }

        // BattleManager/BattleUI are in the Battle scene; do NOT create them here
        battleManager = null;
        battleUI = null;

        if (npcDialogue != null)
        {
            npcDialogue.gameManager = this;
            npcDialogue.playerMove = playerMove;
            npcDialogue.cameraFollow = cameraFollow;
            npcDialogue.characterName = "司令官";

            npcDialogue.lines = new string[]
            {
                "司令官「新入生の皆さん、入学おめでとう。ここは人類最後の希望、セラフ部隊の拠点です。」",
                "司令官「あなたたちには、キャンサーと呼ばれる地球外生命体と戦ってもらいます。」",
                "主人公「あたしが…人類の希望？なんだか実感が湧かないなぁ。」",
                "戦友「大丈夫、私たち一緒ならきっとやれるよ！」"
            };
        }
    }

    private void CreateGlobalUI()
    {
        EnsureJapaneseFont();

        // Find or create main canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        GameObject canvasObj = (canvas != null) ? canvas.gameObject : new GameObject("GlobalCanvas");
        if (canvas == null)
        {
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Ensure EventSystem exists (critical for mouse clicks!)
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            DontDestroyOnLoad(esObj);
            Debug.Log("GameManager: Created missing EventSystem.");
        }

        // Global Black Fade Panel
        globalFadePanel = new GameObject("GlobalFadePanel");
        globalFadePanel.transform.SetParent(canvasObj.transform, false);
        RectTransform fadeRT = globalFadePanel.AddComponent<RectTransform>();
        fadeRT.anchorMin = Vector2.zero;
        fadeRT.anchorMax = Vector2.one;
        fadeRT.sizeDelta = Vector2.zero;
        Image fadeImg = globalFadePanel.AddComponent<Image>();
        fadeImg.color = Color.clear;
        fadeImg.raycastTarget = false;

        // Chant Cut-In Panel
        cutInCanvasParent = new GameObject("ChantCutInPanel");
        cutInCanvasParent.transform.SetParent(canvasObj.transform, false);
        RectTransform cutInRT = cutInCanvasParent.AddComponent<RectTransform>();
        cutInRT.anchorMin = Vector2.zero;
        cutInRT.anchorMax = Vector2.one;
        cutInRT.sizeDelta = Vector2.zero;

        GameObject stripe = new GameObject("StripeBg");
        stripe.transform.SetParent(cutInCanvasParent.transform, false);
        RectTransform stRT = stripe.AddComponent<RectTransform>();
        stRT.anchorMin = new Vector2(0f, 0.4f);
        stRT.anchorMax = new Vector2(1f, 0.6f);
        stRT.sizeDelta = Vector2.zero;
        stripe.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        stripe.AddComponent<Outline>().effectColor = Color.cyan;

        GameObject textObj = new GameObject("ChantText");
        textObj.transform.SetParent(stripe.transform, false);
        RectTransform txtRT = textObj.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.sizeDelta = Vector2.zero;

        chantCutInText = textObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) chantCutInText.font = customFont;
        chantCutInText.text = "";
        chantCutInText.fontSize = 32f;
        chantCutInText.fontStyle = FontStyles.Bold;
        chantCutInText.color = Color.white;
        chantCutInText.alignment = TextAlignmentOptions.Center;

        cutInCanvasParent.SetActive(false);

        // HBR Loading screen on dedicated high-sort canvas
        GameObject loadingCanvasObj = new GameObject("GlobalLoadingCanvas");
        Canvas loadCanvas = loadingCanvasObj.AddComponent<Canvas>();
        loadCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        loadCanvas.sortingOrder = 999;

        CanvasScaler scaler = loadingCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        loadingCanvasObj.AddComponent<GraphicRaycaster>();

        globalLoadingPanel = new GameObject("GlobalLoadingPanel");
        globalLoadingPanel.transform.SetParent(loadingCanvasObj.transform, false);

        RectTransform loadRT = globalLoadingPanel.AddComponent<RectTransform>();
        loadRT.anchorMin = Vector2.zero;
        loadRT.anchorMax = Vector2.one;
        loadRT.sizeDelta = Vector2.zero;
        loadRT.anchoredPosition = Vector2.zero;

        Image loadImg = globalLoadingPanel.AddComponent<Image>();
        loadImg.color = new Color(0.04f, 0.05f, 0.08f, 1f);

        GameObject textCont = new GameObject("TextContainer");
        textCont.transform.SetParent(globalLoadingPanel.transform, false);
        RectTransform tcRT = textCont.AddComponent<RectTransform>();
        tcRT.anchorMin = new Vector2(0.1f, 0.45f);
        tcRT.anchorMax = new Vector2(0.9f, 0.65f);
        tcRT.sizeDelta = Vector2.zero;

        GameObject titleObj = new GameObject("TransitionTitle");
        titleObj.transform.SetParent(textCont.transform, false);
        RectTransform tRT = titleObj.AddComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0.4f);
        tRT.anchorMax = new Vector2(1f, 1f);
        tRT.sizeDelta = Vector2.zero;
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) titleText.font = customFont;
        titleText.text = "校舎 <color=#00e5ff>➔</color> <color=#ffb300>校庭</color>";
        titleText.fontSize = 42f;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        GameObject subObj = new GameObject("TransitionSubtitle");
        subObj.transform.SetParent(textCont.transform, false);
        RectTransform sRT = subObj.AddComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 0f);
        sRT.anchorMax = new Vector2(1f, 0.35f);
        sRT.sizeDelta = Vector2.zero;
        loadingStatusText = subObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) loadingStatusText.font = customFont;
        loadingStatusText.text = "NOW LOADING...";
        loadingStatusText.fontSize = 14f;
        loadingStatusText.fontStyle = FontStyles.Italic;
        loadingStatusText.alignment = TextAlignmentOptions.Center;
        loadingStatusText.color = new Color(0.5f, 0.6f, 0.7f, 0.8f);

        GameObject progressFrame = new GameObject("ProgressFrame");
        progressFrame.transform.SetParent(globalLoadingPanel.transform, false);
        RectTransform pfRT = progressFrame.AddComponent<RectTransform>();
        pfRT.anchorMin = new Vector2(0.5f, 0.35f);
        pfRT.anchorMax = new Vector2(0.5f, 0.35f);
        pfRT.sizeDelta = new Vector2(350f, 8f);
        pfRT.anchoredPosition = new Vector2(0f, 0f);
        Image pfImg = progressFrame.AddComponent<Image>();
        pfImg.color = new Color(0.1f, 0.15f, 0.2f, 0.8f);
        Outline pfOutline = progressFrame.AddComponent<Outline>();
        pfOutline.effectColor = new Color(0f, 0.9f, 1f, 0.3f);
        pfOutline.effectDistance = new Vector2(1f, 1f);

        GameObject progressFill = new GameObject("ProgressFill");
        progressFill.transform.SetParent(progressFrame.transform, false);
        RectTransform pfillRT = progressFill.AddComponent<RectTransform>();
        pfillRT.anchorMin = Vector2.zero;
        pfillRT.anchorMax = Vector2.one;
        pfillRT.sizeDelta = Vector2.zero;
        loadingProgressBarFill = progressFill.AddComponent<Image>();
        loadingProgressBarFill.color = new Color(0f, 0.9f, 1f, 1f);
        loadingProgressBarFill.type = Image.Type.Filled;
        loadingProgressBarFill.fillMethod = Image.FillMethod.Horizontal;
        loadingProgressBarFill.fillAmount = 0f;

        globalLoadingPanel.SetActive(false);
    }

    private void ApplyGlobalTMPFontFix()
    {
        EnsureJapaneseFont();
        if (customFont == null) return;

        TMP_Text[] allTexts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (TMP_Text txt in allTexts)
        {
            if (txt == null) continue;
#if UNITY_EDITOR
            if (UnityEditor.EditorUtility.IsPersistent(txt)) continue;
#endif
            txt.font = customFont;
            txt.ForceMeshUpdate();
        }
    }

    private void EnsureJapaneseFont()
    {
        if (customFont != null && customFont.HasCharacter('司') && customFont.HasCharacter('戦'))
        {
            EnsureTMPDefaultFont(customFont);
            return;
        }

#if UNITY_EDITOR
        if (customFont == null)
        {
            customFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
                "Assets/SU3DJPFont/TextMeshProFont/Dynamic/mplus-1p-medium SDF Dynamic.asset");
        }
#endif

        if (customFont == null || !customFont.HasCharacter('司') || !customFont.HasCharacter('戦'))
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var f in fonts)
            {
                if (f == null) continue;
                if (f.HasCharacter('司') && f.HasCharacter('戦'))
                {
                    customFont = f;
                    break;
                }
            }
        }

        if (customFont == null || !customFont.HasCharacter('司') || !customFont.HasCharacter('戦'))
        {
            customFont = CreateRuntimeJapaneseFontAsset();
        }

        if (customFont != null)
        {
            EnsureTMPDefaultFont(customFont);
        }
    }

    private TMP_FontAsset CreateRuntimeJapaneseFontAsset()
    {
        string[] preferredFonts = { "Yu Gothic UI", "Yu Gothic", "Meiryo", "MS Gothic", "MS UI Gothic" };
        Font osFont = Font.CreateDynamicFontFromOSFont(preferredFonts, 90);
        if (osFont == null) return null;

        TMP_FontAsset runtimeFont = TMP_FontAsset.CreateFontAsset(
            osFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        if (runtimeFont == null) return null;
        runtimeFont.name = "RuntimeJapaneseTMPFont";
        return runtimeFont;
    }

    private void EnsureTMPDefaultFont(TMP_FontAsset font)
    {
        if (font == null || TMP_Settings.instance == null) return;
        TMP_Settings.defaultFontAsset = font;
        if (!TMP_Settings.fallbackFontAssets.Contains(font))
        {
            TMP_Settings.fallbackFontAssets.Add(font);
        }
    }

    // ============================================================
    // SampleScene – Explore phase
    // ============================================================
    private void StartExplorePhase()
    {
        currentState = GameState.Explore;
        dialogueSequence = 0;

        if (playerMove != null)
        {
            TeleportCharacter(playerMove.transform, new Vector3(0f, 0.5f, 2.0f));
            playerMove.canMove = true;
        }

        GameObject npcObj = GameObject.Find("NPC");
        if (npcObj != null) TeleportCharacter(npcObj.transform, new Vector3(3f, 0.5f, 2.0f));

        if (cameraFollow != null)
        {
            cameraFollow.enabled = true;
            cameraFollow.target = playerMove != null ? playerMove.transform : null;
            cameraFollow.isTalking = false;
        }
    }

    // ============================================================
    // Dialogue completion flow
    // ============================================================
    public void OnDialogueComplete(NPCDialogue source)
    {
        if (dialogueSequence == 0)
        {
            StartCoroutine(TriggerAlertSequence());
        }
        else if (dialogueSequence == 1)
        {
            StartCoroutine(TransitionToBattleScene());
        }
        else if (dialogueSequence == 2)
        {
            StartCoroutine(SummonRitualSequence());
        }
    }

    private IEnumerator TriggerAlertSequence()
    {
        currentState = GameState.Alert;
        if (playerMove != null) playerMove.canMove = false;

        if (alertSystem != null) alertSystem.StartAlert();

        yield return new WaitForSeconds(2.0f);

        dialogueSequence = 1;

        string[] alertLines = new string[]
        {
            "司令官「【緊急通信】非常事態発生！校庭にキャンサーの群れが出現しました！」",
            "司令官「新入生たちよ、直ちに校庭へ急行し、キャンサーを撃退しなさい！」"
        };

        if (npcDialogue != null) npcDialogue.StartDialogueSequence("司令官", alertLines);
    }

    // ============================================================
    // Transition to Battle scene via SceneManager
    // ============================================================
    private IEnumerator TransitionToBattleScene()
    {
        if (globalLoadingPanel == null) CreateGlobalUI();

        globalLoadingPanel.SetActive(true);
        CanvasGroup cg = globalLoadingPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = globalLoadingPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // Fade in loading screen
        float elapsed = 0f;
        float fadeDuration = 0.5f;
        while (elapsed < fadeDuration)
        {
            cg.alpha = elapsed / fadeDuration;
            elapsed += Time.deltaTime;
            yield return null;
        }
        cg.alpha = 1f;

        if (alertSystem != null) alertSystem.StopAlert();

        // Progress bar animation while loading async
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Battle");
        asyncLoad.allowSceneActivation = false;

        float loadElapsed = 0f;
        float loadDuration = 2.0f;
        while (loadElapsed < loadDuration || asyncLoad.progress < 0.9f)
        {
            loadElapsed += Time.deltaTime;
            float engineProgress = asyncLoad.progress / 0.9f;
            float timerProgress = Mathf.Clamp01(loadElapsed / loadDuration);
            float progress = Mathf.Max(engineProgress, timerProgress);
            progress = Mathf.Clamp01(progress);

            if (loadingProgressBarFill != null) loadingProgressBarFill.fillAmount = progress;
            if (loadingStatusText != null)
                loadingStatusText.text = "LOCATING BATTLEFIELD ZONE... " + Mathf.RoundToInt(progress * 100f) + "%";

            yield return null;
        }

        if (loadingProgressBarFill != null) loadingProgressBarFill.fillAmount = 1f;
        if (loadingStatusText != null) loadingStatusText.text = "PREPARATION COMPLETE";
        yield return new WaitForSeconds(0.4f);

        // Activate the Battle scene
        asyncLoad.allowSceneActivation = true;

        // Fade out loading screen after scene switch
        yield return new WaitForSeconds(0.3f);
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            if (cg != null) cg.alpha = 1f - (elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (globalLoadingPanel != null) globalLoadingPanel.SetActive(false);
    }

    // ============================================================
    // Summon ritual (runs in Battle scene after dialogue)
    // ============================================================
    private IEnumerator SummonRitualSequence()
    {
        if (npcDialogue != null && npcDialogue.dialoguePanel != null)
            npcDialogue.dialoguePanel.SetActive(false);

        if (cutInCanvasParent == null) CreateGlobalUI();

        if (cutInCanvasParent != null) cutInCanvasParent.SetActive(true);

        if (chantCutInText != null)
        {
            chantCutInText.text = "戦友\n『 HelloWorld! 』";
            chantCutInText.color = Color.green;
        }
        StartCoroutine(ScreenShake(0.4f, 0.1f));
        PlayChantAudioSynth(800f, 0.4f);

        yield return new WaitForSeconds(1.5f);

        if (chantCutInText != null)
        {
            chantCutInText.text = "主人公\n『 あたしの快進撃はこれから始まる！ 』";
            chantCutInText.color = new Color(0f, 0.8f, 1f);
        }
        StartCoroutine(ScreenShake(0.5f, 0.15f));
        PlayChantAudioSynth(1200f, 0.5f);

        yield return new WaitForSeconds(1.8f);
        if (cutInCanvasParent != null) cutInCanvasParent.SetActive(false);

        // Weapon summon then start battle
        List<Transform> allies = new List<Transform>();

        // In Battle scene, look for BattleSceneBootstrapper ally transforms
        BattleSceneBootstrapper bootstrapper = FindObjectOfType<BattleSceneBootstrapper>();
        if (bootstrapper != null)
        {
            allies.AddRange(bootstrapper.allyTransforms);
        }
        else
        {
            // Fallback: create placeholder positions
            for (int i = 0; i < 6; i++)
            {
                GameObject dummy = new GameObject("BattleAlly_" + i);
                dummy.transform.position = new Vector3(-4f - (i * 1.0f), 0f, -1f + (i % 2) * 2f);
                allies.Add(dummy.transform);
            }
        }

        if (weaponSummoner != null)
        {
            weaponSummoner.SummonWeapons(allies, () =>
            {
                StartBattleMode(allies);
            });
        }
        else
        {
            StartBattleMode(allies);
        }
    }

    // ============================================================
    // Battle mode start (within Battle scene)
    // ============================================================
    private void StartBattleMode(List<Transform> allyTransforms)
    {
        currentState = GameState.Battle;

        List<Transform> enemies = new List<Transform>();

        // Find enemy spawns set up by BattleSceneBootstrapper
        BattleSceneBootstrapper bootstrapper = FindObjectOfType<BattleSceneBootstrapper>();
        if (bootstrapper != null)
        {
            enemies.AddRange(bootstrapper.enemyTransforms);
        }
        else
        {
            // Fallback placeholder enemies
            for (int i = 0; i < 2; i++)
            {
                GameObject dummy = new GameObject("Enemy_" + i);
                dummy.transform.position = new Vector3(4f + i * 2f, 0f, 0f);
                enemies.Add(dummy.transform);
            }
        }

        if (battleManager != null) battleManager.StartBattle(allyTransforms, enemies);
        else Debug.LogError("GameManager: BattleManager is null in Battle scene!");
    }

    // ============================================================
    // Battle end
    // ============================================================
    public void EndBattle(bool isPlayerVictory)
    {
        if (isPlayerVictory)
        {
            currentState = GameState.Victory;
            StartCoroutine(ShowResultScreen("VICTORY", new Color(0f, 0.8f, 1f, 0.85f), true));
        }
        else
        {
            currentState = GameState.GameOver;
            StartCoroutine(ShowResultScreen("DEFEAT", new Color(0.6f, 0f, 0f, 0.85f), false));
        }
    }

    private IEnumerator ShowResultScreen(string banner, Color bgCol, bool isVictory)
    {
        PlayEndChimeSynth(isVictory);

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject co = new GameObject("ResultCanvas");
            canvas = co.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            co.AddComponent<CanvasScaler>();
            co.AddComponent<GraphicRaycaster>();
        }

        GameObject resultPanel = new GameObject("BattleResultPanel");
        resultPanel.transform.SetParent(canvas.transform, false);

        RectTransform rt = resultPanel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        Image img = resultPanel.AddComponent<Image>();
        img.color = bgCol;

        // Banner text
        GameObject bannerObj = new GameObject("BannerText");
        bannerObj.transform.SetParent(resultPanel.transform, false);
        RectTransform btRT = bannerObj.AddComponent<RectTransform>();
        btRT.anchorMin = new Vector2(0.1f, 0.45f);
        btRT.anchorMax = new Vector2(0.9f, 0.65f);
        btRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI bt = bannerObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) bt.font = customFont;
        bt.text = banner;
        bt.fontSize = 72f;
        bt.fontStyle = FontStyles.Bold;
        bt.color = Color.white;
        bt.alignment = TextAlignmentOptions.Center;

        // Sub description
        GameObject descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(resultPanel.transform, false);
        RectTransform dtRT = descObj.AddComponent<RectTransform>();
        dtRT.anchorMin = new Vector2(0.1f, 0.25f);
        dtRT.anchorMax = new Vector2(0.9f, 0.42f);
        dtRT.sizeDelta = Vector2.zero;

        TextMeshProUGUI dt = descObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) dt.font = customFont;
        dt.text = isVictory
            ? "キャンサーの撃破に成功しました！学校の平和は保たれました。\n画面をクリックすると、探索モードに戻ります。"
            : "敗北しました…。セラフ部隊は全滅しました。\n画面をクリックすると、再挑戦します。";
        dt.fontSize = 18f;
        dt.alignment = TextAlignmentOptions.Center;
        dt.color = Color.white;

        yield return new WaitForSeconds(1.5f);

        Button clickBtn = resultPanel.AddComponent<Button>();
        clickBtn.onClick.AddListener(() =>
        {
            Destroy(resultPanel);
            if (isVictory)
            {
                SceneManager.LoadScene("SampleScene");
            }
            else
            {
                SceneManager.LoadScene("Battle");
            }
        });
    }

    // ============================================================
    // Audio synth helpers
    // ============================================================
    private void PlayChantAudioSynth(float freq, float duration)
    {
        GameObject synthObj = new GameObject("ChantSynthSound");
        AudioSource src = synthObj.AddComponent<AudioSource>();
        src.volume = 0.5f;

        ChantSynth synth = synthObj.AddComponent<ChantSynth>();
        synth.frequency = freq;
        synth.duration = duration;

        src.Play();
        Destroy(synthObj, duration + 0.2f);
    }

    private void PlayEndChimeSynth(bool isVictory)
    {
        GameObject synthObj = new GameObject("EndChimeSynth");
        AudioSource src = synthObj.AddComponent<AudioSource>();
        src.volume = 0.5f;

        EndChimeSynth synth = synthObj.AddComponent<EndChimeSynth>();
        synth.isVictory = isVictory;

        src.Play();
        Destroy(synthObj, 4.0f);
    }

    // ============================================================
    // Camera shake
    // ============================================================
    private IEnumerator ScreenShake(float duration, float magnitude)
    {
        if (Camera.main == null) yield break;
        Vector3 camOrigPos = Camera.main.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            Camera.main.transform.position = new Vector3(camOrigPos.x + x, camOrigPos.y + y, camOrigPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Camera.main.transform.position = camOrigPos;
    }

    // ============================================================
    // Teleport helper
    // ============================================================
    private void TeleportCharacter(Transform target, Vector3 targetPosition)
    {
        if (target == null) return;

        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            target.position = targetPosition;
            cc.enabled = true;
        }
        else
        {
            target.position = targetPosition;
        }
    }
}

// ============================================================
// Audio synth sub-components
// ============================================================
public class ChantSynth : MonoBehaviour
{
    public float frequency = 1000f;
    public float duration = 0.5f;
    private double phase = 0.0;
    private double sampling_frequency = 48000;
    private long sampleCount = 0;

    void OnAudioFilterRead(float[] data, int channels)
    {
        double doubleSampleRate = sampling_frequency;

        for (int i = 0; i < data.Length; i += channels)
        {
            float t = (float)((double)sampleCount / doubleSampleRate);

            if (t > duration)
            {
                for (int c = 0; c < channels; c++) data[i + c] = 0f;
                sampleCount++;
                continue;
            }

            double pitch = frequency + t * 400f;
            phase += 2.0 * Mathf.PI * pitch / doubleSampleRate;
            float sample = (float)Mathf.Sin((float)phase);
            float amp = Mathf.Sin(t / duration * Mathf.PI);

            for (int c = 0; c < channels; c++) data[i + c] = sample * amp * 0.35f;

            if (phase > 2.0 * Mathf.PI * 100.0) phase -= 2.0 * Mathf.PI * 100.0;

            sampleCount++;
        }
    }
}

public class EndChimeSynth : MonoBehaviour
{
    public bool isVictory = true;
    private double phase = 0.0;
    private double sampling_frequency = 48000;
    private long sampleCount = 0;

    private float[] victoryPitches = { 523.25f, 659.25f, 783.99f, 1046.50f };
    private float[] defeatPitches = { 220f, 207.65f, 196f, 155.56f };

    void OnAudioFilterRead(float[] data, int channels)
    {
        double doubleSampleRate = sampling_frequency;

        for (int i = 0; i < data.Length; i += channels)
        {
            float t = (float)((double)sampleCount / doubleSampleRate);

            if (t > 3.0f)
            {
                for (int c = 0; c < channels; c++) data[i + c] = 0f;
                sampleCount++;
                continue;
            }

            double pitch = 0.0;
            float sample = 0f;

            if (isVictory)
            {
                int noteIndex = Mathf.Clamp((int)(t / 0.5f), 0, 3);
                pitch = victoryPitches[noteIndex];
                phase += 2.0 * Mathf.PI * pitch / doubleSampleRate;
                sample = (float)Mathf.Sin((float)phase) * 0.4f + (float)Mathf.Sin((float)(phase * 1.5)) * 0.2f;
            }
            else
            {
                int noteIndex = Mathf.Clamp((int)(t / 0.7f), 0, 3);
                pitch = defeatPitches[noteIndex];
                pitch -= (t % 0.7f) * 15f;
                phase += 2.0 * Mathf.PI * pitch / doubleSampleRate;
                float noise = (float)(new System.Random().NextDouble() * 2.0 - 1.0);
                sample = (float)Mathf.Sin((float)phase) * 0.5f + noise * 0.2f;
            }

            float env = Mathf.Clamp01(1f - t / 3.0f);

            for (int c = 0; c < channels; c++) data[i + c] = sample * env * 0.35f;

            if (phase > 2.0 * Mathf.PI * 100.0) phase -= 2.0 * Mathf.PI * 100.0;

            sampleCount++;
        }
    }
}
