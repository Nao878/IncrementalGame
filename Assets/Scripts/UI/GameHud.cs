using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 画面上部のヒント・スコア・モード切替ボタン、および下部の施設配置ボタン(ボトムバー)を生成・管理するHUD。
/// </summary>
public class GameHud : MonoBehaviour
{
    private TextMeshProUGUI coinText;
    private TextMeshProUGUI modeBtnText;
    private GameObject bottomBarGo;
    private GameObject kanjiSelectBtnGo;
    private TextMeshProUGUI kanjiSelectBtnText;
    private GameObject kanjiPickerPanel;

    public static void EnsureExists()
    {
        if (FindFirstObjectByType<GameHud>() != null) return;

        GameObject root = new GameObject("GameHud");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.AddComponent<GraphicRaycaster>();
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        GameHud hud = root.AddComponent<GameHud>();
        hud.BuildTopArea(root.transform);
        hud.BuildBottomButtons(root.transform);
    }

    private void Update()
    {
        if (coinText != null)
        {
            int coins = ScoreManager.Instance != null ? ScoreManager.Instance.Coins : 0;
            coinText.text = "コイン: " + coins;
        }

        if (GameManager.Instance != null && bottomBarGo != null)
        {
            // 稼働モードならボトムバーを隠す、編集モードなら表示する
            bool isRunMode = GameManager.Instance.IsRunMode;
            if (bottomBarGo.activeSelf == isRunMode)
            {
                bottomBarGo.SetActive(!isRunMode);
            }
            if (modeBtnText != null)
            {
                modeBtnText.text = isRunMode ? "編集モードへ" : "稼働モードへ";
            }

            // 漢字指定ボタンの表示制御
            if (kanjiSelectBtnGo != null)
            {
                bool isGenSelected = BuildModeController.Instance != null && BuildModeController.Instance.SelectedType == FacilityType.Generator;
                if (kanjiSelectBtnGo.activeSelf != isGenSelected)
                {
                    kanjiSelectBtnGo.SetActive(isGenSelected);
                }

                if (isGenSelected && kanjiSelectBtnText != null && BuildModeController.Instance != null)
                {
                    kanjiSelectBtnText.text = "生成：" + BuildModeController.Instance.SelectedKanjiToSpawn;
                }
            }
        }
    }

    private void BuildTopArea(Transform parent)
    {
        GameObject topBar = new GameObject("TopBar");
        topBar.transform.SetParent(parent, false);
        RectTransform trt = topBar.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.sizeDelta = new Vector2(0f, 50f);

        // ヒント
        GameObject textGo = new GameObject("Hint");
        textGo.transform.SetParent(topBar.transform, false);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        GameFontSettings.ApplyTo(tmp);
        tmp.text = "編集: ボタン→グリッド配置 / R: 回転 / 右クリック: 詰まり撤去";
        tmp.fontSize = 20;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12;
        tmp.fontSizeMax = 22;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        RectTransform hrt = tmp.rectTransform;
        hrt.anchorMin = new Vector2(0f, 0f);
        hrt.anchorMax = new Vector2(0.6f, 1f);
        hrt.offsetMin = new Vector2(16, 0);
        hrt.offsetMax = Vector2.zero;

        // コイン表示
        GameObject coinGo = new GameObject("CoinText");
        coinGo.transform.SetParent(topBar.transform, false);
        coinText = coinGo.AddComponent<TextMeshProUGUI>();
        GameFontSettings.ApplyTo(coinText);
        coinText.text = "コイン: 0";
        coinText.color = new Color(0.95f, 0.85f, 0.2f, 1f);
        coinText.fontSize = 28;
        coinText.enableAutoSizing = true;
        coinText.fontSizeMin = 14;
        coinText.fontSizeMax = 28;
        coinText.alignment = TextAlignmentOptions.MidlineRight;

        RectTransform crt = coinText.rectTransform;
        crt.anchorMin = new Vector2(0.6f, 0f);
        crt.anchorMax = new Vector2(0.8f, 1f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = new Vector2(-16, 0);

        // モード切替ボタン
        GameObject modeBtnGo = new GameObject("ModeToggleButton");
        modeBtnGo.transform.SetParent(topBar.transform, false);
        RectTransform mrt = modeBtnGo.AddComponent<RectTransform>();
        mrt.anchorMin = new Vector2(0.8f, 0f);
        mrt.anchorMax = new Vector2(1f, 1f);
        mrt.offsetMin = new Vector2(4, 4);
        mrt.offsetMax = new Vector2(-8, -4);

        Image mbg = modeBtnGo.AddComponent<Image>();
        mbg.color = new Color(0.8f, 0.3f, 0.3f, 1f);
        Button btn = modeBtnGo.AddComponent<Button>();
        
        btn.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetRunMode(!GameManager.Instance.IsRunMode);
        });

