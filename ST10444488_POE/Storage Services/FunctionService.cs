using System.Text;
using System.Text.Json;

namespace ST10444488_POE.Storage_Services
{
    public class FunctionService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public FunctionService(IConfiguration config, HttpClient http)
        {
            _config = config;
            _http = http;
        }

        public async Task<string> CallFunctionAsync(string endpointKey, object payload)
        {
            var baseUrl = _config["AzureFunctions:BaseUrl"];
            var functionKey = _config["AzureFunctions:FunctionKey"];
            var endpoint = _config[$"AzureFunctions:Endpoints:{endpointKey}"];

            var fullUrl = $"{baseUrl}{endpoint}?code={functionKey}";
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(fullUrl, content);
            return await response.Content.ReadAsStringAsync();
        }
    }
}
