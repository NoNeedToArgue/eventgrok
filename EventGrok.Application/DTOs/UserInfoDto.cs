namespace EventGrok.Application.DTOs;

public record UserInfoDto(
    Guid Id,
    string Login, 
    string Rrole);