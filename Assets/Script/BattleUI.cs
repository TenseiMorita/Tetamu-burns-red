using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class BattleUI : MonoBehaviour
{
    private BattleManager battleManager;
    public BattleManager Manager => battleManager;
    private GameObject uiParent;

    private List<Slider> enemyHPSliders = new List<Slider>();
    private List<Slider> enemyDPSliders = new List<Slider>();
    private List<TextMeshProUGUI> enemyHPTexts = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> enemyDPTexts = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> enemyNameTexts = new List<TextMeshProUGUI>();

    // 6 Character Cards
    private List<GameObject> cardObjs = new List<GameObject>();
    private List<Slider> charHPSliders = new List<Slider>();
    private List<Slider> charDPSliders = new List<Slider>();
    private List<TextMeshProUGUI> charHPTexts = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> charDPTexts = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> charSPTexts = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> charNameTexts = new List<TextMeshProUGUI>();
    private List<Image> charCardBgs = new List<Image>();
    private List<TextMeshProUGUI> charSelectedActionTexts = new List<TextMeshProUGUI>();

    private ActionType[] selectedActions = new ActionType[3];

    private Button turnStartButton;
    private Button swapAllButton;
    private Slider overdriveSlider;
    private TextMeshProUGUI overdriveText;
    private Button overdriveButton;

    // Skill Menu
    private GameObject skillMenuPanel;
    private int currentSkillMenuCol = -1;
    private List<Button> skillMenuButtons = new List<Button>();
    private List<TextMeshProUGUI> skillMenuTexts = new List<TextMeshProUGUI>();

    private bool uiCreated = false;
    private TMP_FontAsset customFont;
    private int draggingCardIndex = -1;

    // HBR Colour Palette
    private static readonly Color ColBgDark = new Color(0.04f, 0.05f, 0.09f, 0.96f);
    private static readonly Color ColBgMid  = new Color(0.07f, 0.10f, 0.18f, 0.98f);
    private static readonly Color ColNeon   = new Color(0f, 0.90f, 1f, 1f);
    private static readonly Color ColGold   = new Color(1f, 0.70f, 0f, 1f);
    private static readonly Color ColRed    = new Color(0.85f, 0.15f, 0.10f, 1f);
    private static readonly Color ColGreen  = new Color(0f, 0.90f, 0.35f, 1f);
    private static readonly Color ColWhite  = Color.white;
    private static readonly Color ColGray   = new Color(0.45f, 0.45f, 0.50f, 1f);
    private static readonly Color ColMagenta = new Color(0.9f, 0.1f, 0.9f, 1f);

    public void Show(BattleManager manager)
    {
        battleManager = manager;
        EnsureEventSystem();

        if (!uiCreated)
        {
            if (manager != null && manager.gameManager != null)
                customFont = manager.gameManager.customFont;
            CreateProceduralUI();
        }

        for (int i = 0; i < 3; i++) selectedActions[i] = ActionType.Attack;

        uiParent.SetActive(true);
        skillMenuPanel.SetActive(false);
        UpdateUI();
        SetButtonsEnabled(true);
    }

    public void Hide()
    {
        if (uiParent != null) uiParent.SetActive(false);
    }

    public void SetButtonsEnabled(bool enabled)
    {
        if (turnStartButton != null) turnStartButton.interactable = enabled;
        if (overdriveButton != null) overdriveButton.interactable = enabled;
        if (swapAllButton != null) swapAllButton.interactable = enabled;
    }

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<EventSystem>();
        esObj.AddComponent<StandaloneInputModule>();
    }

    public void UpdateUI()
    {
        if (!uiCreated || battleManager == null) return;

        // Enemy panels
        for (int i = 0; i < enemyHPSliders.Count; i++)
        {
            if (i < battleManager.enemies.Count)
            {
                Fighter e = battleManager.enemies[i];
                enemyNameTexts[i].text = e.name;
                enemyHPSliders[i].maxValue = e.maxHP;
                enemyHPSliders[i].value = e.currentHP;
                enemyHPTexts[i].text = $"{e.currentHP} / {e.maxHP}";

                enemyDPSliders[i].maxValue = e.maxDP;
                enemyDPSliders[i].value = e.currentDP;
                enemyDPTexts[i].text = e.currentDP > 0 ? $"{e.currentDP} / {e.maxDP}" : "BREAK";
                enemyDPSliders[i].fillRect.GetComponent<Image>().color = e.currentDP > 0 ? ColNeon : ColGray;

                enemyHPSliders[i].transform.parent.gameObject.SetActive(e.currentHP > 0);
            }
        }

        // 6 Character Cards
        for (int i = 0; i < 6; i++)
        {
            bool isFront = i < 3;
            Fighter f = isFront ? battleManager.frontLine[i] : battleManager.backLine[i - 3];
            
            if (f != null)
            {
                charNameTexts[i].text = f.name;
                charHPSliders[i].maxValue = f.maxHP;
                charHPSliders[i].value = f.currentHP;
                charHPTexts[i].text = $"HP {f.currentHP}";

                charDPSliders[i].maxValue = f.maxDP;
                charDPSliders[i].value = f.currentDP;
                charDPTexts[i].text = f.currentDP > 0 ? $"DP {f.currentDP}" : "BREAK";
                charDPSliders[i].fillRect.GetComponent<Image>().color = f.currentDP > 0 ? ColNeon : ColGray;

                charSPTexts[i].text = $"SP {f.sp}";

                charCardBgs[i].color = isFront ? new Color(0.1f, 0.15f, 0.25f, 0.95f) : new Color(0.05f, 0.05f, 0.08f, 0.85f);
                cardObjs[i].transform.localScale = isFront ? Vector3.one : Vector3.one * 0.9f;

                if (isFront)
                {
                    // Enforce SP rules
                    if (selectedActions[i] == ActionType.Slash && f.sp < 2) selectedActions[i] = ActionType.Attack;
                    if (selectedActions[i] == ActionType.Shot && f.sp < 2) selectedActions[i] = ActionType.Attack;
                    if (selectedActions[i] == ActionType.EpicStrike && f.sp < 6) selectedActions[i] = ActionType.Attack;
                    if (selectedActions[i] == ActionType.Heal && f.sp < 4) selectedActions[i] = ActionType.Attack;
                }
            }
        }

        // Overdrive
        overdriveSlider.maxValue = battleManager.maxOverdrive;
        overdriveSlider.value = battleManager.currentOverdrive;
        int odLevel = battleManager.currentOverdrive / 100;
        overdriveText.text = $"OD LV {odLevel}";
        overdriveButton.interactable = odLevel > 0;

        UpdateSelectionHighlights();
    }

    private void UpdateSelectionHighlights()
    {
        for (int i = 0; i < 3; i++)
        {
            ActionType sel = selectedActions[i];
            string actionName = "通常攻撃"; // Default Attack
            Color col = ColWhite;

            if (sel == ActionType.Slash) { actionName = "クロス斬り"; col = ColNeon; }
            else if (sel == ActionType.Shot) { actionName = "バースト射撃"; col = ColNeon; }
            else if (sel == ActionType.EpicStrike) { actionName = "エピックスストライク"; col = ColMagenta; }
            else if (sel == ActionType.Heal) { actionName = "リカバー"; col = ColGreen; }

            charSelectedActionTexts[i].text = actionName;
            charSelectedActionTexts[i].color = col;
        }
    }

    private void OpenSkillMenu(int colIndex)
    {
        currentSkillMenuCol = colIndex;
        Fighter f = battleManager.frontLine[colIndex];
        if (f == null) return;

        skillMenuPanel.SetActive(true);

        // Setup button 1: Attack
        skillMenuTexts[0].text = "通常攻撃\n<size=14>敵単体にダメージ</size>\n<color=#00ffff>SP 0</color>";
        skillMenuButtons[0].interactable = true;
        skillMenuButtons[0].onClick.RemoveAllListeners();
        skillMenuButtons[0].onClick.AddListener(() => { SelectSkill(ActionType.Attack); });

        // Setup button 2 & 3 based on character
        if (f.name == "主人公" || f.name.Contains("0") || f.name.Contains("1")) // Attacker type
        {
            skillMenuTexts[1].text = "クロス斬り\n<size=14>二刀で敵を斬りつける [対HP+30%]</size>\n<color=#00ffff>SP 2</color>";
            skillMenuButtons[1].interactable = f.sp >= 2;
            skillMenuButtons[1].onClick.RemoveAllListeners();
            skillMenuButtons[1].onClick.AddListener(() => { SelectSkill(ActionType.Slash); });

            skillMenuTexts[2].text = "エピックスストライク\n<size=14>強力な必殺技 [全体ダメージ]</size>\n<color=#ff00ff>SP 6</color>";
            skillMenuButtons[2].interactable = f.sp >= 6;
            skillMenuButtons[2].onClick.RemoveAllListeners();
            skillMenuButtons[2].onClick.AddListener(() => { SelectSkill(ActionType.EpicStrike); });
        }
        else // Support/Healer type
        {
            skillMenuTexts[1].text = "バースト射撃\n<size=14>敵単体に射撃 [対DP+30%]</size>\n<color=#00ffff>SP 2</color>";
            skillMenuButtons[1].interactable = f.sp >= 2;
            skillMenuButtons[1].onClick.RemoveAllListeners();
            skillMenuButtons[1].onClick.AddListener(() => { SelectSkill(ActionType.Shot); });

            skillMenuTexts[2].text = "リカバー\n<size=14>聖なる光で味方単体のDPを回復する</size>\n<color=#00ff33>SP 4</color>";
            skillMenuButtons[2].interactable = f.sp >= 4;
            skillMenuButtons[2].onClick.RemoveAllListeners();
            skillMenuButtons[2].onClick.AddListener(() => { SelectSkill(ActionType.Heal); });
        }
    }

    private void SelectSkill(ActionType action)
    {
        if (currentSkillMenuCol >= 0 && currentSkillMenuCol < 3)
        {
            selectedActions[currentSkillMenuCol] = action;
            UpdateSelectionHighlights();
        }
        skillMenuPanel.SetActive(false);
    }

    private void CreateProceduralUI()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("BattleCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        canvas.sortingOrder = 10;

        uiParent = new GameObject("HBR_BattlePanel");
        uiParent.transform.SetParent(canvas.transform, false);
        RectTransform parentRT = uiParent.AddComponent<RectTransform>();
        parentRT.anchorMin = Vector2.zero;
        parentRT.anchorMax = Vector2.one;
        parentRT.sizeDelta = Vector2.zero;

        // Overlay
        Image rootImg = uiParent.AddComponent<Image>();
        rootImg.color = new Color(0f, 0f, 0f, 0.1f);
        rootImg.raycastTarget = false;

        // Enemy Panels (Top Left)
        for (int i = 0; i < 2; i++)
        {
            float minX = 0.05f + (i * 0.25f);
            CreateEnemyPanel(uiParent.transform, "Enemy_" + i, new Vector2(minX, 0.85f), new Vector2(minX + 0.22f, 0.98f));
        }

        // Overdrive Gauge (Top Right)
        CreateOverdrivePanel(uiParent.transform);

        // Turn Start Button (Bottom Right)
        CreateTurnStartButton(uiParent.transform);

        // 6 Character Cards (Bottom Center)
        for (int i = 0; i < 6; i++)
        {
            float width = 0.12f;
            float spacing = 0.015f;
            float startX = 0.05f;
            float minX = startX + (i * (width + spacing));
            
            CreateCharacterCard(uiParent.transform, "CharCard_" + i, new Vector2(minX, 0.02f), new Vector2(minX + width, 0.22f), i);
        }

        CreateSkillMenuPanel(uiParent.transform);

        uiCreated = true;
    }

    private void CreateSkillMenuPanel(Transform parent)
    {
        skillMenuPanel = MakeRect(parent, "SkillMenuPanel", new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.8f));
        Image bg = skillMenuPanel.AddComponent<Image>();
        bg.color = ColBgMid;
        Outline ol = skillMenuPanel.AddComponent<Outline>();
        ol.effectColor = ColNeon;

        GameObject closeBtnObj = MakeRect(skillMenuPanel.transform, "CloseBtn", new Vector2(0.9f, 0.85f), new Vector2(0.98f, 0.95f));
        Image cbImg = closeBtnObj.AddComponent<Image>();
        cbImg.color = ColRed;
        Button closeBtn = closeBtnObj.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => skillMenuPanel.SetActive(false));
        TextMeshProUGUI cbTxt = MakeRect(closeBtnObj.transform, "Txt", Vector2.zero, Vector2.one).AddComponent<TextMeshProUGUI>();
        cbTxt.text = "X";
        cbTxt.alignment = TextAlignmentOptions.Center;

        for (int i = 0; i < 3; i++)
        {
            float gap = 0.05f;
            float h = 0.25f;
            float yMax = 0.8f - (i * (h + gap));
            
            GameObject btnObj = MakeRect(skillMenuPanel.transform, "SkillBtn_" + i, new Vector2(0.05f, yMax - h), new Vector2(0.95f, yMax));
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);
            Button btn = btnObj.AddComponent<Button>();
            skillMenuButtons.Add(btn);

            GameObject txtObj = MakeRect(btnObj.transform, "Txt", new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.9f));
            TextMeshProUGUI txt = txtObj.AddComponent<TextMeshProUGUI>();
            if (customFont != null) txt.font = customFont;
            txt.alignment = TextAlignmentOptions.MidlineLeft;
            txt.fontSize = 20f;
            skillMenuTexts.Add(txt);
        }

        skillMenuPanel.SetActive(false);
    }

    private void CreateEnemyPanel(Transform parent, string name, Vector2 minAnchor, Vector2 maxAnchor)
    {
        GameObject panel = MakeRect(parent, name, minAnchor, maxAnchor);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);

        GameObject eName = MakeRect(panel.transform, "Name", new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.9f));
        TextMeshProUGUI eNameTxt = eName.AddComponent<TextMeshProUGUI>();
        if (customFont != null) eNameTxt.font = customFont;
        eNameTxt.color = ColNeon;
        eNameTxt.alignment = TextAlignmentOptions.MidlineLeft;
        enemyNameTexts.Add(eNameTxt);

        Slider dpSl = CreateSlider(panel.transform, "DP", new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.55f), ColNeon);
        enemyDPSliders.Add(dpSl);
        enemyDPTexts.Add(dpSl.transform.Find("ValueText").GetComponent<TextMeshProUGUI>());

        Slider hpSl = CreateSlider(panel.transform, "HP", new Vector2(0.05f, 0.1f), new Vector2(0.95f, 0.3f), ColRed);
        enemyHPSliders.Add(hpSl);
        enemyHPTexts.Add(hpSl.transform.Find("ValueText").GetComponent<TextMeshProUGUI>());
    }

    private void CreateCharacterCard(Transform parent, string name, Vector2 minAnchor, Vector2 maxAnchor, int index)
    {
        GameObject card = MakeRect(parent, name, minAnchor, maxAnchor);
        Image bg = card.AddComponent<Image>();
        charCardBgs.Add(bg);

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.2f);
        outline.effectDistance = new Vector2(1f, -1f);

        Button btn = card.AddComponent<Button>();
        if (index >= 3)
        {
            // Backline - Swap
            int col = index - 3;
            btn.onClick.AddListener(() => battleManager.SwapFrontBack(col));
            
            GameObject swapObj = MakeRect(card.transform, "SwapIcon", new Vector2(0.1f, 0.7f), new Vector2(0.3f, 0.9f));
            TextMeshProUGUI swapTxt = swapObj.AddComponent<TextMeshProUGUI>();
            if (customFont != null) swapTxt.font = customFont;
            swapTxt.text = "⇅";
            swapTxt.color = ColGold;
            swapTxt.fontSize = 24f;
            swapTxt.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            // Frontline - Open Skill Menu
            int col = index;
            btn.onClick.AddListener(() => OpenSkillMenu(col));

            GameObject actObj = MakeRect(card.transform, "SelectedAction", new Vector2(0f, 1.05f), new Vector2(1f, 1.25f));
            Image actBg = actObj.AddComponent<Image>();
            actBg.color = new Color(0f, 0f, 0f, 0.7f);
            TextMeshProUGUI actTxt = MakeRect(actObj.transform, "Txt", Vector2.zero, Vector2.one).AddComponent<TextMeshProUGUI>();
            if (customFont != null) actTxt.font = customFont;
            actTxt.alignment = TextAlignmentOptions.Center;
            actTxt.text = "通常攻撃";
            charSelectedActionTexts.Add(actTxt);
        }

        GameObject nObj = MakeRect(card.transform, "Name", new Vector2(0.05f, 0.75f), new Vector2(0.95f, 0.95f));
        TextMeshProUGUI nTxt = nObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) nTxt.font = customFont;
        nTxt.alignment = TextAlignmentOptions.Center;
        charNameTexts.Add(nTxt);

        Slider dpSl = CreateSlider(card.transform, "DP", new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.65f), ColNeon);
        charDPSliders.Add(dpSl);
        charDPTexts.Add(dpSl.transform.Find("ValueText").GetComponent<TextMeshProUGUI>());

        Slider hpSl = CreateSlider(card.transform, "HP", new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.4f), ColRed);
        charHPSliders.Add(hpSl);
        charHPTexts.Add(hpSl.transform.Find("ValueText").GetComponent<TextMeshProUGUI>());

        GameObject spObj = MakeRect(card.transform, "SP", new Vector2(0.05f, 0f), new Vector2(0.95f, 0.18f));
        TextMeshProUGUI spTxt = spObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) spTxt.font = customFont;
        spTxt.color = ColGold;
        spTxt.alignment = TextAlignmentOptions.Center;
        charSPTexts.Add(spTxt);

        cardObjs.Add(card);

        BattleCardDragHandler dragHandler = card.AddComponent<BattleCardDragHandler>();
        dragHandler.Initialize(this, index);

        CanvasGroup cg = card.GetComponent<CanvasGroup>();
        if (cg == null) card.AddComponent<CanvasGroup>();
    }

    private void CreateOverdrivePanel(Transform parent)
    {
        GameObject panel = MakeRect(parent, "Overdrive", new Vector2(0.7f, 0.85f), new Vector2(0.95f, 0.95f));
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);
        
        overdriveSlider = CreateSlider(panel.transform, "ODGauge", new Vector2(0.05f, 0.1f), new Vector2(0.65f, 0.5f), ColMagenta);
        
        GameObject txtObj = MakeRect(panel.transform, "Txt", new Vector2(0.05f, 0.55f), new Vector2(0.65f, 0.9f));
        overdriveText = txtObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) overdriveText.font = customFont;
        overdriveText.color = ColWhite;
        overdriveText.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject btnObj = MakeRect(panel.transform, "ODBtn", new Vector2(0.7f, 0.1f), new Vector2(0.95f, 0.9f));
        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = ColMagenta;
        overdriveButton = btnObj.AddComponent<Button>();
        overdriveButton.onClick.AddListener(() => battleManager.ActivateOverdrive());
        
        GameObject bTxtObj = MakeRect(btnObj.transform, "BtnTxt", Vector2.zero, Vector2.one);
        TextMeshProUGUI bTxt = bTxtObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) bTxt.font = customFont;
        bTxt.text = "OD";
        bTxt.alignment = TextAlignmentOptions.Center;
    }

    private void CreateTurnStartButton(Transform parent)
    {
        GameObject tsObj = MakeRect(parent, "TurnStart", new Vector2(0.86f, 0.05f), new Vector2(0.98f, 0.25f));
        Image img = tsObj.AddComponent<Image>();
        img.color = new Color(0.9f, 0.2f, 0.4f, 1f);

        turnStartButton = tsObj.AddComponent<Button>();
        turnStartButton.onClick.AddListener(() => 
        {
            skillMenuPanel.SetActive(false);
            battleManager.ExecuteTurn(selectedActions);
        });
        
        GameObject tObj = MakeRect(tsObj.transform, "Txt", Vector2.zero, Vector2.one);
        TextMeshProUGUI txt = tObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) txt.font = customFont;
        txt.text = "行動開始\nSTART";
        txt.alignment = TextAlignmentOptions.Center;
        txt.fontSize = 20f;

        GameObject swObj = MakeRect(parent, "SwapAll", new Vector2(0.72f, 0.05f), new Vector2(0.85f, 0.16f));
        Image swImg = swObj.AddComponent<Image>();
        swImg.color = new Color(0.2f, 0.5f, 0.9f, 0.95f);
        swapAllButton = swObj.AddComponent<Button>();
        swapAllButton.onClick.AddListener(() =>
        {
            if (battleManager == null) return;
            if (!battleManager.TrySwapAllFrontBack()) return;
            for (int i = 0; i < selectedActions.Length; i++) selectedActions[i] = ActionType.Attack;
            UpdateSelectionHighlights();
            UpdateUI();
        });

        GameObject sTxtObj = MakeRect(swObj.transform, "Txt", Vector2.zero, Vector2.one);
        TextMeshProUGUI sTxt = sTxtObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) sTxt.font = customFont;
        sTxt.text = "前後衛\n一括入替";
        sTxt.alignment = TextAlignmentOptions.Center;
        sTxt.fontSize = 16f;
    }

    private Slider CreateSlider(Transform parent, string name, Vector2 minA, Vector2 maxA, Color c)
    {
        GameObject slObj = MakeRect(parent, name, minA, maxA);
        Slider slider = slObj.AddComponent<Slider>();
        slider.interactable = false; // Lock dragging
        
        GameObject bgObj = MakeRect(slObj.transform, "Bg", Vector2.zero, Vector2.one);
        bgObj.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

        GameObject fillArea = MakeRect(slObj.transform, "FillArea", Vector2.zero, Vector2.one);
        GameObject fill = MakeRect(fillArea.transform, "Fill", Vector2.zero, Vector2.one);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = c;

        slider.fillRect = fill.GetComponent<RectTransform>();

        GameObject valTxtObj = MakeRect(slObj.transform, "ValueText", Vector2.zero, Vector2.one);
        TextMeshProUGUI valTxt = valTxtObj.AddComponent<TextMeshProUGUI>();
        if (customFont != null) valTxt.font = customFont;
        valTxt.alignment = TextAlignmentOptions.Center;
        valTxt.fontSize = 12f;
        valTxt.fontStyle = FontStyles.Bold;

        return slider;
    }

    private static GameObject MakeRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return go;
    }

    public void BeginCardDrag(int cardIndex)
    {
        draggingCardIndex = cardIndex;
    }

    public void EndCardDrag()
    {
        draggingCardIndex = -1;
    }

    public void HandleCardDrop(int targetCardIndex)
    {
        if (battleManager == null) return;
        if (draggingCardIndex < 0) return;
        if (draggingCardIndex == targetCardIndex) return;

        int from = draggingCardIndex;
        int to = targetCardIndex;

        bool fromFront = from < 3;
        bool toFront = to < 3;

        bool swapped = false;
        if (fromFront && !toFront)
        {
            swapped = battleManager.TrySwapFrontAndBack(from, to - 3);
            if (swapped) selectedActions[from] = ActionType.Attack;
        }
        else if (!fromFront && toFront)
        {
            swapped = battleManager.TrySwapFrontAndBack(to, from - 3);
            if (swapped) selectedActions[to] = ActionType.Attack;
        }

        if (!swapped) return;
        UpdateSelectionHighlights();
        UpdateUI();
    }
}

public class BattleCardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private BattleUI battleUI;
    private int cardIndex;
    private CanvasGroup canvasGroup;
    private bool isDragging;

    public void Initialize(BattleUI ui, int index)
    {
        battleUI = ui;
        cardIndex = index;
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (battleUI == null) return;
        if (canvasGroup == null) return;
        if (battleUI.Manager == null) return;
        if (!battleUI.Manager.CanCardBeSwitched(cardIndex)) return;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.75f;
        isDragging = true;
        battleUI.BeginCardDrag(cardIndex);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Intentionally do nothing.
        // Keep card anchored in layout and use drag only for drop target detection.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || canvasGroup == null) return;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;
        isDragging = false;
        if (battleUI != null) battleUI.EndCardDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (battleUI == null) return;
        battleUI.HandleCardDrop(cardIndex);
    }
}
