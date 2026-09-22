using System.Globalization;
using LabSampleTracker.WebApi.Models;

namespace LabSampleTracker.WebApi.Repositories;

/// <summary>
/// Keeps samples in a list that lives for the process.
/// The first time anything asks for data, the list is filled from Data/samples.csv.
/// After that, add, update, and delete change the list only. The file is not rewritten.
/// </summary>
public class SampleRepository : ISampleRepository
{
    private readonly List<Sample> _samples = [];
    private readonly string _csvFilePath;
    private bool _hasLoadedFromCsv;

    public SampleRepository(string csvFilePath)
    {
        _csvFilePath = csvFilePath;
    }

    public IReadOnlyList<Sample> GetAll()
    {
        EnsureLoaded();
        return _samples.Select(Copy).ToList();
    }

    public Sample? GetById(int id)
    {
        EnsureLoaded();
        var stored = _samples.FirstOrDefault(sample => sample.Id == id);
        return stored is null ? null : Copy(stored);
    }

    public void Add(Sample sample)
    {
        EnsureLoaded();

        if (_samples.Any(existing => existing.Id == sample.Id))
        {
            throw new InvalidOperationException($"A sample with id {sample.Id} already exists.");
        }

        _samples.Add(new Sample
        {
            Id = sample.Id,
            Name = sample.Name.Trim(),
            Status = sample.Status
        });
    }

    public void Update(Sample sample)
    {
        EnsureLoaded();

        var stored = _samples.FirstOrDefault(existing => existing.Id == sample.Id);
        if (stored is null)
        {
            throw new InvalidOperationException($"Sample {sample.Id} was not found.");
        }

        stored.Name = sample.Name.Trim();
        stored.Status = sample.Status;
    }

    public int Delete(IEnumerable<int> ids)
    {
        EnsureLoaded();
        var idSet = ids.ToHashSet();
        return _samples.RemoveAll(sample => idSet.Contains(sample.Id));
    }

    public int Generate(int count)
    {
        EnsureLoaded();

        var nextId = _samples.Count == 0 ? 1 : _samples.Max(sample => sample.Id) + 1;
        _samples.EnsureCapacity(_samples.Count + count);

        for (var i = 0; i < count; i++)
        {
            var id = nextId + i;
            _samples.Add(new Sample
            {
                Id = id,
                Name = $"Generated {id}",
                Status = i % 2 == 0 ? SampleStatus.Pending : SampleStatus.Processing
            });
        }

        return _samples.Count;
    }

    private void EnsureLoaded()
    {
        if (_hasLoadedFromCsv)
        {
            return;
        }

        if (!File.Exists(_csvFilePath))
        {
            throw new FileNotFoundException("The sample CSV file was not found.", _csvFilePath);
        }

        // Line 1 is the header. Names in this file do not contain commas.
        foreach (var line in File.ReadLines(_csvFilePath).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length != 3)
            {
                throw new FormatException($"Each CSV line must be Id,Name,Status. Got: {line}");
            }

            _samples.Add(new Sample
            {
                Id = int.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                Name = parts[1].Trim(),
                Status = Enum.Parse<SampleStatus>(parts[2].Trim(), ignoreCase: true)
            });
        }

        _hasLoadedFromCsv = true;
    }

    private static Sample Copy(Sample sample)
    {
        return new Sample
        {
            Id = sample.Id,
            Name = sample.Name,
            Status = sample.Status
        };
    }
}
