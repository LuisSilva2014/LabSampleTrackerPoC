using LabSampleTracker.WebApi.Models;

namespace LabSampleTracker.WebApi.Services;

public interface ISampleService
{
    IReadOnlyList<Sample> GetAll();

    OperationResult<Sample> Add(Sample sample);

    OperationResult<Sample> Update(Sample sample);

    int Delete(IReadOnlyList<int> ids);

    Task<OperationResult<int>> GenerateAsync(int count);
}
