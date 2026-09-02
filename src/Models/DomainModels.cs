namespace MediAssistAI.Models;

public sealed class Patient
{
	public Guid Id { get; set; }
	public required string Subject { get; set; }
	public required string DisplayName { get; set; }
	public ICollection<Prescription> Prescriptions { get; } = new List<Prescription>();
}

public sealed class Medication
{
	public Guid Id { get; set; }
	public required string GenericName { get; set; }
	public required string DisplayName { get; set; }
	public required string Information { get; set; }
	public ICollection<Prescription> Prescriptions { get; } = new List<Prescription>();
}

public sealed class Prescription
{
	public Guid Id { get; set; }
	public Guid PatientId { get; set; }
	public Patient Patient { get; set; } = null!;
	public Guid MedicationId { get; set; }
	public Medication Medication { get; set; } = null!;
	public required string ReferenceNumber { get; set; }
	public PrescriptionStatus Status { get; set; }
	public int RefillsRemaining { get; set; }
	public DateOnly NextEligibleRefillDate { get; set; }
	public ICollection<RefillRequest> RefillRequests { get; } = new List<RefillRequest>();
}

public enum PrescriptionStatus { Active, Expired, Cancelled }

public sealed class RefillRequest
{
	public Guid Id { get; set; }
	public Guid PatientId { get; set; }
	public Patient Patient { get; set; } = null!;
	public Guid PrescriptionId { get; set; }
	public Prescription Prescription { get; set; } = null!;
	public RefillRequestStatus Status { get; set; }
	public DateTimeOffset RequestedAtUtc { get; set; }
}

public enum RefillRequestStatus { Pending, Approved, Rejected, Cancelled }

public sealed class AuditEvent
{
	public Guid Id { get; set; }
	public required string Subject { get; set; }
	public required string Action { get; set; }
	public required string TargetType { get; set; }
	public Guid TargetId { get; set; }
	public required string Outcome { get; set; }
	public DateTimeOffset OccurredAtUtc { get; set; }
}