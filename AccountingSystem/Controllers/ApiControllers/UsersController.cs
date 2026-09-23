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
public class UsersController(UserManager<User> userManager, ApplicationDbContext context) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ApplicationDbContext _context = context;

    [HttpGet("GetCurrencyUser")]
    public async Task<ActionResult> GetCurrentUser()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("یوزر ونه موندل سو.");
        }

        return Ok(new { user.FirstName, user.LastName, user.ProfilePhoto });
    }

    [HttpGet("GetCurrenctUserRoles")]
    public async Task<ActionResult> GetCurrenctUserRoles()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("یوزر ونه موندل سو.");
        }

        IList<string> userRoles = await _userManager.GetRolesAsync(user);
        return Ok(string.Join(',', userRoles));
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost("CreateUser")]
    public async Task<ActionResult> CreateUser(CreateUserViewModel request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("ټول اړین معلومات ولیکئ.");
        }
        else if (!new EmailAddressAttribute().IsValid(request.Email))
        {
            return BadRequest("صحیح ایمېل ولیکئ.");
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
            .Select(x => new
            {
                x.Id,
                x.FirstName,
                x.LastName,
                x.UserName,
                x.Email,
                x.PhoneNumber,
                x.IsActive
            })
            .ToListAsync();
        return Ok(users);
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
            string.IsNullOrWhiteSpace(request.Email))
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
