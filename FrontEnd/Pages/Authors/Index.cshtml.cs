using FrontEnd.DTOs;
using FrontEnd.Pages.Shared;
using FrontEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FrontEnd.Pages.Authors
{
    public class IndexModel : AuthorizedPageModel
    {
        private readonly AuthorService _authorService;

        public IndexModel(AuthorService authorService)
        {
            _authorService = authorService;
        }

        public List<AuthorDto> Authors { get; set; } = new();

        public async Task OnGetAsync()
        {
            Authors = (await _authorService.GetAllAsync())
                .ToList();
        }
    }
}
