using MediAssistAI.Models;
using Microsoft.EntityFrameworkCore;

namespace MediAssistAI.Data;

public sealed class MediAssistDbContext(DbContextOptions<MediAssistDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<RefillRequest> RefillRequests => Set<RefillRequest>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(entity => { entity.HasKey(patient => patient.Id); entity.Property(patient => patient.Subject).HasMaxLength(100).IsRequired(); entity.Property(patient => patient.DisplayName).HasMaxLength(200).IsRequired(); entity.HasIndex(patient => patient.Subject).IsUnique(); });
        modelBuilder.Entity<Medication>(entity => { entity.HasKey(medication => medication.Id); entity.Property(medication => medication.GenericName).HasMaxLength(200).IsRequired(); entity.Property(medication => medication.DisplayName).HasMaxLength(200).IsRequired(); entity.Property(medication => medication.Information).HasMaxLength(2_000).IsRequired(); });
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasKey(prescription => prescription.Id);
            entity.Property(prescription => prescription.ReferenceNumber).HasMaxLength(100).IsRequired();
            entity.HasIndex(prescription => new { prescription.PatientId, prescription.Status });
            entity.HasOne(prescription => prescription.Patient).WithMany(patient => patient.Prescriptions).HasForeignKey(prescription => prescription.PatientId);
            entity.HasOne(prescription => prescription.Medication).WithMany(medication => medication.Prescriptions).HasForeignKey(prescription => prescription.MedicationId);
        });
        modelBuilder.Entity<RefillRequest>(entity =>
        {
            entity.HasKey(request => request.Id);
            entity.HasIndex(request => new { request.PrescriptionId, request.Status });
            entity.HasOne(request => request.Patient).WithMany().HasForeignKey(request => request.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(request => request.Prescription).WithMany(prescription => prescription.RefillRequests).HasForeignKey(request => request.PrescriptionId);
        });
        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(auditEvent => auditEvent.Id);
            entity.Property(auditEvent => auditEvent.Subject).HasMaxLength(100).IsRequired();
            entity.Property(auditEvent => auditEvent.Action).HasMaxLength(100).IsRequired();
            entity.Property(auditEvent => auditEvent.TargetType).HasMaxLength(100).IsRequired();
            entity.Property(auditEvent => auditEvent.Outcome).HasMaxLength(100).IsRequired();
            entity.HasIndex(auditEvent => auditEvent.OccurredAtUtc);
        });
    }
}

public static class SyntheticDataSeeder
{
    public static async Task SeedAsync(MediAssistDbContext database, CancellationToken cancellationToken = default)
    {
        if (await database.Patients.AnyAsync(cancellationToken)) return;
        var patient = new Patient { Id = Guid.Parse("8cbbbce4-24ba-4ea9-94b8-38a8971c0b76"), Subject = "synthetic-patient-alex", DisplayName = "Alex Example" };
        var secondPatient = new Patient { Id = Guid.Parse("0d0e1776-789f-438a-a0e7-4cadcd30d619"), Subject = "synthetic-patient-jamie", DisplayName = "Jamie Example" };
        var medication = new Medication { Id = Guid.Parse("de4d6616-9770-40f2-b7ec-4853393580b8"), GenericName = "metformin", DisplayName = "Metformin", Information = "Synthetic demonstration medication information only. This application does not provide clinical advice." };
        database.AddRange(patient, secondPatient, medication, new Prescription { Id = Guid.Parse("71d78772-4cac-41b4-b328-2b3f9e5ee2e3"), Patient = patient, Medication = medication, ReferenceNumber = "RX-SYN-1001", Status = PrescriptionStatus.Active, RefillsRemaining = 2, NextEligibleRefillDate = DateOnly.FromDateTime(DateTime.UtcNow.Date) });
        await database.SaveChangesAsync(cancellationToken);
    }
}