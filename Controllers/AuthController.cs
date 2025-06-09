using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Case2;
using Microsoft.AspNetCore.SignalR;
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<ChatHub> _hubContext;
    public AuthController(IHubContext<ChatHub> hubContext, AppDbContext db, IConfiguration configuration)
    {
        
        _hubContext = hubContext;
        _db = db;
        _configuration = configuration;
    }

    public record RegisterRequest(string Username, string Password, IFormFile? Avatar);
    public record LoginRequest(string Username, string Password);
    public class LogoutRequest
    {
        public int UserId { get; set; }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromForm] RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new { message = "Пользователь уже существует" });

        string avatarFileName = null;

        if (request.Avatar != null && request.Avatar.Length > 0)
        {
            // Генерируем уникальное имя файла
            avatarFileName = Guid.NewGuid() + Path.GetExtension(request.Avatar.FileName);
            var filePath = Path.Combine("wwwroot", "avatars", avatarFileName);

            // Создаем папку, если не существует
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.Avatar.CopyToAsync(stream);
            }
        }

        var user = new User
        {
            Username = request.Username,
            PasswordHash = PasswordCrypter.HashPassword(request.Password),
            AvatarUrl = avatarFileName != null ? $"https://localhost:7000/avatars/{avatarFileName}" : "https://localhost:7000/avatars/default.jpg",
            IsOnline = true,
            LastOnline = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        return Ok(new { token, userId = user.Id, username = user.Username, avatarUrl = user.AvatarUrl });
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !PasswordCrypter.VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Неверный логин или пароль" });

        user.IsOnline = true;
        user.LastOnline = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Получаем список собеседников пользователя
        var chatIds = await _db.UserChats
            .Where(uc => uc.UserId == user.Id)
            .Select(uc => uc.ChatId)
            .ToListAsync();

        var otherUserIds = await _db.UserChats
            .Where(uc => chatIds.Contains(uc.ChatId) && uc.UserId != user.Id)
            .Select(uc => uc.UserId)
            .Distinct()
            .ToListAsync();

        // Отправляем уведомление собеседникам через SignalR
        foreach (var otherUserId in otherUserIds)
        {
            await _hubContext.Clients.User(otherUserId.ToString())
                .SendAsync("ReceiveUserOnlineStatus", user.Id, true);
        }

        var token = GenerateJwtToken(user);

        return Ok(new 
        { 
            token, 
            userId = user.Id, 
            username = user.Username, 
            avatarUrl = user.AvatarUrl, 
            isOnline = user.IsOnline 
        });
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
        if (user == null)
            return NotFound(new { message = "Пользователь не найден." });

        user.IsOnline = false;
        user.LastOnline = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Получаем список собеседников пользователя
        var chatIds = await _db.UserChats
            .Where(uc => uc.UserId == user.Id)
            .Select(uc => uc.ChatId)
            .ToListAsync();

        var otherUserIds = await _db.UserChats
            .Where(uc => chatIds.Contains(uc.ChatId) && uc.UserId != user.Id)
            .Select(uc => uc.UserId)
            .Distinct()
            .ToListAsync();

        // Отправляем уведомление собеседникам через SignalR, что пользователь offline
        foreach (var otherUserId in otherUserIds)
        {
            await _hubContext.Clients.User(otherUserId.ToString())
                .SendAsync("ReceiveUserOnlineStatus", user.Id, false);
        }

        return Ok(new { message = "Пользователь вышел из системы." });
    }


    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim("username", user.Username),
            new Claim("userId", user.Id.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
