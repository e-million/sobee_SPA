using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace sobee_API.Tests;

public static class TestJson
{
    public static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
