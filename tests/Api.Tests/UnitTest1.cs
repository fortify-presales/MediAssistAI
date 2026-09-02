using MediAssistAI.Data;
using MediAssistAI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MediAssistAI.Api.Tests;

public sealed class HealthEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetHealth_ReturnsSuccessStatusCode()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetPrescriptions_WithoutToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/prescriptions");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSwaggerDocument_GroupsPatientSecurityTestingOperations()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
        using var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var operation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/patients/search")
            .GetProperty("get");

        Assert.Equal("Patients", operation.GetProperty("tags")[0].GetString());
        Assert.Contains("Security Testing endpoint", operation.GetProperty("description").GetString());
    }

    [Fact]
    public async Task RequestRefill_ForEligiblePrescription_IsIdempotentAndAudited()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<MediAssistDbContext>().UseSqlite(connection).Options;
        await using var database = new MediAssistDbContext(options);
        await database.Database.EnsureCreatedAsync();
        await SyntheticDataSeeder.SeedAsync(database);
        var repository = new EfRefillRepository(database, TimeProvider.System);

        var first = await repository.RequestAsync(
            "synthetic-patient-alex",
            Guid.Parse("71d78772-4cac-41b4-b328-2b3f9e5ee2e3"),
            DateOnly.FromDateTime(DateTime.UtcNow),
            CancellationToken.None);
        var second = await repository.RequestAsync(
            "synthetic-patient-alex",
            Guid.Parse("71d78772-4cac-41b4-b328-2b3f9e5ee2e3"),
            DateOnly.FromDateTime(DateTime.UtcNow),
            CancellationToken.None);

        Assert.Equal("Pending", first.Status);
        Assert.Equal(first.RequestId, second.RequestId);
        Assert.Equal(1, await database.RefillRequests.CountAsync());
        Assert.Single(await database.AuditEvents.Where(item => item.Action == "RequestRefill").ToListAsync());
    }
}
