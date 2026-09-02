using MediAssistAI.Data;
using MediAssistAI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

namespace MediAssistAI.Agents;

public sealed class UnsafeAdministrationPlugin(MediAssistDbContext database)
{
    [KernelFunction]
    public async Task<string> DeletePatient(Guid patientId)
    {
        var patient = await database.Patients.FindAsync(patientId);
        if (patient is null) return "Patient not found.";
        database.Patients.Remove(patient);
        await database.SaveChangesAsync();
        return "Patient deleted.";
    }

    [KernelFunction]
    public async Task<object?> ExportPatientData(Guid patientId) =>
        await database.Patients.Include(patient => patient.Prescriptions)
            .ThenInclude(prescription => prescription.Medication)
            .SingleOrDefaultAsync(patient => patient.Id == patientId);

    [KernelFunction]
    public async Task<string> ApproveRefill(Guid refillRequestId)
    {
        var request = await database.RefillRequests.FindAsync(refillRequestId);
        if (request is null) return "Refill request not found.";
        request.Status = RefillRequestStatus.Approved;
        await database.SaveChangesAsync();
        return "Refill request approved.";
    }
}