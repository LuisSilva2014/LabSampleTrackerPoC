using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using LabSampleTracker.Desktop.Models;

namespace LabSampleTracker.Desktop.Services;

public class SampleApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;

    public SampleApiClient(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromMinutes(2)
        };
    }

    public async Task<IReadOnlyList<SampleDto>> GetAllAsync()
    {
        var samples = await _httpClient.GetFromJsonAsync<List<SampleDto>>("api/samples", JsonOptions);
        return samples ?? [];
    }

    public async Task<SampleDto> AddAsync(SampleDto sample)
    {
        using var response = await _httpClient.PostAsJsonAsync("api/samples", sample, JsonOptions);
        return await ReadSampleAsync(response);
    }

    public async Task<SampleDto> UpdateAsync(SampleDto sample)
    {
        using var response = await _httpClient.PutAsJsonAsync($"api/samples/{sample.Id}", sample, JsonOptions);
        return await ReadSampleAsync(response);
    }

    public async Task<GenerateResultDto> GenerateAsync(int count)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "api/samples/generate",
            new GenerateRequestBody { Count = count },
            JsonOptions);

        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await ReadErrorAsync(response));
        }

        var result = await response.Content.ReadFromJsonAsync<GenerateResultDto>(JsonOptions);
        return result ?? throw new ApiException("The API returned an empty result.");
    }

    public async Task DeleteAsync(IReadOnlyList<int> ids)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "api/samples")
        {
            Content = JsonContent.Create(new DeleteIdsBody { Ids = ids.ToList() }, options: JsonOptions)
        };

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await ReadErrorAsync(response));
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private static async Task<SampleDto> ReadSampleAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await ReadErrorAsync(response));
        }

        var sample = await response.Content.ReadFromJsonAsync<SampleDto>(JsonOptions);
        return sample ?? throw new ApiException("The API returned an empty sample.");
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var fallback = $"The API returned {(int)response.StatusCode}.";

        if (string.IsNullOrWhiteSpace(body))
        {
            return fallback;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (TryReadString(document.RootElement, "message", out var message))
            {
                return message;
            }

            if (TryReadString(document.RootElement, "title", out var title))
            {
                return title;
            }
        }
        catch (JsonException)
        {
            return body;
        }

        return body;
    }

    private static bool TryReadString(JsonElement element, string propertyName, out string value)
    {
        value = "";
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? "";
        return value.Length > 0;
    }

    private sealed class DeleteIdsBody
    {
        public List<int> Ids { get; set; } = [];
    }

    private sealed class GenerateRequestBody
    {
        public int Count { get; set; }
    }
}

public class ApiException : Exception
{
    public ApiException(string message) : base(message)
    {
    }
}
