using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SunSkog.Api.Models;
using SunSkog.Api.Storage.Entities;
using InventoryItemEntity = SunSkog.Api.Storage.Entities.InventoryItem;

namespace SunSkog.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ===== Timesheets =====
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<TimesheetEntry> TimesheetEntries => Set<TimesheetEntry>();
    public DbSet<ApprovalLog> ApprovalLogs => Set<ApprovalLog>();

    // ===== Inventory =====
    public DbSet<InventoryItemEntity> InventoryItems => Set<InventoryItemEntity>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Category> Categories => Set<Category>();  // NOVÉ

    // ===== Org / Teams =====
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMembership> TeamMemberships => Set<TeamMembership>();

    // ===== Rates =====
    public DbSet<Rate> Rates => Set<Rate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --------------------
        // Timesheet
        // --------------------
        modelBuilder.Entity<Timesheet>(b =>
        {
            b.ToTable("Timesheets");
            b.HasKey(x => x.Id);

            b.Property(x => x.EmployeeId)
                .IsRequired();

            b.Property(x => x.PeriodStart).HasColumnType("date");
            b.Property(x => x.PeriodEnd).HasColumnType("date");

            // agregované částky -> dec(18,2)
            b.Property(x => x.TotalHours).HasPrecision(18, 2);
            b.Property(x => x.TotalKm).HasPrecision(18, 2);
            b.Property(x => x.TotalPay).HasPrecision(18, 2);

            b.HasMany(x => x.Entries)
                .WithOne(x => x.Timesheet)
                .HasForeignKey(x => x.TimesheetId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.EmployeeId, x.PeriodStart, x.PeriodEnd });
        });

        // --------------------
        // TimesheetEntry
        // --------------------
        modelBuilder.Entity<TimesheetEntry>(b =>
        {
            b.ToTable("TimesheetEntries");
            b.HasKey(x => x.Id);

            b.Property(x => x.WorkDate).HasColumnType("date");

            // jednotkové sazby a výpočty
            b.Property(x => x.Hours).HasPrecision(18, 2);
            b.Property(x => x.Km).HasPrecision(18, 2);
            b.Property(x => x.HourRate).HasPrecision(18, 2);
            b.Property(x => x.KmRate).HasPrecision(18, 2);
            b.Property(x => x.PieceRate).HasPrecision(18, 2);
            b.Property(x => x.EntryPay).HasPrecision(18, 2);

            // volitelné extra sloupce – pokud jsou v modelu
            b.Property(x => x.FromTime);
            b.Property(x => x.ToTime);
            b.Property(x => x.TravelMinutes);
            b.Property(x => x.PauseMinutes);
            b.Property(x => x.TrKind);
            b.Property(x => x.AreaCode);
            b.Property(x => x.AreaName);
            b.Property(x => x.Hectares).HasPrecision(18, 2);
            b.Property(x => x.HectareRate).HasPrecision(18, 2);
            b.Property(x => x.HectarePay).HasPrecision(18, 2);
            b.Property(x => x.BoxCarryCount);
            b.Property(x => x.ExtraNote);

            b.HasIndex(x => new { x.TimesheetId, x.WorkDate });
        });

        // --------------------
        // ApprovalLog
        // --------------------
        modelBuilder.Entity<ApprovalLog>(b =>
        {
            b.ToTable("ApprovalLogs");
            b.HasKey(x => x.Id);

            b.Property(x => x.At).IsRequired();

            // enum se mapuje jako int (default)
            b.Property(x => x.Action).HasConversion<int>();

            b.HasOne<Timesheet>()
                .WithMany()
                .HasForeignKey(x => x.TimesheetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --------------------
        // Category (NOVÉ)
        // --------------------
        modelBuilder.Entity<Category>(b =>
        {
            b.ToTable("Categories");
            b.HasKey(x => x.Id);

            b.Property(x => x.Name).IsRequired().HasMaxLength(100);
            b.Property(x => x.NameEn).HasMaxLength(100);
            b.Property(x => x.HasSizes).HasDefaultValue(false);
            b.Property(x => x.HasItemTypes).HasDefaultValue(false);
            b.Property(x => x.SortOrder).HasDefaultValue(0);
            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasMany(x => x.Items)
                .WithOne(x => x.Category)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => x.Name);
        });

        // --------------------
        // InventoryItem
        // --------------------
        modelBuilder.Entity<InventoryItemEntity>(b =>
        {
            b.ToTable("InventoryItems");
            b.HasKey(x => x.Id);

            b.Property(x => x.Name).IsRequired().HasMaxLength(200);
            b.Property(x => x.SKU).HasMaxLength(50);
            b.Property(x => x.SerialNumber).HasMaxLength(100);
            b.Property(x => x.Size).HasMaxLength(20);
            b.Property(x => x.ItemType).HasMaxLength(100);
            b.Property(x => x.MinStock).HasDefaultValue(0);
            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasIndex(x => x.SerialNumber);
            b.HasIndex(x => x.SKU);
            b.HasIndex(x => x.Name);
            b.HasIndex(x => x.CategoryId);
        });

        // --------------------
        // StockMovement
        // --------------------
        modelBuilder.Entity<StockMovement>(b =>
        {
            b.ToTable("StockMovements");
            b.HasKey(x => x.Id);

            b.Property(x => x.At).IsRequired();
            b.Property(x => x.Quantity).HasPrecision(18, 2);
            b.Property(x => x.Note).HasMaxLength(500);

            b.Property(x => x.Type)
                .HasConversion<int>();

            b.HasOne(x => x.Item)
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Team)
                .WithMany()
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasIndex(x => x.ItemId);
            b.HasIndex(x => x.At);
        });

        // --------------------
        // Assignment
        // --------------------
        modelBuilder.Entity<Assignment>(b =>
        {
            b.ToTable("Assignments");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.Item)
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasOne(x => x.Team)
                .WithMany()
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.SetNull);

            b.Property(x => x.AssignedAt).IsRequired();
            
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.TeamId);
        });

        // --------------------
        // Rate
        // --------------------
        modelBuilder.Entity<Rate>(b =>
        {
            b.ToTable("Rates");
            b.HasKey(x => x.Id);

            b.Property(x => x.HourRate).HasPrecision(18, 2);
            b.Property(x => x.KmRate).HasPrecision(18, 2);
            b.Property(x => x.PieceRate).HasPrecision(18, 2);

            b.Property(x => x.ValidFrom).HasColumnType("date");
            b.Property(x => x.ValidTo).HasColumnType("date");
            
            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.ChangedByUserId).HasMaxLength(450);
            
            b.HasOne(x => x.ChangedByUser)
                .WithMany()
                .HasForeignKey(x => x.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
                
            b.HasIndex(x => x.ValidFrom);
        });

        // --------------------
        // Team
        // --------------------
        modelBuilder.Entity<Team>(b =>
        {
            b.ToTable("Teams");
            b.HasKey(x => x.Id);

            b.Property(x => x.Name).IsRequired();
            b.HasIndex(x => x.Name).IsUnique(false);
        });

        // --------------------
        // TeamMembership
        // --------------------
        modelBuilder.Entity<TeamMembership>(b =>
        {
            b.ToTable("TeamMemberships");
            b.HasKey(x => x.Id);

            b.HasOne(x => x.Team)
                .WithMany()
                .HasForeignKey(x => x.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Property(x => x.FromDate).HasColumnType("date");
            b.Property(x => x.ToDate).HasColumnType("date");

            b.Property(x => x.Role)
                .HasMaxLength(32)
                .IsRequired()
                .HasDefaultValue("Member");

            b.HasIndex(x => new { x.TeamId, x.UserId, x.FromDate });
        });
    }
}