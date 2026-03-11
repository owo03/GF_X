using System;
using System.Collections.Generic;
using System.Text;
using GameFramework;
using GameFramework.Event;
using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
public class GameProcedure : ProcedureBase
{
    private GameUIForm m_GameUI;
    private IFsm<IProcedureManager> m_Procedure;
    private RunSession m_Run;
    private bool m_IsTransitioning;

    protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        m_Procedure = procedureOwner;
        m_IsTransitioning = false;
        GF.BuiltinView.HideLoadingProgress();
        if (GF.Base.IsGamePaused)
        {
            GF.Base.ResumeGame();
        }

        GF.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        FlipRunConfig config = await FlipRunConfig.GetInstanceSync();
        m_Run = new RunSession(config);
        m_GameUI = await GF.UI.OpenUIFormAwait(UIViews.GameUIForm) as GameUIForm;
        if (m_GameUI != null)
        {
            m_GameUI.SetCardSlotClickHandler(OnCardSlotClicked);
        }
        RefreshRunUI();
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        if (m_IsTransitioning || m_Run == null)
        {
            return;
        }

        if (m_Run.CanAdvanceWithoutBoardClick && Input.GetMouseButtonDown(0))
        {
            if (m_GameUI != null && m_GameUI.IsModalOpen())
            {
                return;
            }
            m_Run.Advance();
            AfterRunStateChanged();
        }
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        if (GF.Base.IsGamePaused)
        {
            GF.Base.ResumeGame();
        }

