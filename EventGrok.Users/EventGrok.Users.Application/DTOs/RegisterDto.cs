using System.ComponentModel.DataAnnotations;

namespace EventGrok.Users.Application.DTOs;

public class RegisterDto
{
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен быть от 3 до 50 символов")]
    public required string Login { get; set; }

    [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен быть от 6 до 100 символов")]
    public required string Password { get; set; }

    public string Role { get; set; } = "User";
}