using WeirdLand.Core.Ai;
using WeirdLand.Core.Data;
using WeirdLand.Core.Duels;
using WeirdLand.Core.Match;
using static WeirdLand.Core.Tests.TestKit;

namespace WeirdLand.Core.Tests
{
    public class PolicyTests
    {
        private static int EnemyDamage = 1;

        private static Stats S(int passing = 3) => new Stats
        {
            Speed = 3, Power = 3, Passing = passing, Dribbling = 3, Shooting = 3,
            Defending = 3, Aerial = 3, Near = 3, Far = 3,
        };

        private static Duel NewDuel(bool userAttacks, DuelKind kind, int passing, params CardDef[] userCards)
        {
            var enemyCard = Card("x", 1, CardRole.Both, Dmg(EnemyDamage));
            var setup = new DuelSetup
            {
                Kind = kind,
                UserIsAttacker = userAttacks,
                UserPlayer = Player("u", Slot.CM, S(passing)),
                EnemyPlayer = Player("e", Slot.CM),
            };
            foreach (var c in userCards) setup.UserDeck.Add(new CardInstance(c, false));
            setup.UserPassTargets.Add(Player("t1", Slot.LW));
            setup.UserPassTargets.Add(Player("t2", Slot.ST));
            setup.EnemyDeck.Add(new CardInstance(enemyCard, false));
            var d = new Duel(setup, new Rng(5));
            d.Begin();
            return d;
        }

        private static GameData Data(params CardDef[] cards)
        {
            var map = new Dictionary<string, CardDef>();
            foreach (var c in cards) map[c.Id] = c;
            return new GameData(map, new Dictionary<string, TeamDef>());
        }

        private static TeamDef Team(string prefix, string card)
        {
            var t = new TeamDef { Id = prefix, Name = prefix, Tier = 1, Style = "x" };
            foreach (var slot in System.Enum.GetValues<Slot>())
                t.Players.Add(Player(prefix + slot, slot, null, card, card, card, card, card, card));
            return t;
        }

        [Test]
        public void RandomPolicy_OnlyLegalActions()
        {
            var rng = new Rng(11);
            var policy = new RandomPolicy();
            for (int i = 0; i < 500; i++)
            {
                var d = NewDuel(true, DuelKind.Field, 3,
                    Card("a", 1, CardRole.Both, Dmg(1)), Card("b", 2, CardRole.Both, Dmg(2)),
                    Card("c", 3, CardRole.Both, Blk(2)), Card("d", 0, CardRole.Both, Dmg(1)));
                var a = policy.Decide(d, null, rng);
                Assert.DoesNotThrow(() =>
                {
                    switch (a)
                    {
                        case PlayCardAction p: d.PlayCard(p.HandIndex); break;
                        case PassAction p: d.Pass(p.TargetId); break;
                        case EndTurnAction _: d.EndTurn(); break;
                    }
                });
            }
        }

        [Test]
        public void GoodPolicy_TakesLethal()
        {
            var d = NewDuel(true, DuelKind.Field, 1,
                Card("small", 1, CardRole.Both, Dmg(1)), Card("big", 1, CardRole.Both, Dmg(50)));
            var a = new GoodPolicy().Decide(d, null, new Rng(1));
            Assert.That(a, Is.TypeOf<PlayCardAction>());
            Assert.That(d.User.Hand[((PlayCardAction)a).HandIndex].Def.Id, Is.EqualTo("big"));
        }

        [Test]
        public void GoodPolicy_BlocksIncomingLethal()
        {
            EnemyDamage = 500;
            var d = NewDuel(false, DuelKind.Field, 3,
                Card("small", 1, CardRole.Both, Dmg(1)), Card("wall", 1, CardRole.Both, Blk(5)),
                Card("wall2", 1, CardRole.Both, Blk(9)));
            Assert.That(d.IncomingDamage(), Is.GreaterThanOrEqualTo(d.User.Hp + d.User.Block));
            var a = new GoodPolicy().Decide(d, null, new Rng(1));
            Assert.That(a, Is.TypeOf<PlayCardAction>());
            Assert.That(d.User.Hand[((PlayCardAction)a).HandIndex].Def.Id, Is.EqualTo("wall2"));
            EnemyDamage = 1;
        }

        [Test]
        public void GoodPolicy_PassesWhenChanceHigh()
        {
            var d = NewDuel(true, DuelKind.Field, 5, Card("small", 1, CardRole.Both, Dmg(1)));
            d.Enemy.StatusMap[StatusId.Unbalanced] = 2;
            Assert.That(d.PassChance(), Is.GreaterThanOrEqualTo(0.75));
            var a = new GoodPolicy().Decide(d, null, new Rng(1));
            Assert.That(a, Is.TypeOf<PassAction>());
        }

        private static Duel ShotVsLeaningKeeper(StatusId lean, params CardDef[] userCards)
        {
            var setup = new DuelSetup
            {
                Kind = DuelKind.Shot,
                UserIsAttacker = true,
                UserPlayer = Player("u", Slot.ST, S()),
                EnemyPlayer = Player("gk", Slot.GK, S()), // Near 3, Far 3: Kale 18
            };
            foreach (var c in userCards) setup.UserDeck.Add(new CardInstance(c, false));
            setup.EnemyDeck.Add(new CardInstance(Card("lean", 1, CardRole.Defense, Sts(lean, 1, EffectTarget.Self), Blk(3)), false));
            var d = new Duel(setup, new Rng(5));
            d.Begin();
            return d;
        }

