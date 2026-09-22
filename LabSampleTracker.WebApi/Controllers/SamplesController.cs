using LabSampleTracker.WebApi.Models;
using LabSampleTracker.WebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace LabSampleTracker.WebApi.Controllers;

[ApiController]
[Route("api/samples")]
public class SamplesController : ControllerBase
{
    private readonly ISampleService _sampleService;

    public SamplesController(ISampleService sampleService)
    {
        _sampleService = sampleService;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<Sample>> GetAll()
    {
        return Ok(_sampleService.GetAll());
    }

    [HttpPost("generate")]
    public async Task<ActionResult<GenerateSamplesResponse>> Generate(GenerateSamplesRequest request)
    {
        var result = await _sampleService.GenerateAsync(request.Count);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new GenerateSamplesResponse
        {
            Created = request.Count,
            Total = result.Value
        });
    }

    [HttpPost]
    public ActionResult<Sample> Add(Sample sample)
    {
        var result = _sampleService.Add(sample);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return Created($"/api/samples/{result.Value!.Id}", result.Value);
    }

    [HttpPut("{id}")]
    public ActionResult<Sample> Update(int id, Sample sample)
    {
        if (id != sample.Id)
        {
            return BadRequest(new { message = "The id in the URL must match the sample id." });
        }

        var result = _sampleService.Update(sample);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Value);
    }

    [HttpDelete]
    public IActionResult Delete(DeleteSamplesRequest request)
    {
        if (request.Ids.Count == 0)
        {
            return BadRequest(new { message = "Select at least one sample id." });
        }

        _sampleService.Delete(request.Ids);
        return NoContent();
    }
}
