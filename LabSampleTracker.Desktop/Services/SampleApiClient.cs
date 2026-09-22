using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using LabSampleTracker.Desktop.Models;

namespace LabSampleTracker.Desktop.Services;

public class SampleApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // This is to make the JSON properties camelCase
        PropertyNameCaseInsensitive = true // This is to make the JSON properties case insensitive
    };

    private readonly HttpClient _httpClient; // Local private field used to instantiate the HTTP client

    public SampleApiClient(string baseUrl)
    {
        // Set the base URL and timeout for the HTTP client. and execute the HTTP request
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
        // using disposes the response when the block ends, so the connection is released.
        using (HttpResponseMessage response = await _httpClient.PostAsJsonAsync("api/samples", sample, JsonOptions))
        {
            return await ReadSampleAsync(response);
        }

        // CSHARP_NEW_WAY_TAG: Implemeting using var for shorter code
        // Make a post request asynchronously to the API and return the response
        // using var response = await _httpClient.PostAsJsonAsync("api/samples", sample, JsonOptions);
        // // Read the response and return the sample
        // return await ReadSampleAsync(response);
    }

    public async Task<SampleDto> UpdateAsync(SampleDto sample)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/samples/{sample.Id}", sample, JsonOptions);
        return await ReadSampleAsync(response);
    }

    public async Task<GenerateResultDto> GenerateAsync(int count)
    {
      
        // using (HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
        //     "api/samples/generate",
        //     new GenerateRequestBody { Count = count },
        //     JsonOptions))
        // {
        //     if (!response.IsSuccessStatusCode)
        //     {
        //         throw new ApiException(await ReadErrorAsync(response));
        //     }

        //     var result = await response.Content.ReadFromJsonAsync<GenerateResultDto>(JsonOptions);
        //     return result ?? throw new ApiException("The API returned an empty result.");
        // }


        // NEW_WAY_TAG: Implemeting using var for shorter code
        using var response = await _httpClient.PostAsJsonAsync(
            "api/samples/generate",
            new GenerateRequestBody { Count = count },
            JsonOptions);

        // Check if the response is successful
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await ReadErrorAsync(response));
        }
        var result = await response.Content.ReadFromJsonAsync<GenerateResultDto>(JsonOptions);
        return result ?? throw new ApiException("The API returned an empty result.");
    }

    // Blocks the caller until the API finishes. The window does not move on early.
    public void Delete(IReadOnlyList<int> ids)
    //  public async Task DeleteAsync(IReadOnlyList<int> ids) -> this is the async version
    {
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, "api/samples");
        request.Content = JsonContent.Create(new DeleteIdsBody { Ids = ids.ToList() }, options: JsonOptions);

        using (HttpResponseMessage response = _httpClient.Send(request))
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(ReadError(response));
            }
        }
    }

    public void Dispose()
    {
        // Dispose from memory the value stored in the _httpClient field
        _httpClient.Dispose();
    }

    private static async Task<SampleDto> ReadSampleAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException(await ReadErrorAsync(response));
        }

        // A json object is expected to be returned, lets deserialize it into a SampleDto object
        var sample = await response.Content.ReadFromJsonAsync<SampleDto>(JsonOptions);
        // If the sample is null, throw an exception
        return sample ?? throw new ApiException("The API returned an empty sample.");
    }

 
    private static string ReadError(HttpResponseMessage response) // This is the sync version
    {
        string body;
        using (Stream stream = response.Content.ReadAsStream())
        using (StreamReader reader = new StreamReader(stream))
        {
            body = reader.ReadToEnd();
        }

        return DescribeError(body, (int)response.StatusCode);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response) // This is the async version
    {
        string body = await response.Content.ReadAsStringAsync();
        return DescribeError(body, (int)response.StatusCode);
    }

    private static string DescribeError(string body, int statusCode)
    {
        string fallback = "The API returned " + statusCode + ".";

        if (string.IsNullOrWhiteSpace(body))
        {
            return fallback;
        }

        try
        {
            using (JsonDocument document = JsonDocument.Parse(body))
            {
                string message;
                if (TryReadString(document.RootElement, "message", out message))
                {
                    return message;
                }

                string title;
                if (TryReadString(document.RootElement, "title", out title))
                {
                    return title;
                }
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
        JsonElement property;
        if (!element.TryGetProperty(propertyName, out property) || property.ValueKind != JsonValueKind.String)
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
