using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Case2;
using Microsoft.AspNetCore.Authorization;
[Authorize]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        try
        {
            var users = await _db.Users
                .Select(u => new UserDto(u.Id, u.Username))
                .ToListAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            // Можно логировать ошибку
            return StatusCode(500, new { message = "Ошибка сервера", details = ex.Message });
        }
    }
        [HttpGet("by-username")]
        public async Task<ActionResult<int>> GetUserIdByUsernameAsync([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new { message = "Имя пользователя не может быть пустым." });
            }

            try
            {
                // Проверяем, есть ли пользователь в локальной базе данных
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user != null)
                {
                    return Ok(user.Id);
                }

                // Если пользователь не найден
                return NotFound(new { message = "Пользователь не найден." });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { message = "Ошибка сервера", details = ex.Message });
            }
        }

}
