namespace EventGrok.Users.Application.DTOs;

public record UserInfoDto(
    Guid Id,
    string Login, 
    string Role);