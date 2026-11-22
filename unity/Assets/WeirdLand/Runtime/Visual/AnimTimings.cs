using System;

namespace WeirdLand.Unity
{
    /// <summary>
    /// Every animation duration of the duel screen, in seconds. Screens read durations only through <see cref="Of"/>,
    /// so tests can speed everything up with <see cref="Scale"/>.
    /// </summary>
    [Serializable]
    public class AnimTimings
    {
        public float Scale = 1;
        public float Draw = 0.25f, DrawStagger = 0.06f, CardFly = 0.35f, EnemyCardHold = 0.5f, Lunge = 0.25f,
            Hit = 0.3f, FloatNumber = 0.6f, Bar = 0.3f, Banner = 0.6f, Pass = 0.5f;

        public float Of(float d) => d * Scale;
    }
}
