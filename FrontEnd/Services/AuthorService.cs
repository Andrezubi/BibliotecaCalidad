using FrontEnd.DTOs;

namespace FrontEnd.Services
{
    public class AuthorService
    {
        private readonly HttpClient _httpClient;

        public AuthorService(
            HttpClient httpClient
            )
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<AuthorDto>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("api/Author");

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<AuthorDto>();
            }

            return await response.Content.ReadFromJsonAsync<IEnumerable<AuthorDto>>()
                   ?? Enumerable.Empty<AuthorDto>();
        }

        public async Task<AuthorDto?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/Author/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AuthorDto>();
        }


    }
}
