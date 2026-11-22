using System.Collections.Generic;
using WeirdLand.Core.Data;

namespace WeirdLand.Core.Duels
{
    public sealed record CardPreview(int Damage, int Block);

    public enum IntentKind { Wait, Card, Pass }

    public sealed record StatusPreview(StatusId Status, int Stacks, bool OnUser);

    public sealed record IntentView(IntentKind Kind, string CardId, string CardName, int Damage, int Block,
        IReadOnlyList<StatusPreview> Statuses, string PassTargetId, string PassTargetName, double PassChance);

    /// <summary>Read-only numbers for the duel screen. The UI shows these and never computes rules itself.</summary>
    public static class DuelPreview
    {
        public static CardPreview Card(Duel d, CardInstance c)
        {
            int damage = 0, block = 0;
            foreach (var e in c.Effects)
            {
                if (e.Type == EffectType.Damage) damage += EffectResolver.ComputeDamage(e, d.User, d.Enemy, d.Kind);
                block += EffectResolver.ComputeBlock(e, d.User);
            }
            return new CardPreview(damage, block);
        }

        public static bool CanPlay(Duel d, int handIndex)
        {
            if (d.Outcome.HasValue) return false;
            if (handIndex < 0 || handIndex >= d.User.Hand.Count) return false;
            return d.User.Hand[handIndex].Cost <= d.User.Energy;
        }

        public static IntentView Intent(Duel d)
        {
            var statuses = new List<StatusPreview>();
            var intent = d.Intent;
            if (intent == null || (intent.Card == null && intent.PassTarget == null))
                return new IntentView(IntentKind.Wait, null, null, 0, 0, statuses, null, null, 0);

            if (intent.PassTarget != null)
                return new IntentView(IntentKind.Pass, null, null, 0, 0, statuses,
                    intent.PassTarget.Id, intent.PassTarget.Name, intent.PassChance);

            int block = 0;
            foreach (var e in intent.Card.Effects)
            {
                block += EffectResolver.ComputeBlock(e, d.Enemy);
                if (e.Type == EffectType.Status && e.Status.HasValue)
                    statuses.Add(new StatusPreview(e.Status.Value, e.Stacks, e.Target != EffectTarget.Self));
            }
            return new IntentView(IntentKind.Card, intent.Card.Def.Id, intent.Card.Def.Name, d.IncomingDamage(), block,
                statuses, null, null, 0);
        }
    }
}
