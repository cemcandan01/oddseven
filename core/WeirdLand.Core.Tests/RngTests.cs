using WeirdLand.Core;

namespace WeirdLand.Core.Tests
{
    public class RngTests
    {
        [Test] public void SameSeed_SameSequence() {
            var a = new Rng(42); var b = new Rng(42);
            for (int i = 0; i < 100; i++) Assert.That(a.NextDouble(), Is.EqualTo(b.NextDouble()));
        }
        [Test] public void NextDouble_InUnitRange() {
            var r = new Rng(1);
            for (int i = 0; i < 10000; i++) { var x = r.NextDouble(); Assert.That(x, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0)); }
        }
        [Test] public void Next_CoversAllValues() {
            var r = new Rng(7); var seen = new HashSet<int>();
            for (int i = 0; i < 1000; i++) seen.Add(r.Next(5));
            Assert.That(seen, Is.EquivalentTo(new[] { 0, 1, 2, 3, 4 }));
        }
        [Test] public void Shuffle_IsPermutation_AndDeterministic() {
            var l1 = Enumerable.Range(0, 20).ToList(); var l2 = Enumerable.Range(0, 20).ToList();
            new Rng(9).Shuffle(l1); new Rng(9).Shuffle(l2);
            Assert.That(l1, Is.EqualTo(l2)); Assert.That(l1, Is.EquivalentTo(Enumerable.Range(0, 20)));
            Assert.That(l1, Is.Not.EqualTo(Enumerable.Range(0, 20)));
        }
        [Test] public void Fork_IsIndependentButDeterministic() {
            var a = new Rng(5); var fa = a.Fork(); var b = new Rng(5); var fb = b.Fork();
            Assert.That(fa.NextDouble(), Is.EqualTo(fb.NextDouble()));
            Assert.That(a.NextDouble(), Is.EqualTo(b.NextDouble()));
        }
        [Test] public void ZeroSeed_StillProducesValues() {
            var r = new Rng(0); Assert.That(Enumerable.Range(0, 10).Select(_ => r.NextDouble()).Distinct().Count(), Is.GreaterThan(1));
        }
    }
}
