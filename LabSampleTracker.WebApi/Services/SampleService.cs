using LabSampleTracker.WebApi.Models;
using LabSampleTracker.WebApi.Repositories;

namespace LabSampleTracker.WebApi.Services;

/// <summary>
/// Checks the rules for a sample, then asks the repository to store the change.
/// </summary>
public class SampleService : ISampleService
{
    private readonly ISampleRepository _repository;

    public SampleService(ISampleRepository repository)
    {
        _repository = repository;
    }

    public IReadOnlyList<Sample> GetAll()
    {
        return _repository.GetAll();
    }

    public OperationResult<Sample> Add(Sample sample)
    {
        var error = Validate(sample);
        if (error is not null)
        {
            return OperationResult<Sample>.Fail(error);
        }

        if (_repository.GetById(sample.Id) is not null)
        {
            return OperationResult<Sample>.Fail($"A sample with id {sample.Id} already exists.");
        }

        _repository.Add(sample);
        return OperationResult<Sample>.Ok(_repository.GetById(sample.Id)!);
    }

    public OperationResult<Sample> Update(Sample sample)
    {
        var error = Validate(sample);
        if (error is not null)
        {
            return OperationResult<Sample>.Fail(error);
        }

        if (_repository.GetById(sample.Id) is null)
        {
            return OperationResult<Sample>.Fail($"Sample {sample.Id} was not found.");
        }

        _repository.Update(sample);
        return OperationResult<Sample>.Ok(_repository.GetById(sample.Id)!);
    }

    public int Delete(IReadOnlyList<int> ids)
    {
        if (ids.Count == 0)
        {
            return 0;
        }

        return _repository.Delete(ids);
    }

    public async Task<OperationResult<int>> GenerateAsync(int count)
    {
        if (count is < 1 or > 1_000_000)
        {
            return OperationResult<int>.Fail("Count must be from 1 to 1,000,000.");
        }

        // Building a million objects is CPU work. Task.Run runs that loop on a
        // thread-pool thread. await waits for it without blocking this call.
        var total = await Task.Run(() => _repository.Generate(count));
        return OperationResult<int>.Ok(total);
    }

    private static string? Validate(Sample sample)
    {
        if (sample.Id <= 0)
        {
            return "Id must be a positive number.";
        }

        if (string.IsNullOrWhiteSpace(sample.Name))
        {
            return "Name is required.";
        }

        if (!Enum.IsDefined(sample.Status))
        {
            return "Status must be Pending or Processing.";
        }

        return null;
    }
}
