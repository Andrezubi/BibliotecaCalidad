using FrontEnd.DTOs;

namespace FrontEnd.Services
{
    public class PublisherService
    {
        private readonly HttpClient _httpClient;

        public PublisherService(
            HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<PublisherDto>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("api/Publisher");

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<PublisherDto>();
            }

            return await response.Content.ReadFromJsonAsync<IEnumerable<PublisherDto>>()
                   ?? Enumerable.Empty<PublisherDto>();
        }

        public async Task<(bool Success, int? Id, string? Error)> CreateAsync(CreatePublisherDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/Publisher", dto);

            if (response.IsSuccessStatusCode)
            {
                var publisher = await response.Content.ReadFromJsonAsync<PublisherDto>();
                return (true, publisher?.Id, null);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var error = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return (false, null, error?.GetValueOrDefault("message") ?? "Ya existe una editorial con ese nombre.");
            }

            return (false, null, "No se pudo crear la editorial.");
        }
    }
}