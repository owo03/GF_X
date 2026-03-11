using System;
using System.Collections.Generic;
using DG.Tweening;
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
        public bool IsClickable;
        public bool IsRewardOffer;
        public bool IsPersistent;
        public int RemainingTurns;
        public bool Triggered;
    }

    public struct DeckEntryViewData
    {
        public string Code;
        public string Name;
        public string Description;
        public int Count;
    }

    private const int DefaultBoardColumns = 4;

    private RectTransform m_StatusRoot;
    private TextMeshProUGUI m_StatusText;
    private Button m_DeckButton;
    private Button m_SettingsButton;
    private RectTransform m_BoardRoot;
    private GridLayoutGroup m_BoardGrid;
    private GameObject m_DeckOverlay;
    private RectTransform m_DeckPanelRoot;
    private TextMeshProUGUI m_DeckTitleText;
    private TextMeshProUGUI m_DeckRelicText;
    private RectTransform m_DeckListRoot;
    private readonly List<TextMeshProUGUI> m_DeckEntryTexts = new List<TextMeshProUGUI>(24);
    private MessageBoxesSystem m_MessageBoxesSystem;
    private GameObject m_MessageBoxTemplate;
    private Action<int> m_OnCardSlotClicked;
    private readonly List<CardSlotWidget> m_CardWidgets = new List<CardSlotWidget>(16);
    private Vector2 m_LastRootSize = Vector2.zero;

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);

        BuildRuntimeLayout();
        SetRunStatus(L("FlipRun.Init"));
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        SetCardWidgetActiveCount(0);
        m_OnCardSlotClicked = null;
        base.OnClose(isShutdown, userData);
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        RectTransform root = GetRootRect();
        if (root == null)
        {
            return;
        }

        Vector2 size = root.rect.size;
        if ((size - m_LastRootSize).sqrMagnitude > 1f)
        {
            ApplyResponsiveLayout(root);
        }
    }

    public void SetRunStatus(string statusText)
    {
        BuildRuntimeLayout();
        if (m_StatusText != null)
        {
            m_StatusText.text = statusText;
        }
    }

    public bool IsModalOpen()
    {
        return m_DeckOverlay != null && m_DeckOverlay.activeSelf;
    }

    public void SetCardSlotClickHandler(Action<int> onCardSlotClicked)
    {
        m_OnCardSlotClicked = onCardSlotClicked;
        for (int i = 0; i < m_CardWidgets.Count; i++)
        {
            m_CardWidgets[i].Bind(i, m_OnCardSlotClicked);
        }
    }

    public void SetBoardSlots(IList<CardSlotViewData> slots)
    {
        BuildRuntimeLayout();
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
            m_CardWidgets[i].Bind(i, m_OnCardSlotClicked);
            m_CardWidgets[i].Apply(slots[i]);
            m_CardWidgets[i].Root.gameObject.SetActive(true);
        }

        SetCardWidgetActiveCount(count);
    }

    public void SetDeckEntries(IList<DeckEntryViewData> entries, string relicSummary)
    {
        BuildRuntimeLayout();
        EnsureDeckEntryCount(entries != null ? entries.Count : 0);

        if (m_DeckTitleText != null)
        {
            m_DeckTitleText.text = L("FlipRun.Deck.Title");
        }
        if (m_DeckRelicText != null)
        {
            m_DeckRelicText.text = string.IsNullOrEmpty(relicSummary) ? L("FlipRun.Deck.NoRelics") : relicSummary;
        }

        int activeCount = entries != null ? entries.Count : 0;
        for (int i = 0; i < activeCount; i++)
        {
            DeckEntryViewData entry = entries[i];
            m_DeckEntryTexts[i].text = LF("FlipRun.Deck.EntryFormat", entry.Name, entry.Count, entry.Description);
            m_DeckEntryTexts[i].gameObject.SetActive(true);
        }

        if (activeCount == 0 && m_DeckEntryTexts.Count > 0)
        {
            m_DeckEntryTexts[0].text = L("FlipRun.Deck.Empty");
            m_DeckEntryTexts[0].gameObject.SetActive(true);
            activeCount = 1;
        }

        for (int i = activeCount; i < m_DeckEntryTexts.Count; i++)
        {
            m_DeckEntryTexts[i].gameObject.SetActive(false);
        }
    }

    private void BuildRuntimeLayout()
    {
        if (coinNumText != null && coinNumText.gameObject.activeSelf)
        {
            coinNumText.gameObject.SetActive(false);
        }

        TMP_FontAsset fontAsset = coinNumText != null ? coinNumText.font : null;
        RectTransform root = GetRootRect();
        if (root == null)
        {
            return;
        }

        if (m_StatusRoot == null)
        {
            var statusObj = new GameObject("RunStatusRoot", typeof(RectTransform), typeof(Image));
            statusObj.transform.SetParent(root, false);
            m_StatusRoot = statusObj.GetComponent<RectTransform>();
            m_StatusRoot.anchorMin = new Vector2(0.5f, 1f);
            m_StatusRoot.anchorMax = new Vector2(0.5f, 1f);
            m_StatusRoot.pivot = new Vector2(0.5f, 1f);
            m_StatusRoot.anchoredPosition = new Vector2(0f, -118f);
            m_StatusRoot.sizeDelta = new Vector2(960f, 210f);

            var statusBg = statusObj.GetComponent<Image>();
            statusBg.color = new Color(0.07f, 0.1f, 0.14f, 0.86f);
            statusBg.raycastTarget = false;

            m_StatusText = CreateText("RunStatusText", m_StatusRoot, fontAsset, 24, TextAlignmentOptions.TopLeft);
            RectTransform statusTextRect = m_StatusText.rectTransform;
            statusTextRect.anchorMin = Vector2.zero;
            statusTextRect.anchorMax = Vector2.one;
            statusTextRect.offsetMin = new Vector2(20f, 18f);
            statusTextRect.offsetMax = new Vector2(-166f, -18f);
            m_StatusText.enableWordWrapping = true;

            m_DeckButton = CreateButton("DeckButton", m_StatusRoot, fontAsset, 24, L("FlipRun.Button.Deck"), ToggleDeckPanel);
            RectTransform deckButtonRect = m_DeckButton.GetComponent<RectTransform>();
            deckButtonRect.sizeDelta = new Vector2(120f, 54f);

            m_SettingsButton = CreateButton("SettingButton", m_StatusRoot, fontAsset, 24, L("Settings.Title"), OpenSettings);
            RectTransform settingsButtonRect = m_SettingsButton.GetComponent<RectTransform>();
            settingsButtonRect.sizeDelta = new Vector2(120f, 54f);
        }

        if (m_BoardRoot == null)
        {
            var boardObj = new GameObject("FlipBoardRoot", typeof(RectTransform), typeof(Image), typeof(GridLayoutGroup));
            boardObj.transform.SetParent(root, false);
            m_BoardRoot = boardObj.GetComponent<RectTransform>();
            m_BoardRoot.anchorMin = new Vector2(0.5f, 0.5f);
            m_BoardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            m_BoardRoot.pivot = new Vector2(0.5f, 0.5f);
            m_BoardRoot.anchoredPosition = new Vector2(0f, -56f);
            m_BoardRoot.sizeDelta = new Vector2(960f, 760f);

            var boardBg = boardObj.GetComponent<Image>();
            boardBg.color = new Color(0.11f, 0.13f, 0.17f, 0.82f);
            boardBg.raycastTarget = false;

            m_BoardGrid = boardObj.GetComponent<GridLayoutGroup>();
            m_BoardGrid.padding = new RectOffset(24, 24, 24, 24);
            m_BoardGrid.spacing = new Vector2(18f, 18f);
            m_BoardGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            m_BoardGrid.constraintCount = DefaultBoardColumns;
            m_BoardGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            m_BoardGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            m_BoardGrid.childAlignment = TextAnchor.MiddleCenter;
        }

        EnsureMessageBoxSystem(root, fontAsset);
        EnsureDeckPanel(root, fontAsset);
        ApplyResponsiveLayout(root);
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
        if (m_BoardRoot == null)
        {
            return;
        }

        TMP_FontAsset fontAsset = coinNumText != null ? coinNumText.font : null;
        while (m_CardWidgets.Count < targetCount)
        {
            var widget = CardSlotWidget.Create(m_BoardRoot, fontAsset);
            widget.Bind(m_CardWidgets.Count, m_OnCardSlotClicked);
            m_CardWidgets.Add(widget);
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
        if (m_BoardGrid == null || m_BoardRoot == null)
        {
            return;
        }

        if (cardCount <= 0)
        {
            m_BoardGrid.constraintCount = DefaultBoardColumns;
            return;
        }

        int preferredColumns = DefaultBoardColumns;
        if (m_BoardRoot.rect.width >= m_BoardRoot.rect.height * 1.15f)
        {
            preferredColumns = 5;
        }
        if (m_BoardRoot.rect.width >= m_BoardRoot.rect.height * 1.55f)
        {
            preferredColumns = 6;
        }

        int columns = Mathf.Clamp(preferredColumns, 1, Mathf.Max(1, cardCount));
        if (cardCount < columns)
        {
            columns = cardCount;
        }
        int rows = Mathf.Max(1, Mathf.CeilToInt(cardCount / (float)columns));

        float contentWidth = Mathf.Max(240f, m_BoardRoot.rect.width - m_BoardGrid.padding.left - m_BoardGrid.padding.right);
        float contentHeight = Mathf.Max(240f, m_BoardRoot.rect.height - m_BoardGrid.padding.top - m_BoardGrid.padding.bottom);
        float cellWidth = (contentWidth - (columns - 1) * m_BoardGrid.spacing.x) / columns;
        float cellHeight = (contentHeight - (rows - 1) * m_BoardGrid.spacing.y) / rows;

        m_BoardGrid.constraintCount = columns;
        m_BoardGrid.cellSize = new Vector2(Mathf.Max(160f, cellWidth), Mathf.Max(180f, cellHeight));
    }

    private void EnsureMessageBoxSystem(RectTransform root, TMP_FontAsset fontAsset)
    {
        if (m_MessageBoxesSystem != null)
        {
            return;
        }

        var systemObj = new GameObject("MessageBoxesSystem", typeof(RectTransform), typeof(MessageBoxesSystem));
        systemObj.transform.SetParent(root, false);
        var systemRect = systemObj.GetComponent<RectTransform>();
        systemRect.anchorMin = Vector2.zero;
        systemRect.anchorMax = Vector2.one;
        systemRect.offsetMin = Vector2.zero;
        systemRect.offsetMax = Vector2.zero;

        m_MessageBoxesSystem = systemObj.GetComponent<MessageBoxesSystem>();
        m_MessageBoxesSystem.linkColor = new Color32(146, 210, 255, 255);
        m_MessageBoxesSystem.selectLinkColor = new Color32(255, 232, 140, 255);
        m_MessageBoxesSystem.messageBoxesBuffer = new List<MessageBox>();
        m_MessageBoxesSystem.waitTime = 0.18f;
        m_MessageBoxesSystem.lockTime = 0.6f;

        m_MessageBoxTemplate = CreateMessageBoxTemplate(systemObj.transform, fontAsset);
        m_MessageBoxesSystem.messageBoxTemplate = m_MessageBoxTemplate;
        m_MessageBoxTemplate.SetActive(false);
    }

    private void EnsureDeckPanel(RectTransform root, TMP_FontAsset fontAsset)
    {
        if (m_DeckOverlay != null)
        {
            return;
        }

        m_DeckOverlay = new GameObject("DeckOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
        m_DeckOverlay.transform.SetParent(root, false);
        var overlayRect = m_DeckOverlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        var overlayImage = m_DeckOverlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.68f);
        overlayImage.raycastTarget = true;

        var overlayButton = m_DeckOverlay.GetComponent<Button>();
        overlayButton.targetGraphic = overlayImage;
        overlayButton.onClick.AddListener(HideDeckPanel);

        m_DeckPanelRoot = new GameObject("DeckPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
        m_DeckPanelRoot.transform.SetParent(m_DeckOverlay.transform, false);
        m_DeckPanelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        m_DeckPanelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        m_DeckPanelRoot.pivot = new Vector2(0.5f, 0.5f);
        m_DeckPanelRoot.sizeDelta = new Vector2(920f, 720f);

        var panelImage = m_DeckPanelRoot.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.11f, 0.15f, 0.98f);
        panelImage.raycastTarget = true;

        var panelLayout = m_DeckPanelRoot.GetComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(24, 24, 24, 24);
        panelLayout.spacing = 16f;
        panelLayout.childControlWidth = true;
        panelLayout.childControlHeight = false;
        panelLayout.childForceExpandWidth = true;
        panelLayout.childForceExpandHeight = false;

        var headerRoot = new GameObject("Header", typeof(RectTransform)).GetComponent<RectTransform>();
        headerRoot.SetParent(m_DeckPanelRoot, false);
        headerRoot.sizeDelta = new Vector2(0f, 64f);

        m_DeckTitleText = CreateText("DeckTitle", headerRoot, fontAsset, 34, TextAlignmentOptions.Left);
        RectTransform deckTitleRect = m_DeckTitleText.rectTransform;
        deckTitleRect.anchorMin = new Vector2(0f, 0f);
        deckTitleRect.anchorMax = new Vector2(1f, 1f);
        deckTitleRect.offsetMin = new Vector2(0f, 0f);
        deckTitleRect.offsetMax = new Vector2(-180f, 0f);
        m_DeckTitleText.text = L("FlipRun.Deck.Title");

        Button closeButton = CreateButton("DeckCloseButton", headerRoot, fontAsset, 24, L("FlipRun.Button.CloseDeck"), HideDeckPanel);
        RectTransform closeButtonRect = closeButton.GetComponent<RectTransform>();
        closeButtonRect.anchorMin = new Vector2(1f, 0.5f);
        closeButtonRect.anchorMax = new Vector2(1f, 0.5f);
        closeButtonRect.pivot = new Vector2(1f, 0.5f);
        closeButtonRect.anchoredPosition = new Vector2(0f, 0f);
        closeButtonRect.sizeDelta = new Vector2(160f, 52f);

        m_DeckRelicText = CreateText("DeckRelics", m_DeckPanelRoot, fontAsset, 22, TextAlignmentOptions.TopLeft);
        m_DeckRelicText.color = new Color(0.90f, 0.84f, 0.60f, 1f);

        var scrollRoot = new GameObject("DeckScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect)).GetComponent<RectTransform>();
        scrollRoot.SetParent(m_DeckPanelRoot, false);
        scrollRoot.sizeDelta = new Vector2(0f, 560f);
        var scrollImage = scrollRoot.GetComponent<Image>();
        scrollImage.color = new Color(0.12f, 0.15f, 0.20f, 0.94f);
        scrollImage.raycastTarget = true;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)).GetComponent<RectTransform>();
        viewport.SetParent(scrollRoot, false);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(10f, 10f);
        viewport.offsetMax = new Vector2(-10f, -10f);
        var viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        viewportImage.raycastTarget = true;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        m_DeckListRoot = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        m_DeckListRoot.SetParent(viewport, false);
        m_DeckListRoot.anchorMin = new Vector2(0f, 1f);
        m_DeckListRoot.anchorMax = new Vector2(1f, 1f);
        m_DeckListRoot.pivot = new Vector2(0.5f, 1f);
        m_DeckListRoot.offsetMin = new Vector2(12f, 0f);
        m_DeckListRoot.offsetMax = new Vector2(-12f, 0f);

        var listLayout = m_DeckListRoot.GetComponent<VerticalLayoutGroup>();
        listLayout.spacing = 10f;
        listLayout.childAlignment = TextAnchor.UpperLeft;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;

        var fitter = m_DeckListRoot.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = scrollRoot.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = m_DeckListRoot;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 24f;

        m_DeckOverlay.SetActive(false);
    }

    private void ApplyResponsiveLayout(RectTransform root)
    {
        if (root == null)
        {
            return;
        }

        m_LastRootSize = root.rect.size;
        float rootWidth = Mathf.Max(960f, root.rect.width);
        float rootHeight = Mathf.Max(720f, root.rect.height);
        bool isLandscape = rootWidth >= rootHeight * 1.35f;
        float outerPadding = Mathf.Clamp(rootWidth * 0.028f, 22f, 42f);

        if (m_StatusRoot != null)
        {
            if (isLandscape)
            {
                float sidebarWidth = Mathf.Clamp(rootWidth * 0.24f, 280f, 380f);
                m_StatusRoot.anchorMin = new Vector2(0f, 0.5f);
                m_StatusRoot.anchorMax = new Vector2(0f, 0.5f);
                m_StatusRoot.pivot = new Vector2(0f, 0.5f);
                m_StatusRoot.anchoredPosition = new Vector2(outerPadding, 0f);
                m_StatusRoot.sizeDelta = new Vector2(sidebarWidth, Mathf.Max(420f, rootHeight - outerPadding * 2f));
            }
            else
            {
                float statusWidth = Mathf.Min(rootWidth - 48f, 1180f);
                float statusHeight = Mathf.Clamp(rootHeight * 0.20f, 170f, 240f);
                m_StatusRoot.anchorMin = new Vector2(0.5f, 1f);
                m_StatusRoot.anchorMax = new Vector2(0.5f, 1f);
                m_StatusRoot.pivot = new Vector2(0.5f, 1f);
                m_StatusRoot.anchoredPosition = new Vector2(0f, -Mathf.Clamp(rootHeight * 0.06f, 36f, 72f));
                m_StatusRoot.sizeDelta = new Vector2(statusWidth, statusHeight);
            }
        }

        if (m_StatusText != null)
        {
            RectTransform statusTextRect = m_StatusText.rectTransform;
            if (isLandscape)
            {
                statusTextRect.offsetMin = new Vector2(22f, 88f);
                statusTextRect.offsetMax = new Vector2(-22f, -22f);
                m_StatusText.fontSize = 24;
            }
            else
            {
                statusTextRect.offsetMin = new Vector2(20f, 18f);
                statusTextRect.offsetMax = new Vector2(-166f, -18f);
                m_StatusText.fontSize = 24;
            }
        }

        if (m_DeckButton != null)
        {
            RectTransform deckButtonRect = m_DeckButton.GetComponent<RectTransform>();
            if (isLandscape)
            {
                deckButtonRect.anchorMin = new Vector2(0f, 0f);
                deckButtonRect.anchorMax = new Vector2(0f, 0f);
                deckButtonRect.pivot = new Vector2(0f, 0f);
                deckButtonRect.anchoredPosition = new Vector2(22f, 20f);
                float sidebarWidth = m_StatusRoot != null ? m_StatusRoot.sizeDelta.x : Mathf.Clamp(rootWidth * 0.24f, 280f, 380f);
                deckButtonRect.sizeDelta = new Vector2(Mathf.Clamp((sidebarWidth - 58f) * 0.5f, 132f, 156f), 56f);
            }
            else
            {
                deckButtonRect.anchorMin = new Vector2(1f, 1f);
                deckButtonRect.anchorMax = new Vector2(1f, 1f);
                deckButtonRect.pivot = new Vector2(1f, 1f);
                deckButtonRect.anchoredPosition = new Vector2(-20f, -20f);
                deckButtonRect.sizeDelta = new Vector2(Mathf.Clamp(rootWidth * 0.10f, 120f, 160f), 54f);
            }
        }

        if (m_SettingsButton != null)
        {
            RectTransform settingsButtonRect = m_SettingsButton.GetComponent<RectTransform>();
            if (isLandscape)
            {
                settingsButtonRect.anchorMin = new Vector2(1f, 0f);
                settingsButtonRect.anchorMax = new Vector2(1f, 0f);
                settingsButtonRect.pivot = new Vector2(1f, 0f);
                settingsButtonRect.anchoredPosition = new Vector2(-22f, 20f);
                float sidebarWidth = m_StatusRoot != null ? m_StatusRoot.sizeDelta.x : Mathf.Clamp(rootWidth * 0.24f, 280f, 380f);
                settingsButtonRect.sizeDelta = new Vector2(Mathf.Clamp((sidebarWidth - 58f) * 0.5f, 132f, 156f), 56f);
            }
            else
            {
                settingsButtonRect.anchorMin = new Vector2(1f, 1f);
                settingsButtonRect.anchorMax = new Vector2(1f, 1f);
                settingsButtonRect.pivot = new Vector2(1f, 1f);
                settingsButtonRect.anchoredPosition = new Vector2(-20f, -84f);
                settingsButtonRect.sizeDelta = new Vector2(Mathf.Clamp(rootWidth * 0.10f, 120f, 160f), 54f);
            }
        }

        if (m_BoardRoot != null)
        {
            if (isLandscape)
            {
                float sidebarWidth = m_StatusRoot != null ? m_StatusRoot.rect.width : Mathf.Clamp(rootWidth * 0.24f, 280f, 380f);
                float boardWidth = Mathf.Max(560f, rootWidth - sidebarWidth - outerPadding * 3f);
                float boardHeight = Mathf.Max(420f, rootHeight - outerPadding * 2f);
                m_BoardRoot.anchorMin = new Vector2(1f, 0.5f);
                m_BoardRoot.anchorMax = new Vector2(1f, 0.5f);
                m_BoardRoot.pivot = new Vector2(1f, 0.5f);
                m_BoardRoot.anchoredPosition = new Vector2(-outerPadding, 0f);
                m_BoardRoot.sizeDelta = new Vector2(boardWidth, boardHeight);
                m_BoardGrid.padding = new RectOffset(24, 24, 24, 24);
                m_BoardGrid.spacing = new Vector2(18f, 18f);
            }
            else
            {
                float boardWidth = Mathf.Min(rootWidth - 48f, 1180f);
                float boardHeight = Mathf.Clamp(rootHeight - 280f, 420f, 860f);
                m_BoardRoot.anchorMin = new Vector2(0.5f, 0.5f);
                m_BoardRoot.anchorMax = new Vector2(0.5f, 0.5f);
                m_BoardRoot.pivot = new Vector2(0.5f, 0.5f);
                m_BoardRoot.anchoredPosition = new Vector2(0f, -Mathf.Clamp(rootHeight * 0.06f, 32f, 64f));
                m_BoardRoot.sizeDelta = new Vector2(boardWidth, boardHeight);
                m_BoardGrid.padding = new RectOffset(24, 24, 24, 24);
                m_BoardGrid.spacing = new Vector2(18f, 18f);
            }
        }

        if (m_DeckPanelRoot != null)
        {
            m_DeckPanelRoot.sizeDelta = new Vector2(Mathf.Min(rootWidth - 80f, 1120f), Mathf.Min(rootHeight - 80f, 780f));
        }

    }

    private GameObject CreateMessageBoxTemplate(Transform parent, TMP_FontAsset fontAsset)
    {
        var rootObj = new GameObject("MessageBoxTemplate", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(LayoutElement), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup), typeof(MessageBox));
        rootObj.transform.SetParent(parent, false);
        var rootRect = rootObj.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(520f, 220f);

        var bg = rootObj.GetComponent<Image>();
        bg.color = new Color(0.07f, 0.09f, 0.12f, 0.96f);
        bg.raycastTarget = false;

        var layoutElement = rootObj.GetComponent<LayoutElement>();
        layoutElement.minWidth = 420f;
        layoutElement.preferredWidth = 520f;
        layoutElement.minHeight = 180f;

        var contentSizeFitter = rootObj.GetComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layoutGroup = rootObj.GetComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(20, 20, 18, 18);
        layoutGroup.spacing = 12f;
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        var headerText = CreateText("Header", rootRect, fontAsset, 30, TextAlignmentOptions.TopLeft);
        headerText.color = new Color(1f, 0.95f, 0.72f, 1f);
        headerText.raycastTarget = false;

        var contentText = CreateText("Content", rootRect, fontAsset, 24, TextAlignmentOptions.TopLeft);
        contentText.color = new Color(0.88f, 0.92f, 0.96f, 1f);
        contentText.lineSpacing = 6f;
        contentText.raycastTarget = false;

        var progressObj = new GameObject("LockProgress", typeof(RectTransform), typeof(Image));
        progressObj.transform.SetParent(rootRect, false);
        var progressRect = progressObj.GetComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0f, 0f);
        progressRect.anchorMax = new Vector2(1f, 0f);
        progressRect.pivot = new Vector2(0.5f, 0f);
        progressRect.anchoredPosition = new Vector2(0f, 2f);
        progressRect.sizeDelta = new Vector2(0f, 4f);

        var progressImage = progressObj.GetComponent<Image>();
        progressImage.type = Image.Type.Filled;
        progressImage.fillMethod = Image.FillMethod.Horizontal;
        progressImage.fillOrigin = 0;
        progressImage.fillAmount = 0f;
        progressImage.color = new Color(0.92f, 0.73f, 0.30f, 0.95f);
        progressImage.raycastTarget = false;

        var messageBox = rootObj.GetComponent<MessageBox>();
        messageBox.header = headerText;
        messageBox.content = contentText;
        messageBox.lockProgressBar = progressImage;
        messageBox.layoutElement = layoutElement;
        messageBox.characterWrapLimit = 28;
        messageBox.pivot = new Vector2(0.05f, 0.95f);
        messageBox.screenOffset = new Vector2(72f, -52f);
        messageBox.enableInteractionLock = false;

        return rootObj;
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
        text.enableWordWrapping = true;
        return text;
    }

    private static Button CreateButton(string nodeName, Transform parent, TMP_FontAsset fontAsset, int fontSize, string text, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObj = new GameObject(nodeName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(parent, false);

        var image = buttonObj.GetComponent<Image>();
        image.color = new Color(0.26f, 0.33f, 0.22f, 0.96f);
        image.raycastTarget = true;

        var button = buttonObj.GetComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        var label = CreateText("Label", buttonObj.transform, fontAsset, fontSize, TextAlignmentOptions.Center);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.text = text;
        label.raycastTarget = false;

        return button;
    }

    private void EnsureDeckEntryCount(int targetCount)
    {
        if (m_DeckListRoot == null)
        {
            return;
        }

        TMP_FontAsset fontAsset = coinNumText != null ? coinNumText.font : null;
        int requiredCount = Mathf.Max(1, targetCount);
        while (m_DeckEntryTexts.Count < requiredCount)
        {
            TextMeshProUGUI entryText = CreateText("DeckEntry" + m_DeckEntryTexts.Count, m_DeckListRoot, fontAsset, 22, TextAlignmentOptions.TopLeft);
            entryText.color = new Color(0.87f, 0.91f, 0.96f, 1f);
            entryText.raycastTarget = false;
            m_DeckEntryTexts.Add(entryText);
        }
    }

    private void ToggleDeckPanel()
    {
        if (m_DeckOverlay == null)
        {
            return;
        }

        if (m_DeckOverlay.activeSelf)
        {
            HideDeckPanel();
        }
        else
        {
            ShowDeckPanel();
        }
    }

    private void ShowDeckPanel()
    {
        if (m_DeckOverlay == null)
        {
            return;
        }

        m_DeckOverlay.SetActive(true);
        if (m_DeckPanelRoot != null)
        {
            m_DeckPanelRoot.DOKill();
            m_DeckPanelRoot.localScale = new Vector3(0.94f, 0.94f, 1f);
            m_DeckPanelRoot.DOScale(Vector3.one, 0.16f).SetEase(Ease.OutBack);
        }
    }

    private void HideDeckPanel()
    {
        if (m_DeckOverlay == null)
        {
            return;
        }

        m_DeckOverlay.SetActive(false);
    }

    private void OpenSettings()
    {
        GF.UI.OpenUIForm(UIViews.SettingDialog);
    }

    private static string L(string key) => GF.Localization.GetString(key);

    private static string LF(string key, params object[] args)
    {
        string format = L(key);
        if (args == null || args.Length == 0)
        {
            return format;
        }

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
        private readonly Button m_Button;
        private readonly CanvasGroup m_CanvasGroup;
        private readonly TextMeshProUGUI m_CodeText;
        private readonly TextMeshProUGUI m_NameText;
        private readonly TextMeshProUGUI m_DescText;
        private readonly MessageBoxTrigger m_MessageBoxTrigger;
        private bool m_HasVisualState;
        private bool m_LastIsFront;
        private bool m_LastIsRewardOffer;

        public RectTransform Root { get; }

        private CardSlotWidget(RectTransform root, Image background, Button button, CanvasGroup canvasGroup, TextMeshProUGUI codeText, TextMeshProUGUI nameText, TextMeshProUGUI descText, MessageBoxTrigger messageBoxTrigger)
        {
            Root = root;
            m_Background = background;
            m_Button = button;
            m_CanvasGroup = canvasGroup;
            m_CodeText = codeText;
            m_NameText = nameText;
            m_DescText = descText;
            m_MessageBoxTrigger = messageBoxTrigger;
        }

        public static CardSlotWidget Create(Transform parent, TMP_FontAsset fontAsset)
        {
            var slotObj = new GameObject("CardSlot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(LayoutElement), typeof(VerticalLayoutGroup), typeof(MessageBoxTrigger));
            slotObj.transform.SetParent(parent, false);
            var slotRect = slotObj.GetComponent<RectTransform>();
            slotRect.localScale = Vector3.one;

            var bg = slotObj.GetComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);
            bg.raycastTarget = true;

            var button = slotObj.GetComponent<Button>();
            button.targetGraphic = bg;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.95f, 1f, 1f);
            colors.pressedColor = new Color(0.82f, 0.88f, 0.94f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.8f);
            button.colors = colors;

            var layout = slotObj.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 14, 14);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var layoutElement = slotObj.GetComponent<LayoutElement>();
            layoutElement.minWidth = 140f;
            layoutElement.minHeight = 180f;

            TextMeshProUGUI codeText = CreateText("Code", slotRect, fontAsset, 20, TextAlignmentOptions.TopLeft);
            codeText.color = new Color(0.90f, 0.85f, 0.45f, 1f);
            codeText.raycastTarget = false;

            TextMeshProUGUI nameText = CreateText("Name", slotRect, fontAsset, 30, TextAlignmentOptions.TopLeft);
            nameText.raycastTarget = false;

            TextMeshProUGUI descText = CreateText("Desc", slotRect, fontAsset, 19, TextAlignmentOptions.TopLeft);
            descText.color = new Color(0.78f, 0.86f, 0.90f, 1f);
            descText.fontStyle = FontStyles.Italic;
            descText.raycastTarget = false;

            return new CardSlotWidget(slotRect, bg, button, slotObj.GetComponent<CanvasGroup>(), codeText, nameText, descText, slotObj.GetComponent<MessageBoxTrigger>());
        }

        public void Bind(int index, Action<int> onClick)
        {
            m_Button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                m_Button.onClick.AddListener(() => onClick(index));
            }
        }

        public void Apply(CardSlotViewData data)
        {
            bool playRevealAnimation = data.IsFront && (!m_HasVisualState || !m_LastIsFront);
            bool playRewardAnimation = data.IsRewardOffer && (!m_HasVisualState || !m_LastIsRewardOffer);

            bool isFront = data.IsFront;
            m_Button.interactable = data.IsClickable;
            m_MessageBoxTrigger.enabled = isFront;

            if (!isFront)
            {
                m_CodeText.alignment = TextAlignmentOptions.Center;
                m_CodeText.fontSize = 18;
                m_CodeText.text = string.Empty;
                m_NameText.alignment = TextAlignmentOptions.Center;
                m_NameText.fontSize = 58;
                m_NameText.text = "?";
                m_DescText.alignment = TextAlignmentOptions.Center;
                m_DescText.text = string.Empty;
                m_Background.color = data.IsClickable
                    ? new Color(0.15f, 0.18f, 0.23f, 0.98f)
                    : new Color(0.10f, 0.12f, 0.16f, 0.92f);
                m_MessageBoxTrigger.SetText(string.Empty, string.Empty);
                CompleteVisualState(data);
                return;
            }

            m_CodeText.alignment = TextAlignmentOptions.TopLeft;
            m_CodeText.fontSize = 20;
            m_CodeText.text = string.IsNullOrEmpty(data.Code) ? "--" : data.Code;
            m_NameText.alignment = TextAlignmentOptions.TopLeft;
            m_NameText.fontSize = 30;
            m_NameText.text = string.IsNullOrEmpty(data.Name) ? "-" : data.Name;
            m_DescText.alignment = TextAlignmentOptions.TopLeft;
            m_DescText.text = string.IsNullOrEmpty(data.Description) ? string.Empty : data.Description;
            m_MessageBoxTrigger.SetText(
                string.IsNullOrEmpty(data.Name) ? L("FlipRun.Hover.FallbackTitle") : data.Name,
                LF("FlipRun.Hover.Content", data.Code, data.Description));
            if (data.IsRewardOffer)
            {
                m_Background.color = new Color(0.29f, 0.24f, 0.13f, 0.98f);
            }
            else if (data.IsPersistent)
            {
                m_Background.color = new Color(0.18f, 0.27f, 0.39f, 0.98f);
            }
            else
            {
                m_Background.color = data.Triggered
                    ? new Color(0.22f, 0.37f, 0.23f, 0.97f)
                    : new Color(0.16f, 0.20f, 0.24f, 0.97f);
            }

            if (playRewardAnimation)
            {
                PlayRewardAnimation();
            }
            else if (playRevealAnimation)
            {
                PlayRevealAnimation();
            }

            CompleteVisualState(data);
        }

        private void CompleteVisualState(CardSlotViewData data)
        {
            m_HasVisualState = true;
            m_LastIsFront = data.IsFront;
            m_LastIsRewardOffer = data.IsRewardOffer;
        }

        private void PlayRevealAnimation()
        {
            Root.DOKill();
            m_CanvasGroup.DOKill();
            Root.localScale = new Vector3(0.84f, 1.03f, 1f);
            m_CanvasGroup.alpha = 0.82f;

            Sequence sequence = DOTween.Sequence();
            sequence.Join(Root.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutBack));
            sequence.Join(m_CanvasGroup.DOFade(1f, 0.14f));
        }

        private void PlayRewardAnimation()
        {
            Root.DOKill();
            m_CanvasGroup.DOKill();
            Root.localScale = new Vector3(0.88f, 0.88f, 1f);
            m_CanvasGroup.alpha = 0f;

            Sequence sequence = DOTween.Sequence();
            sequence.Join(m_CanvasGroup.DOFade(1f, 0.20f));
            sequence.Join(Root.DOScale(1.04f, 0.20f).SetEase(Ease.OutBack));
            sequence.Append(Root.DOScale(Vector3.one, 0.10f).SetEase(Ease.OutQuad));
        }
    }
}
