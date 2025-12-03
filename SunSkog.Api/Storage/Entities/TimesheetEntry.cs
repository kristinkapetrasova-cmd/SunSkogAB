using System;

namespace SunSkog.Api.Storage.Entities;

public class TimesheetEntry
{
    public Guid Id { get; set; }

    // FK na timesheet
    public Guid TimesheetId { get; set; }
    public Timesheet? Timesheet { get; set; }

    // Datum práce
    public DateOnly WorkDate { get; set; }

    // ====== Pole z Excelu (identifikace plochy / objektu) ======
    // CZ: Název Plochy | SWE: Objekt namn
    public string? AreaName { get; set; }

    // CZ: Číslo plochy | SWE: Objekt ID
    public string? AreaCode { get; set; }

    // ====== Časy a pauzy ======
    // CZ: Od / Do
    public TimeOnly? FromTime { get; set; }
    public TimeOnly? ToTime { get; set; }

    // CZ: Pauza (minuty)
    public int? PauseMinutes { get; set; }

    // ====== Měření výkonu ======
    // CZ: Hodiny / Km / Kusy (resp. litry lze mapovat do Pieces)
    public decimal Hours { get; set; }
    public decimal Km { get; set; }
    public int Pieces { get; set; }

    // Sazby (hodinová / km / kusová)
    public decimal HourRate { get; set; }
    public decimal KmRate { get; set; }
    public decimal PieceRate { get; set; }

    // Úkolovka na hektary (Excel: Antal ha, Pris per hektar)
    public decimal? Hectares { get; set; }
    public decimal? HectareRate { get; set; }

    // Volitelné uložení vypočtené hodnoty za hektary
    public decimal? HectarePay { get; set; }

    // TR Druh / Příplatek / Nanoška (Bära lådor)
    public string? TrKind { get; set; }
    public string? ExtraNote { get; set; }
    public int? BoxCarryCount { get; set; }

    // Cesta / Färdtid (minuty)
    public int? TravelMinutes { get; set; }

    // Celkem (součet) – můžeš počítat runtime, pro prototyp ukládáme
    public decimal EntryPay { get; set; }

    // Ponechané interní sloupce
    public string? Project { get; set; }
    public string? Task { get; set; }
    public string? Comment { get; set; }
}