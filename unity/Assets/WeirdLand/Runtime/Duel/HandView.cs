using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Unity
{
    /// <summary>
    /// The user's hand on the duel screen: one CardView per card, in hand order, laid out with HandLayout.
    /// Draw, play and discard animations live here; <see cref="Sync"/> snaps the views back to the duel state.
    /// </summary>
    public sealed class HandView
    {
        public const float CardWidth = 200, MaxWidth = 1100, Spacing = 20;

        private readonly List<CardView> _cards = new List<CardView>();
        private readonly RectTransform _root, _fx, _deckPoint;
        private readonly VisualCatalog _cat;
        private readonly TweenSet _tw;
        private readonly Action<CardView> _clicked;

        public HandView(RectTransform root, RectTransform fx, RectTransform deckPoint, VisualCatalog cat, TweenSet tw,
            Action<CardView> clicked)
        {
            _root = root;
            _fx = fx;
            _deckPoint = deckPoint;
            _cat = cat;
            _tw = tw;
            _clicked = clicked;
        }

        public UiText Text { get; set; }

        /// <summary>The card the user clicked; CardPlayed(user) flies this one to the centre.</summary>
        public CardView Playing { get; set; }

        public int Count => _cards.Count;
        public CardView this[int i] => _cards[i];
        public int IndexOf(CardView v) => _cards.IndexOf(v);

        public static Vector2[] Positions(int n) => HandLayout.Positions(n, CardWidth, MaxWidth, Spacing);

        public void Clear()
        {
            foreach (var v in _cards)
                if (v != null) UnityEngine.Object.Destroy(v.gameObject);
            _cards.Clear();
            Playing = null;
        }

        /// <summary>Raises a clicked card while its command runs.</summary>
        public void Lift(CardView v) => ((RectTransform)v.transform).anchoredPosition += new Vector2(0, 40);

        /// <summary>Puts a card back in its slot (the command was refused).</summary>
        public void Return(CardView v)
        {
            int i = _cards.IndexOf(v);
            if (i >= 0) _tw.Move((RectTransform)v.transform, Positions(_cards.Count)[i], T(_cat.Timings.Draw));
        }

        /// <summary>The first <paramref name="n"/> hand cards without a view become views and fly in from the deck.</summary>
        public async UniTask Draw(Duel duel, int n, CancellationToken ct)
        {
            var fresh = new List<CardView>();
            foreach (var c in duel.User.Hand)
            {
                if (fresh.Count >= n) break;
                if (ViewOf(c) != null) continue;
                var v = Create(c, duel);
                fresh.Add(v);
                _cards.Add(v);
            }
            _cards.Sort((a, b) => SortKey(duel, a).CompareTo(SortKey(duel, b)));
            Bind(duel);

            var t = _cat.Timings;
            var pos = Positions(_cards.Count);
            var deck = (Vector2)_root.InverseTransformPoint(_deckPoint.position);
            var waits = new List<UniTask>();
            for (int i = 0; i < _cards.Count; i++)
            {
                var r = (RectTransform)_cards[i].transform;
                int k = fresh.IndexOf(_cards[i]);
                if (k < 0)
                {
                    waits.Add(TweenAwait.Wait(_tw.Move(r, pos[i], T(t.Draw)), ct));
                    continue;
                }
                float delay = T(t.DrawStagger) * k;
                r.anchoredPosition = deck;
                r.localScale = Vector3.one * 0.4f;
                waits.Add(TweenAwait.Wait(_tw.Move(r, pos[i], T(t.Draw), delay), ct));
                waits.Add(TweenAwait.Wait(_tw.Scale(r, 0.4f, 1f, T(t.Draw), delay), ct));
            }
            await UniTask.WhenAll(waits);
        }

        /// <summary>The played card flies to the centre, grows and fades, then is destroyed; the rest close the gap.</summary>
        public async UniTask FlyPlayed(CancellationToken ct)
        {
            var v = Playing;
            Playing = null;
            if (v == null) return;
            _cards.Remove(v);
            var r = (RectTransform)v.transform;
            r.SetParent(_fx, true);
            v.Group.blocksRaycasts = false;
            float d = T(_cat.Timings.CardFly);
            var pos = Positions(_cards.Count);
            var waits = new List<UniTask>
            {
                TweenAwait.Wait(_tw.Move(r, new Vector2(0, 60), d * 0.6f), ct),
                TweenAwait.Wait(_tw.Scale(r, 1f, 1.3f, d * 0.6f), ct),
                TweenAwait.Wait(_tw.Fade(v.Group, v.Group.alpha, 0, d * 0.4f, d * 0.6f), ct),
            };
            for (int i = 0; i < _cards.Count; i++)
                waits.Add(TweenAwait.Wait(_tw.Move((RectTransform)_cards[i].transform, pos[i], d), ct));
            try
            {
                await UniTask.WhenAll(waits);
            }
            finally
            {
                if (v != null) UnityEngine.Object.Destroy(v.gameObject);
            }
        }

        /// <summary>End of turn: the whole hand drops away and fades.</summary>
        public async UniTask Discard(CancellationToken ct)
        {
            var views = _cards.ToArray();
            _cards.Clear();
            Playing = null;
            float d = T(_cat.Timings.CardFly) * 0.6f;
            var waits = new List<UniTask>();
            foreach (var v in views)
            {
                var r = (RectTransform)v.transform;
                v.Group.blocksRaycasts = false;
                waits.Add(TweenAwait.Wait(_tw.Move(r, r.anchoredPosition + new Vector2(0, -220), d), ct));
                waits.Add(TweenAwait.Wait(_tw.Fade(v.Group, v.Group.alpha, 0, d), ct));
            }
            try
            {
                await UniTask.WhenAll(waits);
            }
            finally
            {
                foreach (var v in views)
                    if (v != null) UnityEngine.Object.Destroy(v.gameObject);
            }
        }

        /// <summary>Destroys views whose card left the hand, adds missing ones, rebinds all and snaps them into place.</summary>
        public void Sync(Duel duel)
        {
            Playing = null;
            var keep = new List<CardView>(duel.User.Hand.Count);
            foreach (var c in duel.User.Hand) keep.Add(ViewOf(c) ?? Create(c, duel));
            foreach (var v in _cards)
                if (v != null && !keep.Contains(v)) UnityEngine.Object.Destroy(v.gameObject);
            _cards.Clear();
            _cards.AddRange(keep);
            Bind(duel);
            var pos = Positions(_cards.Count);
            for (int i = 0; i < _cards.Count; i++)
            {
                var v = _cards[i];
                var r = (RectTransform)v.transform;
                Tween.StopAll(r);
                Tween.StopAll(v.Group);
                r.anchoredPosition = pos[i];
                r.localScale = Vector3.one;
                r.SetSiblingIndex(i);
                v.Group.blocksRaycasts = true;
            }
        }

        /// <summary>Every view gets DuelPreview numbers and playability for its card's current hand index.</summary>
        private void Bind(Duel duel)
        {
            foreach (var v in _cards)
            {
                int i = IndexIn(duel.User.Hand, v.Card);
                v.Bind(v.Card, DuelPreview.Card(duel, v.Card), i >= 0 && DuelPreview.CanPlay(duel, i), Text, _cat);
            }
        }

        private CardView Create(CardInstance c, Duel duel)
        {
            var v = UnityEngine.Object.Instantiate(_cat.Card, _root, false);
            v.Bind(c, DuelPreview.Card(duel, c), false, Text, _cat);
            v.Clicked += _clicked;
            return v;
        }

        private CardView ViewOf(CardInstance c)
        {
            foreach (var v in _cards)
                if (v != null && ReferenceEquals(v.Card, c)) return v;
            return null;
        }

        private static int SortKey(Duel duel, CardView v)
        {
            int i = IndexIn(duel.User.Hand, v.Card);
            return i < 0 ? int.MaxValue : i;
        }

        private static int IndexIn(IReadOnlyList<CardInstance> hand, CardInstance c)
        {
            for (int i = 0; i < hand.Count; i++)
                if (ReferenceEquals(hand[i], c)) return i;
            return -1;
        }

        private float T(float d) => _cat.Timings.Of(d);
    }
}
