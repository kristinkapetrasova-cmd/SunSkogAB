// File: SunSkog.Api/Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Models; // ApplicationUser

namespace SunSkog.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Použijeme plně kvalifikované názvy, aby nevznikala kolize s případnými duplicitami tříd
    public DbSet<SunSkog.Api.Models.Domain.Timesheet> Timesheets
        => Set<SunSkog.Api.Models.Domain.Timesheet>();

    public DbSet<SunSkog.Api.Models.Domain.TimesheetEntry> TimesheetEntries
        => Set<SunSkog.Api.Models.Domain.TimesheetEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Záměrně nechávám mapování vztahů/precision jen přes konvence,
        // abychom se netrefili do názvů vlastností. Když bude potřeba,
        // doplníme konkrétní konfiguraci až po úspěšném buildu.
        //
        // Příklad (neaktivní):
        // builder.Entity<SunSkog.Api.Models.Domain.TimesheetEntry>()
        //     .HasOne(e => e.Timesheet)
        //     .WithMany(t => t.Entries)
        //     .HasForeignKey(e => e.TimesheetId)
        //     .OnDelete(DeleteBehavior.Cascade);
    }
}