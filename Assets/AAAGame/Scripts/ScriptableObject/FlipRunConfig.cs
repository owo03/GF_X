using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum FlipRunCardEffectType
{
    FlatGain,
    ComboFlatGain,
    EconomyScaling,
    GoldCache,
    TicketCashout,
    RabbitScaling,
    RabbitSupport,
    ExtraFlipCharm,
    SavingsEndBonus,
    LedgerEndBonus,
    MirrorPrevious,
    ComboBoost,
    EconomyMagnet,
    PrestigeScoring,
    LastFlipBurst,
    RandomGain,
    PersistentFlatGain,
}

public enum FlipRunCardRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
}

[Serializable]
public sealed class FlipRunDeckEntry
{
    public string cardId;
    public int count = 1;
}

[Serializable]
public sealed class FlipRunCardDefinition
{
    public string id;
    public string code;
    public string nameKey;
    public string descKey;
    public FlipRunCardRarity rarity;
    public string[] tags;
    public FlipRunCardEffectType effectType;
    public int immediateCoins;
    public int immediateScore;
    public int bonusValueA;
    public int bonusValueB;
    public int percentValue;
    public int persistentTurns;
    public int turnStartCoins;
    public int turnStartScore;
    public int turnEndCoins;
    public int turnEndScore;
    public int randomCoinsMin;
    public int randomCoinsMax;
    public int randomScoreMin;
    public int randomScoreMax;
    public int offerWeight = 1;
    public int rewardWeight = 0;
    public int shopWeight = 0;
    public bool canAppearInRewards = true;
    public bool canAppearInShop = true;
}

[CreateAssetMenu(fileName = "FlipRunConfig", menuName = "GF/FlipRun Config")]
public sealed class FlipRunConfig : ScriptableObject
{
    private static readonly int[] s_DefaultTargetScores = { 40, 100, 190 };
    private static FlipRunConfig s_Instance;

    [Header("Board")]
    [SerializeField] private int m_BoardColumns = 4;
    [SerializeField] private int m_BoardRows = 3;
    [SerializeField] private int m_BaseFlips = 5;
    [SerializeField] private int m_MaxFlips = 7;
    [SerializeField] private int m_RewardOfferCount = 3;
    [SerializeField] private int m_SmallRoundsPerBigRound = 3;

    [Header("Economy")]
    [SerializeField] private int m_StartCoins = 12;
    [SerializeField] private int m_RelicPrice = 14;
    [SerializeField] private int m_CardPrice = 8;

    [Header("Progression")]
    [SerializeField] private int[] m_TargetScores = { 40, 100, 190 };
    [SerializeField] private List<FlipRunDeckEntry> m_StartingDeck = new List<FlipRunDeckEntry>();

    [Header("Cards")]
    [SerializeField] private List<FlipRunCardDefinition> m_Cards = new List<FlipRunCardDefinition>();

    public int BoardColumns => Mathf.Max(1, m_BoardColumns);
    public int BoardRows => Mathf.Max(1, m_BoardRows);
    public int BoardSize => BoardColumns * BoardRows;
    public int BaseFlips => Mathf.Max(1, m_BaseFlips);
    public int MaxFlips => Mathf.Max(BaseFlips, m_MaxFlips);
    public int RewardOfferCount => Mathf.Max(1, m_RewardOfferCount);
    public int SmallRoundsPerBigRound => Mathf.Max(1, m_SmallRoundsPerBigRound);
    public int StartCoins => Mathf.Max(0, m_StartCoins);
    public int RelicPrice => Mathf.Max(0, m_RelicPrice);
    public int CardPrice => Mathf.Max(0, m_CardPrice);
    public IReadOnlyList<int> TargetScores => m_TargetScores != null && m_TargetScores.Length > 0 ? m_TargetScores : s_DefaultTargetScores;
    public IReadOnlyList<FlipRunDeckEntry> StartingDeck => m_StartingDeck;
    public IReadOnlyList<FlipRunCardDefinition> Cards => m_Cards;

    private void Awake()
    {
        s_Instance = this;
    }

#if UNITY_EDITOR
    public static FlipRunConfig GetInstanceEditor()
    {
        if (s_Instance == null)
        {
            string assetPath = UtilityBuiltin.AssetsPath.GetScriptableAsset("FlipRun/FlipRunConfig");
            s_Instance = AssetDatabase.LoadAssetAtPath<FlipRunConfig>(assetPath);
        }

        if (s_Instance == null)
        {
            s_Instance = CreateFallbackRuntime();
        }

        return s_Instance;
    }
#endif