        GameObject modeTextGo = new GameObject("Text");
        modeTextGo.transform.SetParent(modeBtnGo.transform, false);
        modeBtnText = modeTextGo.AddComponent<TextMeshProUGUI>();
        GameFontSettings.ApplyTo(modeBtnText);
        modeBtnText.text = "モード切替";
        modeBtnText.color = Color.white;
        modeBtnText.alignment = TextAlignmentOptions.Center;
        modeBtnText.enableAutoSizing = true;
        modeBtnText.fontSizeMin = 12;
        modeBtnText.fontSizeMax = 20;

        RectTransform tRt = modeBtnText.rectTransform;
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = Vector2.zero;
        tRt.offsetMax = Vector2.zero;
    }

    private void BuildBottomButtons(Transform parent)
    {
        bottomBarGo = new GameObject("BuildBar");
        bottomBarGo.transform.SetParent(parent, false);
        Image barBg = bottomBarGo.AddComponent<Image>();
        // 背景を完全に不透明にし、明確な区切りをつける
        barBg.color = new Color(0.12f, 0.14f, 0.18f, 1f);
        
        RectTransform brt = bottomBarGo.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.sizeDelta = new Vector2(0f, 96f);

        HorizontalLayoutGroup hlg = bottomBarGo.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(12, 12, 12, 12);
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        CreateButton(bottomBarGo.transform, "ベルトコンベア", () => BuildModeController.Instance?.SelectFacility(FacilityType.Conveyor));
        
        // 漢字指定ボタン (発生機の隣に配置)
        kanjiSelectBtnGo = CreateButtonWithRef(bottomBarGo.transform, "生成：木", () => ToggleKanjiPicker(), out kanjiSelectBtnText);
        kanjiSelectBtnGo.SetActive(false); // 初期は非表示

        CreateButton(bottomBarGo.transform, "発生機", () => BuildModeController.Instance?.SelectFacility(FacilityType.Generator));
        CreateButton(bottomBarGo.transform, "合成機", () => BuildModeController.Instance?.SelectFacility(FacilityType.Combiner));
        CreateButton(bottomBarGo.transform, "焼却炉", () => BuildModeController.Instance?.SelectFacility(FacilityType.Incinerator));
        CreateButton(bottomBarGo.transform, "削除", () => BuildModeController.Instance?.SelectFacility(FacilityType.Delete));
        CreateButton(bottomBarGo.transform, "回転", () => BuildModeController.Instance?.RotatePreview());
        CreateButton(bottomBarGo.transform, "キャンセル", () => BuildModeController.Instance?.SelectFacility(FacilityType.None));
    }

    private void ToggleKanjiPicker()
    {
        if (kanjiPickerPanel == null)
        {
            kanjiPickerPanel = CreateKanjiPicker(transform);
        }
        else
        {
            kanjiPickerPanel.SetActive(!kanjiPickerPanel.activeSelf);
        }
    }

    private GameObject CreateKanjiPicker(Transform parent)
    {
        GameObject panel = new GameObject("KanjiPickerPanel");
        panel.transform.SetParent(parent, false);
        
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.8f);
        
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400, 300);
        rt.anchoredPosition = new Vector2(0, 50);

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panel.transform, false);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        GameFontSettings.ApplyTo(titleText);
        titleText.text = "生成する漢字を選択";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.rectTransform.anchorMin = new Vector2(0, 1);
        titleText.rectTransform.anchorMax = new Vector2(1, 1);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1);
        titleText.rectTransform.anchoredPosition = new Vector2(0, -10);
        titleText.rectTransform.sizeDelta = new Vector2(0, 40);

        GameObject gridGo = new GameObject("Grid");
        gridGo.transform.SetParent(panel.transform, false);
        RectTransform grt = gridGo.AddComponent<RectTransform>();
        grt.anchorMin = Vector2.zero;
        grt.anchorMax = Vector2.one;
        grt.offsetMin = new RectOffset(20, 20, 20, 50).left * Vector2.right + new RectOffset(20, 20, 20, 50).bottom * Vector2.up;
        grt.offsetMax = -new RectOffset(20, 20, 20, 50).right * Vector2.right - new RectOffset(20, 20, 20, 50).top * Vector2.up;

        GridLayoutGroup glg = gridGo.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(60, 60);
        glg.spacing = new Vector2(10, 10);
        glg.childAlignment = TextAnchor.MiddleCenter;

        string[] kanjis = { "木", "日", "月", "火", "水", "金", "土" };
        foreach (var k in kanjis)
        {
            CreatePickerButton(gridGo.transform, k, () => {
                BuildModeController.Instance?.SetSelectedKanji(k);
                panel.SetActive(false);
            });
        }

        return panel;
    }

    private void CreatePickerButton(Transform parent, string kanji, UnityEngine.Events.UnityAction action)
    {
        GameObject btnGo = new GameObject("PickerBtn_" + kanji);
        btnGo.transform.SetParent(parent, false);
        
        Image img = btnGo.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        
        Button btn = btnGo.AddComponent<Button>();
        btn.onClick.AddListener(action);
        
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        var t = textGo.AddComponent<TextMeshProUGUI>();
        GameFontSettings.ApplyTo(t);
        t.text = kanji;
        t.fontSize = 32;
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = Vector2.zero;
        t.rectTransform.offsetMax = Vector2.zero;
    }

    private GameObject CreateButtonWithRef(Transform parent, string label, UnityEngine.Events.UnityAction action, out TextMeshProUGUI textRef)
    {
        GameObject buttonGo = new GameObject(label + "Button");
        buttonGo.transform.SetParent(parent, false);
        Image bg = buttonGo.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);
        Button btn = buttonGo.AddComponent<Button>();
        btn.onClick.AddListener(action);

        LayoutElement le = buttonGo.AddComponent<LayoutElement>();
        le.preferredWidth = 140f;
        le.preferredHeight = 72f;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(buttonGo.transform, false);
        textRef = textGo.AddComponent<TextMeshProUGUI>();
        GameFontSettings.ApplyTo(textRef);
        textRef.text = label;
        textRef.color = Color.white;
        textRef.enableAutoSizing = true;
        textRef.fontSizeMin = 14;
        textRef.fontSizeMax = 26;
        textRef.alignment = TextAlignmentOptions.Center;
        
        RectTransform tr = textRef.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        
        return buttonGo;
    }

    private void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonGo = new GameObject(label + "Button");
        buttonGo.transform.SetParent(parent, false);
        Image bg = buttonGo.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.22f, 0.28f, 0.95f);
        Button btn = buttonGo.AddComponent<Button>();
        btn.onClick.AddListener(action);
        ColorBlock cb = btn.colors;
        cb.normalColor = bg.color;
        cb.highlightedColor = new Color(0.26f, 0.34f, 0.46f, 1f);
        cb.pressedColor = new Color(0.14f, 0.18f, 0.24f, 1f);
        btn.colors = cb;

        LayoutElement le = buttonGo.AddComponent<LayoutElement>();
        le.preferredWidth = 160f;
        le.preferredHeight = 72f;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(buttonGo.transform, false);
        TextMeshProUGUI t = textGo.AddComponent<TextMeshProUGUI>();
        GameFontSettings.ApplyTo(t);
        t.text = label;
        t.color = Color.white;
        t.enableAutoSizing = true;
        t.fontSizeMin = 14;
        t.fontSizeMax = 26;
        t.alignment = TextAlignmentOptions.Center;
        RectTransform tr = t.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
    }
}
