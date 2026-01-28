//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DefaultTypeDroneCatalog
//-----------------------------------------------------------------------------

using System.Collections.ObjectModel;

namespace eRaven.Application.Catalogs.CombatTask.TypeDrones;

public class DefaultTypeDroneCatalog : ITypeDroneCatalog
{
    private static readonly IReadOnlyList<TypeDroneOption> _all = new ReadOnlyCollection<TypeDroneOption>(
    [
        new TypeDroneOption("MAVIC Розвідка Class I multirotor endurance GNSS-aided UAS"),
        new TypeDroneOption("MATRICE Розвідка Class I multirotor endurance GNSS-aided UAS"),
        new TypeDroneOption("GNSS groupe DJI Розвідка Class I multirotor endurance GNSS-aided UAS"),

        new TypeDroneOption("MAVIC Скиди Class I multirotor ISR GNSS-aided UAS"),
        new TypeDroneOption("MATRICE Скиди Class I multirotor ISR GNSS-aided UAS"),
        new TypeDroneOption("ISR groupe DJI Скиди Class I multirotor ISR GNSS-aided UAS"),

        new TypeDroneOption("FPV groupe Ударний Class I multirotor strike FPV-piloted UAS"),
        new TypeDroneOption("LUCKY STRIKE Скиди Class I multirotor strike FPV-piloted UAS"),
        new TypeDroneOption("INSOMNIA Скиди Class I multirotor strike FPV-piloted UAS"),

        new TypeDroneOption("ЛЕЛЕКА-100 Розвідка крило Class I fixed-wing endurance UAS"),
        new TypeDroneOption("QS VECTOR Розвідка крило Class I fixed-wing endurance UAS"),
        new TypeDroneOption("HEIDRUN RQ-35 Розвідка крило Class I fixed-wing endurance UAS"),
        new TypeDroneOption("FLY EYE Розвідка крило Class I fixed-wing endurance UAS"),
        new TypeDroneOption("Wing groupe Розвідка крило Class I fixed-wing endurance UAS"),

        new TypeDroneOption("ПЕРУН Бомбер Class I multirotor bomber GNSS-aided UAS (reusable)"),
        new TypeDroneOption("NEMESIS Бомбер Class I multirotor bomber GNSS-aided UAS (reusable)"),
        new TypeDroneOption("VAMPIRE Бомбер Class I multirotor bomber GNSS-aided UAS (reusable)"),
        new TypeDroneOption("HEAVY SHOT Бомбер Class I multirotor bomber GNSS-aided UAS (reusable)"),
        new TypeDroneOption("Heavy groupe (AGD) Бомбери Class I multirotor bomber GNSS-aided UAS (reusable)")
    ]);

    public IReadOnlyList<TypeDroneOption> GetActive()
        => [.. _all];
}
