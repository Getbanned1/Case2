// using Microsoft.AspNetCore.Mvc;
// using System.Collections.Generic;
// using System.Linq;
// using Case2;
// [ApiController]
// [Route("api/users")]
// public class UsersController : ControllerBase
// {
//     [HttpGet]
//     public ActionResult<List<UserDto>> GetUsers()
//     {
//         var users = DataStore.Users.Select(u => new UserDto(u.Id, u.Username)).ToList();
//         return Ok(users);
//     }
// }