        [TestCase(StatusId.CoverNear, "far")]
        [TestCase(StatusId.CoverFar, "near")]
        public void GoodPolicy_Shooter_PicksUncoveredCorner(StatusId lean, string expected)
        {
            var d = ShotVsLeaningKeeper(lean,
                Card("near", 1, CardRole.Attack, AtCorner(Dmg(6), Corner.Near)),
                Card("far", 1, CardRole.Attack, AtCorner(Dmg(6), Corner.Far)));
            var a = new GoodPolicy().Decide(d, null, new Rng(1));
            Assert.That(a, Is.TypeOf<PlayCardAction>());
            Assert.That(d.User.Hand[((PlayCardAction)a).HandIndex].Def.Id, Is.EqualTo(expected));
        }

        [Test]
        public void GoodPolicy_Shooter_AvoidsCoveredCorner_EvenWithMoreRawDamage()
        {
            // Covered near: 10 - 3 = 7. Uncovered far: (6 - 3) * 2 = 6. Neither is lethal (Kale 18).
            var d = ShotVsLeaningKeeper(StatusId.CoverNear,
                Card("near", 1, CardRole.Attack, AtCorner(Dmg(10), Corner.Near)),
                Card("far", 1, CardRole.Attack, AtCorner(Dmg(6), Corner.Far)));
            var a = new GoodPolicy().Decide(d, null, new Rng(1));
            Assert.That(d.User.Hand[((PlayCardAction)a).HandIndex].Def.Id, Is.EqualTo("far"));
        }

        [Test]
        public void MatchRunner_CompletesMatch()
        {
            var card = Card("c", 1, CardRole.Both, Dmg(3));
            var m = new MatchEngine(Data(card), Team("u", "c"), Team("o", "c"), 7);
            var r = MatchRunner.Play(m, new GoodPolicy(), new Rng(7));
            Assert.That(m.Phase, Is.EqualTo(MatchPhase.Ended));
            Assert.That(r.Duels, Is.GreaterThan(0));
            Assert.That(r.Moments, Is.EqualTo(m.MomentsPlayed));
            Assert.That(r.Events, Is.Not.Empty);
        }

        [Test]
        public void SameSeed_SameMatch()
        {
            var card = Card("c", 1, CardRole.Both, Dmg(3));
            MatchResult Run()
            {
                var m = new MatchEngine(Data(card), Team("u", "c"), Team("o", "c"), 42);
                return MatchRunner.Play(m, new GoodPolicy(), new Rng(99));
            }
            var a = Run();
            var b = Run();
            Assert.That(b.UserScore, Is.EqualTo(a.UserScore));
            Assert.That(b.OppScore, Is.EqualTo(a.OppScore));
            Assert.That(b.Duels, Is.EqualTo(a.Duels));
            Assert.That(b.Turns, Is.EqualTo(a.Turns));
            Assert.That(b.Events.Count, Is.EqualTo(a.Events.Count));
            for (int i = 0; i < a.Events.Count; i++) Assert.That(b.Events[i], Is.EqualTo(a.Events[i]));
        }

        private record BogusAction : DuelAction;

        private class BogusPolicy : IUserPolicy
        {
            public DuelAction Decide(Duel duel, MatchEngine match, Rng rng) => new BogusAction();
        }

        [Test]
        public void MatchRunner_RejectsUnknownAction()
        {
            var card = Card("c", 1, CardRole.Both, Dmg(3));
            var m = new MatchEngine(Data(card), Team("u", "c"), Team("o", "c"), 7);
            var ex = Assert.Throws<System.InvalidOperationException>(() => MatchRunner.Play(m, new BogusPolicy(), new Rng(7)));
            Assert.That(ex!.Message, Does.Contain(nameof(BogusAction)));
        }

        private class AlwaysPlayZero : IUserPolicy
        {
            public DuelAction Decide(Duel duel, MatchEngine match, Rng rng) => new PlayCardAction(0);
        }

        [Test]
        public void MatchRunner_ThrowsOnRunawayPolicy()
        {
            var draw = new EffectDef { Type = EffectType.Draw, Target = EffectTarget.Self, Amount = 1 };
            var card = Card("loop", 0, CardRole.Both, draw);
            var t = new TeamDef { Id = "u", Name = "u", Tier = 1, Style = "x" };
            var o = new TeamDef { Id = "o", Name = "o", Tier = 1, Style = "x" };
            foreach (var slot in System.Enum.GetValues<Slot>())
            {
                t.Players.Add(Player("u" + slot, slot, null, "loop"));
                o.Players.Add(Player("o" + slot, slot, null, "loop"));
            }
            var m = new MatchEngine(Data(card), t, o, 3);
            Assert.Throws<System.InvalidOperationException>(() => MatchRunner.Play(m, new AlwaysPlayZero(), new Rng(1)));
        }
    }
}
