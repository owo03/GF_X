using System.Collections.Generic;
using GameFramework;
using GameFramework.Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public partial class SettingDialog : UIFormBase
{
    int m_ClickCount;
    float m_LastClickTime;
    readonly float clickInterval = 0.4f;
    private RectTransform m_SettingsPanelRoot;
    private RectTransform m_SettingsContentRoot;
    private TextMeshProUGUI m_TitleText;
    private Button m_CloseButton;
    private TextMeshProUGUI m_MusicLabel;
    private Slider m_MusicSlider;
    private TextMeshProUGUI m_MusicValue;
    private TextMeshProUGUI m_SfxLabel;
    private Slider m_SfxSlider;
    private TextMeshProUGUI m_SfxValue;
    private TextMeshProUGUI m_LanguageLabel;
    private Button m_LanguageButton;
    private TextMeshProUGUI m_LanguageValue;
    private TextMeshProUGUI m_ResolutionLabel;
    private TextMeshProUGUI m_ResolutionValue;
    private TextMeshProUGUI m_FullScreenLabel;
    private Toggle m_FullScreenToggle;
    private readonly List<Vector2Int> m_Resolutions = new List<Vector2Int>();
    private int m_CurrentResolutionIndex;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        RestoreLegacySettingsControls();
        BindLegacySettingsControls();
    }

    public override void InitLocalization()
    {
        base.InitLocalization();
        if (varVersionTxt != null)
        {
            varVersionTxt.text = Utility.Text.Format("{0}v{1}", AppSettings.Instance.DebugMode ? "Debug " : string.Empty, GF.Base.EditorResourceMode ? Application.version : Utility.Text.Format("{0}({1})", Application.version, GF.Resource.InternalResourceVersion));
        }
        RefreshLanguage();
        RefreshAudioValueTexts();
        RefreshVibrateToggle();
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        GF.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        m_ClickCount = 0;
        m_LastClickTime = Time.time;
        InitSettings();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        GF.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        base.OnClose(isShutdown, userData);
    }

    private void InitSettings()
    {
        if (m_MusicSlider != null)
        {
            m_MusicSlider.SetValueWithoutNotify(GF.Setting.GetMediaMute(Const.SoundGroup.Music) ? 0f : GF.Setting.GetMediaVolume(Const.SoundGroup.Music));
        }

        if (m_SfxSlider != null)
        {
            m_SfxSlider.SetValueWithoutNotify(GF.Setting.GetMediaMute(Const.SoundGroup.Sound) ? 0f : GF.Setting.GetMediaVolume(Const.SoundGroup.Sound));
        }

        RefreshAudioValueTexts();
        RefreshLanguage();
        RefreshVibrateToggle();
    }

    private void OnSoundFxSliderChanged(float value)
    {
        GF.Setting.SetMediaVolume(Const.SoundGroup.Sound, value);
        GF.Setting.SetMediaMute(Const.SoundGroup.Sound, value <= 0.001f);
        RefreshAudioValueTexts();
    }

    private void OnMusicSliderChanged(float value)
    {
        GF.Setting.SetMediaVolume(Const.SoundGroup.Music, value);
        GF.Setting.SetMediaMute(Const.SoundGroup.Music, value <= 0.001f);
        RefreshAudioValueTexts();
    }

    private void RefreshAudioValueTexts()
    {
        if (m_MusicValue != null && m_MusicSlider != null)
        {
            m_MusicValue.text = Utility.Text.Format("{0}%", Mathf.RoundToInt(m_MusicSlider.value * 100f));
        }

        if (m_SfxValue != null && m_SfxSlider != null)
        {
            m_SfxValue.text = Utility.Text.Format("{0}%", Mathf.RoundToInt(m_SfxSlider.value * 100f));
        }
    }

    private void RefreshLanguage()
    {
        if (m_LanguageValue == null && varLanguageName == null)
        {
            return;
        }

        var curLang = GF.Setting.GetLanguage();
        var langTb = GF.DataTable.GetDataTable<LanguagesTable>();
        var langRow = langTb.GetDataRow(row => row.LanguageKey == curLang.ToString());
        string display = langRow != null ? langRow.LanguageDisplay : curLang.ToString();
        if (m_LanguageValue != null)
        {
            m_LanguageValue.text = display;
        }
        if (varLanguageName != null)
        {
            varLanguageName.text = display;
        }
        if (varIconFlag != null && langRow != null && !string.IsNullOrWhiteSpace(langRow.LanguageIcon))
        {
            varIconFlag.SetSprite(langRow.LanguageIcon);
        }
    }

    protected override void OnButtonClick(object sender, Button btSelf)
    {
        base.OnButtonClick(sender, btSelf);
        if (btSelf == varBtnLanguage)
        {
            OpenLanguageDialog();
        }
    }

    private void OpenLanguageDialog()
    {
        var uiParms = UIParams.Create();
        VarAction action = ReferencePool.Acquire<VarAction>();
        action.Value = OnLanguageChanged;
        uiParms.Set<VarAction>(LanguagesDialog.P_LangChangedCb, action);
        GF.UI.OpenUIForm(UIViews.LanguagesDialog, uiParms);
    }

    void OnLanguageChanged()
    {
        RefreshLanguage();
        GF.UI.CloseUIForms(UIViews.LanguagesDialog);
        ReloadLanguage();
    }

    private void ReloadLanguage()
    {
        GF.Localization.RemoveAllRawStrings();
        GF.Localization.LoadLanguage(GF.Localization.Language.ToString(), this);
    }

    private void OnLanguageReloaded(object sender, GameEventArgs e)
    {
        GF.UI.UpdateLocalizationTexts();
        RefreshLanguage();
        RefreshDisplaySettingTexts();
        RefreshAudioValueTexts();
    }

    public void OnClickVersionText()
    {
        if (Time.time - m_LastClickTime <= clickInterval)
        {
            m_ClickCount++;
            if (m_ClickCount > 5)
            {
                GF.Debugger.ActiveWindow = !GF.Debugger.ActiveWindow;
                m_ClickCount = 0;
            }
        }
        else
        {
            m_ClickCount = 0;
        }
        m_LastClickTime = Time.time;
    }

    private void Back2Home()
    {
        var curProcedure = GF.Procedure.CurrentProcedure;
        if (curProcedure is GameProcedure)
        {
            var gameProcedure = curProcedure as GameProcedure;
            gameProcedure.BackHome();
        }
    }

    private void RestoreLegacySettingsControls()
    {
        Transform popup = transform.Find("Popup08_Topbar_Divided");
        if (popup != null)
        {
            popup.gameObject.SetActive(true);
        }

        Transform maskPopup = transform.Find("Mask/Popup08_Topbar_Divided");
        if (maskPopup != null)
        {
            maskPopup.gameObject.SetActive(true);
        }

        if (varVersionTxt != null)
        {
            varVersionTxt.gameObject.SetActive(true);
        }

        ShowLegacyControl(varMusicSlider);
        ShowLegacyControl(varSoundFxSlider);
        ShowLegacyControl(varBtnLanguage);
        ShowLegacyControl(varToggleVibrate);
        ShowLegacyControl(varBtnHelp);
        ShowLegacyControl(varBtnPrivacy);
        ShowLegacyControl(varBtnTermsOfService);
        ShowLegacyControl(varBtnRating);

        if (m_SettingsPanelRoot != null)
        {
            m_SettingsPanelRoot.gameObject.SetActive(false);
        }
    }

    private void BindLegacySettingsControls()
    {
        if (varMusicSlider != null)
        {
            varMusicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
            varMusicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        if (varSoundFxSlider != null)
        {
            varSoundFxSlider.onValueChanged.RemoveListener(OnSoundFxSliderChanged);
            varSoundFxSlider.onValueChanged.AddListener(OnSoundFxSliderChanged);
        }

        if (varToggleVibrate != null)
        {
            varToggleVibrate.onValueChanged.RemoveListener(OnVibrateToggleChanged);
            varToggleVibrate.onValueChanged.AddListener(OnVibrateToggleChanged);
        }
    }

    private static void ShowLegacyControl(Component component)
    {
        if (component != null)
        {
            component.gameObject.SetActive(true);
        }
    }

    private void RefreshVibrateToggle()
    {
        if (varToggleVibrate == null)
        {
            return;
        }

        bool isOn = !GF.Setting.GetMediaMute(Const.SoundGroup.Vibrate);
        varToggleVibrate.SetIsOnWithoutNotify(isOn);
        if (varVibrateHandle != null)
        {
            varVibrateHandle.anchorMin = isOn ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
            varVibrateHandle.anchorMax = varVibrateHandle.anchorMin;
            varVibrateHandle.anchoredPosition = Vector2.zero;
        }
    }

    private void OnVibrateToggleChanged(bool isOn)
    {
        GF.Setting.SetMediaMute(Const.SoundGroup.Vibrate, !isOn);
        RefreshVibrateToggle();
    }

    private void BuildSettingsUI()
    {
        if (m_SettingsPanelRoot != null)
        {
            return;
        }

        RectTransform layoutRoot = transform as RectTransform;
        if (layoutRoot == null)
        {
            return;
        }

        TMP_FontAsset fontAsset = varVersionTxt != null ? varVersionTxt.font : null;
        m_SettingsPanelRoot = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        m_SettingsPanelRoot.SetParent(layoutRoot, false);
        m_SettingsPanelRoot.anchorMin = new Vector2(0.5f, 0.5f);
        m_SettingsPanelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        m_SettingsPanelRoot.pivot = new Vector2(0.5f, 0.5f);
        m_SettingsPanelRoot.anchoredPosition = new Vector2(0f, 0f);
        m_SettingsPanelRoot.sizeDelta = new Vector2(820f, 520f);
        m_SettingsPanelRoot.SetAsLastSibling();

        var panelImage = m_SettingsPanelRoot.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.11f, 0.15f, 0.96f);
        panelImage.raycastTarget = true;

        RectTransform headerRoot = new GameObject("Header", typeof(RectTransform)).GetComponent<RectTransform>();
        headerRoot.SetParent(m_SettingsPanelRoot, false);
        headerRoot.anchorMin = new Vector2(0f, 1f);
        headerRoot.anchorMax = new Vector2(1f, 1f);
        headerRoot.pivot = new Vector2(0.5f, 1f);
        headerRoot.offsetMin = new Vector2(28f, -84f);
        headerRoot.offsetMax = new Vector2(-28f, -24f);

        m_TitleText = CreateText("Title", headerRoot, fontAsset, 40, TextAlignmentOptions.Left);
        RectTransform titleRect = m_TitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 0f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(0f, 0f);
        titleRect.offsetMax = new Vector2(-108f, 0f);

        m_CloseButton = CreateActionButton("CloseButton", headerRoot, fontAsset, "X", OnClickClose);
        RectTransform closeRect = m_CloseButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 0.5f);
        closeRect.anchorMax = new Vector2(1f, 0.5f);
        closeRect.pivot = new Vector2(1f, 0.5f);
        closeRect.anchoredPosition = new Vector2(0f, 0f);
        closeRect.sizeDelta = new Vector2(64f, 48f);

        m_SettingsContentRoot = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
        m_SettingsContentRoot.SetParent(m_SettingsPanelRoot, false);
        m_SettingsContentRoot.anchorMin = new Vector2(0f, 0f);
        m_SettingsContentRoot.anchorMax = new Vector2(1f, 1f);
        m_SettingsContentRoot.pivot = new Vector2(0.5f, 0.5f);
        m_SettingsContentRoot.offsetMin = new Vector2(28f, 28f);
        m_SettingsContentRoot.offsetMax = new Vector2(-28f, -96f);

        var contentLayout = m_SettingsContentRoot.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 16f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childAlignment = TextAnchor.UpperCenter;

        RectTransform musicRow = CreateRow("MusicRow");
        m_MusicLabel = CreateRowLabel("MusicLabel", musicRow, fontAsset);
        m_MusicSlider = CreateRuntimeSlider(musicRow, OnMusicSliderChanged);
        m_MusicValue = CreateValueText("MusicValue", musicRow, fontAsset);

        RectTransform sfxRow = CreateRow("SfxRow");
        m_SfxLabel = CreateRowLabel("SfxLabel", sfxRow, fontAsset);
        m_SfxSlider = CreateRuntimeSlider(sfxRow, OnSoundFxSliderChanged);
        m_SfxValue = CreateValueText("SfxValue", sfxRow, fontAsset);

        RectTransform languageRow = CreateRow("LanguageRow");
        m_LanguageLabel = CreateRowLabel("LanguageLabel", languageRow, fontAsset);
        m_LanguageButton = CreateValueButton(languageRow, fontAsset, OpenLanguageDialog, out m_LanguageValue);
        m_LanguageValue.rectTransform.sizeDelta = new Vector2(262f, 54f);
        var languageValueLayout = m_LanguageValue.GetComponent<LayoutElement>();
        if (languageValueLayout == null)
        {
            languageValueLayout = m_LanguageValue.gameObject.AddComponent<LayoutElement>();
        }
        languageValueLayout.minWidth = 262f;
        languageValueLayout.preferredWidth = 262f;

        RectTransform resolutionRow = CreateRow("ResolutionRow");
        m_ResolutionLabel = CreateRowLabel("ResolutionLabel", resolutionRow, fontAsset);
        CreateActionButton("PrevResolution", resolutionRow, fontAsset, "<", OnClickPrevResolution);
        m_ResolutionValue = CreateValueText("ResolutionValue", resolutionRow, fontAsset);
        m_ResolutionValue.rectTransform.sizeDelta = new Vector2(240f, 54f);
        var resolutionValueLayout = m_ResolutionValue.GetComponent<LayoutElement>();
        resolutionValueLayout.minWidth = 240f;
        resolutionValueLayout.preferredWidth = 240f;
        CreateActionButton("NextResolution", resolutionRow, fontAsset, ">", OnClickNextResolution);

        RectTransform fullScreenRow = CreateRow("FullScreenRow");
        m_FullScreenLabel = CreateRowLabel("FullScreenLabel", fullScreenRow, fontAsset);
        m_FullScreenToggle = CreateRuntimeToggle(fullScreenRow);
        m_FullScreenToggle.onValueChanged.AddListener(OnFullScreenChanged);
    }

    private RectTransform CreateRow(string name)
    {
        var row = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement)).GetComponent<RectTransform>();
        row.SetParent(m_SettingsContentRoot, false);
        row.sizeDelta = new Vector2(0f, 62f);

        var image = row.GetComponent<Image>();
        image.color = new Color(0.13f, 0.17f, 0.22f, 0.94f);
        image.raycastTarget = false;

        var layoutElement = row.GetComponent<LayoutElement>();
        layoutElement.minHeight = 62f;
        layoutElement.preferredHeight = 62f;

        var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 14f;
        rowLayout.padding = new RectOffset(18, 18, 4, 4);
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        return row;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset fontAsset, int fontSize, TextAlignmentOptions alignment)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.font = fontAsset;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private TextMeshProUGUI CreateRowLabel(string name, Transform parent, TMP_FontAsset fontAsset)
    {
        TextMeshProUGUI label = CreateText(name, parent, fontAsset, 28, TextAlignmentOptions.Left);
        label.rectTransform.sizeDelta = new Vector2(190f, 54f);
        var layout = label.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 190f;
        layout.preferredWidth = 190f;
        layout.minHeight = 54f;
        layout.preferredHeight = 54f;
        return label;
    }

    private TextMeshProUGUI CreateValueText(string name, Transform parent, TMP_FontAsset fontAsset)
    {
        TextMeshProUGUI value = CreateText(name, parent, fontAsset, 24, TextAlignmentOptions.Center);
        value.rectTransform.sizeDelta = new Vector2(88f, 54f);
        value.color = new Color(0.92f, 0.95f, 0.99f, 1f);
        var layout = value.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 88f;
        layout.preferredWidth = 88f;
        layout.minHeight = 54f;
        layout.preferredHeight = 54f;
        return value;
    }

    private Button CreateValueButton(Transform parent, TMP_FontAsset fontAsset, UnityEngine.Events.UnityAction onClick, out TextMeshProUGUI valueText)
    {
        Button button = new GameObject("ValueButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement)).GetComponent<Button>();
        button.transform.SetParent(parent, false);
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(290f, 54f);
        var layout = button.GetComponent<LayoutElement>();
        layout.minWidth = 290f;
        layout.preferredWidth = 290f;
        layout.minHeight = 54f;
        layout.preferredHeight = 54f;

        var image = button.GetComponent<Image>();
        image.color = new Color(0.18f, 0.24f, 0.30f, 0.98f);
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        valueText = CreateText("Value", button.transform, fontAsset, 24, TextAlignmentOptions.Center);
        RectTransform textRect = valueText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 0f);
        textRect.offsetMax = new Vector2(-14f, 0f);
        var valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
        valueLayout.minHeight = 54f;
        valueLayout.preferredHeight = 54f;
        return button;
    }

    private Button CreateActionButton(string name, Transform parent, TMP_FontAsset fontAsset, string text, UnityEngine.Events.UnityAction onClick)
    {
        Button button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement)).GetComponent<Button>();
        button.transform.SetParent(parent, false);
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(58f, 54f);
        var layout = button.GetComponent<LayoutElement>();
        layout.minWidth = 58f;
        layout.preferredWidth = 58f;
        layout.minHeight = 54f;
        layout.preferredHeight = 54f;

        var image = button.GetComponent<Image>();
        image.color = new Color(0.24f, 0.31f, 0.22f, 0.98f);
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI label = CreateText("Text", button.transform, fontAsset, 28, TextAlignmentOptions.Center);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.text = text;
        return button;
    }

    private Slider CreateRuntimeSlider(Transform parent, UnityEngine.Events.UnityAction<float> onChanged)
    {
        Slider slider = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement)).GetComponent<Slider>();
        RectTransform sliderRect = slider.GetComponent<RectTransform>();
        sliderRect.SetParent(parent, false);
        sliderRect.sizeDelta = new Vector2(360f, 40f);
        var layout = slider.GetComponent<LayoutElement>();
        layout.minWidth = 360f;
        layout.preferredWidth = 360f;
        layout.minHeight = 40f;
        layout.preferredHeight = 40f;

        var background = new GameObject("Background", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        background.SetParent(sliderRect, false);
        background.anchorMin = new Vector2(0f, 0.5f);
        background.anchorMax = new Vector2(1f, 0.5f);
        background.pivot = new Vector2(0.5f, 0.5f);
        background.sizeDelta = new Vector2(0f, 8f);
        background.GetComponent<Image>().color = new Color(0.20f, 0.24f, 0.28f, 1f);

        var fillArea = new GameObject("FillArea", typeof(RectTransform)).GetComponent<RectTransform>();
        fillArea.SetParent(sliderRect, false);
        fillArea.anchorMin = new Vector2(0f, 0.5f);
        fillArea.anchorMax = new Vector2(1f, 0.5f);
        fillArea.offsetMin = new Vector2(0f, -4f);
        fillArea.offsetMax = new Vector2(0f, 4f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        fill.SetParent(fillArea, false);
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(1f, 1f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = new Color(0.90f, 0.74f, 0.30f, 1f);

        var handleArea = new GameObject("HandleSlideArea", typeof(RectTransform)).GetComponent<RectTransform>();
        handleArea.SetParent(sliderRect, false);
        handleArea.anchorMin = new Vector2(0f, 0f);
        handleArea.anchorMax = new Vector2(1f, 1f);
        handleArea.offsetMin = new Vector2(0f, 0f);
        handleArea.offsetMax = new Vector2(0f, 0f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        handle.SetParent(handleArea, false);
        handle.sizeDelta = new Vector2(18f, 18f);
        handle.GetComponent<Image>().color = new Color(0.96f, 0.97f, 0.99f, 1f);

        slider.targetGraphic = handle.GetComponent<Image>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.onValueChanged.AddListener(onChanged);
        return slider;
    }

    private Toggle CreateRuntimeToggle(Transform parent)
    {
        Toggle toggle = new GameObject("FullScreenToggle", typeof(RectTransform), typeof(Image), typeof(Toggle), typeof(LayoutElement)).GetComponent<Toggle>();
        RectTransform toggleRect = toggle.GetComponent<RectTransform>();
        toggleRect.SetParent(parent, false);
        toggleRect.sizeDelta = new Vector2(120f, 40f);
        var layout = toggle.GetComponent<LayoutElement>();
        layout.minWidth = 120f;
        layout.preferredWidth = 120f;
        layout.minHeight = 40f;
        layout.preferredHeight = 40f;

        var background = toggle.GetComponent<Image>();
        background.color = new Color(0.16f, 0.22f, 0.28f, 0.98f);

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        handle.SetParent(toggle.transform, false);
        handle.anchorMin = new Vector2(0.24f, 0.5f);
        handle.anchorMax = new Vector2(0.24f, 0.5f);
        handle.pivot = new Vector2(0.5f, 0.5f);
        handle.sizeDelta = new Vector2(34f, 24f);
        handle.GetComponent<Image>().color = new Color(0.96f, 0.96f, 0.96f, 1f);

        TextMeshProUGUI label = CreateText("StateText", toggle.transform, varVersionTxt != null ? varVersionTxt.font : null, 22, TextAlignmentOptions.Center);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 0f);
        labelRect.offsetMax = new Vector2(-10f, 0f);
        label.fontSize = 18;

        toggle.graphic = handle.GetComponent<Image>();
        toggle.targetGraphic = background;
        toggle.onValueChanged.AddListener(isOn =>
        {
            label.text = isOn ? GF.Localization.GetString("ON") : GF.Localization.GetString("OFF");
            handle.anchorMin = isOn ? new Vector2(0.76f, 0.5f) : new Vector2(0.24f, 0.5f);
            handle.anchorMax = handle.anchorMin;
            handle.anchoredPosition = Vector2.zero;
        });
        return toggle;
    }

    private void InitDisplaySettings()
    {
        m_Resolutions.Clear();
        m_Resolutions.AddRange(DisplaySettingsExtension.GetSupportedResolutions());
        if (m_Resolutions.Count <= 0)
        {
            m_Resolutions.Add(new Vector2Int(Screen.width, Screen.height));
        }

        Vector2Int current = new Vector2Int(Screen.width, Screen.height);
        int bestIndex = 0;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < m_Resolutions.Count; i++)
        {
            int distance = Mathf.Abs(m_Resolutions[i].x - current.x) + Mathf.Abs(m_Resolutions[i].y - current.y);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        m_CurrentResolutionIndex = bestIndex;
        if (m_FullScreenToggle != null)
        {
            m_FullScreenToggle.SetIsOnWithoutNotify(Screen.fullScreen);
            RefreshToggleVisual(m_FullScreenToggle);
        }
        RefreshDisplaySettingTexts();
    }

    private void OnClickPrevResolution()
    {
        if (m_Resolutions.Count <= 0)
        {
            return;
        }

        m_CurrentResolutionIndex = (m_CurrentResolutionIndex - 1 + m_Resolutions.Count) % m_Resolutions.Count;
        ApplyCurrentDisplaySettings();
    }

    private void OnClickNextResolution()
    {
        if (m_Resolutions.Count <= 0)
        {
            return;
        }

        m_CurrentResolutionIndex = (m_CurrentResolutionIndex + 1) % m_Resolutions.Count;
        ApplyCurrentDisplaySettings();
    }

    private void OnFullScreenChanged(bool isOn)
    {
        RefreshToggleVisual(m_FullScreenToggle);
        ApplyCurrentDisplaySettings();
    }

    private void ApplyCurrentDisplaySettings()
    {
        if (m_Resolutions.Count <= 0)
        {
            return;
        }

        Vector2Int value = m_Resolutions[m_CurrentResolutionIndex];
        GF.Setting.ApplyDisplaySettings(value.x, value.y, m_FullScreenToggle != null && m_FullScreenToggle.isOn);
        RefreshDisplaySettingTexts();
    }

    private void RefreshDisplaySettingTexts()
    {
        if (m_TitleText != null)
        {
            m_TitleText.text = GF.Localization.GetString("Settings.Title");
        }
        if (m_MusicLabel != null)
        {
            m_MusicLabel.text = GF.Localization.GetString("Settings.Music");
        }
        if (m_SfxLabel != null)
        {
            m_SfxLabel.text = GF.Localization.GetString("Settings.SFX");
        }
        if (m_LanguageLabel != null)
        {
            m_LanguageLabel.text = GF.Localization.GetString("Settings.Language");
        }
        if (m_ResolutionLabel != null)
        {
            m_ResolutionLabel.text = GF.Localization.GetString("Settings.Resolution");
        }
        if (m_FullScreenLabel != null)
        {
            m_FullScreenLabel.text = GF.Localization.GetString("Settings.FullScreen");
        }
        if (m_ResolutionValue != null)
        {
            Vector2Int value = m_Resolutions.Count > 0 ? m_Resolutions[Mathf.Clamp(m_CurrentResolutionIndex, 0, m_Resolutions.Count - 1)] : new Vector2Int(Screen.width, Screen.height);
            m_ResolutionValue.text = value.x + " x " + value.y;
        }

        RefreshToggleVisual(m_FullScreenToggle);
    }

    private static void RefreshToggleVisual(Toggle toggle)
    {
        if (toggle == null)
        {
            return;
        }

        var stateText = toggle.GetComponentInChildren<TextMeshProUGUI>();
        if (stateText != null)
        {
            stateText.text = toggle.isOn ? GF.Localization.GetString("ON") : GF.Localization.GetString("OFF");
        }

        if (toggle.graphic is Image handleImage)
        {
            RectTransform handle = handleImage.rectTransform;
            handle.anchorMin = toggle.isOn ? new Vector2(0.76f, 0.5f) : new Vector2(0.24f, 0.5f);
            handle.anchorMax = handle.anchorMin;
            handle.anchoredPosition = Vector2.zero;
        }
    }
}
