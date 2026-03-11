#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlipRunConfig))]
public sealed class FlipRunConfigInspector : Editor
{
    private static readonly string[] s_RarityNames =
    {
        "普通",
        "优秀",
        "稀有",
        "史诗",
    };

    private static readonly string[] s_EffectNames =
    {
        "基础收益",
        "连击收益",
        "经济联动",
        "金块爆发",
        "门票结算",
        "兔子成长",
        "萝卜辅助",
        "额外翻牌",
        "存钱罐结算",
        "账本结算",
        "镜像复制",
        "连锁增幅",
        "磁石联动",
        "皇冠加分",
        "收尾烟花",
        "随机收益",
        "持续卡",
    };

    private SerializedProperty m_BoardColumns;
    private SerializedProperty m_BoardRows;
    private SerializedProperty m_BaseFlips;
    private SerializedProperty m_MaxFlips;
    private SerializedProperty m_RewardOfferCount;
    private SerializedProperty m_SmallRoundsPerBigRound;
    private SerializedProperty m_StartCoins;
    private SerializedProperty m_RelicPrice;
    private SerializedProperty m_CardPrice;
    private SerializedProperty m_TargetScores;
    private SerializedProperty m_StartingDeck;
    private SerializedProperty m_Cards;

    private int m_SelectedCardIndex;
    private string m_SearchText = string.Empty;
    private Vector2 m_CardListScroll;
    private Vector2 m_CardDetailScroll;

