using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;
using WeirdLand.Core;
using WeirdLand.Core.Data;
using WeirdLand.Core.Match;

namespace WeirdLand.Unity
{
    /// <summary>Keys of data/ui_tr.json used by the grid match (Unity side; core UiKeys stays untouched).</summary>
    public static class GridUiKeys
    {
        public const string Advance = "grid.advance";
        public const string Pass = "grid.pass";
        public const string Field = "grid.field";
        public const string Move = "grid.move";
        public const string ShotChance = "grid.shotChance";
        public const string PossessionLost = "grid.possessionLost";
        public const string Entry = "grid.entry";
        public const string Error = "grid.error";
        public const string MenuTitle = "menu.title";
        public const string MenuDuel = "menu.duel";
    }

    /// <summary>
    /// The grid match screen. Sends commands to the core GridMatch, plays the returned events in order on the
    /// board and locks input meanwhile. When the opponent has the ball it resolves the opponent's move by itself.
    /// A DuelTriggered event hands the rest of the event list (the duel's opening) to an embedded DuelScreen;
    /// once that duel has an outcome, CompleteDuel's events continue on the board. Phase is the source of truth.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class GridMatchController : MonoBehaviour
    {
        private enum Mode { None, Advance, Pass }

        private readonly TweenSet _tw = new TweenSet();
        private VisualCatalog _cat;
        private LoadedData _data;
        private TeamDef _user, _opp;
        private uint _seed;
        private CancellationTokenSource _cts;
        private RectTransform _content;
        private TMP_Text _moveLabel, _scoreLabel, _toast;
        private CanvasGroup _toastGroup;
        private ResultOverlay _result;
        private Mode _mode;
        private int _shownUser, _shownOpp;

        public GridMatch Match { get; private set; }
        public GridBoardView Board { get; private set; }
        public GridActionPanel Panel { get; private set; }
        public DuelScreen DuelView { get; private set; }
        public bool IsBusy { get; private set; }
        public bool DuelOpen => DuelView != null && DuelView.gameObject.activeSelf;
        public bool PanelVisible => Panel != null && Panel.Visible;
        public string MoveText => _moveLabel != null ? _moveLabel.text : "";
        public string ScoreText => _scoreLabel != null ? _scoreLabel.text : "";
        public bool ResultShown => _result != null && _result.IsShown;
        public event Action BackRequested;

        /// <summary>True once a failure stopped the match and the error route (result overlay) is shown.</summary>
        public bool Failed { get; private set; }

        /// <summary>Raised after each MatchMoveAdvanced is shown, once the board's stun look is refreshed.</summary>
        public event Action<int> MoveShown;

        /// <summary>Test hook: when set, called at the start of each duel step; an exception it throws is a failure.</summary>
        public Action DuelStepHook { get; set; }

        public void Open(LoadedData data, string userTeam, string oppTeam, uint seed)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            CancelWork();
            _cat = VisualCatalog.Load();
            _data = data;
            _user = data.Game.Team(userTeam);
            _opp = data.Game.Team(oppTeam);
            _seed = seed;
            gameObject.SetActive(true);
            Match = new GridMatch(data.Game, _user, _opp, seed);
            Build();
            _cts = new CancellationTokenSource();
            Run(Match.Kickoff, _cts.Token).Forget();
        }

        // ---- input: buttons and tests use the same hooks ----

        public void ClickAdvance()
        {
            if (!CanAct()) return;
            ClearMode();
            _mode = Mode.Advance;
            Board.Highlight(Match.ReachableCells());
        }

        public void ClickPassMode()
        {
            if (!CanAct()) return;
            ClearMode();
            _mode = Mode.Pass;
            Board.ShowPassLines(Match);
        }

        public void ClickCell(Cell c)
        {
            if (!CanAct() || _mode != Mode.Advance || !Board.IsLit(c)) return;
            ClearMode();
            Run(() => Match.Advance(c), _cts.Token).Forget();
        }

        public void ClickPlayer(string id)
        {
            if (!CanAct() || _mode != Mode.Pass) return;
            bool target = false;
            foreach (var p in Match.PassTargets()) if (p.Def.Id == id) target = true;
            if (!target) return;
            ClearMode();
            Run(() => Match.Pass(id), _cts.Token).Forget();
        }

        /// <summary>Saha: hides or shows the action panel. Costs no move.</summary>
        public void ClickField()
        {
            if (Panel == null) return;
            bool show = !Panel.Visible;
            if (!show) ClearMode();
            Panel.SetVisible(show);
        }

        private bool CanAct() => Match != null && !Failed && _cts != null && !IsBusy && Panel.Visible
            && Match.Phase == GridPhase.AwaitingAction && Match.UserHasBall;

        private void ClearMode()
        {
            _mode = Mode.None;
            Board.ClearHighlights();
            Board.ClearLines();
        }

        // ---- command loop ----

