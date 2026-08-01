using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 极简对话 UI(占位版):运行时自建 Canvas,不需要在编辑器里手搭任何 UI。
/// 对外只暴露四个方法 + 一个状态:ShowPrompt / HidePrompt / Show / Hide / IsOpen。
/// 后续接 LLM 网关时,只需把 Show() 收到的字符串换成网关返回值,本文件不用改。
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    private GameObject _panel;
    private Text _nameText;
    private Text _lineText;
    private GameObject _promptRoot;
    private Text _promptText;
    private Font _font;

    void Awake()
    {
        // Unity 2022+ 的内置动态字体,走系统字体渲染,中文可直接显示
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildCanvas();
        HidePrompt();
        Hide();
    }

    // ---------- 对外接口 ----------

    /// <summary>玩家附近有可交互 NPC 时,屏幕下方浮现"按 E 对话"提示</summary>
    public void ShowPrompt(string npcName)
    {
        _promptText.text = $"按 E 与 {npcName} 对话";
        _promptRoot.SetActive(true);
    }

    public void HidePrompt()
    {
        _promptRoot.SetActive(false);
    }

    /// <summary>打开对话框显示台词。打开期间自动隐藏提示。</summary>
    public void Show(string npcName, string line)
    {
        _nameText.text = npcName;
        _lineText.text = line;
        _panel.SetActive(true);
        HidePrompt();
        IsOpen = true;
    }

    public void Hide()
    {
        _panel.SetActive(false);
        IsOpen = false;
    }

    // ---------- 以下全是 Canvas 组装,无逻辑 ----------

    private void BuildCanvas()
    {
        var canvasGo = new GameObject("DialogueCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 对话面板:底部居中的半透明黑底
        _panel = MakePanel(canvasGo.transform, "DialoguePanel",
            new Vector2(0.15f, 0.03f), new Vector2(0.85f, 0.24f),
            new Color(0f, 0f, 0f, 0.78f));

        _nameText = MakeText(_panel.transform, "NameText", "", 30, FontStyle.Bold,
            new Vector2(0.02f, 0.62f), new Vector2(0.5f, 0.95f), TextAnchor.MiddleLeft,
            new Color(1f, 0.85f, 0.4f));

        _lineText = MakeText(_panel.transform, "LineText", "", 26, FontStyle.Normal,
            new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.60f), TextAnchor.UpperLeft,
            Color.white);

        MakeText(_panel.transform, "HintText", "按 E 或 Esc 关闭", 16, FontStyle.Italic,
            new Vector2(0.72f, 0.02f), new Vector2(0.98f, 0.22f), TextAnchor.LowerRight,
            new Color(1f, 1f, 1f, 0.45f));

        // 交互提示:面板上方一条小字
        _promptRoot = MakePanel(canvasGo.transform, "PromptRoot",
            new Vector2(0.35f, 0.26f), new Vector2(0.65f, 0.31f),
            new Color(0f, 0f, 0f, 0.55f));
        _promptText = MakeText(_promptRoot.transform, "PromptText", "", 20, FontStyle.Normal,
            new Vector2(0f, 0f), new Vector2(1f, 1f), TextAnchor.MiddleCenter,
            Color.white);
    }

    private GameObject MakePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    private Text MakeText(Transform parent, string name, string content, int size, FontStyle style,
        Vector2 anchorMin, Vector2 anchorMax, TextAnchor align, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = _font;
        text.text = content;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = align;
        text.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return text;
    }
}
