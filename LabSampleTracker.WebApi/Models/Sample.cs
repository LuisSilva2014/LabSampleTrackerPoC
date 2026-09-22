namespace LabSampleTracker.WebApi.Models;

public class Sample
{
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public SampleStatus Status { get; set; }
}
