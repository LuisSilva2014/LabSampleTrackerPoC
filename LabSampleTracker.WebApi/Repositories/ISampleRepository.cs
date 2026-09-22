using LabSampleTracker.WebApi.Models;

namespace LabSampleTracker.WebApi.Repositories;

public interface ISampleRepository
{
    IReadOnlyList<Sample> GetAll();

    Sample? GetById(int id);

    void Add(Sample sample);

    void Update(Sample sample);

    int Delete(IEnumerable<int> ids);

    int Generate(int count);
}