        GF.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        if (!isShutdown && m_GameUI != null)
        {
            GF.UI.CloseUIForm(m_GameUI.UIForm.SerialId);
        }
        m_GameUI = null;
        m_Run = null;
        base.OnLeave(procedureOwner, isShutdown);
    }

    public void Restart() => ChangeState<GameProcedure>(m_Procedure);
    public void BackHome() => ChangeState<GameProcedure>(m_Procedure);

    private void OnCardSlotClicked(int index)
    {
        if (m_IsTransitioning || m_Run == null || (m_GameUI != null && m_GameUI.IsModalOpen()))
        {
            return;
        }

        if (!m_Run.TryClickCard(index))
        {
            return;
        }

        AfterRunStateChanged();
    }

    private void AfterRunStateChanged()
    {
        RefreshRunUI();
        if (m_Run != null && m_Run.IsEnded)
        {
            OnGameOver(m_Run.IsWin);
        }
    }

    private void RefreshRunUI()
    {
        if (m_GameUI == null || m_Run == null)
        {
            return;
        }

        m_GameUI.SetRunStatus(m_Run.BuildStatusText());
        m_GameUI.SetBoardSlots(m_Run.BuildBoardViewData());
        m_GameUI.SetDeckEntries(m_Run.BuildDeckViewData(), m_Run.BuildRelicSummaryText());
        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        playerDm.Coins = m_Run.Coins;
    }

    private void OnGameOver(bool isWin)
    {
        if (m_IsTransitioning)
        {
            return;
        }

        m_IsTransitioning = true;
        m_Procedure.SetData<VarBoolean>("IsWin", isWin);
        ChangeState<GameOverProcedure>(m_Procedure);
    }

    private void OnLanguageReloaded(object sender, GameEventArgs e)
    {
        if (m_IsTransitioning || m_Run == null)
        {
            return;
        }

        RefreshRunUI();
    }

    private enum RunPhase
    {
        Battle,
        Reward,
        Shop,
        Ended
    }

    private enum RelicId
    {
        CoinPouch,
        ScoreSeal,
        FlipCharm
    }

    private struct CardSlot
    {
        public string Id;
        public bool Triggered;
        public bool IsPersistent;
        public int RemainingTurns;
    }

    private sealed class RunSession
    {
        private readonly FlipRunConfig m_Config;
        private readonly System.Random m_Rng = new System.Random(Environment.TickCount);
        private readonly List<CardSlot> m_Board;
        private readonly List<string> m_RewardOffers;
        private readonly List<string> m_Logs = new List<string>(8);
        private readonly Dictionary<string, int> m_Deck = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, FlipRunCardDefinition> m_CardDefinitions = new Dictionary<string, FlipRunCardDefinition>(StringComparer.Ordinal);
        private readonly List<FlipRunCardDefinition> m_OfferCards = new List<FlipRunCardDefinition>(24);
        private readonly HashSet<RelicId> m_Relics = new HashSet<RelicId>();

        private RunPhase m_Phase;
        private RunPhase m_AfterRewardPhase;
        private int m_Coins;
        private int m_Score;
        private int m_BigRound;
        private int m_SmallRound;
        private int m_TotalTurn;
        private int m_NextTurnExtraFlip;
        private int m_RemainingFlips;
        private int m_TurnCoins;
        private int m_TurnScore;
        private int m_PiggyCount;
        private int m_LedgerCount;
        private int m_RabbitCount;
        private int m_GoldCount;
        private int m_EcoCount;
        private int m_PreviousCoins;
        private int m_PreviousScore;
        private int m_FlipsResolvedThisTurn;
        private float m_ComboMultiplier;
        private string m_PreviousCard;
        private bool m_HasPreviousCard;
        private bool m_IsWin;

        public bool IsEnded => m_Phase == RunPhase.Ended;
        public bool IsWin => m_IsWin;
        public bool CanAdvanceWithoutBoardClick => m_Phase == RunPhase.Shop;
        public int Coins => m_Coins;

        public RunSession(FlipRunConfig config)
        {
            m_Config = config ?? FlipRunConfig.CreateFallbackRuntime();
            m_Board = new List<CardSlot>(GetBoardSize());
            m_RewardOffers = new List<string>(GetRewardOfferCount());
            m_Coins = m_Config.StartCoins;
            m_Phase = RunPhase.Battle;
            m_AfterRewardPhase = RunPhase.Battle;
            BuildCardDefinitions();
            InitializeStartingDeck();

            AddLog(L("FlipRun.Log.RunStart"));
            StartBattleBoard();
        }
        public void Advance()
        {
            if (m_Phase == RunPhase.Shop)
            {
                ResolveShop();
            }
        }

        public bool TryClickCard(int index)
        {
            if (m_Phase == RunPhase.Battle)
            {
                return TryFlipCard(index);
            }

            if (m_Phase == RunPhase.Reward)
            {
                return TryChooseReward(index);
            }

            return false;
        }

        public List<GameUIForm.CardSlotViewData> BuildBoardViewData()
        {
            if (m_Phase == RunPhase.Reward)
            {
                return BuildRewardViewData();
            }

            var list = new List<GameUIForm.CardSlotViewData>(GetBoardSize());
            bool canFlip = m_Phase == RunPhase.Battle && m_RemainingFlips > 0;
            for (int i = 0; i < GetBoardSize(); i++)
            {
                CardSlot slot = m_Board[i];
                FlipRunCardDefinition definition = GetCardDefinition(slot.Id);
                bool isFront = slot.Triggered;
                list.Add(new GameUIForm.CardSlotViewData
                {
                    Code = slot.IsPersistent ? $"{definition.code} T{slot.RemainingTurns}" : definition.code,
                    Name = L(definition.nameKey),
                    Description = BuildBoardDescription(slot),
                    IsFront = isFront,
                    IsClickable = canFlip && !slot.Triggered,
                    IsRewardOffer = false,
                    IsPersistent = slot.IsPersistent,
                    RemainingTurns = slot.RemainingTurns,
                    Triggered = slot.Triggered,
                });
            }

            return list;
        }

        public List<GameUIForm.DeckEntryViewData> BuildDeckViewData()
        {
            var entries = new List<GameUIForm.DeckEntryViewData>(m_Deck.Count);
            foreach (KeyValuePair<string, int> kv in m_Deck)
            {
                FlipRunCardDefinition definition = GetCardDefinition(kv.Key);
                entries.Add(new GameUIForm.DeckEntryViewData
                {
                    Code = definition.code,
                    Name = L(definition.nameKey),
                    Description = L(definition.descKey),
                    Count = kv.Value,
                });
            }

            entries.Sort((left, right) =>
            {
                int countCompare = right.Count.CompareTo(left.Count);
                if (countCompare != 0)
                {
                    return countCompare;
                }

                return string.CompareOrdinal(left.Code, right.Code);
            });
            return entries;
        }

        public string BuildRelicSummaryText()
        {
            if (m_Relics.Count <= 0)
            {
                return L("FlipRun.Deck.NoRelics");
            }

            var names = new List<string>(m_Relics.Count);
            foreach (RelicId relic in m_Relics)
            {
                names.Add(L(GetRelicNameKey(relic)));
            }
            names.Sort(StringComparer.Ordinal);
            return LF("FlipRun.Deck.Relics", string.Join(" / ", names));
        }

        public string BuildStatusText()
        {
            var sb = new StringBuilder(1200);
            int roundIndex = m_Phase == RunPhase.Battle
                ? Mathf.Clamp(m_SmallRound + 1, 1, GetSmallRoundsPerBigRound())
                : Mathf.Clamp(m_SmallRound, 1, GetSmallRoundsPerBigRound());
            int targetIndex = Mathf.Clamp(m_BigRound, 0, GetTargetScoreCount() - 1);

            sb.AppendLine(L("FlipRun.Title"));
            sb.AppendLine(LF("FlipRun.Status.ScoreCoins", m_Score, GetTargetScore(targetIndex), m_Coins));
            sb.AppendLine(LF("FlipRun.Status.BigRound", m_BigRound + 1, GetTargetScoreCount(), roundIndex, GetSmallRoundsPerBigRound()));
            sb.AppendLine(LF("FlipRun.Status.DeckRelics", GetDeckCount(), m_Relics.Count));
            sb.AppendLine(m_Phase == RunPhase.Reward
                ? LF("FlipRun.Status.RewardCount", m_RewardOffers.Count)
                : LF("FlipRun.Status.BoardConfig", GetBoardColumns(), GetBoardRows(), m_Phase == RunPhase.Battle ? m_RemainingFlips : 0));
            sb.AppendLine(LF("FlipRun.Status.PersistentCount", CountActivePersistentSlots()));
            sb.AppendLine();
            sb.AppendLine(L("FlipRun.Label.Recent"));
            foreach (string log in m_Logs)
            {
                sb.Append("- ").AppendLine(log);
            }
            sb.AppendLine();
            sb.Append(m_Phase == RunPhase.Battle
                ? L("FlipRun.Action.Battle")
                : m_Phase == RunPhase.Reward
                    ? L("FlipRun.Action.Reward")
                    : m_Phase == RunPhase.Shop
                        ? L("FlipRun.Action.Shop")
                        : L("FlipRun.Action.Ended"));
            return sb.ToString();
        }

        private List<GameUIForm.CardSlotViewData> BuildRewardViewData()
        {
            var list = new List<GameUIForm.CardSlotViewData>(m_RewardOffers.Count);
            for (int i = 0; i < m_RewardOffers.Count; i++)
            {
                string id = m_RewardOffers[i];
                FlipRunCardDefinition definition = GetCardDefinition(id);
                list.Add(new GameUIForm.CardSlotViewData
                {
                    Code = definition.code,
                    Name = L(definition.nameKey),
                    Description = L(definition.descKey),
                    IsFront = true,
                    IsClickable = true,
                    IsRewardOffer = true,
                    IsPersistent = false,
                    RemainingTurns = 0,
                    Triggered = false,
                });
            }

            return list;
        }

        private void StartBattleBoard()
        {
            if (m_Phase == RunPhase.Ended)
            {
                return;
            }

            ResetTurnState();
            m_TotalTurn++;
            m_Phase = RunPhase.Battle;

            PrepareBoardSlotsForBattle();
            ResolvePersistentTurnStartEffects();

            int flipCount = GetFlipCount();
            m_NextTurnExtraFlip = 0;
            m_RemainingFlips = flipCount;

            AddLog(LF("FlipRun.Log.BoardGenerated", GetBoardColumns(), GetBoardRows()));
            AddLog(LF("FlipRun.Log.TurnReveal", m_TotalTurn, flipCount, GetBoardColumns(), GetBoardRows()));
        }

        private void ResetTurnState()
        {
            m_TurnCoins = 0;
            m_TurnScore = 0;
            m_PiggyCount = 0;
            m_LedgerCount = 0;
            m_RabbitCount = 0;
            m_GoldCount = 0;
            m_EcoCount = 0;
            m_PreviousCoins = 0;
            m_PreviousScore = 0;
            m_FlipsResolvedThisTurn = 0;
            m_ComboMultiplier = 1f;
            m_HasPreviousCard = false;
        }

        private void PrepareBoardSlotsForBattle()
        {
            while (m_Board.Count < GetBoardSize())
            {
                m_Board.Add(CreateBoardSlot(RollBoardCard()));
            }

            for (int i = 0; i < GetBoardSize(); i++)
            {
                CardSlot slot = m_Board[i];
                if (slot.IsPersistent && slot.RemainingTurns > 0)
                {
                    slot.Triggered = true;
                    m_Board[i] = slot;
                    continue;
                }

                m_Board[i] = CreateBoardSlot(RollBoardCard());
            }
        }
        private bool TryFlipCard(int index)
        {
            if (m_Phase != RunPhase.Battle || index < 0 || index >= m_Board.Count || m_RemainingFlips <= 0)
            {
                return false;
            }

            CardSlot slot = m_Board[index];
            if (slot.Triggered)
            {
                return false;
            }

            slot.Triggered = true;
            m_Board[index] = slot;
            ResolveCard(index, slot.Id);
            m_RemainingFlips--;

            if (m_RemainingFlips <= 0 || AreAllCardsFlipped())
            {
                FinalizeBattleTurn();
            }

            return true;
        }

        private void ResolveCard(int slotIndex, string card)
        {
            FlipRunCardDefinition definition = GetCardDefinition(card);
            bool combo = m_HasPreviousCard && string.Equals(card, m_PreviousCard, StringComparison.Ordinal);
            int comboBonus = combo ? Mathf.CeilToInt(2f * m_ComboMultiplier) : 0;
            int currentFlipIndex = m_FlipsResolvedThisTurn + 1;

            int gainCoins = 0;
            int gainScore = 0;
            switch (definition.effectType)
            {
                case FlipRunCardEffectType.FlatGain:
                    gainCoins += definition.immediateCoins;
                    gainScore += definition.immediateScore;
                    break;

                case FlipRunCardEffectType.ComboFlatGain:
                    gainCoins += definition.immediateCoins + comboBonus;
                    gainScore += definition.immediateScore;
                    m_EcoCount++;
                    break;

                case FlipRunCardEffectType.EconomyScaling:
                    gainCoins += definition.immediateCoins + comboBonus;
                    gainScore += definition.immediateScore + (m_EcoCount > 0 ? definition.bonusValueA : 0);
                    m_EcoCount++;
                    break;

                case FlipRunCardEffectType.GoldCache:
                    gainCoins += definition.immediateCoins + comboBonus * Mathf.Max(1, definition.bonusValueA);
                    gainScore += definition.immediateScore;
                    m_EcoCount++;
                    m_GoldCount++;
                    AddLog(LF("FlipRun.Log.GoldBlock", gainCoins));
                    break;

                case FlipRunCardEffectType.TicketCashout:
                    gainCoins += definition.immediateCoins;
                    gainScore += definition.immediateScore + (m_GoldCount > 0 ? definition.bonusValueA : 0);
                    break;

                case FlipRunCardEffectType.RabbitScaling:
                    m_RabbitCount++;
                    gainScore += definition.immediateScore + Mathf.Max(0, m_RabbitCount - 1) * definition.bonusValueA + comboBonus;
                    AddLog(LF("FlipRun.Log.Rabbit", gainScore));
                    break;

                case FlipRunCardEffectType.RabbitSupport:
                    gainCoins += definition.immediateCoins;
                    gainScore += definition.immediateScore + (m_RabbitCount > 0 ? definition.bonusValueA : 0);
                    break;

                case FlipRunCardEffectType.ExtraFlipCharm:
                    int extraFlip = definition.bonusValueA + (combo ? definition.bonusValueB : 0);
                    m_NextTurnExtraFlip = Mathf.Clamp(m_NextTurnExtraFlip + extraFlip, 0, 2);
                    gainScore += definition.immediateScore;
                    AddLog(LF("FlipRun.Log.Clover", extraFlip));
                    break;

                case FlipRunCardEffectType.SavingsEndBonus:
                    m_PiggyCount++;
                    gainScore += definition.immediateScore + comboBonus;
                    break;

                case FlipRunCardEffectType.LedgerEndBonus:
                    m_LedgerCount++;
                    gainScore += definition.immediateScore + comboBonus;
                    break;

                case FlipRunCardEffectType.MirrorPrevious:
                    gainCoins += Mathf.FloorToInt(m_PreviousCoins * Mathf.Max(0, definition.percentValue) * 0.01f);
                    gainScore += Mathf.FloorToInt(m_PreviousScore * Mathf.Max(0, definition.percentValue) * 0.01f);
                    AddLog(LF("FlipRun.Log.Mirror", gainCoins, gainScore));
                    break;

                case FlipRunCardEffectType.ComboBoost:
                    m_ComboMultiplier += Mathf.Max(0, definition.percentValue) * 0.01f;
                    gainScore += definition.immediateScore;
                    AddLog(LF("FlipRun.Log.ChainBadge", m_ComboMultiplier));
                    break;

                case FlipRunCardEffectType.EconomyMagnet:
                    gainCoins += definition.immediateCoins + m_EcoCount * definition.bonusValueA;
                    gainScore += definition.immediateScore;
                    if (m_EcoCount > 0)
                    {
                        AddLog(LF("FlipRun.Log.Magnet", m_EcoCount * definition.bonusValueA));
                    }
                    break;

                case FlipRunCardEffectType.PrestigeScoring:
                    gainScore += definition.immediateScore + (m_Relics.Count > 0 ? definition.bonusValueA : 0) + (m_GoldCount > 0 ? definition.bonusValueB : 0);
                    break;

                case FlipRunCardEffectType.LastFlipBurst:
                    gainCoins += definition.immediateCoins;
                    gainScore += definition.immediateScore;
                    if (m_RemainingFlips == 1)
                    {
                        gainScore += definition.bonusValueA;
                        AddLog(LF("FlipRun.Log.Firework", definition.bonusValueA));
                    }
                    break;

                case FlipRunCardEffectType.RandomGain:
                    gainCoins += RollRange(definition.randomCoinsMin, definition.randomCoinsMax);
                    gainScore += RollRange(definition.randomScoreMin, definition.randomScoreMax);
                    AddLog(LF("FlipRun.Log.Dice", gainCoins, gainScore));
                    break;

                case FlipRunCardEffectType.PersistentFlatGain:
                    gainCoins += definition.immediateCoins;
                    gainScore += definition.immediateScore;
                    break;
            }

            if (definition.persistentTurns > 0)
            {
                ActivatePersistentSlot(slotIndex, definition.persistentTurns);
                AddLog(LF("FlipRun.Log.PersistentArm", L(definition.nameKey), definition.persistentTurns));
            }

            m_TurnCoins += gainCoins;
            m_TurnScore += gainScore;
            m_PreviousCoins = gainCoins;
            m_PreviousScore = gainScore;
            m_PreviousCard = card;
            m_HasPreviousCard = true;
            m_FlipsResolvedThisTurn = currentFlipIndex;

            AddLog(LF("FlipRun.Log.FlippedCards", L(definition.nameKey)));
        }

        private void ResolvePersistentTurnStartEffects()
        {
            for (int i = 0; i < m_Board.Count; i++)
            {
                CardSlot slot = m_Board[i];
                if (!slot.IsPersistent || slot.RemainingTurns <= 0)
                {
                    continue;
                }

                FlipRunCardDefinition definition = GetCardDefinition(slot.Id);
                ApplyPassiveGain(slot.Id, definition.turnStartCoins, definition.turnStartScore, true);
            }
        }

        private void ResolvePersistentTurnEndEffects()
        {
            for (int i = 0; i < m_Board.Count; i++)
            {
                CardSlot slot = m_Board[i];
                if (!slot.IsPersistent || slot.RemainingTurns <= 0)
                {
                    continue;
                }

                FlipRunCardDefinition definition = GetCardDefinition(slot.Id);
                ApplyPassiveGain(slot.Id, definition.turnEndCoins, definition.turnEndScore, false);
            }
        }

        private void ApplyPassiveGain(string cardId, int gainCoins, int gainScore, bool isTurnStart)
        {
            if (gainCoins == 0 && gainScore == 0)
            {
                return;
            }

            m_TurnCoins += gainCoins;
            m_TurnScore += gainScore;
            AddLog(LF(isTurnStart ? "FlipRun.Log.PersistentTickStart" : "FlipRun.Log.PersistentTickEnd",
                L(GetCardDefinition(cardId).nameKey),
                gainCoins,
                gainScore));
        }
        private void FinalizeBattleTurn()
        {
            ResolvePersistentTurnEndEffects();

            if (m_PiggyCount > 0)
            {
                int bonusCoins = Mathf.FloorToInt(m_TurnCoins * GetCardDefinition("PiggyBank").percentValue * 0.01f * m_PiggyCount);
                if (bonusCoins > 0)
                {
                    m_TurnCoins += bonusCoins;
                    AddLog(LF("FlipRun.Log.PiggyBank", bonusCoins));
                }
            }

            if (m_LedgerCount > 0)
            {
                int bonusScore = Mathf.FloorToInt(m_TurnCoins * GetCardDefinition("Ledger").percentValue * 0.01f * m_LedgerCount);
                if (bonusScore > 0)
                {
                    m_TurnScore += bonusScore;
                    AddLog(LF("FlipRun.Log.Ledger", bonusScore));
                }
            }

            if (m_Relics.Contains(RelicId.CoinPouch))
            {
                int beforeCoins = m_TurnCoins;
                m_TurnCoins = Mathf.CeilToInt(m_TurnCoins * 1.15f);
                if (m_TurnCoins > beforeCoins)
                {
                    AddLog(LF("FlipRun.Log.CoinPouch", m_TurnCoins - beforeCoins));
                }
            }

            if (m_Relics.Contains(RelicId.ScoreSeal))
            {
                int beforeScore = m_TurnScore;
                m_TurnScore = Mathf.CeilToInt(m_TurnScore * 1.15f);
                if (m_TurnScore > beforeScore)
                {
                    AddLog(LF("FlipRun.Log.ScoreSeal", m_TurnScore - beforeScore));
                }
            }

            m_Coins += m_TurnCoins;
            m_Score += m_TurnScore;
            m_SmallRound++;
            AddLog(LF("FlipRun.Log.GainTurn", m_TurnCoins, m_TurnScore, m_Coins, m_Score));

            TickPersistentDurations();

            if (m_SmallRound >= GetSmallRoundsPerBigRound())
            {
                int target = GetTargetScore(Mathf.Clamp(m_BigRound, 0, GetTargetScoreCount() - 1));
                if (m_Score < target)
                {
                    AddLog(LF("FlipRun.Log.TargetFail", m_Score, target));
                    m_Phase = RunPhase.Ended;
                    m_IsWin = false;
                    AddLog(L("FlipRun.Log.RunLose"));
                    return;
                }

                AddLog(LF("FlipRun.Log.TargetPass", m_Score, target));
                if (m_BigRound >= GetTargetScoreCount() - 1)
                {
                    m_Phase = RunPhase.Ended;
                    m_IsWin = true;
                    AddLog(L("FlipRun.Log.RunWin"));
                    return;
                }

                m_AfterRewardPhase = RunPhase.Shop;
                ShowRewardOffers();
                return;
            }

            m_AfterRewardPhase = RunPhase.Battle;
            ShowRewardOffers();
        }

        private void TickPersistentDurations()
        {
            for (int i = 0; i < m_Board.Count; i++)
            {
                CardSlot slot = m_Board[i];
                if (!slot.IsPersistent || slot.RemainingTurns <= 0)
                {
                    continue;
                }

                slot.RemainingTurns--;
                if (slot.RemainingTurns <= 0)
                {
                    AddLog(LF("FlipRun.Log.PersistentExpire", L(GetCardDefinition(slot.Id).nameKey)));
                    slot.IsPersistent = false;
                    slot.Triggered = false;
                }

                m_Board[i] = slot;
            }
        }

        private void ShowRewardOffers()
        {
            BuildRewardOffers();
            if (m_RewardOffers.Count <= 0)
            {
                if (m_AfterRewardPhase == RunPhase.Shop)
                {
                    m_Phase = RunPhase.Shop;
                    AddLog(L("FlipRun.Log.EnterShop"));
                }
                else
                {
                    StartBattleBoard();
                }
                return;
            }

            m_Phase = RunPhase.Reward;
            if (m_RewardOffers.Count >= 3)
            {
                AddLog(LF("FlipRun.Log.RewardOffer",
                    L(GetCardDefinition(m_RewardOffers[0]).nameKey),
                    L(GetCardDefinition(m_RewardOffers[1]).nameKey),
                    L(GetCardDefinition(m_RewardOffers[2]).nameKey)));
            }
        }

        private bool TryChooseReward(int index)
        {
            if (m_Phase != RunPhase.Reward || index < 0 || index >= m_RewardOffers.Count)
            {
                return false;
            }

            string picked = m_RewardOffers[index];
            AddCard(picked, 1);
            AddLog(LF("FlipRun.Log.RewardClaim", L(GetCardDefinition(picked).nameKey)));
            m_RewardOffers.Clear();

            if (m_AfterRewardPhase == RunPhase.Shop)
            {
                m_Phase = RunPhase.Shop;
                AddLog(L("FlipRun.Log.EnterShop"));
            }
            else
            {
                StartBattleBoard();
            }

            return true;
        }

        private void BuildRewardOffers()
        {
            m_RewardOffers.Clear();
            FillOfferCardPool(definition => definition.canAppearInRewards);
            int targetCount = Mathf.Min(GetRewardOfferCount(), m_OfferCards.Count);
            while (m_RewardOffers.Count < targetCount && m_OfferCards.Count > 0)
            {
                FlipRunCardDefinition offer = PickWeighted(m_OfferCards, GetRewardWeight);
                if (offer == null)
                {
                    break;
                }

                string offerId = offer.id;
                if (!m_RewardOffers.Contains(offerId))
                {
                    m_RewardOffers.Add(offerId);
                }
                m_OfferCards.Remove(offer);
            }
        }

        private void ResolveShop()
        {
            bool bought = false;
            if (!m_Relics.Contains(RelicId.CoinPouch) && m_Coins >= GetRelicPrice())
            {
                m_Coins -= GetRelicPrice();
                m_Relics.Add(RelicId.CoinPouch);
                AddLog(LF("FlipRun.Log.ShopBuyRelic", L("FlipRun.Relic.CoinPouch"), GetRelicPrice()));
                bought = true;
            }
            else if (!m_Relics.Contains(RelicId.ScoreSeal) && m_Coins >= GetRelicPrice())
            {
                m_Coins -= GetRelicPrice();
                m_Relics.Add(RelicId.ScoreSeal);
                AddLog(LF("FlipRun.Log.ShopBuyRelic", L("FlipRun.Relic.ScoreSeal"), GetRelicPrice()));
                bought = true;
            }
            else if (!m_Relics.Contains(RelicId.FlipCharm) && m_Coins >= GetRelicPrice())
            {
                m_Coins -= GetRelicPrice();
                m_Relics.Add(RelicId.FlipCharm);
                AddLog(LF("FlipRun.Log.ShopBuyRelic", L("FlipRun.Relic.FlipCharm"), GetRelicPrice()));
                bought = true;
            }

            if (m_Coins >= GetCardPrice())
            {
                string shopCard = GetRandomCardId(definition => definition.canAppearInShop);
                if (HasCardDefinition(shopCard))
                {
                    AddCard(shopCard, 1);
                    m_Coins -= GetCardPrice();
                    bought = true;
                    AddLog(LF("FlipRun.Log.ShopBuyCardSimple", GetCardPrice()));
                }
            }

            if (!bought)
            {
                AddLog(L("FlipRun.Log.ShopSkip"));
            }

            m_BigRound++;
            m_SmallRound = 0;
            StartBattleBoard();
        }
        private void ActivatePersistentSlot(int slotIndex, int duration)
        {
            CardSlot slot = m_Board[slotIndex];
            slot.IsPersistent = true;
            slot.RemainingTurns = duration;
            slot.Triggered = true;
            m_Board[slotIndex] = slot;
        }

        private CardSlot CreateBoardSlot(string id)
        {
            return new CardSlot
            {
                Id = id,
                Triggered = false,
                IsPersistent = false,
                RemainingTurns = 0,
            };
        }

        private string BuildBoardDescription(CardSlot slot)
        {
            string description = L(GetCardDefinition(slot.Id).descKey);
            if (slot.IsPersistent && slot.RemainingTurns > 0)
            {
                return LF("FlipRun.CardDesc.PersistentActive", description, slot.RemainingTurns);
            }

            return description;
        }

        private int CountActivePersistentSlots()
        {
            int count = 0;
            for (int i = 0; i < m_Board.Count; i++)
            {
                if (m_Board[i].IsPersistent && m_Board[i].RemainingTurns > 0)
                {
                    count++;
                }
            }
            return count;
        }

        private string RollBoardCard()
        {
            int total = 0;
            foreach (KeyValuePair<string, int> kv in m_Deck)
            {
                if (HasCardDefinition(kv.Key))
                {
                    total += Mathf.Max(1, kv.Value);
                }
            }

            if (total <= 0)
            {
                return GetFallbackCardId();
            }

            int pick = m_Rng.Next(total);
            int acc = 0;
            foreach (KeyValuePair<string, int> kv in m_Deck)
            {
                if (!HasCardDefinition(kv.Key))
                {
                    continue;
                }

                acc += Mathf.Max(1, kv.Value);
                if (pick < acc)
                {
                    return kv.Key;
                }
            }

            return GetFallbackCardId();
        }

        private string GetRandomCardId(Predicate<FlipRunCardDefinition> filter = null)
        {
            FillOfferCardPool(filter);
            FlipRunCardDefinition picked = PickWeighted(m_OfferCards, GetShopWeight);
            return picked != null ? picked.id : GetFallbackCardId();
        }

        private bool AreAllCardsFlipped()
        {
            for (int i = 0; i < m_Board.Count; i++)
            {
                if (!m_Board[i].Triggered)
                {
                    return false;
                }
            }

            return true;
        }

        private int GetFlipCount() => Mathf.Clamp(m_Config.BaseFlips + m_NextTurnExtraFlip + (m_Relics.Contains(RelicId.FlipCharm) ? 1 : 0), 1, m_Config.MaxFlips);

        private int GetDeckCount()
        {
            int count = 0;
            foreach (KeyValuePair<string, int> kv in m_Deck)
            {
                count += kv.Value;
            }
            return count;
        }

        private void AddCard(string id, int add)
        {
            if (!HasCardDefinition(id) || add <= 0)
            {
                return;
            }

            if (m_Deck.TryGetValue(id, out int old))
            {
                m_Deck[id] = old + add;
            }
            else
            {
                m_Deck[id] = add;
            }
        }

        private void AddLog(string line)
        {
            m_Logs.Add(line);
            if (m_Logs.Count > 7)
            {
                m_Logs.RemoveAt(0);
            }
        }

        private void BuildCardDefinitions()
        {
            m_CardDefinitions.Clear();
            IReadOnlyList<FlipRunCardDefinition> cards = m_Config.Cards;
            if (cards == null)
            {
                return;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                FlipRunCardDefinition definition = cards[i];
                if (definition == null || string.IsNullOrWhiteSpace(definition.id))
                {
                    continue;
                }

                if (m_CardDefinitions.ContainsKey(definition.id))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.code))
                {
                    definition.code = definition.id;
                }
                if (string.IsNullOrWhiteSpace(definition.nameKey))
                {
                    definition.nameKey = "FlipRun.Card." + definition.id;
                }
                if (string.IsNullOrWhiteSpace(definition.descKey))
                {
                    definition.descKey = "FlipRun.CardDesc." + definition.id;
                }

                m_CardDefinitions.Add(definition.id, definition);
            }
        }

        private void InitializeStartingDeck()
        {
            IReadOnlyList<FlipRunDeckEntry> entries = m_Config.StartingDeck;
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    FlipRunDeckEntry entry = entries[i];
                    if (entry == null || entry.count <= 0 || string.IsNullOrWhiteSpace(entry.cardId))
                    {
                        continue;
                    }

                    AddCard(entry.cardId, entry.count);
                }
            }

            if (m_Deck.Count == 0)
            {
                AddCard(GetFallbackCardId(), 1);
            }
        }

        private FlipRunCardDefinition GetCardDefinition(string id)
        {
            if (m_CardDefinitions.TryGetValue(id, out FlipRunCardDefinition definition))
            {
                return definition;
            }

            return new FlipRunCardDefinition
            {
                id = string.IsNullOrWhiteSpace(id) ? "MissingCard" : id,
                code = string.IsNullOrWhiteSpace(id) ? "MIS" : id,
                nameKey = "FlipRun.Card." + id,
                descKey = "FlipRun.CardDesc." + id,
                effectType = FlipRunCardEffectType.FlatGain,
            };
        }

        private bool HasCardDefinition(string id)
        {
            return m_CardDefinitions.ContainsKey(id);
        }

        private void FillOfferCardPool(Predicate<FlipRunCardDefinition> filter)
        {
            m_OfferCards.Clear();
            foreach (KeyValuePair<string, FlipRunCardDefinition> pair in m_CardDefinitions)
            {
                if (filter == null || filter(pair.Value))
                {
                    m_OfferCards.Add(pair.Value);
                }
            }
        }

        private string GetFallbackCardId()
        {
            foreach (KeyValuePair<string, FlipRunCardDefinition> pair in m_CardDefinitions)
            {
                return pair.Key;
            }

            return "CopperCoin";
        }

        private FlipRunCardDefinition PickWeighted(List<FlipRunCardDefinition> pool, Func<FlipRunCardDefinition, int> getWeight)
        {
            if (pool == null || pool.Count == 0)
            {
                return null;
            }

            int totalWeight = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                totalWeight += Mathf.Max(1, getWeight != null ? getWeight(pool[i]) : pool[i].offerWeight);
            }

            int pick = m_Rng.Next(Mathf.Max(1, totalWeight));
            int acc = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                acc += Mathf.Max(1, getWeight != null ? getWeight(pool[i]) : pool[i].offerWeight);
                if (pick < acc)
                {
                    return pool[i];
                }
            }

            return pool[pool.Count - 1];
        }

        private int RollRange(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                return minInclusive;
            }

            return m_Rng.Next(minInclusive, maxInclusive + 1);
        }

        private int GetBoardColumns() => m_Config.BoardColumns;
        private int GetBoardRows() => m_Config.BoardRows;
        private int GetBoardSize() => m_Config.BoardSize;
        private int GetRewardOfferCount() => m_Config.RewardOfferCount;
        private int GetSmallRoundsPerBigRound() => m_Config.SmallRoundsPerBigRound;
        private int GetRelicPrice() => m_Config.RelicPrice;
        private int GetCardPrice() => m_Config.CardPrice;

        private int GetRewardWeight(FlipRunCardDefinition definition)
        {
            return definition != null && definition.rewardWeight > 0 ? definition.rewardWeight : definition != null ? definition.offerWeight : 1;
        }

        private int GetShopWeight(FlipRunCardDefinition definition)
        {
            return definition != null && definition.shopWeight > 0 ? definition.shopWeight : definition != null ? definition.offerWeight : 1;
        }

        private int GetTargetScore(int index)
        {
            IReadOnlyList<int> targets = m_Config.TargetScores;
            if (targets == null || targets.Count == 0)
            {
                return 0;
            }

            return targets[Mathf.Clamp(index, 0, targets.Count - 1)];
        }

        private int GetTargetScoreCount()
        {
            IReadOnlyList<int> targets = m_Config.TargetScores;
            return targets != null && targets.Count > 0 ? targets.Count : 1;
        }

        private static string GetRelicNameKey(RelicId id) => "FlipRun.Relic." + id;

        private static string L(string key) => GF.Localization.GetString(key);

        private static string LF(string key, params object[] args)
        {
            string format = GF.Localization.GetString(key);
            if (args == null || args.Length == 0)
            {
                return format;
            }

            try
            {
                return string.Format(format, args);
            }
            catch (Exception ex)
            {
                return $"<Error>{key},{format},{args},{ex}";
            }
        }
    }
}
