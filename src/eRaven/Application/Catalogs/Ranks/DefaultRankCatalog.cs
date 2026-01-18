using System.Collections.ObjectModel;

namespace eRaven.Application.Catalogs.Ranks;

public sealed class DefaultRankCatalog : IRankCatalog
{
    private static readonly IReadOnlyList<RankOption> _all = new ReadOnlyCollection<RankOption>(
    [
        new RankOption("рекрут"),
        new RankOption("солдат"),
        new RankOption("старший солдат"),
        new RankOption("молодший сержант"),
        new RankOption("сержант"),
        new RankOption("старший сержант"),
        new RankOption("головний сержант"),
        new RankOption("штаб-сержант"),
        new RankOption("майстер-сержант"),
        new RankOption("старший майстер-сержант"),
        new RankOption("головний майстер-сержант"),
        new RankOption("молодший лейтенант"),
        new RankOption("лейтенант"),
        new RankOption("старший лейтенант"),
        new RankOption("капітан"),
        new RankOption("майор"),
        new RankOption("підполковник"),
        new RankOption("полковник"),
    ]);

    public IReadOnlyList<RankOption> GetActive()
        => [.. _all];
}