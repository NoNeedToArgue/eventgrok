using EventGrok.Application.DTOs;
using EventGrok.Application.Interfaces;
using EventGrok.Domain.Entities;
using EventGrok.Domain.Exceptions;

namespace EventGrok.Application.Services;

public class UserService(IUserRepository userRepo, IPasswordHasher passwordHasher, ITokenService tokenService) : IUserService
{
    public async Task<UserInfoDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        User? existingUser = await userRepo.GetUserByLoginAsync(dto.Login, ct);
        if (existingUser is not null)
            throw new UserAlreadyExistsException(dto.Login);

        string passwordHash = passwordHasher.HashPassword(dto.Password);
        Role role = Enum.TryParse<Role>(dto.Role, ignoreCase: true, out var parsedRole) 
            ? parsedRole 
            : Role.User;

        User newUser = User.Create(dto.Login, passwordHash, role);

        await userRepo.AddUserAsync(newUser, ct);
        await userRepo.SaveChangesAsync(ct);

        return new UserInfoDto(newUser.Id, newUser.Login, newUser.Role.ToString());
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        User? user = await userRepo.GetUserByLoginAsync(dto.Login, ct);
        
        if (user is null || !passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return tokenService.GenerateToken(user);
    }
}