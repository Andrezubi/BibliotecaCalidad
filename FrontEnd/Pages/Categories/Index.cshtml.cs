using FrontEnd.DTOs;
using FrontEnd.Pages.Shared;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Categories
{
    public class IndexModel : AuthorizedPageModel
    {
        private readonly CategoryService _categoryService;

        public IndexModel(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public List<CategoryDto> Categories { get; set; } = new();

        public async Task OnGetAsync()
        {
            Categories = (await _categoryService.GetAllAsync())
                .ToList();
        }
    }
}
