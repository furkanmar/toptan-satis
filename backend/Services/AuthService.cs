using Microsoft.EntityFrameworkCore;
using WholesaleApi.Data;
using WholesaleApi.DTOs;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public class AuthService(AppDbContext db, TokenService tokenService)
{
    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await db.Users
            .Include(u => u.Wholesaler)
            .Include(u => u.Store)
            .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower() && u.IsActive);

        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Geçersiz email veya şifre");

        var token = tokenService.Generate(user);

        return new AuthResponseDto
        {
            Token = token,
            Role = user.Role.ToString(),
            UserId = user.Id,
            ProfileId = user.Role switch
            {
                UserRole.Wholesaler => user.Wholesaler?.Id,
                UserRole.Store => user.Store?.Id,
                _ => null
            },
            DisplayName = user.Role switch
            {
                UserRole.Wholesaler => user.Wholesaler?.CompanyName ?? user.Email,
                UserRole.Store => user.Store?.StoreName ?? user.Email,
                _ => "Admin"
            }
        };
    }

    public async Task<User> RegisterAsync(RegisterDto dto, UserRole role)
    {
        if (await db.Users.AnyAsync(u => u.Email == dto.Email.ToLower()))
            throw new InvalidOperationException("Bu email zaten kayıtlı");

        var user = new User
        {
            Email = dto.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = role
        };

        db.Users.Add(user);

        if (role == UserRole.Wholesaler)
        {
            db.Wholesalers.Add(new Wholesaler
            {
                UserId = user.Id,
                CompanyName = dto.CompanyName ?? dto.Email
            });
        }
        else if (role == UserRole.Store)
        {
            db.Stores.Add(new Store
            {
                UserId = user.Id,
                StoreName = dto.CompanyName ?? dto.Email
            });
        }

        await db.SaveChangesAsync();
        return user;
    }
}
