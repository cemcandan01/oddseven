using System.Globalization;
using WeirdLand.Core;
using WeirdLand.Core.Ai;
using WeirdLand.Core.Match;

namespace WeirdLand.Sim;

public static class SimReport
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static double EstimatedMinutes(MatchResult r) => (r.Turns * 15 + r.Duels * 5) / 60.0;

    public static List<string> Build(string userId, string oppId, string policy, uint seed, List<MatchResult> results)
    {
        int n = results.Count;
        var lines = new List<string>
        {
            $"=== {userId} - {oppId} | politika: {policy} | maç: {n} | seed: {seed} ==="
        };
        if (n == 0)
        {
            lines.Add("Maç yok.");
            return lines;
        }

        int w = results.Count(r => r.UserScore > r.OppScore);
        int d = results.Count(r => r.UserScore == r.OppScore);
        int l = results.Count(r => r.UserScore < r.OppScore);
        lines.Add($"Galibiyet: %{Pct(w, n)}  Beraberlik: %{Pct(d, n)}  Mağlubiyet: %{Pct(l, n)}");
        lines.Add($"Ortalama gol: {Avg(results.Average(r => r.UserScore))} - {Avg(results.Average(r => r.OppScore))} (kullanıcı - rakip)");
        lines.Add($"Ortalama düello: {Avg(results.Average(r => r.Duels))}  tur: {Avg(results.Average(r => r.Turns))}  pas: {Avg(results.Average(r => r.Passes))}");

        var mins = results.Select(EstimatedMinutes).OrderBy(x => x).ToList();
        int rank = (int)Math.Ceiling(0.9 * n); // nearest-rank
        double p90 = mins[Math.Max(1, rank) - 1];
        lines.Add($"Tahmini süre (dk): ortalama {mins.Average().ToString("F1", Inv)}  %90'lık dilim {p90.ToString("F1", Inv)}");

        var top = results
            .GroupBy(r => $"{r.UserScore}-{r.OppScore}")
            .Select(g => (Score: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Score, StringComparer.Ordinal)
            .Take(8)
            .Select(x => $"{x.Score} (%{Pct(x.Count, n)})");
        lines.Add("En sık skorlar: " + string.Join(", ", top));
        return lines;
    }

    public static List<string> BuildGrid(string userId, string oppId, string policy, uint seed,
        List<GridResult> results, ISet<string> userPlayerIds)
    {
        int n = results.Count;
        var lines = new List<string>
        {
            $"=== [grid] {userId} - {oppId} | politika: {policy} | maç: {n} | seed: {seed} ==="
        };
        if (n == 0)
        {
            lines.Add("Maç yok.");
            return lines;
        }
        int w = results.Count(r => r.UserScore > r.OppScore);
        int d = results.Count(r => r.UserScore == r.OppScore);
        int l = results.Count(r => r.UserScore < r.OppScore);
        lines.Add($"Galibiyet: %{Pct(w, n)}  Beraberlik: %{Pct(d, n)}  Mağlubiyet: %{Pct(l, n)}");
        lines.Add($"Ortalama gol: {Avg(results.Average(r => r.UserScore))} - {Avg(results.Average(r => r.OppScore))} (kullanıcı - rakip)");

        int passed = 0, intercepted = 0;
        int atkDuels = 0, atkWon = 0, defDuels = 0, defWon = 0;
        foreach (var r in results)
        {
            bool? userAttacking = null;
            foreach (var e in r.Events)
            {
                switch (e)
                {
                    case BallPassed _: passed++; break;
                    case PassIntercepted _: intercepted++; break;
                    case DuelTriggered t: userAttacking = userPlayerIds.Contains(t.CarrierId); break;
                    case DuelEnded de when userAttacking.HasValue:
                        bool attackerWon = de.Outcome == DuelOutcome.AttackerWon;
                        if (userAttacking.Value) { atkDuels++; if (!attackerWon) atkWon++; }
                        else { defDuels++; if (!attackerWon) defWon++; }
                       
                        userAttacking = null;
                        break;
                }
            }
        }
        double passes = (passed + intercepted) / (double)n;
        string rate = passed + intercepted == 0 ? "-" : "%" + Pct(passed, passed + intercepted);
        lines.Add($"Ortalama düello: {Avg(results.Average(r => r.Duels))}  hamle: {Avg(results.Average(r => r.Moves))}  pas: {Avg(passes)}  pas başarısı: {rate}");
        lines.Add($"En uzun aynı ikili düello serisi: {results.Max(r => r.MaxSameDuelStreak)}");
        lines.Add($"Düello (kullanıcı hücumda): {Avg(atkDuels / (double)n)}/maç, savunan kazanır %{PctOrDash(atkWon, atkDuels)}");
        lines.Add($"Düello (kullanıcı savunmada): {Avg(defDuels / (double)n)}/maç, savunan kazanır %{PctOrDash(defWon, defDuels)}");
        return lines;
    }

    static string PctOrDash(int c, int n) => n == 0 ? "-" : Pct(c, n);

    static string Pct(int count, int n) =>
        ((int)Math.Round(100.0 * count / n, MidpointRounding.AwayFromZero)).ToString(Inv);

    static string Avg(double v) => v.ToString("F2", Inv);
}
