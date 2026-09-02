using MediAssistAI.Models;
using MediAssistAI.Services;
using Microsoft.EntityFrameworkCore;

namespace MediAssistAI.Data;

public sealed class EfRefillRepository(MediAssistDbContext database, TimeProvider timeProvider) : IRefillRepository
{
    public async Task<RefillRequestResult> GetStatusAsync(string patientSubject, Guid refillRequestId, CancellationToken cancellationToken)
    {
        var request = await database.RefillRequests.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == refillRequestId && item.Patient.Subject == patientSubject, cancellationToken);
        return request is null
            ? new RefillRequestResult(null, "NotFound", "The requested synthetic refill request was not found.")
            : new RefillRequestResult(request.Id, request.Status.ToString(), "Synthetic refill request status retrieved.");
    }

    public async Task<RefillRequestResult> RequestAsync(
        string patientSubject,
        Guid prescriptionId,
        DateOnly requestDate,
        CancellationToken cancellationToken)
    {
        var prescription = await database.Prescriptions
            .Include(item => item.Patient)
            .SingleOrDefaultAsync(item => item.Id == prescriptionId && item.Patient.Subject == patientSubject, cancellationToken);

        if (prescription is null)
        {
            return new RefillRequestResult(null, "NotFound", "The requested synthetic prescription was not found.");
        }

        if (prescription.Status != PrescriptionStatus.Active || prescription.RefillsRemaining <= 0 || prescription.NextEligibleRefillDate > requestDate)
        {
            await WriteAuditEventAsync(patientSubject, prescriptionId, "Rejected", cancellationToken);
            return new RefillRequestResult(null, "Ineligible", "This prescription is not currently eligible for a refill request.");
        }

        var existingRequest = await database.RefillRequests
            .SingleOrDefaultAsync(item => item.PrescriptionId == prescriptionId && item.Status == RefillRequestStatus.Pending, cancellationToken);

        if (existingRequest is not null)
        {
            return new RefillRequestResult(existingRequest.Id, existingRequest.Status.ToString(), "A refill request is already pending.");
        }

        var refillRequest = new RefillRequest
        {
            Id = Guid.NewGuid(),
            PatientId = prescription.PatientId,
            PrescriptionId = prescription.Id,
            Status = RefillRequestStatus.Pending,
            RequestedAtUtc = timeProvider.GetUtcNow()
        };
        database.RefillRequests.Add(refillRequest);
        await WriteAuditEventAsync(patientSubject, refillRequest.Id, "Created", cancellationToken);
        await database.SaveChangesAsync(cancellationToken);

        return new RefillRequestResult(refillRequest.Id, refillRequest.Status.ToString(), "A synthetic refill request has been submitted for review.");
    }

    private Task WriteAuditEventAsync(string subject, Guid targetId, string outcome, CancellationToken cancellationToken)
    {
        database.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            Subject = subject,
            Action = "RequestRefill",
            TargetType = "RefillRequest",
            TargetId = targetId,
            Outcome = outcome,
            OccurredAtUtc = timeProvider.GetUtcNow()
        });
        return Task.CompletedTask;
    }
}