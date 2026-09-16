using FrontEnd.DTOs;

namespace FrontEnd.Services
{
    public class CategoryService
    {
        private readonly HttpClient _httpClient;
        public CategoryService(
            HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("api/Category");

            if (!response.IsSuccessStatusCode)
            {
                return Enumerable.Empty<CategoryDto>();
            }

            return await response.Content.ReadFromJsonAsync<IEnumerable<CategoryDto>>()
                    ?? Enumerable.Empty<CategoryDto>();
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/Category/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CategoryDto>();

        }
    }
}