    public static async Task<FlipRunConfig> GetInstanceSync()
    {
        if (s_Instance != null)
        {
            return s_Instance;
        }

        string assetPath = UtilityBuiltin.AssetsPath.GetScriptableAsset("FlipRun/FlipRunConfig");
        try
        {
            s_Instance = await GFBuiltin.Resource.LoadAssetAwait<FlipRunConfig>(assetPath);
        }
        catch (Exception exception)
        {
            Log.Warning("Load FlipRunConfig failed, fallback to runtime defaults. {0}", exception.Message);
        }

        if (s_Instance == null)
        {
            s_Instance = CreateFallbackRuntime();
        }

        return s_Instance;
    }

    public static FlipRunConfig CreateFallbackRuntime()
    {
        var config = CreateInstance<FlipRunConfig>();
        config.name = "FlipRunConfig_Fallback";
        config.ApplyDefaultValues();
        return config;
    }

    private void ApplyDefaultValues()
    {
        m_BoardColumns = 4;
        m_BoardRows = 3;
        m_BaseFlips = 5;
        m_MaxFlips = 7;
        m_RewardOfferCount = 3;
        m_SmallRoundsPerBigRound = 3;
        m_StartCoins = 12;
        m_RelicPrice = 14;
        m_CardPrice = 8;
        m_TargetScores = new[] { 40, 100, 190 };
        m_StartingDeck = new List<FlipRunDeckEntry>
        {
            Entry("CopperCoin", 5),
            Entry("Ticket", 2),
            Entry("Rabbit", 2),
            Entry("Carrot", 2),
            Entry("GoldBlock", 1),
            Entry("Clover", 2),
            Entry("SilverCoin", 2),
            Entry("Magnet", 1),
            Entry("PiggyBank", 1),
            Entry("Mine", 1),
        };
        m_Cards = new List<FlipRunCardDefinition>
        {
            Card("CopperCoin", "COP", "FlipRun.Card.CopperCoin", "FlipRun.CardDesc.CopperCoin", FlipRunCardRarity.Common, FlipRunCardEffectType.ComboFlatGain, immediateCoins: 2, immediateScore: 1, tags: new[] { "Economy", "Combo" }),
            Card("SilverCoin", "SLV", "FlipRun.Card.SilverCoin", "FlipRun.CardDesc.SilverCoin", FlipRunCardRarity.Common, FlipRunCardEffectType.EconomyScaling, immediateCoins: 4, immediateScore: 2, bonusValueA: 2, tags: new[] { "Economy" }),
            Card("GoldBlock", "GLD", "FlipRun.Card.GoldBlock", "FlipRun.CardDesc.GoldBlock", FlipRunCardRarity.Uncommon, FlipRunCardEffectType.GoldCache, immediateCoins: 7, immediateScore: 3, bonusValueA: 2, rewardWeight: 1, shopWeight: 1, tags: new[] { "Economy", "Treasure" }),
            Card("Ticket", "TIK", "FlipRun.Card.Ticket", "FlipRun.CardDesc.Ticket", FlipRunCardRarity.Common, FlipRunCardEffectType.TicketCashout, immediateCoins: 1, immediateScore: 6, bonusValueA: 5, tags: new[] { "Cashout" }),
            Card("Rabbit", "RAB", "FlipRun.Card.Rabbit", "FlipRun.CardDesc.Rabbit", FlipRunCardRarity.Common, FlipRunCardEffectType.RabbitScaling, immediateScore: 3, bonusValueA: 4, tags: new[] { "Rabbit", "Scaling" }),
            Card("Carrot", "CAR", "FlipRun.Card.Carrot", "FlipRun.CardDesc.Carrot", FlipRunCardRarity.Common, FlipRunCardEffectType.RabbitSupport, immediateCoins: 1, immediateScore: 4, bonusValueA: 6, tags: new[] { "Rabbit", "Support" }),
            Card("Clover", "CLV", "FlipRun.Card.Clover", "FlipRun.CardDesc.Clover", FlipRunCardRarity.Uncommon, FlipRunCardEffectType.ExtraFlipCharm, immediateScore: 2, bonusValueA: 1, bonusValueB: 1, tags: new[] { "Combo", "Tempo" }),
            Card("PiggyBank", "PIG", "FlipRun.Card.PiggyBank", "FlipRun.CardDesc.PiggyBank", FlipRunCardRarity.Uncommon, FlipRunCardEffectType.SavingsEndBonus, immediateScore: 1, percentValue: 15, tags: new[] { "Economy", "Cashout" }),
            Card("Ledger", "LED", "FlipRun.Card.Ledger", "FlipRun.CardDesc.Ledger", FlipRunCardRarity.Uncommon, FlipRunCardEffectType.LedgerEndBonus, immediateScore: 2, percentValue: 25, tags: new[] { "Economy", "Cashout" }),
            Card("Mirror", "MIR", "FlipRun.Card.Mirror", "FlipRun.CardDesc.Mirror", FlipRunCardRarity.Rare, FlipRunCardEffectType.MirrorPrevious, percentValue: 70, rewardWeight: 1, shopWeight: 1, tags: new[] { "Combo", "Copy" }),
            Card("ChainBadge", "CHN", "FlipRun.Card.ChainBadge", "FlipRun.CardDesc.ChainBadge", FlipRunCardRarity.Uncommon, FlipRunCardEffectType.ComboBoost, immediateScore: 2, percentValue: 50, tags: new[] { "Combo" }),
            Card("Magnet", "MAG", "FlipRun.Card.Magnet", "FlipRun.CardDesc.Magnet", FlipRunCardRarity.Uncommon, FlipRunCardEffectType.EconomyMagnet, immediateCoins: 2, immediateScore: 2, bonusValueA: 2, tags: new[] { "Economy", "Synergy" }),
            Card("Crown", "CRW", "FlipRun.Card.Crown", "FlipRun.CardDesc.Crown", FlipRunCardRarity.Rare, FlipRunCardEffectType.PrestigeScoring, immediateScore: 7, bonusValueA: 3, bonusValueB: 2, rewardWeight: 1, shopWeight: 1, tags: new[] { "Prestige", "Treasure" }),
            Card("Firework", "FIR", "FlipRun.Card.Firework", "FlipRun.CardDesc.Firework", FlipRunCardRarity.Uncommon, FlipRunCardEffectType.LastFlipBurst, immediateCoins: 1, immediateScore: 4, bonusValueA: 6, tags: new[] { "Finale", "Tempo" }),
            Card("Dice", "DIC", "FlipRun.Card.Dice", "FlipRun.CardDesc.Dice", FlipRunCardRarity.Uncommon, FlipRunCardEffectType.RandomGain, rewardWeight: 2, shopWeight: 2, randomCoinsMin: 1, randomCoinsMax: 5, randomScoreMin: 1, randomScoreMax: 6, tags: new[] { "Random" }),
            Card("Mine", "MIN", "FlipRun.Card.Mine", "FlipRun.CardDesc.Mine", FlipRunCardRarity.Uncommon, FlipRunCardEffectType.PersistentFlatGain, immediateCoins: 1, persistentTurns: 2, turnStartCoins: 2, tags: new[] { "Economy", "Persistent" }),
            Card("Fountain", "FNT", "FlipRun.Card.Fountain", "FlipRun.CardDesc.Fountain", FlipRunCardRarity.Uncommon, FlipRunCardEffectType.PersistentFlatGain, immediateScore: 2, persistentTurns: 2, turnEndScore: 4, tags: new[] { "Score", "Persistent" }),
            Card("Market", "MKT", "FlipRun.Card.Market", "FlipRun.CardDesc.Market", FlipRunCardRarity.Rare, FlipRunCardEffectType.PersistentFlatGain, immediateCoins: 1, immediateScore: 1, persistentTurns: 3, turnStartCoins: 1, turnEndScore: 2, rewardWeight: 1, shopWeight: 1, tags: new[] { "Economy", "Persistent", "Score" }),
        };
    }

