//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Plan
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Models;

/// <summary>
/// План разової дії: опис/момент часу (квотований по 15 хв), номер, тип, місце, група, інструмент, склад.
/// Статус: Open → Close (закривається наказом).
/// </summary>
public class Plan
{
    public Guid Id { get; set; }

    /// <summary>Людський номер плану (унікальний у межах періоду/організації — як домовитеся).</summary>
    public string PlanNumber { get; set; } = default!;

    /// <summary>Тип плану: відрядити / повернути.</summary>
    public PlanType Type { get; set; }

    /// <summary>
    /// Опорний момент часу (UTC) з кроком 15 хв.
    /// Для Type=Dispatch зазвичай це початок (Start), для Type=Return — кінець (End).
    /// </summary>
    public DateTime PlannedAtUtc { get; set; }

    /// <summary>Позначка, що саме означає PlannedAtUtc.</summary>
    public PlanTimeKind TimeKind { get; set; }

    /// <summary>Локація виконання (куди/де).</summary>
    public string? Location { get; set; }

    /// <summary>Назва робочої групи (група/екіпаж).</summary>
    public string? GroupName { get; set; }

    /// <summary>Тип робочого засобу / інструмент.</summary>
    public string? ToolType { get; set; }

    /// <summary>Список залучених людей (снапшот на момент плану).</summary>
    public ICollection<PlanParticipantSnapshot> Participants { get; set; } = [];

    /// <summary>Необов’язковий короткий опис робіт/призначення.</summary>
    public string? TaskDescription { get; set; }

    /// <summary>Стан плану: Open → Close (Close робиться наказом).</summary>
    public PlanState State { get; set; } = PlanState.Open;

    /// <summary>Службові поля аудиту.</summary>
    public string? Author { get; set; }
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;

    // -------------------- невеликі перевірочні хелпери (опційно) --------------------

    /// <summary>Перевіряє, що PlannedAtUtc має крок 15 хв (00/15/30/45).</summary>
    public static bool IsQuarterAligned(DateTime dtUtc)
        => dtUtc.Minute % 15 == 0 && dtUtc.Second == 0 && dtUtc.Millisecond == 0;

    /// <summary>Кине виняток, якщо PlannedAtUtc не кратний 15 хв.</summary>
    public void EnsureQuarterAligned()
    {
        if (!IsQuarterAligned(PlannedAtUtc))
            throw new InvalidOperationException("Час плану має бути в інтервалах 00/15/30/45 хв без секунд.");
    }
}