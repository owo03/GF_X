using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class GameUIForm : UIFormBase
{
    public struct CardSlotViewData
    {
        public string Code;
        public string Name;
        public string Description;
        public bool IsFront;
        public bool Triggered;
    }

    private const int DefaultBoardColumns = 4;
    private const float HoverDialogCooldown = 0.2f;

    private RectTransform m_StatusRoot;
    private TextMeshProUGUI m_StatusText;
    private RectTransform m_BoardRoot;
    private GridLayoutGroup m_BoardGrid;
    private Camera m_HitTestCamera;
    private readonly List<CardSlotWidget> m_CardWidgets = new List<CardSlotWidget>(16);
    private readonly List<CardSlotViewData> m_CurrentSlots = new List<CardSlotViewData>(16);
    private bool m_IsShowingHoverDialog;
    private float m_LastHoverDialogTime;
    private int m_LastHoverCardIndex = -1;
    private int m_CurrentHoverCardIndex = -1;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        var uiparms = UIParams.Create();
        uiparms.Set<VarBoolean>(UITopbar.P_EnableBG, false);
        uiparms.Set<VarBoolean>(UITopbar.P_EnableSettingBtn, true);
        OpenSubUIForm(UIViews.Topbar, 1, uiparms);

        BuildRuntimeLayout();
        SetRunStatus(L("FlipRun.Init"));
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        m_IsShowingHoverDialog = false;
        m_LastHoverCardIndex = -1;
        m_CurrentHoverCardIndex = -1;
        m_CurrentSlots.Clear();
        SetCardWidgetActiveCount(0);
        base.OnClose(isShutdown, userData);
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        UpdateCardHoverByMouse();
    }

    public void SetRunStatus(string statusText)
    {
        BuildRuntimeLayout();
        if (m_StatusText != null)
        {
            m_StatusText.text = statusText;
        }
    }

    public void SetBoardSlots(IList<CardSlotViewData> slots)
    {
        BuildRuntimeLayout();
        m_CurrentHoverCardIndex = -1;
        m_CurrentSlots.Clear();
        if (slots == null)
        {
            SetCardWidgetActiveCount(0);
            return;
        }

        int count = slots.Count;
        EnsureCardWidgetCount(count);
        UpdateGridCellSize(count);

        for (int i = 0; i < count; i++)
        {
            var data = slots[i];
            m_CurrentSlots.Add(data);
            var widget = m_CardWidgets[i];
            widget.Apply(data);
            widget.Root.gameObject.SetActive(true);
        }

        SetCardWidgetActiveCount(count);
    }

    private void OnCardPointerEnter(int cardIndex)
    {
        if (m_IsShowingHoverDialog) return;
        if (cardIndex < 0 || cardIndex >= m_CurrentSlots.Count) return;
        if (!m_CurrentSlots[cardIndex].IsFront) return;
        if (Time.unscaledTime - m_LastHoverDialogTime < HoverDialogCooldown) return;
        if (m_LastHoverCardIndex == cardIndex && Time.unscaledTime - m_LastHoverDialogTime < 0.8f) return;

        var card = m_CurrentSlots[cardIndex];
        string title = string.IsNullOrEmpty(card.Name) ? L("FlipRun.Hover.FallbackTitle") : card.Name;
        string content = LF("FlipRun.Hover.Content", card.Code, card.Description);
        m_LastHoverCardIndex = cardIndex;
        m_LastHoverDialogTime = Time.unscaledTime;

        m_IsShowingHoverDialog = true;
        if (!MessageBoxesSystem.Show(title, content, OnHoverDialogConfirmed, null, false))
        {
            m_IsShowingHoverDialog = false;
            GF.UI.ShowToast(content);
            return;
        }
    }

    private void OnHoverDialogConfirmed()
    {
        m_IsShowingHoverDialog = false;
        m_CurrentHoverCardIndex = -1;
        m_LastHoverDialogTime = Time.unscaledTime;
    }

    private void UpdateCardHoverByMouse()
    {
        if (m_IsShowingHoverDialog || m_CurrentSlots.Count <= 0 || m_CardWidgets.Count <= 0)
        {
            m_CurrentHoverCardIndex = -1;
            return;
        }

        Vector2 mousePosition = Input.mousePosition;
        int activeCount = Mathf.Min(m_CurrentSlots.Count, m_CardWidgets.Count);
        int hoverIndex = -1;
        for (int i = 0; i < activeCount; i++)
        {
            var widget = m_CardWidgets[i];
            if (!widget.Root.gameObject.activeInHierarchy || !widget.IsFront)
            {
                continue;
            }

            if (widget.ContainsScreenPoint(mousePosition, m_HitTestCamera))
            {
                hoverIndex = i;
                break;
            }
        }

        if (hoverIndex < 0)
        {
            m_CurrentHoverCardIndex = -1;
            return;
        }

        if (hoverIndex == m_CurrentHoverCardIndex)
        {
            return;
        }

        m_CurrentHoverCardIndex = hoverIndex;
        OnCardPointerEnter(hoverIndex);
    }

    private void BuildRuntimeLayout()
    {
        if (coinNumText != null && coinNumText.gameObject.activeSelf)
        {
            coinNumText.gameObject.SetActive(false);
        }

        TMP_FontAsset fontAsset = coinNumText != null ? coinNumText.font : null;
        RectTransform root = GetRootRect();
        if (root == null) return;
        var rootCanvas = GetComponentInParent<Canvas>();
        m_HitTestCamera = rootCanvas != null ? rootCanvas.worldCamera : GFBuiltin.UICamera;

        if (m_StatusRoot == null)
        {
            var statusObj = new GameObject("RunStatusRoot", typeof(RectTransform), typeof(Image));
            statusObj.transform.SetParent(root, false);
            m_StatusRoot = statusObj.GetComponent<RectTransform>();
            m_StatusRoot.anchorMin = new Vector2(0f, 1f);
            m_StatusRoot.anchorMax = new Vector2(0f, 1f);
            m_StatusRoot.pivot = new Vector2(0f, 1f);
            m_StatusRoot.anchoredPosition = new Vector2(24f, -136f);
            m_StatusRoot.sizeDelta = new Vector2(560f, 900f);

            var statusBg = statusObj.GetComponent<Image>();
            statusBg.color = new Color(0.07f, 0.1f, 0.14f, 0.82f);

            m_StatusText = CreateText("RunStatusText", m_StatusRoot, fontAsset, 24, TextAlignmentOptions.TopLeft);
            RectTransform statusTextRect = m_StatusText.rectTransform;
            statusTextRect.anchorMin = Vector2.zero;
            statusTextRect.anchorMax = Vector2.one;
            statusTextRect.offsetMin = new Vector2(18f, 18f);
            statusTextRect.offsetMax = new Vector2(-18f, -18f);
            m_StatusText.enableWordWrapping = true;
        }

        if (m_BoardRoot == null)
        {
            var boardObj = new GameObject("FlipBoardRoot", typeof(RectTransform), typeof(Image), typeof(GridLayoutGroup));
            boardObj.transform.SetParent(root, false);
            m_BoardRoot = boardObj.GetComponent<RectTransform>();
            m_BoardRoot.anchorMin = new Vector2(0.5f, 0.5f);
            m_BoardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            m_BoardRoot.pivot = new Vector2(0.5f, 0.5f);
            m_BoardRoot.anchoredPosition = new Vector2(280f, -110f);
            m_BoardRoot.sizeDelta = new Vector2(860f, 920f);

            var boardBg = boardObj.GetComponent<Image>();
            boardBg.color = new Color(0.11f, 0.13f, 0.17f, 0.82f);

            m_BoardGrid = boardObj.GetComponent<GridLayoutGroup>();
            m_BoardGrid.padding = new RectOffset(16, 16, 16, 16);
            m_BoardGrid.spacing = new Vector2(14f, 14f);
            m_BoardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            m_BoardGrid.constraintCount = DefaultBoardColumns;
            m_BoardGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            m_BoardGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            m_BoardGrid.childAlignment = TextAnchor.UpperLeft;
        }
    }

    private RectTransform GetRootRect()
    {
        Transform mask = transform.Find("Mask");
        if (mask != null)
        {
            return mask as RectTransform;
        }

        return transform as RectTransform;
    }

    private void EnsureCardWidgetCount(int targetCount)
    {
        if (m_BoardRoot == null) return;
        TMP_FontAsset fontAsset = coinNumText != null ? coinNumText.font : null;
        while (m_CardWidgets.Count < targetCount)
        {
            m_CardWidgets.Add(CardSlotWidget.Create(m_BoardRoot, fontAsset));
        }
    }

    private void SetCardWidgetActiveCount(int activeCount)
    {
        for (int i = 0; i < m_CardWidgets.Count; i++)
        {
            bool active = i < activeCount;
            if (m_CardWidgets[i].Root.gameObject.activeSelf != active)
            {
                m_CardWidgets[i].Root.gameObject.SetActive(active);
            }
        }
    }

    private void UpdateGridCellSize(int cardCount)
    {
        if (m_BoardGrid == null || m_BoardRoot == null) return;
        if (cardCount <= 0)
        {
            m_BoardGrid.constraintCount = DefaultBoardColumns;
            return;
        }

        int columns = Mathf.Clamp(DefaultBoardColumns, 1, Mathf.Max(1, cardCount));
        if (cardCount < columns)
        {
            columns = cardCount;
        }
        int rows = Mathf.Max(1, Mathf.CeilToInt(cardCount / (float)columns));

        float contentWidth = Mathf.Max(200f, m_BoardRoot.rect.width - m_BoardGrid.padding.left - m_BoardGrid.padding.right);
        float contentHeight = Mathf.Max(200f, m_BoardRoot.rect.height - m_BoardGrid.padding.top - m_BoardGrid.padding.bottom);
        float cellWidth = (contentWidth - (columns - 1) * m_BoardGrid.spacing.x) / columns;
        float cellHeight = (contentHeight - (rows - 1) * m_BoardGrid.spacing.y) / rows;

        m_BoardGrid.constraintCount = columns;
        m_BoardGrid.cellSize = new Vector2(Mathf.Max(140f, cellWidth), Mathf.Max(140f, cellHeight));
    }

    private static TextMeshProUGUI CreateText(string nodeName, Transform parent, TMP_FontAsset fontAsset, int fontSize, TextAlignmentOptions alignment)
    {
        var textObj = new GameObject(nodeName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);
        var text = textObj.GetComponent<TextMeshProUGUI>();
        if (fontAsset != null)
        {
            text.font = fontAsset;
        }
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        return text;
    }

    private static string L(string key) => GF.Localization.GetString(key);
    private static string LF(string key, params object[] args)
    {
        string format = L(key);
        if (args == null || args.Length == 0) return format;
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }

    private sealed class CardSlotWidget
    {
        private readonly Image m_Background;
        private readonly TextMeshProUGUI m_CodeText;
        private readonly TextMeshProUGUI m_NameText;
        private readonly TextMeshProUGUI m_DescText;
        public bool IsFront { get; private set; }

        public RectTransform Root { get; private set; }

        private CardSlotWidget(
            RectTransform root,
            Image background,
            TextMeshProUGUI codeText,
            TextMeshProUGUI nameText,
            TextMeshProUGUI descText)
        {
            Root = root;
            m_Background = background;
            m_CodeText = codeText;
            m_NameText = nameText;
            m_DescText = descText;
        }

        public static CardSlotWidget Create(Transform parent, TMP_FontAsset fontAsset)
        {
            var slotObj = new GameObject("CardSlot", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            slotObj.transform.SetParent(parent, false);
            var slotRect = slotObj.GetComponent<RectTransform>();
            slotRect.localScale = Vector3.one;

            var bg = slotObj.GetComponent<Image>();
            bg.color = new Color(0.16f, 0.20f, 0.24f, 0.97f);

            var layout = slotObj.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var layoutElement = slotObj.GetComponent<LayoutElement>();
            layoutElement.minWidth = 120f;
            layoutElement.minHeight = 120f;

            TextMeshProUGUI codeText = CreateText("Code", slotRect, fontAsset, 20, TextAlignmentOptions.TopLeft);
            codeText.color = new Color(0.90f, 0.85f, 0.45f, 1f);

            TextMeshProUGUI nameText = CreateText("Name", slotRect, fontAsset, 28, TextAlignmentOptions.TopLeft);

            TextMeshProUGUI descText = CreateText("Desc", slotRect, fontAsset, 18, TextAlignmentOptions.TopLeft);
            descText.color = new Color(0.78f, 0.86f, 0.90f, 1f);
            descText.fontStyle = FontStyles.Italic;

            return new CardSlotWidget(slotRect, bg, codeText, nameText, descText);
        }

        public bool ContainsScreenPoint(Vector2 point, Camera uiCamera)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(Root, point, uiCamera))
            {
                return true;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(Root, point, null);
        }

        public void Apply(CardSlotViewData data)
        {
            IsFront = data.IsFront;
            if (!IsFront)
            {
                m_CodeText.text = "???";
                m_NameText.text = "???";
                m_DescText.text = string.Empty;
                m_Background.color = new Color(0.12f, 0.14f, 0.18f, 0.97f);
                return;
            }

            m_CodeText.text = string.IsNullOrEmpty(data.Code) ? "--" : data.Code;
            m_NameText.text = string.IsNullOrEmpty(data.Name) ? "-" : data.Name;
            m_DescText.text = string.IsNullOrEmpty(data.Description) ? string.Empty : data.Description;
            m_Background.color = data.Triggered
                ? new Color(0.22f, 0.37f, 0.23f, 0.97f)
                : new Color(0.16f, 0.20f, 0.24f, 0.97f);
        }
    }
}
