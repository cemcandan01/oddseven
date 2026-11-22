using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using TMPro;
using UnityEngine;
using WeirdLand.Core;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Unity
{
    /// <summary>
    /// The duel screen: animates each core event with the event's own values and rebuilds everything from
    /// state in <see cref="Sync"/>. Clicks go through the DuelController, which ignores them while a command
    /// plays. Widgets come from <see cref="DuelLayout"/>, hand cards from <see cref="HandView"/>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class DuelScreen : MonoBehaviour, IDuelView
    {
        private static readonly Color DamageInk = new Color32(0xFF, 0x5A, 0x4A, 0xFF);
        private static readonly Color GoodInk = new Color32(0x8C, 0xF0, 0x8C, 0xFF);
        private static readonly Color BadInk = new Color32(0xFF, 0x8A, 0x7A, 0xFF);

        private readonly TweenSet _tw = new TweenSet();
        private readonly Dictionary<StatusId, int> _userStatus = new Dictionary<StatusId, int>();
        private readonly Dictionary<StatusId, int> _enemyStatus = new Dictionary<StatusId, int>();

        private VisualCatalog _cat;
        private LoadedData _data;
        private Duel _duel;
        private uint _seed;
        private CancellationTokenSource _cts;
        private DuelLayout _ui;
        private HandView _hand;
        private bool _discardHand, _fresh;
        private float _userHp, _enemyHp;
        private int _userBlock, _enemyBlock;

        public DuelController Controller { get; private set; }
        public event Action BackRequested, RetrySameSeed, RetryNewSeed;

        public bool ResultShown => _ui != null && _ui.Result.IsShown;
        public string ResultText => ResultShown ? _ui.Result.Text : "";
        public bool PassButtonVisible => _ui != null && _ui.Pass.gameObject.activeSelf;
        public string PassButtonText => _ui != null ? UiFactory.GetLabel(_ui.Pass) : "";
        public bool PassPickerOpen => _ui != null && _ui.Picker.IsOpen;
        public float UserBarValue => _userHp;
        public float EnemyBarValue => _enemyHp;
        public int HandCardCount => _hand != null ? _hand.Count : 0;
        public string IntentText => _ui != null && _ui.Intent.gameObject.activeSelf ? _ui.Intent.Text : "";

        public void Open(Duel duel, LoadedData data, uint seed)
        {
            if (duel == null) throw new ArgumentNullException(nameof(duel));
            if (data == null) throw new ArgumentNullException(nameof(data));
            CancelWork();
            _cat = VisualCatalog.Load();
            _data = data;
            _duel = duel;
            _seed = seed;
            gameObject.SetActive(true);
            if (_ui == null)
            {
                _ui = DuelLayout.Build((RectTransform)transform, _cat, data.Text, this);
                _hand = new HandView(_ui.Hand, _ui.Fx, _ui.DeckPoint, _cat, _tw, OnCardClicked);
            }
            _hand.Text = data.Text;
            ResetView();
            _cts = new CancellationTokenSource();
            Controller = new DuelController(duel, this);
            Controller.Begin(_cts.Token).Forget();
        }

        // ---- input: buttons and tests use the same hooks ----

        public void ClickCard(int handIndex)
        {
            if (!Ready() || handIndex < 0 || handIndex >= _hand.Count) return;
            PlayCard(_hand[handIndex], handIndex, _cts.Token).Forget();
        }

        public void ClickEndTurn()
        {
            if (!Ready()) return;
            EndTurn(_cts.Token).Forget();
        }

        public void ClickPass()
        {
            if (!Ready() || !_duel.CanPass) return;
            _ui.Picker.Open(_duel.UserPassTargets, _data.Text, ClickPassTarget);
        }

        public void ClickPassTarget(string id)
        {
            if (Controller == null || _cts == null || Controller.IsBusy || !PassPickerOpen) return;
            _ui.Picker.Close();
            Controller.Pass(id, _cts.Token).Forget();
        }

        public void ClickCancelPass()
        {
            if (_ui != null) _ui.Picker.Close();
        }

        public void ClickBack() => BackRequested?.Invoke();
        public void ClickAgain() => RetrySameSeed?.Invoke();
        public void ClickNewSeed() => RetryNewSeed?.Invoke();

        private bool Ready() => Controller != null && _cts != null && !Controller.IsBusy && !_duel.Outcome.HasValue && !PassPickerOpen;

        private void OnCardClicked(CardView v)
        {
            int i = _hand.IndexOf(v);
            if (i >= 0) ClickCard(i);
        }

        private async UniTaskVoid PlayCard(CardView v, int index, CancellationToken ct)
        {
            _hand.Playing = v;
            _hand.Lift(v);
            bool ok = await Controller.PlayCard(index, ct);
            if (ok || ct.IsCancellationRequested || v == null || _hand.Playing != v) return;
            _hand.Playing = null;
            _hand.Return(v);
        }

        private async UniTaskVoid EndTurn(CancellationToken ct)
        {
            _discardHand = true; // consumed by the first animated event of this command
            await Controller.EndTurn(ct);
            if (!ct.IsCancellationRequested) _discardHand = false;
        }

        // ---- IDuelView ----

        public async UniTask Animate(GameEvent e, Duel duel, CancellationToken ct)
        {
            if (_fresh)
            {
                _fresh = false;
                ShowStart(duel);
            }
            if (_discardHand)
            {
                _discardHand = false;
                await _hand.Discard(ct);
            }
            var t = _cat.Timings;
            switch (e)
            {
                case TurnStarted ts:
                    _ui.Turn.text = TurnLine(ts.Turn, duel.MaxTurns);
                    _ui.Banner.text = _ui.Turn.text;
                    await TweenAwait.Wait(_tw.Flash(_ui.BannerGroup, T(t.Banner)), ct);
                    break;
                case EnergyChanged en when en.User:
                    _ui.Energy.text = EnergyLine(en.Energy);
                    _tw.Punch(_ui.Energy.transform, 0.3f, T(t.Hit));
                    break;
                case CardsDrawn cd when cd.User:
                    SyncCounts(duel);
                    await _hand.Draw(duel, cd.Count, ct);
                    break;
                case IntentRevealed _:
                    ShowIntent(duel);
                    if (_ui.Intent.gameObject.activeSelf) await TweenAwait.Wait(_tw.Punch(_ui.Intent.transform, 0.15f, T(t.Hit)), ct);
                    break;
                case CardPlayed cp:
                    SyncCounts(duel);
                    if (cp.ByUser) await _hand.FlyPlayed(ct);
                    else await EnemyCard(duel, cp.CardId, ct);
                    break;
                case DamageDealt d:
                    await Damage(duel, d, ct);
                    break;
                case BlockGained b:
                    SetShownBlock(b.User, ShownBlock(b.User) + b.Amount);
                    var badge = _ui.Fighter(b.User).BlockBadge;
                    if (badge.gameObject.activeInHierarchy) await TweenAwait.Wait(_tw.Punch(badge, 0.35f, T(t.Hit)), ct);
                    break;
                case BlockReset br:
                    SetShownBlock(br.User, 0);
                    break;
                case StatusApplied sa:
                    SetShownStatus(sa.ToUser, sa.Status, sa.Stacks);
                    break;
                case StatusChanged sc:
                    SetShownStatus(sc.ToUser, sc.Status, sc.Stacks);
                    break;
                case PassAttempted p:
                    await PassAnimation(duel, p, ct);
                    break;
                case DuelEnded end:
                    ShowResult(duel, end.Outcome);
                    await TweenAwait.Wait(_tw.Fade(_ui.Result.Group, 0, 1, T(t.Banner) * 0.5f), ct);
                    break;
            }
        }

        public void Sync(Duel duel)
        {
            // Runs while the controller is still busy: nothing here may depend on IsBusy.
            _fresh = false;
            _discardHand = false;
            SyncSide(duel, true);
            SyncSide(duel, false);
            _ui.Turn.text = TurnLine(Math.Max(1, duel.Turn), duel.MaxTurns);
            _ui.Energy.text = EnergyLine(duel.User.Energy);
            SyncCounts(duel);
            _hand.Sync(duel);

            bool over = duel.Outcome.HasValue;
            _ui.NoCards.gameObject.SetActive(!over && duel.User.Hand.Count == 0);
            _ui.EndTurn.interactable = !over;
            _ui.Pass.gameObject.SetActive(duel.CanPass);
            if (duel.CanPass) UiFactory.SetLabel(_ui.Pass, PassLabel(duel));
            else _ui.Picker.Close();
            ShowIntent(duel);
            if (over)
            {
                if (!_ui.Result.IsShown) ShowResult(duel, duel.Outcome.Value);
                _ui.Result.Group.alpha = 1;
            }
            else _ui.Result.Hide();
        }

        // ---- animations ----

        private async UniTask EnemyCard(Duel duel, string cardId, CancellationToken ct)
        {
            var t = _cat.Timings;
            _ui.EnemyCard.text = _data.Text.Format(UiKeys.DuelEnemyPlayed, duel.Enemy.Player.Name, _data.Game.Card(cardId).Name);
            _ui.EnemyCardGroup.alpha = 1;
            await TweenAwait.Wait(_tw.Lunge((RectTransform)_ui.Enemy.transform, new Vector2(-90, 0), T(t.Lunge)), ct);
            if (T(t.EnemyCardHold) > 0) await TweenAwait.Wait(_tw.Add(Tween.Delay(T(t.EnemyCardHold))), ct);
            _tw.Fade(_ui.EnemyCardGroup, 1, 0, T(t.Hit));
        }

        private async UniTask Damage(Duel duel, DamageDealt d, CancellationToken ct)
        {
            var t = _cat.Timings;
            bool toUser = d.ToUser;
            var f = _ui.Fighter(toUser);
            int max = (toUser ? duel.User : duel.Enemy).MaxHp;
            if (d.Blocked > 0) SetShownBlock(toUser, Math.Max(0, ShownBlock(toUser) - d.Blocked));
            if (d.Amount > 0) FloatNumber(f, "-" + d.Amount.ToString(CultureInfo.InvariantCulture), ct).Forget();
            var shake = d.Amount > 0 ? _tw.Shake(f.Body, 14, T(t.Hit)) : default;
            var bar = _tw.Value(f, toUser ? _userHp : _enemyHp, d.HpAfter, T(t.Bar), (_, v) => SetShownHp(toUser, v, max));
            await UniTask.WhenAll(TweenAwait.Wait(shake, ct), TweenAwait.Wait(bar, ct));
            SetShownHp(toUser, d.HpAfter, max);
        }

        private async UniTaskVoid FloatNumber(FighterView f, string text, CancellationToken ct)
        {
            var label = UiFactory.Label("FloatNumber", _ui.Fx, 64, DamageInk);
            label.fontStyle = FontStyles.Bold;
            label.text = text;
            var r = label.rectTransform;
            r.sizeDelta = new Vector2(240, 90);
            r.position = f.NumberAnchor.position;
            var g = UiFactory.Group(label, 1);
            float d = T(_cat.Timings.FloatNumber);
            try
            {
                await UniTask.WhenAll(
                    TweenAwait.Wait(_tw.Move(r, r.anchoredPosition + new Vector2(0, 110), d, 0, Ease.OutQuad), ct),
                    TweenAwait.Wait(_tw.Fade(g, 1, 0, d * 0.5f, d * 0.5f), ct));
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (label != null) Destroy(label.gameObject);
            }
        }

        private async UniTask PassAnimation(Duel duel, PassAttempted p, CancellationToken ct)
        {
            var passer = _ui.Fighter(p.ByUser);
            var outcome = p.Success ? DuelOutcome.PassSucceeded : DuelOutcome.PassFailed;
            _ui.PassLine.text = _data.Text.Format(UiKeys.IntentPass, TargetName(duel, p.TargetId), UiText.Percent(p.Chance))
                + "\n" + _data.Text.Get(UiKeys.Result(duel.Kind, outcome, duel.UserIsAttacker));
            _ui.PassLine.color = p.Success == p.ByUser ? GoodInk : BadInk;
            _ui.PassGroup.alpha = 1;

            var src = passer.Ball;
            var ball = (RectTransform)Instantiate(src.gameObject, _ui.Fx, true).transform;
            ball.gameObject.SetActive(true);
            var world = src.position;
            ball.anchorMin = ball.anchorMax = ball.pivot = new Vector2(0.5f, 0.5f);
            ball.position = world;
            passer.SetBall(false);
            var target = new Vector2(p.ByUser ? -820 : 820, 400);
            try
            {
                await TweenAwait.Wait(_tw.Move(ball, target, T(_cat.Timings.Pass), 0, Ease.OutQuad), ct);
            }
            finally
            {
                if (ball != null) Destroy(ball.gameObject);
            }
        }

        // ---- state helpers ----

        private void ShowStart(Duel duel)
        {
            _userStatus.Clear();
            _enemyStatus.Clear();
            foreach (var user in new[] { true, false })
            {
                var c = user ? duel.User : duel.Enemy;
                SetShownHp(user, c.Hp, c.MaxHp);
                SetShownBlock(user, 0);
                _ui.Fighter(user).SetStatuses(user ? _userStatus : _enemyStatus, _data.Text);
                _ui.Fighter(user).SetBall(HasBall(duel, user));
            }
        }

        private void SyncSide(Duel duel, bool user)
        {
            var c = user ? duel.User : duel.Enemy;
            var shown = user ? _userStatus : _enemyStatus;
            shown.Clear();
            foreach (var kv in c.Statuses) shown[kv.Key] = kv.Value; // FighterView orders chips by StatusId
            _ui.Fighter(user).SetStatuses(shown, _data.Text);
            SetShownHp(user, c.Hp, c.MaxHp);
            SetShownBlock(user, c.Block);
            _ui.Fighter(user).SetBall(HasBall(duel, user));
        }

        private void ShowIntent(Duel duel)
        {
            if (duel.Outcome.HasValue) _ui.Intent.Hide(); // a finished duel's intent may be stale
            else _ui.Intent.Show(DuelPreview.Intent(duel), _data.Text);
        }

        private void ShowResult(Duel duel, DuelOutcome outcome)
        {
            var attacker = duel.UserIsAttacker ? duel.User : duel.Enemy;
            bool timeout = outcome == DuelOutcome.DefenderWon && attacker.Hp > 0;
            _ui.Picker.Close();
            _ui.Intent.Hide();
            _ui.NoCards.gameObject.SetActive(false);
            _ui.Result.Show(_data.Text.Get(UiKeys.Result(duel.Kind, outcome, duel.UserIsAttacker)),
                timeout ? _data.Text.Get(UiKeys.ResultTimeout) : null);
        }

        private void SetShownHp(bool user, float hp, int max)
        {
            if (user) _userHp = hp;
            else _enemyHp = hp;
            _ui.Fighter(user).SetHp(Mathf.RoundToInt(hp), max);
        }

        private int ShownBlock(bool user) => user ? _userBlock : _enemyBlock;

        private void SetShownBlock(bool user, int block)
        {
            if (user) _userBlock = block;
            else _enemyBlock = block;
            _ui.Fighter(user).SetBlock(block);
        }

        private void SetShownStatus(bool user, StatusId id, int stacks)
        {
            var shown = user ? _userStatus : _enemyStatus;
            if (stacks > 0) shown[id] = stacks;
            else shown.Remove(id);
            _ui.Fighter(user).SetStatuses(shown, _data.Text);
        }

        private void SyncCounts(Duel duel)
        {
            _ui.Draw.text = _data.Text.Format(UiKeys.DuelDrawPile, duel.User.DrawPile.Count);
            _ui.Discard.text = _data.Text.Format(UiKeys.DuelDiscardPile, duel.User.Discard.Count);
        }

        private static bool HasBall(Duel duel, bool user)
        {
            bool passed = duel.Outcome == DuelOutcome.PassSucceeded || duel.Outcome == DuelOutcome.PassFailed;
            return user == duel.UserIsAttacker && !passed;
        }

        private static string TargetName(Duel duel, string id)
        {
            foreach (var list in new[] { duel.UserPassTargets, duel.EnemyPassTargets })
                foreach (var p in list)
                    if (p.Id == id) return p.Name;
            return id;
        }

        private static ScenarioKind KindOf(Duel d)
        {
            if (d.Kind == DuelKind.Field) return d.UserIsAttacker ? ScenarioKind.FieldAttack : ScenarioKind.FieldDefense;
            return d.UserIsAttacker ? ScenarioKind.ShotAsShooter : ScenarioKind.ShotAsKeeper;
        }

        private float T(float d) => _cat.Timings.Of(d);
        private string TurnLine(int turn, int max) => _data.Text.Format(UiKeys.DuelTurn, turn, max);

        private static string EnergyLine(int e)
            => e.ToString(CultureInfo.InvariantCulture) + "/" + DuelRules.EnergyPerTurn.ToString(CultureInfo.InvariantCulture);

        private string PassLabel(Duel duel)
            => _data.Text.Get(UiKeys.DuelPass) + "\n" + _data.Text.Format(UiKeys.DuelPassChance, UiText.Percent(duel.PassChance()));

        // ---- lifetime ----

        private void ResetView()
        {
            var text = _data.Text;
            _hand.Clear();
            for (int i = _ui.Fx.childCount - 1; i >= 0; i--) Destroy(_ui.Fx.GetChild(i).gameObject);
            _discardHand = false;
            _fresh = true;
            _ui.BannerGroup.alpha = 0;
            _ui.EnemyCardGroup.alpha = 0;
            _ui.PassGroup.alpha = 0;
            _ui.Picker.Close();
            _ui.Result.Hide();
            _ui.Intent.Hide();
            foreach (var user in new[] { true, false })
            {
                var c = user ? _duel.User : _duel.Enemy;
                var color = c.Player.Slot == Slot.GK ? _cat.Keeper : user ? _cat.UserTeam : _cat.EnemyTeam;
                var hpLabel = text.Get(UiKeys.HpLabel(_duel.Kind, user == _duel.UserIsAttacker));
                _ui.SetupFighter(user, c.Player.Name, hpLabel, color, c.Hp, c.MaxHp);
            }
            _ui.Kind.text = text.Get(UiKeys.Scenario(KindOf(_duel)));
            _ui.Seed.text = text.Format(UiKeys.DuelSeed, _seed);
            _ui.Turn.text = TurnLine(Math.Max(1, _duel.Turn), _duel.MaxTurns);
            _ui.EnergyLabel.text = text.Get(UiKeys.DuelEnergy);
            _ui.Energy.text = EnergyLine(0);
            _ui.NoCards.text = text.Get(UiKeys.DuelNoCards);
            _ui.NoCards.gameObject.SetActive(false);
            UiFactory.SetLabel(_ui.EndTurn, text.Get(UiKeys.DuelEndTurn));
            _ui.EndTurn.interactable = true;
            UiFactory.SetLabel(_ui.Back, text.Get(UiKeys.DuelBack));
            _ui.Pass.gameObject.SetActive(false);
            SyncCounts(_duel);
        }

        private void CancelWork()
        {
            if (_cts != null)
            {
                // Not disposed: awaiting code may still read the token once after cancellation.
                _cts.Cancel();
                _cts = null;
            }
            _tw.StopAll();
        }

        private void OnDisable() => CancelWork();

        private void OnDestroy() => CancelWork();
    }
}
