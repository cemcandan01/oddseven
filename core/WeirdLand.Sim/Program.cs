using System.Globalization;
using WeirdLand.Core;
using WeirdLand.Core.Ai;
using WeirdLand.Core.Data;
using WeirdLand.Core.Match;
using WeirdLand.Sim;

const string Usage =
    "Kullanım: dotnet run --project WeirdLand.Sim -- [--matches N] [--seed S] [--policy good|random] [--user ID] [--opp ID|all] [--data DIR]\n" +
    "Varsayılanlar: --matches 1000 --seed 1 --policy good --user mavi --opp kirmizi --data ../data";

int matches = 1000;
uint seed = 1;
string policyName = "good", userId = "mavi", oppId = "kirmizi", dataDir = "../data";

for (int i = 0; i < args.Length; i += 2)
{
    if (i + 1 >= args.Length) return Fail();
    string v = args[i + 1];
    switch (args[i])
    {
        case "--matches":
            if (!int.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out matches) || matches < 1) return Fail();
            break;
        case "--seed":
            if (!uint.TryParse(v, NumberStyles.None, CultureInfo.InvariantCulture, out seed)) return Fail();
            break;
        case "--policy":
            if (v != "good" && v != "random") return Fail();
            policyName = v;
            break;
        case "--user": userId = v; break;
        case "--opp": oppId = v; break;
        case "--data": dataDir = v; break;
        default: return Fail();
    }
}

int Fail()
{
    Console.Error.WriteLine(Usage);
    return 1;
}

GameData data;
try { data = DataLoader.Load(dataDir); }
catch (Exception e)
{
    Console.Error.WriteLine($"Veri yüklenemedi ({dataDir}): {e.Message}");
    return 1;
}

if (!data.Teams.TryGetValue(userId, out var user))
{
    Console.Error.WriteLine($"Bilinmeyen takım: {userId}");
    return 1;
}

List<TeamDef> opponents;
if (oppId == "all")
    opponents = data.Teams.Values.Where(t => t.Id != userId)
        .OrderBy(t => t.Tier).ThenBy(t => t.Id, StringComparer.Ordinal).ToList();
else if (data.Teams.TryGetValue(oppId, out var o))
    opponents = new List<TeamDef> { o };
else
{
    Console.Error.WriteLine($"Bilinmeyen takım: {oppId}");
    return 1;
}

IUserPolicy policy = policyName == "good" ? new GoodPolicy() : new RandomPolicy();
Console.OutputEncoding = new System.Text.UTF8Encoding(false);

bool first = true;
foreach (var opp in opponents)
{
    var results = new List<MatchResult>();
    for (int i = 0; i < matches; i++)
    {
        uint s = unchecked(seed + (uint)i);
        var engine = new MatchEngine(data, user, opp, s);
        // Policy stream derived from the match seed, so it is not a copy of the engine's stream.
        results.Add(MatchRunner.Play(engine, policy, new Rng(s ^ 0x5bd1e995u)));
    }
    if (!first) Console.WriteLine();
    first = false;
    foreach (var line in SimReport.Build(user.Id, opp.Id, policyName, seed, results))
        Console.WriteLine(line);
}
return 0;
