using System;
using System.Collections.Generic;
using System.Text;
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
    private bool m_IsShowingMessageBox;

    protected override async void OnEnter(IFsm<IProcedureManager> procedureOwner)
    {
        base.OnEnter(procedureOwner);
        m_Procedure = procedureOwner;
        m_IsTransitioning = false;
        m_IsShowingMessageBox = false;
        if (GF.Base.IsGamePaused) GF.Base.ResumeGame();
        GF.Event.Subscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        m_Run = new RunSession();
        m_GameUI = await GF.UI.OpenUIFormAwait(UIViews.GameUIForm) as GameUIForm;
        RefreshRunUI();
    }

    protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
        if (m_IsTransitioning || m_IsShowingMessageBox || m_Run == null) return;
        if (Input.GetMouseButtonDown(0) && !GF.UI.IsPointerOverUIObject(Input.mousePosition))
        {
            m_Run.Advance();
            RefreshRunUI();
            TryShowRunMessageBox();
            if (m_Run.IsEnded) OnGameOver(m_Run.IsWin);
        }
    }

    protected override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
    {
        if (GF.Base.IsGamePaused) GF.Base.ResumeGame();
        GF.Event.Unsubscribe(LoadDictionarySuccessEventArgs.EventId, OnLanguageReloaded);
        if (!isShutdown && m_GameUI != null) GF.UI.CloseUIForm(m_GameUI.UIForm.SerialId);
        m_GameUI = null;
        m_Run = null;
        base.OnLeave(procedureOwner, isShutdown);
    }

    public void Restart() => ChangeState<MenuProcedure>(m_Procedure);
    public void BackHome() => ChangeState<MenuProcedure>(m_Procedure);

    private void RefreshRunUI()
    {
        if (m_GameUI == null || m_Run == null) return;
        m_GameUI.SetRunStatus(m_Run.BuildStatusText());
        m_GameUI.SetBoardSlots(m_Run.BuildBoardViewData());
        var playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        playerDm.Coins = m_Run.Coins;
    }

    private void TryShowRunMessageBox()
    {
        if (m_IsShowingMessageBox || m_Run == null) return;
        if (!m_Run.TryDequeueNotice(out string title, out string content)) return;
        m_IsShowingMessageBox = true;
        if (!MessageBoxesSystem.Show(title, content, OnMessageBoxConfirmed, null, false))
        {
            m_IsShowingMessageBox = false;
            GF.UI.ShowToast($"{title}\n{content}");
            TryShowRunMessageBox();
        }
    }

    private void OnMessageBoxConfirmed()
    {
        m_IsShowingMessageBox = false;
        TryShowRunMessageBox();
    }

    private void OnGameOver(bool isWin)
    {
        if (m_IsTransitioning) return;
        m_IsTransitioning = true;
        m_Procedure.SetData<VarBoolean>("IsWin", isWin);
        ChangeState<GameOverProcedure>(m_Procedure);
    }

    private void OnLanguageReloaded(object sender, GameEventArgs e)
    {
        if (m_IsTransitioning || m_Run == null) return;
        RefreshRunUI();
    }

    private enum RunPhase { Battle, Shop, Ended }
    private enum CardId { CopperCoin, GoldBlock, Ticket, Rabbit, Carrot, Clover, PiggyBank, Ledger, Mirror, ChainBadge }
    private enum RelicId { CoinPouch, ScoreSeal, FlipCharm }

    private struct CardSlot { public CardId Id; public bool Triggered; }
    private struct RunNotice { public string Title; public string Content; }

    private sealed class RunSession
    {
        private const int BoardCols = 4;
        private const int BoardRows = 3;
        private const int BoardSize = BoardCols * BoardRows;
        private const int BaseFlips = 3;
        private const int SmallRoundsPerBigRound = 3;

        private readonly int[] m_TargetScores = { 120, 280, 520 };
        private readonly System.Random m_Rng = new System.Random(Environment.TickCount);
        private readonly List<CardSlot> m_Board = new List<CardSlot>(BoardSize);
        private readonly List<string> m_Logs = new List<string>(8);
        private readonly Queue<RunNotice> m_Notices = new Queue<RunNotice>(4);
        private readonly Dictionary<CardId, int> m_Deck = new Dictionary<CardId, int>();
        private readonly HashSet<RelicId> m_Relics = new HashSet<RelicId>();

        private RunPhase m_Phase;
        private int m_Coins;
        private int m_Score;
        private int m_BigRound;
        private int m_SmallRound;
        private int m_TotalTurn;
        private int m_NextTurnExtraFlip;
        private bool m_RabbitNoticed;
        private bool m_GoldNoticed;
        private bool m_IsWin;

        public bool IsEnded => m_Phase == RunPhase.Ended;
        public bool IsWin => m_IsWin;
        public int Coins => m_Coins;

        public RunSession()
        {
            m_Coins = 8;
            m_Phase = RunPhase.Battle;
            AddCard(CardId.CopperCoin, 4);
            AddCard(CardId.Ticket, 2);
            AddCard(CardId.Rabbit, 2);
            AddCard(CardId.Carrot, 1);
            AddCard(CardId.GoldBlock, 1);
            AddLog(L("FlipRun.Log.RunStart"));
            PrepareBoard();
        }

        public void Advance()
        {
            if (m_Phase == RunPhase.Ended) return;
            if (m_Phase == RunPhase.Shop) { ResolveShop(); return; }
            ResolveBattleTurn();
        }

        public bool TryDequeueNotice(out string title, out string content)
        {
            if (m_Notices.Count > 0) { var n = m_Notices.Dequeue(); title = n.Title; content = n.Content; return true; }
            title = string.Empty; content = string.Empty; return false;
        }

        public List<GameUIForm.CardSlotViewData> BuildBoardViewData()
        {
            var list = new List<GameUIForm.CardSlotViewData>(BoardSize);
            for (int i = 0; i < BoardSize; i++)
            {
                var slot = m_Board[i];
                list.Add(new GameUIForm.CardSlotViewData
                {
                    Code = GetCode(slot.Id),
                    Name = L(GetNameKey(slot.Id)),
                    Description = L(GetDescKey(slot.Id)),
                    IsFront = true,
                    Triggered = slot.Triggered,
                });
            }
            return list;
        }

        public string BuildStatusText()
        {
            var sb = new StringBuilder(1200);
            sb.AppendLine(L("FlipRun.Title"));
            sb.AppendLine(LF("FlipRun.Status.ScoreCoins", m_Score, m_TargetScores[Mathf.Clamp(m_BigRound, 0, m_TargetScores.Length - 1)], m_Coins));
            sb.AppendLine(LF("FlipRun.Status.BigRound", m_BigRound + 1, m_TargetScores.Length, m_SmallRound, SmallRoundsPerBigRound));
            sb.AppendLine(LF("FlipRun.Status.DeckRelics", GetDeckCount(), m_Relics.Count));
            sb.AppendLine(LF("FlipRun.Status.BoardConfig", BoardCols, BoardRows, GetFlipCount()));
            sb.AppendLine();
            sb.AppendLine(L("FlipRun.Label.Recent"));
            foreach (var log in m_Logs) sb.Append("- ").AppendLine(log);
            sb.AppendLine();
            sb.Append(m_Phase == RunPhase.Battle ? L("FlipRun.Action.Battle") : m_Phase == RunPhase.Shop ? L("FlipRun.Action.Shop") : L("FlipRun.Action.Ended"));
            return sb.ToString();
        }

        private void ResolveBattleTurn()
        {
            int flipCount = GetFlipCount();
            m_NextTurnExtraFlip = 0;
            m_TotalTurn++;
            m_SmallRound++;

            var idxes = new List<int>(BoardSize);
            for (int i = 0; i < BoardSize; i++) idxes.Add(i);
            var flipped = new List<CardId>(flipCount);
            for (int i = 0; i < flipCount; i++)
            {
                int pick = m_Rng.Next(idxes.Count);
                int idx = idxes[pick];
                idxes.RemoveAt(pick);
                var slot = m_Board[idx]; slot.Triggered = true; m_Board[idx] = slot;
                flipped.Add(slot.Id);
            }

            int turnCoins = 0;
            int turnScore = 0;
            int piggyCount = 0;
            int ledgerCount = 0;
            int ecoCount = 0;
            float comboMul = 1f;
            CardId prev = CardId.CopperCoin;
            bool hasPrev = false;
            int rabbitCount = 0;
            int goldCount = 0;
            int prevCoins = 0, prevScore = 0;

            AddLog(LF("FlipRun.Log.TurnReveal", m_TotalTurn, flipCount, BoardCols, BoardRows));
            AddLog(LF("FlipRun.Log.FlippedCards", BuildCardsText(flipped)));

            foreach (var card in flipped)
            {
                bool combo = hasPrev && card == prev;
                int comboBonus = combo ? Mathf.CeilToInt(2 * comboMul) : 0;

                int gainCoins = 0;
                int gainScore = 0;
                switch (card)
                {
                    case CardId.CopperCoin: gainCoins += 3 + comboBonus; gainScore += 1; ecoCount++; break;
                    case CardId.GoldBlock:
                        gainCoins += 8 + comboBonus * 2; gainScore += 2; ecoCount++; goldCount++;
                        AddLog(LF("FlipRun.Log.GoldBlock", gainCoins));
                        if (!m_GoldNoticed) { EnqueueNotice("FlipRun.Msg.GoldBlock.Title", "FlipRun.Msg.GoldBlock.Content", gainCoins); m_GoldNoticed = true; }
                        break;
                    case CardId.Ticket:
                        gainCoins += 1; gainScore += 8 + (goldCount > 0 ? 6 : 0); break;
                    case CardId.Rabbit:
                        rabbitCount++; gainScore += 2 + Mathf.Max(0, rabbitCount - 1) * 3 + comboBonus;
                        AddLog(LF("FlipRun.Log.Rabbit", gainScore));
                        if (!m_RabbitNoticed) { EnqueueNotice("FlipRun.Msg.Rabbit.Title", "FlipRun.Msg.Rabbit.Content", gainScore); m_RabbitNoticed = true; }
                        break;
                    case CardId.Carrot:
                        gainCoins += 1; gainScore += 3 + (rabbitCount > 0 ? 8 : 0); break;
                    case CardId.Clover:
                        m_NextTurnExtraFlip = Mathf.Clamp(m_NextTurnExtraFlip + (combo ? 2 : 1), 0, 2);
                        AddLog(LF("FlipRun.Log.Clover", combo ? 2 : 1));
                        gainScore += 1;
                        break;
                    case CardId.PiggyBank:
                        piggyCount++; gainScore += 2 + comboBonus; break;
                    case CardId.Ledger:
                        ledgerCount++; gainScore += 1 + comboBonus; break;
                    case CardId.Mirror:
                        gainCoins += Mathf.FloorToInt(prevCoins * 0.6f);
                        gainScore += Mathf.FloorToInt(prevScore * 0.6f);
                        AddLog(LF("FlipRun.Log.Mirror", gainCoins, gainScore));
                        break;
                    case CardId.ChainBadge:
                        comboMul += 0.5f; gainScore += 3; AddLog(LF("FlipRun.Log.ChainBadge", comboMul));
                        break;
                }

                turnCoins += gainCoins;
                turnScore += gainScore;
                prevCoins = gainCoins;
                prevScore = gainScore;
                prev = card;
                hasPrev = true;
            }

            if (piggyCount > 0)
            {
                int add = Mathf.FloorToInt(turnCoins * 0.2f * piggyCount);
                if (add > 0) { turnCoins += add; AddLog(LF("FlipRun.Log.PiggyBank", add)); }
            }
            if (ledgerCount > 0)
            {
                int add = Mathf.FloorToInt(turnCoins * 0.3f * ledgerCount);
                if (add > 0) { turnScore += add; AddLog(LF("FlipRun.Log.Ledger", add)); }
            }
            if (m_Relics.Contains(RelicId.CoinPouch)) { int b = turnCoins; turnCoins = Mathf.CeilToInt(turnCoins * 1.2f); if (turnCoins > b) AddLog(LF("FlipRun.Log.CoinPouch", turnCoins - b)); }
            if (m_Relics.Contains(RelicId.ScoreSeal)) { int b = turnScore; turnScore = Mathf.CeilToInt(turnScore * 1.2f); if (turnScore > b) AddLog(LF("FlipRun.Log.ScoreSeal", turnScore - b)); }

            m_Coins += turnCoins;
            m_Score += turnScore;
            AddLog(LF("FlipRun.Log.GainTurn", turnCoins, turnScore, m_Coins, m_Score));

            GiveOneRewardCard();

            if (m_SmallRound >= SmallRoundsPerBigRound)
            {
                int target = m_TargetScores[Mathf.Clamp(m_BigRound, 0, m_TargetScores.Length - 1)];
                if (m_Score < target)
                {
                    AddLog(LF("FlipRun.Log.TargetFail", m_Score, target));
                    m_Phase = RunPhase.Ended; m_IsWin = false; AddLog(L("FlipRun.Log.RunLose"));
                    return;
                }

                AddLog(LF("FlipRun.Log.TargetPass", m_Score, target));
                if (m_BigRound >= m_TargetScores.Length - 1)
                {
                    m_Phase = RunPhase.Ended; m_IsWin = true; AddLog(L("FlipRun.Log.RunWin"));
                    return;
                }

                m_Phase = RunPhase.Shop;
                AddLog(L("FlipRun.Log.EnterShop"));
                return;
            }

            PrepareBoard();
        }

        private void ResolveShop()
        {
            bool bought = false;
            if (!m_Relics.Contains(RelicId.CoinPouch) && m_Coins >= 18) { m_Coins -= 18; m_Relics.Add(RelicId.CoinPouch); AddLog(LF("FlipRun.Log.ShopBuyRelic", L("FlipRun.Relic.CoinPouch"), 18)); bought = true; }
            else if (!m_Relics.Contains(RelicId.ScoreSeal) && m_Coins >= 18) { m_Coins -= 18; m_Relics.Add(RelicId.ScoreSeal); AddLog(LF("FlipRun.Log.ShopBuyRelic", L("FlipRun.Relic.ScoreSeal"), 18)); bought = true; }
            else if (!m_Relics.Contains(RelicId.FlipCharm) && m_Coins >= 18) { m_Coins -= 18; m_Relics.Add(RelicId.FlipCharm); AddLog(LF("FlipRun.Log.ShopBuyRelic", L("FlipRun.Relic.FlipCharm"), 18)); bought = true; }
            if (m_Coins >= 14) { AddCard((CardId)m_Rng.Next(0, 10), 1); m_Coins -= 14; bought = true; AddLog(L("FlipRun.Log.ShopBuyCardSimple")); }
            if (!bought) AddLog(L("FlipRun.Log.ShopSkip"));

            m_BigRound++;
            m_SmallRound = 0;
            m_RabbitNoticed = false;
            m_GoldNoticed = false;
            m_Phase = RunPhase.Battle;
            PrepareBoard();
        }

        private void GiveOneRewardCard()
        {
            CardId[] offer = { (CardId)m_Rng.Next(0, 10), (CardId)m_Rng.Next(0, 10), (CardId)m_Rng.Next(0, 10) };
            CardId pick = offer[0];
            AddCard(pick, 1);
            AddLog(LF("FlipRun.Log.RewardPick", L(GetNameKey(pick)), L(GetNameKey(offer[0])), L(GetNameKey(offer[1])), L(GetNameKey(offer[2]))));
        }

        private void PrepareBoard()
        {
            m_Board.Clear();
            for (int i = 0; i < BoardSize; i++) m_Board.Add(new CardSlot { Id = RollBoardCard(), Triggered = false });
            AddLog(LF("FlipRun.Log.BoardGenerated", BoardCols, BoardRows));
        }

        private CardId RollBoardCard()
        {
            int total = 0;
            foreach (var kv in m_Deck) total += Mathf.Max(1, kv.Value);
            if (total <= 0) return CardId.CopperCoin;
            int pick = m_Rng.Next(total);
            int acc = 0;
            foreach (var kv in m_Deck) { acc += Mathf.Max(1, kv.Value); if (pick < acc) return kv.Key; }
            return CardId.CopperCoin;
        }

        private int GetFlipCount() => Mathf.Clamp(BaseFlips + m_NextTurnExtraFlip + (m_Relics.Contains(RelicId.FlipCharm) ? 1 : 0), 1, 6);
        private int GetDeckCount() { int c = 0; foreach (var kv in m_Deck) c += kv.Value; return c; }
        private void AddCard(CardId id, int add) { if (m_Deck.TryGetValue(id, out int old)) m_Deck[id] = old + add; else m_Deck[id] = add; }
        private void EnqueueNotice(string titleKey, string contentKey, params object[] args) => m_Notices.Enqueue(new RunNotice { Title = L(titleKey), Content = LF(contentKey, args) });

        private void AddLog(string line)
        {
            m_Logs.Add(line);
            if (m_Logs.Count > 7) m_Logs.RemoveAt(0);
        }

        private static string BuildCardsText(List<CardId> cards)
        {
            if (cards == null || cards.Count == 0) return "-";
            var sb = new StringBuilder(64);
            for (int i = 0; i < cards.Count; i++)
            {
                if (i > 0) sb.Append(" / ");
                sb.Append(L(GetNameKey(cards[i])));
            }
            return sb.ToString();
        }

        private static string GetCode(CardId id)
        {
            switch (id)
            {
                case CardId.CopperCoin: return "COP";
                case CardId.GoldBlock: return "GLD";
                case CardId.Ticket: return "TIK";
                case CardId.Rabbit: return "RAB";
                case CardId.Carrot: return "CAR";
                case CardId.Clover: return "CLV";
                case CardId.PiggyBank: return "PIG";
                case CardId.Ledger: return "LED";
                case CardId.Mirror: return "MIR";
                default: return "CHN";
            }
        }

        private static string GetNameKey(CardId id) => "FlipRun.Card." + id;
        private static string GetDescKey(CardId id) => "FlipRun.CardDesc." + id;

        private static string L(string key) => GF.Localization.GetString(key);
        private static string LF(string key, params object[] args)
        {
            string format = GF.Localization.GetString(key);
            if (args == null || args.Length == 0) return format;
            try { return string.Format(format, args); }
            catch (Exception ex) { return $"<Error>{key},{format},{args},{ex}"; }
        }
    }
}
