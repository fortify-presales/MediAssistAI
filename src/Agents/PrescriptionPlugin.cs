using MediAssistAI.Security;
using MediAssistAI.Services;
using Microsoft.SemanticKernel;

namespace MediAssistAI.Agents;

public sealed class PrescriptionPlugin(IPatientContext patientContext, IPrescriptionService prescriptionService, IRefillService refillService)
{
    [KernelFunction]
    public async Task<IReadOnlyList<PrescriptionSummary>> GetPrescriptions(CancellationToken cancellationToken) =>
        await prescriptionService.GetForPatientAsync(patientContext.Subject ?? throw new UnauthorizedAccessException(), cancellationToken);

    [KernelFunction]
    public async Task<RefillRequestResult> RequestRefill(Guid prescriptionId, CancellationToken cancellationToken) =>
        await refillService.RequestAsync(patientContext.Subject ?? throw new UnauthorizedAccessException(), prescriptionId, cancellationToken);

    [KernelFunction]
    public async Task<RefillRequestResult> GetRefillStatus(Guid refillRequestId, CancellationToken cancellationToken) =>
        await refillService.GetStatusAsync(patientContext.Subject ?? throw new UnauthorizedAccessException(), refillRequestId, cancellationToken);
}