    private void OnEnable()
    {
        m_BoardColumns = serializedObject.FindProperty("m_BoardColumns");
        m_BoardRows = serializedObject.FindProperty("m_BoardRows");
        m_BaseFlips = serializedObject.FindProperty("m_BaseFlips");
        m_MaxFlips = serializedObject.FindProperty("m_MaxFlips");
        m_RewardOfferCount = serializedObject.FindProperty("m_RewardOfferCount");
        m_SmallRoundsPerBigRound = serializedObject.FindProperty("m_SmallRoundsPerBigRound");
        m_StartCoins = serializedObject.FindProperty("m_StartCoins");
        m_RelicPrice = serializedObject.FindProperty("m_RelicPrice");
        m_CardPrice = serializedObject.FindProperty("m_CardPrice");
        m_TargetScores = serializedObject.FindProperty("m_TargetScores");
        m_StartingDeck = serializedObject.FindProperty("m_StartingDeck");
        m_Cards = serializedObject.FindProperty("m_Cards");
        ClampSelection();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        ClampSelection();

        DrawInspectorHeader();
        EditorGUILayout.Space(6f);
        DrawRuntimeSettings();
        EditorGUILayout.Space(6f);
        DrawStartingDeck();
        EditorGUILayout.Space(8f);
        DrawCardsEditor();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawInspectorHeader()
    {
        EditorGUILayout.HelpBox(
            "这是翻牌玩法的中文配置面板。\n" +
            "左侧管理卡牌列表，右侧编辑卡牌详情；上方区域用于调整棋盘、回合和商店数值。",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        DrawStatBox("卡牌总数", m_Cards.arraySize.ToString());
        DrawStatBox("初始牌库项", m_StartingDeck.arraySize.ToString());
        DrawStatBox("棋盘尺寸", $"{m_BoardColumns.intValue} x {m_BoardRows.intValue}");
        EditorGUILayout.EndHorizontal();
    }

    private void DrawRuntimeSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("运行参数", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(m_BoardColumns, new GUIContent("棋盘列数"));
        EditorGUILayout.PropertyField(m_BoardRows, new GUIContent("棋盘行数"));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(m_BaseFlips, new GUIContent("基础翻牌数"));
        EditorGUILayout.PropertyField(m_MaxFlips, new GUIContent("最大翻牌数"));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(m_RewardOfferCount, new GUIContent("奖励张数"));
        EditorGUILayout.PropertyField(m_SmallRoundsPerBigRound, new GUIContent("每大回合小回合数"));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(m_StartCoins, new GUIContent("初始金币"));
        EditorGUILayout.PropertyField(m_RelicPrice, new GUIContent("遗物价格"));
        EditorGUILayout.PropertyField(m_CardPrice, new GUIContent("随机卡价格"));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(m_TargetScores, new GUIContent("目标分数组"));
        EditorGUILayout.EndVertical();
    }

    private void DrawStartingDeck()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("初始牌库", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("添加牌库项", GUILayout.Width(100f)))
        {
            AddStartingDeckEntry();
        }
        EditorGUILayout.EndHorizontal();

        if (m_StartingDeck.arraySize <= 0)
        {
            EditorGUILayout.HelpBox("当前没有初始牌库项。", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        string[] cardIds = GetAllCardIds();
        for (int i = 0; i < m_StartingDeck.arraySize; i++)
        {
            SerializedProperty entry = m_StartingDeck.GetArrayElementAtIndex(i);
            SerializedProperty cardId = entry.FindPropertyRelative("cardId");
            SerializedProperty count = entry.FindPropertyRelative("count");

            EditorGUILayout.BeginHorizontal();
            DrawCardIdPopup(cardId, cardIds, "卡牌");
            count.intValue = Mathf.Max(0, EditorGUILayout.IntField("数量", count.intValue, GUILayout.Width(160f)));
            if (GUILayout.Button("删", GUILayout.Width(36f)))
            {
                m_StartingDeck.DeleteArrayElementAtIndex(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawCardsEditor()
    {
        EditorGUILayout.BeginHorizontal();
        DrawCardListPanel();
        DrawCardDetailPanel();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawCardListPanel()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(320f), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("卡牌列表", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        m_SearchText = EditorGUILayout.TextField("搜索", m_SearchText);
        if (GUILayout.Button("清空", GUILayout.Width(48f)))
        {
            m_SearchText = string.Empty;
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("新增卡牌"))
        {
            AddCard();
        }
        if (GUILayout.Button("复制"))
        {
            DuplicateSelectedCard();
        }
        if (GUILayout.Button("删除"))
        {
            DeleteSelectedCard();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("按稀有度填建议权重"))
        {
            ApplySuggestedWeights();
        }
        EditorGUILayout.EndHorizontal();

        m_CardListScroll = EditorGUILayout.BeginScrollView(m_CardListScroll);
        for (int i = 0; i < m_Cards.arraySize; i++)
        {
            SerializedProperty card = m_Cards.GetArrayElementAtIndex(i);
            if (!IsCardVisible(card))
            {
                continue;
            }

            bool selected = i == m_SelectedCardIndex;
            GUIStyle style = new GUIStyle(EditorStyles.miniButtonMid);
            style.alignment = TextAnchor.MiddleLeft;
            style.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            style.normal.textColor = selected ? new Color(0.20f, 0.85f, 0.55f) : EditorStyles.label.normal.textColor;

            if (GUILayout.Button(BuildCardListTitle(card), style, GUILayout.Height(28f)))
            {
                m_SelectedCardIndex = i;
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawCardDetailPanel()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        if (m_Cards.arraySize <= 0 || m_SelectedCardIndex < 0 || m_SelectedCardIndex >= m_Cards.arraySize)
        {
            EditorGUILayout.HelpBox("先在左侧添加一张卡牌。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        SerializedProperty card = m_Cards.GetArrayElementAtIndex(m_SelectedCardIndex);
        SerializedProperty id = card.FindPropertyRelative("id");
        SerializedProperty code = card.FindPropertyRelative("code");
        SerializedProperty nameKey = card.FindPropertyRelative("nameKey");
        SerializedProperty descKey = card.FindPropertyRelative("descKey");
        SerializedProperty rarity = card.FindPropertyRelative("rarity");
        SerializedProperty effectType = card.FindPropertyRelative("effectType");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"卡牌详情 #{m_SelectedCardIndex + 1}", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField(BuildCardListTitle(card), EditorStyles.miniBoldLabel, GUILayout.Width(280f));
        EditorGUILayout.EndHorizontal();

        m_CardDetailScroll = EditorGUILayout.BeginScrollView(m_CardDetailScroll);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(id, new GUIContent("卡牌 ID"));
        EditorGUILayout.PropertyField(code, new GUIContent("缩写代码"));
        EditorGUILayout.PropertyField(nameKey, new GUIContent("名称多语言 Key"));
        EditorGUILayout.PropertyField(descKey, new GUIContent("描述多语言 Key"));
        DrawEnumPopup(rarity, s_RarityNames, "稀有度");
        DrawEnumPopup(effectType, s_EffectNames, "效果模板");
        DrawTagsField(card.FindPropertyRelative("tags"));
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("数值参数", EditorStyles.boldLabel);
        DrawIntField(card, "immediateCoins", "立即金币");
        DrawIntField(card, "immediateScore", "立即分数");
        DrawIntField(card, "bonusValueA", "附加参数 A");
        DrawIntField(card, "bonusValueB", "附加参数 B");
        DrawIntField(card, "percentValue", "百分比参数");
        DrawIntField(card, "randomCoinsMin", "随机金币最小");
        DrawIntField(card, "randomCoinsMax", "随机金币最大");
        DrawIntField(card, "randomScoreMin", "随机分数最小");
        DrawIntField(card, "randomScoreMax", "随机分数最大");
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("持续效果", EditorStyles.boldLabel);
        DrawIntField(card, "persistentTurns", "持续回合数");
        DrawIntField(card, "turnStartCoins", "回合开始金币");
        DrawIntField(card, "turnStartScore", "回合开始分数");
        DrawIntField(card, "turnEndCoins", "回合结束金币");
        DrawIntField(card, "turnEndScore", "回合结束分数");
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("投放设置", EditorStyles.boldLabel);
        DrawIntField(card, "offerWeight", "通用权重");
        DrawIntField(card, "rewardWeight", "奖励权重");
        DrawIntField(card, "shopWeight", "商店权重");
        EditorGUILayout.PropertyField(card.FindPropertyRelative("canAppearInRewards"), new GUIContent("可出现在奖励"));
        EditorGUILayout.PropertyField(card.FindPropertyRelative("canAppearInShop"), new GUIContent("可出现在商店"));
        EditorGUILayout.HelpBox("奖励权重或商店权重填 0 时，会自动回退到“通用权重”。", MessageType.None);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void AddStartingDeckEntry()
    {
        m_StartingDeck.InsertArrayElementAtIndex(m_StartingDeck.arraySize);
        SerializedProperty entry = m_StartingDeck.GetArrayElementAtIndex(m_StartingDeck.arraySize - 1);
        entry.FindPropertyRelative("cardId").stringValue = GetFallbackCardId();
        entry.FindPropertyRelative("count").intValue = 1;
    }

    private void AddCard()
    {
        m_Cards.InsertArrayElementAtIndex(m_Cards.arraySize);
        SerializedProperty card = m_Cards.GetArrayElementAtIndex(m_Cards.arraySize - 1);
        string baseId = "NewCard";
        string uniqueId = MakeUniqueCardId(baseId);
        card.FindPropertyRelative("id").stringValue = uniqueId;
        card.FindPropertyRelative("code").stringValue = uniqueId.ToUpperInvariant();
        card.FindPropertyRelative("nameKey").stringValue = "FlipRun.Card." + uniqueId;
        card.FindPropertyRelative("descKey").stringValue = "FlipRun.CardDesc." + uniqueId;
        card.FindPropertyRelative("rarity").enumValueIndex = 0;
        card.FindPropertyRelative("effectType").enumValueIndex = 0;
        card.FindPropertyRelative("offerWeight").intValue = 1;
        card.FindPropertyRelative("rewardWeight").intValue = 0;
        card.FindPropertyRelative("shopWeight").intValue = 0;
        card.FindPropertyRelative("canAppearInRewards").boolValue = true;
        card.FindPropertyRelative("canAppearInShop").boolValue = true;
        ClearStringArray(card.FindPropertyRelative("tags"));
        m_SelectedCardIndex = m_Cards.arraySize - 1;
    }

    private void DuplicateSelectedCard()
    {
        if (!HasSelectedCard())
        {
            return;
        }

        SerializedProperty source = m_Cards.GetArrayElementAtIndex(m_SelectedCardIndex);
        string sourceId = source.FindPropertyRelative("id").stringValue;
        m_Cards.InsertArrayElementAtIndex(m_SelectedCardIndex + 1);
        SerializedProperty copy = m_Cards.GetArrayElementAtIndex(m_SelectedCardIndex + 1);
        copy.serializedObject.ApplyModifiedProperties();
        copy.FindPropertyRelative("id").stringValue = MakeUniqueCardId(sourceId + "Copy");
        copy.FindPropertyRelative("code").stringValue = source.FindPropertyRelative("code").stringValue + "_C";
        copy.FindPropertyRelative("nameKey").stringValue = "FlipRun.Card." + copy.FindPropertyRelative("id").stringValue;
        copy.FindPropertyRelative("descKey").stringValue = "FlipRun.CardDesc." + copy.FindPropertyRelative("id").stringValue;
        m_SelectedCardIndex++;
    }

    private void DeleteSelectedCard()
    {
        if (!HasSelectedCard())
        {
            return;
        }

        m_Cards.DeleteArrayElementAtIndex(m_SelectedCardIndex);
        ClampSelection();
    }

    private void ApplySuggestedWeights()
    {
        for (int i = 0; i < m_Cards.arraySize; i++)
        {
            SerializedProperty card = m_Cards.GetArrayElementAtIndex(i);
            int rarity = card.FindPropertyRelative("rarity").enumValueIndex;
            int weight = rarity switch
            {
                0 => 10,
                1 => 6,
                2 => 3,
                3 => 1,
                _ => 1
            };

            card.FindPropertyRelative("offerWeight").intValue = weight;
            if (card.FindPropertyRelative("rewardWeight").intValue <= 0)
            {
                card.FindPropertyRelative("rewardWeight").intValue = weight;
            }
            if (card.FindPropertyRelative("shopWeight").intValue <= 0)
            {
                card.FindPropertyRelative("shopWeight").intValue = weight;
            }
        }
    }

    private void DrawStatBox(string title, string value)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(title, EditorStyles.miniLabel);
        EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawEnumPopup(SerializedProperty property, string[] options, string label)
    {
        int next = EditorGUILayout.Popup(label, property.enumValueIndex, options);
        property.enumValueIndex = Mathf.Clamp(next, 0, property.enumNames.Length - 1);
    }

    private void DrawIntField(SerializedProperty root, string fieldName, string label)
    {
        SerializedProperty property = root.FindPropertyRelative(fieldName);
        property.intValue = EditorGUILayout.IntField(label, property.intValue);
    }

    private void DrawTagsField(SerializedProperty tagsProperty)
    {
        string current = JoinTags(tagsProperty);
        string next = EditorGUILayout.DelayedTextField("标签（逗号分隔）", current);
        if (!string.Equals(current, next, StringComparison.Ordinal))
        {
            SetTags(tagsProperty, next);
        }
    }

    private void DrawCardIdPopup(SerializedProperty cardIdProperty, string[] cardIds, string label)
    {
        if (cardIds.Length <= 0)
        {
            EditorGUILayout.PropertyField(cardIdProperty, new GUIContent(label));
            return;
        }

        int selected = Array.IndexOf(cardIds, cardIdProperty.stringValue);
        if (selected < 0)
        {
            selected = 0;
        }

        int next = EditorGUILayout.Popup(label, selected, cardIds);
        cardIdProperty.stringValue = cardIds[Mathf.Clamp(next, 0, cardIds.Length - 1)];
    }

    private bool IsCardVisible(SerializedProperty card)
    {
        if (string.IsNullOrWhiteSpace(m_SearchText))
        {
            return true;
        }

        string keyword = m_SearchText.Trim();
        return BuildCardListTitle(card).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string BuildCardListTitle(SerializedProperty card)
    {
        string code = card.FindPropertyRelative("code").stringValue;
        string id = card.FindPropertyRelative("id").stringValue;
        int rarityIndex = Mathf.Clamp(card.FindPropertyRelative("rarity").enumValueIndex, 0, s_RarityNames.Length - 1);
        return $"{code} | {id} | {s_RarityNames[rarityIndex]}";
    }

    private string[] GetAllCardIds()
    {
        var ids = new List<string>(m_Cards.arraySize);
        for (int i = 0; i < m_Cards.arraySize; i++)
        {
            string id = m_Cards.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
            if (!string.IsNullOrWhiteSpace(id))
            {
                ids.Add(id);
            }
        }

        if (ids.Count == 0)
        {
            ids.Add("CopperCoin");
        }

        return ids.ToArray();
    }

    private string GetFallbackCardId()
    {
        string[] ids = GetAllCardIds();
        return ids.Length > 0 ? ids[0] : "CopperCoin";
    }

    private string MakeUniqueCardId(string baseId)
    {
        string candidate = string.IsNullOrWhiteSpace(baseId) ? "NewCard" : baseId;
        HashSet<string> exists = new HashSet<string>(GetAllCardIds(), StringComparer.Ordinal);
        if (!exists.Contains(candidate))
        {
            return candidate;
        }

        int index = 2;
        while (exists.Contains(candidate + index))
        {
            index++;
        }

        return candidate + index;
    }

    private static string JoinTags(SerializedProperty tagsProperty)
    {
        var parts = new List<string>(tagsProperty.arraySize);
        for (int i = 0; i < tagsProperty.arraySize; i++)
        {
            string value = tagsProperty.GetArrayElementAtIndex(i).stringValue;
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add(value);
            }
        }

        return string.Join(", ", parts);
    }

    private static void SetTags(SerializedProperty tagsProperty, string text)
    {
        string[] split = string.IsNullOrWhiteSpace(text)
            ? Array.Empty<string>()
            : text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        tagsProperty.arraySize = 0;
        for (int i = 0; i < split.Length; i++)
        {
            string tag = split[i].Trim();
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize);
            tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = tag;
        }
    }

    private static void ClearStringArray(SerializedProperty property)
    {
        property.arraySize = 0;
    }

    private bool HasSelectedCard()
    {
        return m_SelectedCardIndex >= 0 && m_SelectedCardIndex < m_Cards.arraySize;
    }

    private void ClampSelection()
    {
        if (m_Cards == null || m_Cards.arraySize <= 0)
        {
            m_SelectedCardIndex = -1;
            return;
        }

        m_SelectedCardIndex = Mathf.Clamp(m_SelectedCardIndex, 0, m_Cards.arraySize - 1);
    }
}
#endif
