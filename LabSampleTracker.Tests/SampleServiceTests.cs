using LabSampleTracker.WebApi.Models;
using LabSampleTracker.WebApi.Repositories;
using LabSampleTracker.WebApi.Services;

namespace LabSampleTracker.Tests;

public class SampleServiceTests
{
    [Fact]
    public void GetAll_LoadsTheFiveCsvRecords()
    {
        var service = CreateService();

        var samples = service.GetAll();

        Assert.Equal(5, samples.Count);
        Assert.Equal(
            new[] { 1001, 1002, 1003, 1004, 1005 },
            samples.Select(sample => sample.Id).ToArray());
        Assert.Equal("CBC Morning Draw", samples[0].Name);
        Assert.Equal(SampleStatus.Pending, samples[0].Status);
        Assert.Equal(SampleStatus.Processing, samples[1].Status);
        Assert.Equal("Blood Culture Set", samples[4].Name);
    }

    [Fact]
    public void GetAll_ReadsTheCsvOnce_ThenUsesTheRuntimeCollection()
    {
        var service = CreateService();

        Assert.Equal(5, service.GetAll().Count);

        var added = service.Add(new Sample
        {
            Id = 1006,
            Name = "Iron Study",
            Status = SampleStatus.Pending
        });
        Assert.True(added.Succeeded);

        var removed = service.Delete(new[] { 1001 });
        Assert.Equal(1, removed);

        var samples = service.GetAll();
        Assert.Equal(5, samples.Count);
        Assert.DoesNotContain(samples, sample => sample.Id == 1001);
        Assert.Equal("Iron Study", samples[^1].Name);
        Assert.Equal(SampleStatus.Pending, samples[^1].Status);
    }

    [Fact]
    public void Add_AppendsASample()
    {
        var service = CreateService();

        var result = service.Add(new Sample
        {
            Id = 1006,
            Name = "  Iron Study  ",
            Status = SampleStatus.Processing
        });

        Assert.True(result.Succeeded);
        Assert.Equal("Iron Study", result.Value!.Name);

        var samples = service.GetAll();
        Assert.Equal(6, samples.Count);
        Assert.Equal(1006, samples[^1].Id);
        Assert.Equal(SampleStatus.Processing, samples[^1].Status);
    }

    [Fact]
    public void Add_RejectsADuplicateId()
    {
        var service = CreateService();

        var result = service.Add(new Sample
        {
            Id = 1001,
            Name = "Duplicate",
            Status = SampleStatus.Pending
        });

        Assert.False(result.Succeeded);
        Assert.Equal("A sample with id 1001 already exists.", result.Error);
        Assert.Equal(5, service.GetAll().Count);
    }

    [Fact]
    public void Add_RejectsAMissingName()
    {
        var service = CreateService();

        var result = service.Add(new Sample
        {
            Id = 1006,
            Name = "   ",
            Status = SampleStatus.Pending
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Name is required.", result.Error);
    }

    [Fact]
    public void Add_RejectsANonPositiveId()
    {
        var service = CreateService();

        var result = service.Add(new Sample
        {
            Id = 0,
            Name = "Missing id",
            Status = SampleStatus.Pending
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Id must be a positive number.", result.Error);
    }

    [Fact]
    public void Update_ChangesNameAndStatusWithoutAddingARow()
    {
        var service = CreateService();

        var result = service.Update(new Sample
        {
            Id = 1003,
            Name = "Respiratory PCR rerun",
            Status = SampleStatus.Processing
        });

        Assert.True(result.Succeeded);

        var samples = service.GetAll();
        Assert.Equal(5, samples.Count);

        var updated = samples.Single(sample => sample.Id == 1003);
        Assert.Equal("Respiratory PCR rerun", updated.Name);
        Assert.Equal(SampleStatus.Processing, updated.Status);
    }

    [Fact]
    public void Update_RejectsAnUnknownId()
    {
        var service = CreateService();

        var result = service.Update(new Sample
        {
            Id = 9999,
            Name = "Not on the bench",
            Status = SampleStatus.Pending
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Sample 9999 was not found.", result.Error);
        Assert.Equal(5, service.GetAll().Count);
    }

    [Fact]
    public void Update_RejectsABlankName()
    {
        var service = CreateService();

        var result = service.Update(new Sample
        {
            Id = 1002,
            Name = "",
            Status = SampleStatus.Processing
        });

        Assert.False(result.Succeeded);
        Assert.Equal("Name is required.", result.Error);
        Assert.Equal("Urinalysis Ward B", service.GetAll().Single(sample => sample.Id == 1002).Name);
    }

    [Fact]
    public void Delete_RemovesOnlyTheGivenIds()
    {
        var service = CreateService();

        var removed = service.Delete(new[] { 1002, 1005, 4242 });

        Assert.Equal(2, removed);

        var remaining = service.GetAll().Select(sample => sample.Id).ToArray();
        Assert.Equal(new[] { 1001, 1003, 1004 }, remaining);
    }

    [Fact]
    public void Delete_WithNoIds_LeavesTheListAlone()
    {
        var service = CreateService();

        var removed = service.Delete(Array.Empty<int>());

        Assert.Equal(0, removed);
        Assert.Equal(5, service.GetAll().Count);
    }

    [Fact]
    public async Task GenerateAsync_AppendsThatManySamples()
    {
        var service = CreateService();

        var result = await service.GenerateAsync(3);

        Assert.True(result.Succeeded);
        Assert.Equal(8, result.Value);

        var samples = service.GetAll();
        Assert.Equal(1006, samples[5].Id);
        Assert.Equal("Generated 1006", samples[5].Name);
        Assert.Equal(SampleStatus.Pending, samples[5].Status);
        Assert.Equal(SampleStatus.Processing, samples[6].Status);
        Assert.Equal("Generated 1008", samples[^1].Name);
    }

    [Fact]
    public async Task GenerateAsync_RejectsACountOutsideTheAllowedRange()
    {
        var service = CreateService();

        var result = await service.GenerateAsync(0);

        Assert.False(result.Succeeded);
        Assert.Equal("Count must be from 1 to 1,000,000.", result.Error);
        Assert.Equal(5, service.GetAll().Count);
    }

    private static SampleService CreateService()
    {
        var csvPath = Path.Combine(AppContext.BaseDirectory, "Data", "samples.csv");
        var repository = new SampleRepository(csvPath);
        return new SampleService(repository);
    }
}
