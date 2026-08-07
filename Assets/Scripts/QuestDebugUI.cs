using UnityEngine;
using UnityEngine.UI;

public class QuestDebugUI : MonoBehaviour
{
    [SerializeField] private QuestStateManager questStateManager;

    private Text _questText;
    private string _lastState;

    private void Awake()
    {
        BuildCanvas();
    }

    private void Start()
    {
        if (questStateManager == null)
        {
            questStateManager = FindFirstObjectByType<QuestStateManager>();
        }

        RefreshQuestText();
    }

    private void Update()
    {
        if (questStateManager == null)
        {
            return;
        }

        if (_lastState == questStateManager.CurrentState)
        {
            return;
        }

        RefreshQuestText();
    }

    private void RefreshQuestText()
    {
        if (questStateManager == null || _questText == null)
        {
            return;
        }

        _lastState = questStateManager.CurrentState;
        _questText.text = BuildQuestText(_lastState);
    }

    private string BuildQuestText(string state)
    {
        if (state == "quest_completed")
        {
            return "\u4efb\u52a1\uff1a\u5bfb\u627e\u957f\u8001\u7684\u6000\u8868\n\u72b6\u6001\uff1a\u4efb\u52a1\u5b8c\u6210";
        }

        string objective = state switch
        {
            "not_started" => "\u53bb\u627e\u957f\u8001\u4e86\u89e3\u60c5\u51b5",
            "accepted_watch_quest" => "\u53bb\u627e\u536b\u5175\u94c1\u725b\u6253\u542c\u6cb3\u8fb9\u7ebf\u7d22",
            "got_river_clue" => "\u524d\u5f80\u6cb3\u8fb9\u82a6\u82c7\u8361\u5bfb\u627e\u6000\u8868",
            "watch_found" => "\u628a\u6000\u8868\u4ea4\u8fd8\u7ed9\u957f\u8001",
            _ => "\u6682\u65e0\u76ee\u6807"
        };

        return $"\u4efb\u52a1\uff1a\u5bfb\u627e\u957f\u8001\u7684\u6000\u8868\n\u5f53\u524d\u76ee\u6807\uff1a{objective}";
    }

    private void BuildCanvas()
    {
        GameObject canvasGo = new GameObject("QuestDebugCanvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject textGo = new GameObject("QuestText");
        textGo.transform.SetParent(canvasGo.transform, false);

        _questText = textGo.AddComponent<Text>();
        _questText.fontSize = 28;
        _questText.font = LoadUiFont(_questText.fontSize);
        _questText.color = Color.white;
        _questText.alignment = TextAnchor.UpperLeft;
        _questText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _questText.verticalOverflow = VerticalWrapMode.Overflow;
        _questText.text = "\u4efb\u52a1\uff1a\u5bfb\u627e\u957f\u8001\u7684\u6000\u8868\n\u5f53\u524d\u76ee\u6807\uff1a\u52a0\u8f7d\u4e2d...";

        RectTransform rect = textGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(30f, -30f);
        rect.sizeDelta = new Vector2(700f, 120f);
    }

    private Font LoadUiFont(int size)
    {
        Font font = Font.CreateDynamicFontFromOSFont(
            new string[] { "Microsoft YaHei", "SimHei", "Microsoft JhengHei", "Arial Unicode MS" },
            size
        );

        if (font != null)
        {
            return font;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