        private async UniTaskVoid Run(Func<IReadOnlyList<GameEvent>> command, CancellationToken ct)
        {
            IsBusy = true;
            RefreshButtons();
            try
            {
                await Play(command(), ct);
                while (Match.Phase == GridPhase.AwaitingAction && !Match.UserHasBall)
                {
                    await Delay(T(_cat.Timings.Hit), ct);
                    await Play(Match.ResolveOpponentMove(), ct);
                }
                SyncAll();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Fail();
            }
            finally
            {
                if (!ct.IsCancellationRequested)
                {
                    IsBusy = false;
                    RefreshButtons();
                }
            }
        }

        private async UniTask Play(IReadOnlyList<GameEvent> events, CancellationToken ct)
        {
            var t = _cat.Timings;
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                switch (e)
                {
                    case PlayerMoved _:
                        // Consecutive moves (the carrier, then everyone the movement step shifts) play together.
                        var batch = new List<UniTask>();
                        var seen = new List<string>();
                        while (i < events.Count && events[i] is PlayerMoved pm && !seen.Contains(pm.PlayerId))
                        {
                            seen.Add(pm.PlayerId);
                            batch.Add(TweenAwait.Wait(Board.MoveToken(pm.PlayerId, new Cell(pm.ToX, pm.ToY), T(t.Lunge)), ct));
                            i++;
                        }
                        i--;
                        await UniTask.WhenAll(batch);
                        break;
                    case GridKickoff k:
                        var all = new List<UniTask>();
                        foreach (var p in Match.Players)
                        {
                            Board.SetStunned(p.Def.Id, false);
                            all.Add(TweenAwait.Wait(Board.MoveToken(p.Def.Id, GridGeometry.Home(p.Def.Slot, p.IsUser), T(t.Lunge)), ct));
                        }
                        await UniTask.WhenAll(all);
                        Board.PlaceBall(k.CarrierId);
                        break;
                    case BallPassed bp:
                        await TweenAwait.Wait(Board.FlyBall(bp.ToId, T(t.Pass)), ct);
                        break;
                    case PassIntercepted pi:
                        await TweenAwait.Wait(Board.FlyBall(pi.InterceptorId, T(t.Pass)), ct);
                        break;
                    case PossessionChanged pc:
                        if (Board.BallHolder != pc.CarrierId) await TweenAwait.Wait(Board.FlyBall(pc.CarrierId, T(t.Hit)), ct);
                        if (!pc.UserHasBall) await Toast(_data.Text.Get(GridUiKeys.PossessionLost), ct);
                        break;
                    case PlayerStunned ps:
                        Board.SetStunned(ps.PlayerId, true);
                        await TweenAwait.Wait(Board.Punch(ps.PlayerId, T(t.Hit)), ct);
                        break;
                    case ShotChance sc:
                        if (sc.ByUser) _shownUser++; else _shownOpp++;
                        SetScore(_shownUser, _shownOpp);
                        _tw.Punch(_scoreLabel.transform, 0.3f, T(t.Hit));
                        await Toast(_data.Text.Get(GridUiKeys.ShotChance), ct);
                        break;
                    case MatchMoveAdvanced mm:
                        SetMove(mm.Move);
                        // The match state equals the end of this list here; recovered players lose the faded look.
                        Board.SyncStuns(Match);
                        MoveShown?.Invoke(mm.Move);
                        break;
                    case MatchEnded me:
                        SetScore(me.UserScore, me.OppScore);
                        ShowResult(me.UserScore, me.OppScore);
                        break;
                    case DuelTriggered _:
                        var opening = DuelOpening(events);
                        LastDuelOpening = opening;
                        await RunDuel(opening, ct);
                        return; // the rest of the list belonged to the duel
                }
            }
        }

        /// <summary>
        /// The opening events of the duel a list opens: everything after its DuelTriggered. Core guarantees a
        /// list holds at most one DuelTriggered and nothing but the duel's Begin() after it (pinned by a core test),
        /// so Play and this method agree on the same index.
        /// </summary>
        public static IReadOnlyList<GameEvent> DuelOpening(IReadOnlyList<GameEvent> events)
        {
            int at = -1;
            for (int i = 0; i < events.Count; i++) if (events[i] is DuelTriggered) { at = i; break; }
            var r = new List<GameEvent>();
            if (at < 0) return r;
            for (int j = at + 1; j < events.Count; j++) r.Add(events[j]);
            return r;
        }

        /// <summary>The opening slice handed to the duel screen most recently (tests check it).</summary>
        public IReadOnlyList<GameEvent> LastDuelOpening { get; private set; }

        private async UniTask RunDuel(IReadOnlyList<GameEvent> opening, CancellationToken ct)
        {
            var duel = Match.CurrentDuel;
            DuelView.OpenBegun(duel, _data, _seed, opening);
            DuelView.transform.SetAsLastSibling();
            DuelStepHook?.Invoke();
            while (!duel.Outcome.HasValue || DuelView.Controller.IsBusy) await UniTask.Yield(ct);
            await Delay(T(_cat.Timings.Banner) * 1.5f, ct);
            DuelView.gameObject.SetActive(false);
            await Play(Match.CompleteDuel(), ct);
        }

        // ---- view ----

        private void Build()
        {
            if (_content != null) Destroy(_content.gameObject);
            _content = UiFactory.Stretch(UiFactory.Rect("GridContent", transform));
            var root = (RectTransform)transform;
            UiFactory.Stretch(root);

            Board = new GridBoardView(_tw);
            Board.Build(_content, ClickCell, ClickPlayer, Match, _cat, _data.Text);
            Panel = new GridActionPanel();
            Panel.Build(_content, _cat, _data.Text, ClickAdvance, ClickPassMode, ClickField);

            _moveLabel = UiFactory.Label("Move", _content, 40, Color.white);
            UiFactory.Place(_moveLabel.rectTransform, new Vector2(0, 1), new Vector2(40, -20), new Vector2(420, 60));
            _moveLabel.alignment = TextAlignmentOptions.Left;
            _scoreLabel = UiFactory.Label("Score", _content, 48, Color.white);
            _scoreLabel.fontStyle = FontStyles.Bold;
            UiFactory.Place(_scoreLabel.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(900, 64));
            _toast = UiFactory.Label("Toast", _content, 72, new Color32(0xFF, 0xE0, 0x8A, 0xFF));
            _toast.fontStyle = FontStyles.Bold;
            UiFactory.Center(_toast.rectTransform, new Vector2(0, 40), new Vector2(1000, 110));
            _toastGroup = UiFactory.Group(_toast, 0);

            var duelGo = new GameObject("DuelScreen", typeof(RectTransform)) { layer = UiFactory.UiLayer };
            duelGo.transform.SetParent(_content, false);
            var bg = duelGo.AddComponent<UnityEngine.UI.Image>();
            bg.color = _cat.Background;
            UiFactory.Stretch((RectTransform)duelGo.transform);
            DuelView = duelGo.AddComponent<DuelScreen>();
            duelGo.SetActive(false);

            _result = ResultOverlay.Create(_content, _cat, _data.Text,
                () => Open(_data, _user.Id, _opp.Id, _seed),
                () => Open(_data, _user.Id, _opp.Id, (uint)Environment.TickCount),
                () => BackRequested?.Invoke());

            _mode = Mode.None;
            Failed = false;
            _shownUser = _shownOpp = 0;
            SetScore(0, 0);
            SetMove(0);
            Board.Sync(Match);
        }

        private void SyncAll()
        {
            Board.Sync(Match);
            _shownUser = Match.UserScore;
            _shownOpp = Match.OppScore;
            SetScore(_shownUser, _shownOpp);
            SetMove(Match.Move);
            if (Match.Phase == GridPhase.Ended && !_result.IsShown) ShowResult(Match.UserScore, Match.OppScore);
        }

        // Unrecoverable failure: close the duel, stop input and show the result overlay as the way out
        // (again / new seed / back to menu).
        private void Fail()
        {
            Failed = true;
            try
            {
                if (DuelView != null) DuelView.gameObject.SetActive(false);
                ClearMode();
                Board.Sync(Match);
                if (Panel != null) Panel.SetInteractable(false);
            }
            catch (Exception ex2) { Debug.LogException(ex2); }
            _result.Show(_data.Text.Get(GridUiKeys.Error),
                _user.Name + "  " + Match.UserScore + " - " + Match.OppScore + "  " + _opp.Name);
        }

        private void RefreshButtons()
        {
            if (Panel == null || Match == null) return;
            Panel.SetInteractable(!Failed && !IsBusy && Match.Phase == GridPhase.AwaitingAction && Match.UserHasBall);
        }

        private void SetMove(int move) => _moveLabel.text = _data.Text.Format(GridUiKeys.Move, move, MatchRules.GridMatchMoves);

        private void SetScore(int u, int o) => _scoreLabel.text = _user.Name + "  " + u + " - " + o + "  " + _opp.Name;

        private void ShowResult(int u, int o)
        {
            ClearMode();
            _result.Show(_user.Name + "  " + u + " - " + o + "  " + _opp.Name, null);
        }

        private async UniTask Toast(string text, CancellationToken ct)
        {
            _toast.text = text;
            _toast.transform.SetAsLastSibling();
            await TweenAwait.Wait(_tw.Flash(_toastGroup, T(_cat.Timings.Banner) * 1.5f), ct);
        }

        private UniTask Delay(float seconds, CancellationToken ct)
            => seconds > 0 ? TweenAwait.Wait(_tw.Add(Tween.Delay(seconds)), ct) : UniTask.CompletedTask;

        private float T(float d) => _cat.Timings.Of(d);

        // ---- lifetime ----

        private void CancelWork()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }
            _tw.StopAll();
            IsBusy = false;
        }

        private void OnDisable() => CancelWork();

        private void OnDestroy() => CancelWork();
    }
}
