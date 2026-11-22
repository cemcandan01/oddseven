using UnityEngine;

namespace WeirdLand.Unity
{
    /// <summary>Card positions for a hand, centred on x = 0, all at y = 0.</summary>
    public static class HandLayout
    {
        /// <summary>
        /// Cards sit <paramref name="spacing"/> apart. When that row is wider than <paramref name="maxWidth"/>,
        /// cards overlap so that the outer card edges stay within ±maxWidth/2.
        /// </summary>
        public static Vector2[] Positions(int count, float cardWidth, float maxWidth, float spacing)
        {
            if (count <= 0) return new Vector2[0];
            float step = cardWidth + spacing;
            if (count > 1 && count * cardWidth + (count - 1) * spacing > maxWidth)
                step = Mathf.Max(0f, (maxWidth - cardWidth) / (count - 1));
            var result = new Vector2[count];
            float mid = (count - 1) * 0.5f;
            for (int i = 0; i < count; i++) result[i] = new Vector2((i - mid) * step, 0f);
            return result;
        }
    }
}
