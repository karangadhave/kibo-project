using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Kibo.TestingFramework
{
    public class KiboApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly bool _enableLogging;
        private readonly string _tenantHeader;
        private readonly bool _enableCorrelation;

        public KiboApiClient(string? baseUrl = null, string? tenantHeader = null, bool enableLogging = false, bool enableCorrelation = false, HttpClient? httpClient = null)
        {
            _baseUrl = baseUrl ?? Environment.GetEnvironmentVariable("KIBO_API_BASE_URL") ?? "http://localhost:5000";
            _tenantHeader = tenantHeader ?? "tenant-abc-123";
            _enableLogging = enableLogging || Environment.GetEnvironmentVariable("KIBO_API_LOGGING") == "1";
            _enableCorrelation = enableCorrelation || Environment.GetEnvironmentVariable("KIBO_API_CORRELATION") == "1";
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<ApiResponse<T>> PostAsync<T>(string path, object body, bool includeTenant = true)
        {
            var url = _baseUrl.TrimEnd('/') + path;
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            if (includeTenant)
                request.Headers.Add("x-kibo-tenant", _tenantHeader);
            return await SendAsync<T>(request);
        }

        public async Task<ApiResponse<T>> GetAsync<T>(string path, bool includeTenant = true)
        {
            var url = _baseUrl.TrimEnd('/') + path;
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (includeTenant)
                request.Headers.Add("x-kibo-tenant", _tenantHeader);
            return await SendAsync<T>(request);
        }

        private async Task<ApiResponse<T>> SendAsync<T>(HttpRequestMessage request)
        {
            var sw = Stopwatch.StartNew();
            string? requestLog = null;
            string? responseLog = null;
            string? correlationId = null;
            try
            {
                if (_enableCorrelation)
                {
                    correlationId = Guid.NewGuid().ToString();
                    request.Headers.Add("x-correlation-id", correlationId);
                }
                if (_enableLogging)
                {
                    requestLog = await FormatRequest(request);
                }
                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                if (_enableLogging)
                {
                    responseLog = FormatResponse(response, responseBody);
                }
                sw.Stop();
                var result = JsonSerializer.Deserialize<T>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return new ApiResponse<T>
                {
                    StatusCode = (int)response.StatusCode,
                    Data = result,
                    RawBody = responseBody,
                    ElapsedMs = sw.ElapsedMilliseconds,
                    RequestLog = requestLog,
                    ResponseLog = responseLog,
                    CorrelationId = correlationId
                };
            }
            catch (Exception ex)
            {
                sw.Stop();
                return new ApiResponse<T>
                {
                    StatusCode = 0,
                    Data = default,
                    RawBody = ex.ToString(),
                    ElapsedMs = sw.ElapsedMilliseconds,
                    RequestLog = requestLog,
                    ResponseLog = responseLog,
                    CorrelationId = correlationId
                };
            }
        }

        private async Task<string> FormatRequest(HttpRequestMessage request)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{request.Method} {request.RequestUri}");
            foreach (var h in request.Headers)
                sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
            if (request.Content != null)
            {
                foreach (var h in request.Content.Headers)
                    sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
                sb.AppendLine();
                sb.AppendLine(await request.Content.ReadAsStringAsync());
            }
            return sb.ToString();
        }

        private string FormatResponse(HttpResponseMessage response, string body)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{(int)response.StatusCode} {response.ReasonPhrase}");
            foreach (var h in response.Headers)
                sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
            foreach (var h in response.Content.Headers)
                sb.AppendLine($"{h.Key}: {string.Join(",", h.Value)}");
            sb.AppendLine();
            sb.AppendLine(body);
            return sb.ToString();
        }
    }

    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public T? Data { get; set; }
        public string? RawBody { get; set; }
        public long ElapsedMs { get; set; }
        public string? RequestLog { get; set; }
        public string? ResponseLog { get; set; }
        public string? CorrelationId { get; set; }
    }
}
