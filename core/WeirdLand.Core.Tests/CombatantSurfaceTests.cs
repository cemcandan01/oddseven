using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;

namespace WeirdLand.Core.Tests
{
    public class CombatantSurfaceTests
    {
        private static void AssertNoPublicMutation(System.Type t)
        {
            Assert.That(t.GetFields(BindingFlags.Public | BindingFlags.Instance), Is.Empty);
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                Assert.That(p.SetMethod == null || !p.SetMethod.IsPublic, p.Name);
        }

        [Test] public void Combatant_HasNoPublicFieldsOrSetters() => AssertNoPublicMutation(typeof(Combatant));

        [Test] public void Combatant_CollectionsCannotBeCastBackToMutable()
        {
            var c = new Combatant();
            Assert.That(c.Hand, Is.Not.InstanceOf<List<CardInstance>>());
            Assert.That(c.DrawPile, Is.Not.InstanceOf<List<CardInstance>>());
            Assert.That(c.Discard, Is.Not.InstanceOf<List<CardInstance>>());
            Assert.That(c.Statuses, Is.Not.InstanceOf<Dictionary<StatusId, int>>());
        }

        [Test] public void EnemyIntent_HasNoPublicSetters() => AssertNoPublicMutation(typeof(EnemyIntent));
    }
}
