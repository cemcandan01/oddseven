using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

namespace WeirdLand.Unity
{
    /// <summary>
    /// The tweens one screen started, so the screen can stop exactly its own tweens. Every helper follows the
    /// global rule: a duration of 0 or less starts no tween and assigns the end value directly.
    /// </summary>
    public sealed class TweenSet
    {
        private readonly List<Tween> _live = new List<Tween>();

        public Tween Add(Tween t)
        {
            _live.RemoveAll(x => !x.isAlive);
            if (t.isAlive) _live.Add(t);
            return t;
        }

        public void StopAll()
        {
            foreach (var t in _live)
                if (t.isAlive) t.Stop();
            _live.Clear();
        }

        public Tween Move(RectTransform r, Vector2 to, float duration, float delay = 0, Ease ease = Ease.OutCubic)
        {
            if (duration <= 0)
            {
                r.anchoredPosition = to;
                return default;
            }
            return Add(Tween.UIAnchoredPosition(r, to, duration, ease, startDelay: Mathf.Max(0, delay)));
        }

        public Tween Scale(Transform t, float from, float to, float duration, float delay = 0, Ease ease = Ease.OutCubic)
        {
            if (duration <= 0)
            {
                t.localScale = Vector3.one * to;
                return default;
            }
            return Add(Tween.Scale(t, from, to, duration, ease, startDelay: Mathf.Max(0, delay)));
        }

        public Tween Fade(CanvasGroup g, float from, float to, float duration, float delay = 0)
        {
            if (duration <= 0)
            {
                g.alpha = to;
                return default;
            }
            return Add(Tween.Alpha(g, from, to, duration, Ease.Linear, startDelay: Mathf.Max(0, delay)));
        }

        /// <summary>Fades in and back out over <paramref name="duration"/>; ends at alpha 0.</summary>
        public Tween Flash(CanvasGroup g, float duration)
        {
            if (duration <= 0)
            {
                g.alpha = 0;
                return default;
            }
            return Add(Tween.Alpha(g, 0, 1, duration * 0.5f, Ease.OutQuad, cycles: 2, cycleMode: CycleMode.Yoyo));
        }

        /// <summary>Moves by <paramref name="offset"/> and back (a lunge); ends where it started.</summary>
        public Tween Lunge(RectTransform r, Vector2 offset, float duration)
        {
            if (duration <= 0) return default;
            var start = r.anchoredPosition;
            return Add(Tween.UIAnchoredPosition(r, start, start + offset, duration * 0.5f, Ease.OutQuad, cycles: 2,
                cycleMode: CycleMode.Yoyo));
        }

        /// <summary>A short grow-and-settle; resets the scale to 1 first so overlapping punches never drift.</summary>
        public Tween Punch(Transform t, float strength, float duration)
        {
            Tween.StopAll(t);
            t.localScale = Vector3.one;
            if (duration <= 0) return default;
            return Add(Tween.PunchScale(t, Vector3.one * strength, duration, frequency: 4));
        }

        public Tween Shake(Transform t, float strength, float duration)
        {
            if (duration <= 0) return default;
            return Add(Tween.ShakeLocalPosition(t, new Vector3(strength, strength * 0.3f, 0), duration));
        }

        /// <summary>Animates a value on <paramref name="target"/>; the tween dies with the target.</summary>
        public Tween Value<T>(T target, float from, float to, float duration, Action<T, float> apply) where T : class
        {
            if (duration <= 0)
            {
                apply(target, to);
                return default;
            }
            return Add(Tween.Custom(target, from, to, duration, apply, Ease.OutCubic));
        }
    }
}
