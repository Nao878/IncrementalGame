using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 画面上部のヒント用 TMP（フォント統一）。
/// </summary>
public class GameHud : MonoBehaviour
{
    private TextMeshProUGUI coinText;

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

        GameObject textGo = new GameObject("Hint");
        textGo.transform.SetParent(root.transform, false);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        GameFontSettings.ApplyTo(tmp);
        tmp.text = "Build: select button -> place on grid / R: rotate / RMB on clog: clear";
        tmp.fontSize = 20;
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12;
        tmp.fontSizeMax = 22;
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.margin = new Vector4(16, 40, 16, 12);
        tmp.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        RectTransform rt = tmp.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 96f);

        GameHud hud = root.AddComponent<GameHud>();
        hud.BuildBottomButtons(root.transform);
    }

    private void Update()
    {
        if (coinText == null) return;
        int coins = ScoreManager.Instance != null ? ScoreManager.Instance.Coins : 0;
        coinText.text = "Coins: " + coins;
    }

    void BuildBottomButtons(Transform parent)
    {
        GameObject bar = new GameObject("BuildBar");
        bar.transform.SetParent(parent, false);
        Image barBg = bar.AddComponent<Image>();
        barBg.color = new Color(0f, 0f, 0f, 0.35f);
        RectTransform brt = bar.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 0f);
        brt.pivot = new Vector2(0.5f, 0f);
        brt.sizeDelta = new Vector2(0f, 96f);

        HorizontalLayoutGroup hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(12, 12, 12, 12);
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        coinText = CreateLabel(bar.transform, "Coins: 0", 220f);
        CreateButton(bar.transform, "Conveyor", () => BuildModeController.Instance?.SelectFacility(FacilityType.Conveyor));
        CreateButton(bar.transform, "Combiner", () => BuildModeController.Instance?.SelectFacility(FacilityType.Combiner));
        CreateButton(bar.transform, "Collector", () => BuildModeController.Instance?.SelectFacility(FacilityType.Collector));
        CreateButton(bar.transform, "Rotate", () => BuildModeController.Instance?.RotatePreview());
        CreateButton(bar.transform, "Cancel", () => BuildModeController.Instance?.SelectFacility(FacilityType.None));
    }

    TextMeshProUGUI CreateLabel(Transform parent, string text, float width)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        GameFontSettings.ApplyTo(t);
        t.text = text;
        t.color = Color.white;
        t.enableAutoSizing = true;
        t.fontSizeMin = 14;
        t.fontSizeMax = 28;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        return t;
    }

    void CreateButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
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
