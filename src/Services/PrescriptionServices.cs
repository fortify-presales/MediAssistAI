using MediAssistAI.Models;

namespace MediAssistAI.Services;

public interface IPrescriptionRepository
{
	Task<IReadOnlyList<Prescription>> GetForPatientAsync(string patientSubject, CancellationToken cancellationToken);
}

public interface IPrescriptionService
{
	Task<IReadOnlyList<PrescriptionSummary>> GetForPatientAsync(string patientSubject, CancellationToken cancellationToken);
}

public sealed class PrescriptionService(IPrescriptionRepository repository) : IPrescriptionService
{
	public async Task<IReadOnlyList<PrescriptionSummary>> GetForPatientAsync(string patientSubject, CancellationToken cancellationToken)
	{
		var prescriptions = await repository.GetForPatientAsync(patientSubject, cancellationToken);
		return prescriptions.Select(prescription => new PrescriptionSummary(
			prescription.Id, prescription.Medication.DisplayName, prescription.Medication.GenericName,
			prescription.ReferenceNumber, prescription.Status.ToString(), prescription.RefillsRemaining,
			prescription.NextEligibleRefillDate)).ToList();
	}
}

public sealed record PrescriptionSummary(Guid Id, string MedicationName, string GenericMedicationName,
	string ReferenceNumber, string Status, int RefillsRemaining, DateOnly NextEligibleRefillDate);

public interface IAgentChatClient
{
	Task<string> CompleteAsync(string userMessage, CancellationToken cancellationToken);
}

public interface IRefillRepository
{
	Task<RefillRequestResult> RequestAsync(string patientSubject, Guid prescriptionId, DateOnly requestDate, CancellationToken cancellationToken);
	Task<RefillRequestResult> GetStatusAsync(string patientSubject, Guid refillRequestId, CancellationToken cancellationToken);
}

public interface IRefillService
{
	Task<RefillRequestResult> RequestAsync(string patientSubject, Guid prescriptionId, CancellationToken cancellationToken);
	Task<RefillRequestResult> GetStatusAsync(string patientSubject, Guid refillRequestId, CancellationToken cancellationToken);
}

public sealed class RefillService(IRefillRepository repository, TimeProvider timeProvider) : IRefillService
{
	public Task<RefillRequestResult> RequestAsync(string patientSubject, Guid prescriptionId, CancellationToken cancellationToken) =>
		repository.RequestAsync(patientSubject, prescriptionId, DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime), cancellationToken);

	public Task<RefillRequestResult> GetStatusAsync(string patientSubject, Guid refillRequestId, CancellationToken cancellationToken) =>
		repository.GetStatusAsync(patientSubject, refillRequestId, cancellationToken);
}

public sealed record RefillRequestResult(Guid? RequestId, string Status, string Message)
{
	public bool IsSuccessful => RequestId is not null;
}