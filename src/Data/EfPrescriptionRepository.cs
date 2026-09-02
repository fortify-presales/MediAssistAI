using MediAssistAI.Models;
using MediAssistAI.Services;
using Microsoft.EntityFrameworkCore;

namespace MediAssistAI.Data;

public sealed class EfPrescriptionRepository(MediAssistDbContext database) : IPrescriptionRepository
{
    public async Task<IReadOnlyList<Prescription>> GetForPatientAsync(string patientSubject, CancellationToken cancellationToken) =>
        await database.Prescriptions.AsNoTracking().Where(prescription => prescription.Patient.Subject == patientSubject)
            .Include(prescription => prescription.Medication).OrderBy(prescription => prescription.Medication.DisplayName).ToListAsync(cancellationToken);
}