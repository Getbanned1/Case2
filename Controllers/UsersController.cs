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
}
