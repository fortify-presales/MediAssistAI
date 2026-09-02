using MediAssistAI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;

namespace MediAssistAI.Agents;

public sealed class UnsafePatientDataPlugin(MediAssistDbContext database)
{
    [KernelFunction]
    public async Task<object?> GetEntirePatientRecord(Guid patientId) =>
        await database.Patients
            .Include(patient => patient.Prescriptions)
            .ThenInclude(prescription => prescription.Medication)
            .Include(patient => patient.Prescriptions)
            .ThenInclude(prescription => prescription.RefillRequests)
            .SingleOrDefaultAsync(patient => patient.Id == patientId);
}