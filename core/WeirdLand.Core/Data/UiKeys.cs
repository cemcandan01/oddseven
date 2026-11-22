using System;
using System.Collections.Generic;
using System.Reflection;
using WeirdLand.Core.Duels;

namespace WeirdLand.Core.Data
{
    /// <summary>Catalogue of keys in data/ui_tr.json. Keys built from enums come from the builder functions.</summary>
    public static class UiKeys
    {
        public const string DuelEndTurn = "duel.endTurn";
        public const string DuelPass = "duel.pass";
        public const string DuelPassChance = "duel.passChance";
        public const string DuelPassPickTitle = "duel.passPickTitle";
        public const string DuelCancel = "duel.cancel";
        public const string DuelTurn = "duel.turn";
        public const string DuelEnergy = "duel.energy";
        public const string DuelDrawPile = "duel.drawPile";
        public const string DuelDiscardPile = "duel.discardPile";
        public const string DuelNoCards = "duel.noCards";
        public const string DuelEnemyPlayed = "duel.enemyPlayed";
        public const string DuelBack = "duel.back";
        public const string DuelSeed = "duel.seed";
        public const string DuelBlock = "duel.block";
        public const string HpKontrol = "hp.kontrol";
        public const string HpDenge = "hp.denge";
        public const string HpKale = "hp.kale";
        public const string CardDamage = "card.damage";
        public const string CardBlock = "card.block";
        public const string IntentWait = "intent.wait";
        public const string IntentDamage = "intent.damage";
        public const string IntentBlock = "intent.block";
        public const string IntentPass = "intent.pass";
        public const string ScenarioTitle = "scenario.title";
        public const string ScenarioUserTeam = "scenario.userTeam";
        public const string ScenarioOpponentTeam = "scenario.opponentTeam";
        public const string ScenarioKindLabel = "scenario.kind";
        public const string ScenarioUserPlayer = "scenario.userPlayer";
        public const string ScenarioEnemyPlayer = "scenario.enemyPlayer";
        public const string ScenarioSeed = "scenario.seed";
        public const string ScenarioStart = "scenario.start";
        public const string ResultTimeout = "result.timeout";
        public const string ResultAgain = "result.again";
        public const string ResultNewSeed = "result.newSeed";
        public const string ResultBack = "result.back";

        public static string Status(StatusId s) => "status." + s;
        public static string StatusDesc(StatusId s) => "status." + s + ".desc";
        public static string SlotName(Slot s) => "slot." + s;
        public static string Role(CardRole r) => "role." + r;
        public static string Scenario(ScenarioKind k) => "scenario." + k;

        public static string HpLabel(DuelKind kind, bool isAttacker)
        {
            if (isAttacker) return HpKontrol;
            return kind == DuelKind.Field ? HpDenge : HpKale;
        }

        public static string Result(DuelKind kind, DuelOutcome o, bool userIsAttacker)
            => "result." + (kind == DuelKind.Field ? "field" : "shot") + "." + o + "." + (userIsAttacker ? "attacker" : "defender");

        /// <summary>Every key the UI can ask for: all constants plus all valid builder combinations.</summary>
        public static IEnumerable<string> All()
        {
            foreach (var f in typeof(UiKeys).GetFields(BindingFlags.Public | BindingFlags.Static))
                if (f.IsLiteral && f.FieldType == typeof(string)) yield return (string)f.GetRawConstantValue();
            foreach (StatusId s in Enum.GetValues(typeof(StatusId)))
            {
                yield return Status(s);
                yield return StatusDesc(s);
            }
            foreach (Slot s in Enum.GetValues(typeof(Slot))) yield return SlotName(s);
            foreach (CardRole r in Enum.GetValues(typeof(CardRole))) yield return Role(r);
            foreach (ScenarioKind k in Enum.GetValues(typeof(ScenarioKind))) yield return Scenario(k);
            foreach (var attacker in new[] { true, false })
            {
                foreach (var o in new[] { DuelOutcome.AttackerWon, DuelOutcome.DefenderWon, DuelOutcome.PassSucceeded, DuelOutcome.PassFailed })
                    yield return Result(DuelKind.Field, o, attacker);
                foreach (var o in new[] { DuelOutcome.AttackerWon, DuelOutcome.DefenderWon })
                    yield return Result(DuelKind.Shot, o, attacker);
            }
        }
    }
}
