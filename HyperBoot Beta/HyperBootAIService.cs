using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace HyperBoot_Beta
{
    internal class HyperBootAIService
    {
        public class SystemSpecs
        {
            [JsonPropertyName("ramGb")]
            public string RamGb { get; set; } = string.Empty;

            [JsonPropertyName("cpuCores")]
            public string CpuCores { get; set; } = string.Empty;

            [JsonPropertyName("gpuVendor")]
            public string GpuVendor { get; set; } = string.Empty;

            [JsonPropertyName("diskType")]
            public string DiskType { get; set; } = string.Empty;

            [JsonPropertyName("userGoal")]
            public string UserGoal { get; set; } = string.Empty;
        }

        public class OsRecommendation
        {
            [JsonPropertyName("recommendedOs")]
            public string RecommendedOs { get; set; }

            [JsonPropertyName("matchPercentage")]
            public int MatchPercentage { get; set; }

            [JsonPropertyName("reason")]
            public string Reason { get; set; }

            [JsonPropertyName("isoUrl")]
            public string IsoUrl { get; set; }
        }

        private const string ProxyUrl = "https://hyperboot-proxy1.yahyasekizbes.workers.dev/";

        private static readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(25)
        };

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task<OsRecommendation> GetOsRec(
            string ram,
            string cpuCores,
            string gpuVendor,
            string diskType,
            string userGoal,
            CancellationToken cancellationToken = default)
        {
            var specs = new SystemSpecs
            {
                RamGb = ram?.Trim() ?? string.Empty,
                CpuCores = cpuCores?.Trim() ?? string.Empty,
                GpuVendor = gpuVendor?.Trim() ?? string.Empty,
                DiskType = diskType?.Trim() ?? string.Empty,
                UserGoal = userGoal?.Trim() ?? string.Empty
            };

            try
            {
                string jsonPayload = JsonSerializer.Serialize(specs, JsonOptions);
                using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await HttpClient.PostAsync(ProxyUrl, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Worker HTTP Hatası: {response.StatusCode}");
                    return null;
                }

                string responseJson = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"AI Yanıtı: {responseJson}");

                return JsonSerializer.Deserialize<OsRecommendation>(responseJson, JsonOptions);
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine("AI isteği zaman aşımına uğradı veya iptal edildi.");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AI Service Error: {ex.Message}");
                return null;
            }
        }
    }
}