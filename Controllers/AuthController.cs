using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Case2;
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;

    public AuthController(AppDbContext db)
    {
        _db = db;
    }
    public record RegisterRequest(string Username, string Password);
    public record LoginRequest(string Username, string Password);
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest(new { message = "Пользователь уже существует" });

        var user = new User
        {
            Id = 1,
            Username = request.Username,
            PasswordHash = HashPassword(request.Password),
            IsOnline = false,
            LastOnline = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Пользователь зарегистрирован" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || user.PasswordHash != HashPassword(request.Password))
            return Unauthorized(new { message = "Неверный логин или пароль" });

        user.IsOnline = true;
        user.LastOnline = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { token = "jwt-token-placeholder", userId = user.Id, username = user.Username });
    }

    private string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
