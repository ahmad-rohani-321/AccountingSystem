using AccountingSystem.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers.ApiControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController(UserManager<User> userManager ) : ControllerBase
    {
        private readonly UserManager<User> _userManager = userManager;

        [HttpGet("GetCurrencyUser")]
        public async Task<ActionResult> GetCurrentUser()
        {
            return Ok(await _userManager.GetUserAsync(User));
        }

        [HttpGet("GetCurrenctUserRoles")]
        public async Task<ActionResult> GetCurrenctUserRoles()
        {
            var user = await _userManager.GetUserAsync(User);
            IList<string> userRoles = await _userManager.GetRolesAsync(user);
            return Ok(string.Join(',', userRoles));
        }
    }
}
