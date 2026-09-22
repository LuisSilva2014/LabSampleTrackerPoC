using System.Text.Json.Serialization;
using LabSampleTracker.WebApi.Repositories;
using LabSampleTracker.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Send status as "Pending" / "Processing" instead of 0 / 1.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

// One shared list for the life of the API process.
// A scoped repository would forget earlier saves on the next request.
builder.Services.AddSingleton<ISampleRepository>(_ =>
{
    var csvPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "samples.csv");
    return new SampleRepository(csvPath);
});

builder.Services.AddScoped<ISampleService, SampleService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
