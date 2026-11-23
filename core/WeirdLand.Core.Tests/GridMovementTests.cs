using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WeirdLand.Core.Match;

namespace WeirdLand.Core.Tests
{
    public class GridMovementTests
    {
        // Parks everyone on rows 6..7 (row 7 x0..11, then row 6 x0..1); tests place actors on rows 0..5.
        private static GridMatch Setup(string carrierId, Cell at)
        {
            var m = TestKit.NewGridMatch(seed: 1); m.Kickoff();
            int i = 0;
            foreach (var p in m.Players)
            {
                m.SetPos(p.Def.Id, i < 12 ? new Cell(i, 7) : new Cell(i - 12, 6));
                i++;
            }
            m.SetCarrier(carrierId);
            m.SetPos(carrierId, at);
            return m;
        }

        private static List<GameEvent> Step(GridMatch m)
        {
            var ev = new List<GameEvent>();
            GridMovement.Step(m, ev);
            return ev;
        }

        private static GridPlayer P(GridMatch m, string id) => m.Players.First(p => p.Def.Id == id);

        [Test] public void Presser_IsNearestNonKeeper_MovesOneCellTowardCarrier()
        {
            var m = Setup("mavi_cm", new Cell(3, 3));
            m.SetPos("kirmizi_gk", new Cell(4, 3)); // keeper is closest but must be ignored
            m.SetPos("kirmizi_cm", new Cell(7, 3));
            m.SetPos("kirmizi_lb", new Cell(9, 2));
            var ev = Step(m);
            Assert.That(P(m, "kirmizi_cm").Pos, Is.EqualTo(new Cell(6, 3)));
            var moved = ev.OfType<PlayerMoved>().Single(e => e.PlayerId == "kirmizi_cm");
            Assert.That(moved.FromX, Is.EqualTo(7));
            Assert.That(moved.ToX, Is.EqualTo(6));
        }

        [Test] public void StunnedPlayer_DoesNotMove()
        {
            var m = Setup("mavi_cm", new Cell(3, 3));
            m.SetPos("kirmizi_cm", new Cell(6, 3));
            P(m, "kirmizi_cm").Stun = 1;
            var ev = Step(m);
            Assert.That(P(m, "kirmizi_cm").Pos, Is.EqualTo(new Cell(6, 3)));
            Assert.That(ev.OfType<PlayerMoved>().Any(e => e.PlayerId == "kirmizi_cm"), Is.False);
        }

        [Test] public void Keeper_TracksBallY_ClampedTo2_5()
        {
            var m = Setup("mavi_cm", new Cell(3, 0));
            m.SetPos("kirmizi_gk", new Cell(11, 3));
            Step(m);
            Assert.That(P(m, "kirmizi_gk").Pos, Is.EqualTo(new Cell(11, 2)));

            m = Setup("mavi_cm", new Cell(3, 6));
            m.SetPos("kirmizi_gk", new Cell(11, 3));
            Step(m);
            Assert.That(P(m, "kirmizi_gk").Pos, Is.EqualTo(new Cell(11, 4)));

            m = Setup("mavi_cm", new Cell(3, 5));
            m.SetPos("kirmizi_gk", new Cell(11, 3));
            m.SetPos("kirmizi_gk", new Cell(11, 5));
            Step(m);
            Assert.That(P(m, "kirmizi_gk").Pos, Is.EqualTo(new Cell(11, 5)));
        }

        [Test] public void DefensiveLine_ShiftsWithBall()
        {
            // carrier x=3: shift = round(-2.5*0.5 = -1.25) = -1
            var m = Setup("mavi_cm", new Cell(3, 3));
            m.SetPos("kirmizi_cm", new Cell(6, 3)); // presser, so lb is not
            var lb = P(m, "kirmizi_lb");
            m.SetPos("kirmizi_lb", new Cell(9, 1));
            Assert.That(GridMovement.ShiftedHome(lb, new Cell(3, 3)), Is.EqualTo(new Cell(8, 1)));
            Step(m);
            Assert.That(lb.Pos, Is.EqualTo(new Cell(8, 1)));

            // deep ball, carrier x=10: round(2.25) = 2 -> kirmizi home x=9 shifts to 11 (clamped to board)
            Assert.That(GridMovement.ShiftedHome(lb, new Cell(10, 3)), Is.EqualTo(new Cell(11, 1)));
            // attacking side pushes forward: mavi_lb home x=2, carrier x=9 -> round(1.75)=2 -> 4
            Assert.That(GridMovement.ShiftedHome(P(m, "mavi_lb"), new Cell(9, 3)), Is.EqualTo(new Cell(4, 1)));
            // clamp at left edge
            Assert.That(GridMovement.ShiftedHome(P(m, "mavi_lb"), new Cell(0, 3)).X, Is.EqualTo(0));
        }

        [Test] public void Positions_StayUnique_Over200Moves()
        {
            for (uint seed = 1; seed <= 10; seed++)
            {
                var m = TestKit.NewGridMatch(seed); m.Kickoff();
                for (int i = 0; i < 200; i++)
                {
                    var r = m.ReachableCells();
                    if (r.Count == 0) break;
                    m.Advance(r[0]);
                    if (m.Phase == GridPhase.InDuel) { m.CurrentDuel.ForceOutcome(DuelOutcome.AttackerWon); m.CompleteDuel(); }
                    Assert.That(m.Players.Select(p => p.Pos).Distinct().Count(), Is.EqualTo(14), "seed " + seed + " move " + i);
                }
            }
        }

        [Test] public void Movement_IsDeterministic()
        {
            Func<List<string>> run = () =>
            {
                var log = new List<string>();
                var m = TestKit.NewGridMatch(7); m.Kickoff();
                for (int i = 0; i < 30 && m.Phase != GridPhase.Ended; i++)
                {
                    var r = m.ReachableCells();
                    if (r.Count == 0) break;
                    var ev = m.Advance(r[r.Count / 2]).ToList();
                    if (m.Phase == GridPhase.InDuel) { m.CurrentDuel.ForceOutcome(DuelOutcome.AttackerWon); ev.AddRange(m.CompleteDuel()); }
                    foreach (var e in ev.OfType<PlayerMoved>())
                        log.Add(e.PlayerId + ":" + e.FromX + "," + e.FromY + ">" + e.ToX + "," + e.ToY);
                }
                return log;
            };
            var first = run();
            Assert.That(first, Is.Not.Empty);
            Assert.That(first, Is.EqualTo(run()));
        }
    }
}