    private static FlipRunDeckEntry Entry(string cardId, int count)
    {
        return new FlipRunDeckEntry
        {
            cardId = cardId,
            count = count,
        };
    }

    private static FlipRunCardDefinition Card(
        string id,
        string code,
        string nameKey,
        string descKey,
        FlipRunCardRarity rarity,
        FlipRunCardEffectType effectType,
        int immediateCoins = 0,
        int immediateScore = 0,
        int bonusValueA = 0,
        int bonusValueB = 0,
        int percentValue = 0,
        int persistentTurns = 0,
        int turnStartCoins = 0,
        int turnStartScore = 0,
        int turnEndCoins = 0,
        int turnEndScore = 0,
        int randomCoinsMin = 0,
        int randomCoinsMax = 0,
        int randomScoreMin = 0,
        int randomScoreMax = 0,
        int offerWeight = 1,
        int rewardWeight = 0,
        int shopWeight = 0,
        string[] tags = null,
        bool canAppearInRewards = true,
        bool canAppearInShop = true)
    {
        return new FlipRunCardDefinition
        {
            id = id,
            code = code,
            nameKey = nameKey,
            descKey = descKey,
            rarity = rarity,
            tags = tags ?? Array.Empty<string>(),
            effectType = effectType,
            immediateCoins = immediateCoins,
            immediateScore = immediateScore,
            bonusValueA = bonusValueA,
            bonusValueB = bonusValueB,
            percentValue = percentValue,
            persistentTurns = persistentTurns,
            turnStartCoins = turnStartCoins,
            turnStartScore = turnStartScore,
            turnEndCoins = turnEndCoins,
            turnEndScore = turnEndScore,
            randomCoinsMin = randomCoinsMin,
            randomCoinsMax = randomCoinsMax,
            randomScoreMin = randomScoreMin,
            randomScoreMax = randomScoreMax,
            offerWeight = offerWeight,
            rewardWeight = rewardWeight,
            shopWeight = shopWeight,
            canAppearInRewards = canAppearInRewards,
            canAppearInShop = canAppearInShop,
        };
    }
}
