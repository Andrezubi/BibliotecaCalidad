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
    }
}