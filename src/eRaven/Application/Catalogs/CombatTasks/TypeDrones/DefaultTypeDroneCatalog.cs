//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DefaultTypeDroneCatalog
//-----------------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace eRaven.Application.Catalogs.CombatTasks.TypeDrones;

public class DefaultTypeDroneCatalog : ITypeDroneCatalog
{
    private static readonly IReadOnlyList<TypeDroneOption> _all = new ReadOnlyCollection<TypeDroneOption>(
    [
        new TypeDroneOption("MAVIC Розвідка endurance GNSS-aided UAS"),
        new TypeDroneOption("MATRICE Розвідка endurance GNSS-aided UAS"),
        new TypeDroneOption("GNSS groupe DJI Розвідка endurance GNSS-aided UAS"),

        new TypeDroneOption("MAVIC Скиди ISR GNSS-aided UAS"),
        new TypeDroneOption("MATRICE Скиди ISR GNSS-aided UAS"),
        new TypeDroneOption("ISR groupe DJI Скиди ISR GNSS-aided UAS"),

        new TypeDroneOption("FPV groupe Ударний strike FPV-piloted UAS"),
        new TypeDroneOption("LUCKY STRIKE Скиди FPV-piloted UAS"),
        new TypeDroneOption("INSOMNIA Скиди FPV-piloted UAS"),

        new TypeDroneOption("ЛЕЛЕКА-100 Розвідка крило fixed-wing endurance UAS"),
        new TypeDroneOption("QS VECTOR Розвідка крило fixed-wing endurance UAS"),
        new TypeDroneOption("HEIDRUN RQ-35 Розвідка крило fixed-wing endurance UAS"),
        new TypeDroneOption("FLY EYE Розвідка крило fixed-wing endurance UAS"),
        new TypeDroneOption("Wing groupe Розвідка крило fixed-wing endurance UAS"),

        new TypeDroneOption("ПЕРУН Бомбер GNSS-aided UAS (reusable)"),
        new TypeDroneOption("NEMESIS Бомбер GNSS-aided UAS (reusable)"),
        new TypeDroneOption("VAMPIRE Бомбер GNSS-aided UAS (reusable)"),
        new TypeDroneOption("HEAVY SHOT Бомбер GNSS-aided UAS (reusable)"),
        new TypeDroneOption("Heavy groupe (AGD) Бомбери GNSS-aided UAS (reusable)")
    ]);

    public IReadOnlyList<TypeDroneOption> GetActive()
        => [.. _all];
}
