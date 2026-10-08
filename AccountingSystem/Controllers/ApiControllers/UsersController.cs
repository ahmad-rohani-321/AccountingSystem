using AccountingSystem.Data;
using AccountingSystem.Models.Identity;
using AccountingSystem.ViewModels;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.ApiControllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UsersController(UserManager<User> userManager, RoleManager<Role> roleManager, ApplicationDbContext context) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly RoleManager<Role> _roleManager = roleManager;
    private readonly ApplicationDbContext _context = context;

    [HttpGet("GetCurrentUser")]
    public async Task<ActionResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("یوزر ونه موندل سو.");
        }

        return Ok(new { user.FirstName, user.LastName, user.ProfilePhoto });
    }

    [HttpGet("GetCurrentUserRoles")]
    public async Task<ActionResult> GetCurrentUserRoles()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("یوزر ونه موندل سو.");
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var rolePashtoNames = await _roleManager.Roles
            .Where(x => userRoles.Contains(x.Name))
            .Select(x => x.PashtoName)
            .ToListAsync();
        return Ok(string.Join(',', rolePashtoNames));
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet("GetRoles")]
    public async Task<ActionResult> GetRoles()
    {
        var roles = await _roleManager.Roles
            .Where(x => SystemRoles.All.Contains(x.Name))
            .OrderBy(x => x.Name)
            .Select(x => new { x.Name, x.PashtoName })
            .ToListAsync();
        return Ok(roles);
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet("CheckDuplicate")]
    public async Task<ActionResult> CheckDuplicate(string userName, string firstName, string id = null)
    {
        var normalizedUserName = _userManager.NormalizeName(userName?.Trim());
        var normalizedFirstName = firstName?.Trim().ToUpper();
        return Ok(new
        {
            UserNameExists = !string.IsNullOrWhiteSpace(normalizedUserName) && await _userManager.Users.AnyAsync(x => x.Id != id && x.NormalizedUserName == normalizedUserName),
            FirstNameExists = !string.IsNullOrWhiteSpace(normalizedFirstName) && await _userManager.Users.AnyAsync(x => x.Id != id && x.FirstName.ToUpper() == normalizedFirstName)
        });
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost("CreateUser")]
    public async Task<ActionResult> CreateUser(CreateUserViewModel request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest("ټول اړین معلومات ولیکئ.");
        }
        else if (!new EmailAddressAttribute().IsValid(request.Email))
        {
            return BadRequest("صحیح ایمېل ولیکئ.");
        }
        else if (await UserNameExists(request.UserName, null))
        {
            return BadRequest("دغه یوزر نوم مخکې موجود دی.");
        }
        else if (await FirstNameExists(request.FirstName, null))
        {
            return BadRequest("دغه نوم مخکې موجود دی.");
        }
        else if (!SystemRoles.All.Contains(request.Role) || !await _roleManager.RoleExistsAsync(request.Role))
        {
            return BadRequest("انتخاب سوی صلاحیت اعتبار نه لري.");
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return NotFound("یوزر ونه موندل سو.");
        }

        var user = new User()
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            ProfilePhoto = string.Empty
        };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(string.Join(" ", result.Errors.Select(x => x.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return BadRequest(string.Join(" ", roleResult.Errors.Select(x => x.Description)));
        }

        await _context.UserHistories.AddAsync(new UserHistory()
        {
            CreatedByUserId = currentUser.Id,
            CreationDate = DateTime.Now,
            ModelName = "یوزر",
            Details = $"د {user.UserName} په نوم نوی یوزر جوړ سو."
        });
        await _context.SaveChangesAsync();
        return Ok();
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet("GetUsers")]
    public async Task<ActionResult> GetUsers()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return NotFound("یوزر ونه موندل سو.");
        }

        var users = await _userManager.Users
            .Where(x => x.Id != currentUser.Id)
            .OrderBy(x => x.UserName)
            .ToListAsync();

        var result = new List<object>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.UserName,
                user.Email,
                user.PhoneNumber,
                user.ProfilePhoto,
                user.IsActive,
                Role = roles.FirstOrDefault() ?? string.Empty
            });
        }
        return Ok(result);
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet("GetUserHistory/{id}")]
    public async Task<ActionResult> GetUserHistory(string id)
    {
        if (!await _userManager.Users.AnyAsync(x => x.Id == id))
        {
            return NotFound("یوزر ونه موندل سو.");
        }

        var history = await _context.UserHistories
            .Where(x => x.CreatedByUserId == id)
            .OrderByDescending(x => x.CreationDate)
            .Select(x => new
            {
                x.ModelName,
                x.Details,
                x.CreationDate
            })
            .ToListAsync();
        return Ok(history);
    }

    [Authorize(Roles = "Administrator")]
    [HttpPut("UpdateUser")]
    public async Task<ActionResult> UpdateUser(UserUpdateViewModel request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) || string.IsNullOrWhiteSpace(request.UserName) ||
            string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Role))
        {
            return BadRequest("ټول اړین معلومات ولیکئ.");
        }

        var currentUser = await _userManager.GetUserAsync(User);
        var user = await _userManager.FindByIdAsync(request.Id);
        if (user == null)
        {
            return NotFound("یوزر ونه موندل سو.");
        }
        else if (currentUser == null || user.Id == currentUser.Id)
        {
            return BadRequest("خپل یوزر له دې برخې نه سي تغیرولای.");
        }
        else if (await UserNameExists(request.UserName, user.Id))
        {
            return BadRequest("دغه یوزر نوم مخکې موجود دی.");
        }
        else if (await FirstNameExists(request.FirstName, user.Id))
        {
            return BadRequest("دغه نوم مخکې موجود دی.");
        }
        else if (!SystemRoles.All.Contains(request.Role) || !await _roleManager.RoleExistsAsync(request.Role))
        {
            return BadRequest("انتخاب سوی صلاحیت اعتبار نه لري.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UserName = request.UserName.Trim();
        user.Email = request.Email.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(string.Join(" ", result.Errors.Select(x => x.Description)));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Count != 1 || currentRoles[0] != request.Role)
        {
            if (currentRoles.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    return BadRequest(string.Join(" ", removeResult.Errors.Select(x => x.Description)));
                }
            }

            var roleResult = await _userManager.AddToRoleAsync(user, request.Role);
            if (!roleResult.Succeeded)
            {
                if (currentRoles.Count > 0)
                {
                    await _userManager.AddToRolesAsync(user, currentRoles);
                }
                return BadRequest(string.Join(" ", roleResult.Errors.Select(x => x.Description)));
            }

            await _userManager.UpdateSecurityStampAsync(user);
        }

        await _context.UserHistories.AddAsync(new UserHistory()
        {
            CreatedByUserId = currentUser.Id,
            CreationDate = DateTime.Now,
            ModelName = "یوزر",
            Details = $"د {user.UserName} یوزر معلومات تغیر سول."
        });
        await _context.SaveChangesAsync();
        return Ok();
    }

    private async Task<bool> UserNameExists(string userName, string excludeId)
    {
        var normalizedUserName = _userManager.NormalizeName(userName.Trim());
        return await _userManager.Users.AnyAsync(x => x.Id != excludeId && x.NormalizedUserName == normalizedUserName);
    }

    private async Task<bool> FirstNameExists(string firstName, string excludeId)
    {
        var normalizedFirstName = firstName.Trim().ToUpper();
        return await _userManager.Users.AnyAsync(x => x.Id != excludeId && x.FirstName.ToUpper() == normalizedFirstName);
    }

    [Authorize(Roles = "Administrator")]
    [HttpPut("UpdateUserActivation/{id}")]
    public async Task<ActionResult> UpdateUserActivation(string id)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound("یوزر ونه موندل سو.");
        }
        else if (currentUser == null || user.Id == currentUser.Id)
        {
            return BadRequest("خپل یوزر غیر فعالولای نه سئ.");
        }

        user.IsActive = !user.IsActive;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(string.Join(" ", result.Errors.Select(x => x.Description)));
        }

        await _context.UserHistories.AddAsync(new UserHistory()
        {
            CreatedByUserId = currentUser.Id,
            CreationDate = DateTime.Now,
            ModelName = "یوزر",
            Details = $"د {user.UserName} یوزر فعالیت تغیر سو."
        });
        await _context.SaveChangesAsync();
        return Ok();
    }
}
