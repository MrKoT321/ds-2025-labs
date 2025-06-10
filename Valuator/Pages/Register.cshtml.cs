using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Services;

namespace Valuator.Pages;

public class RegisterModel : PageModel
{
    private readonly IUserService _userService;

    public RegisterModel(IUserService userService)
    {
        _userService = userService;
    }

    [BindProperty]
    public string Username { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    public string? ErrorMessage { get; set; }

    public void OnGet() {}

    public async Task<IActionResult> OnPostAsync()
    {
        var existingUser = await _userService.FindByUsernameAsync(Username);
        if (existingUser != null)
        {
            ErrorMessage = "Пользователь с таким именем уже существует.";
            return Page();
        }

        await _userService.CreateUserAsync(Username, Password);
        return RedirectToPage("/Login");
    }
}