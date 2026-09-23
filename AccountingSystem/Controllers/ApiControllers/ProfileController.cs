using System.ComponentModel.DataAnnotations;
using AccountingSystem.Data;
using AccountingSystem.Models.Identity;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.ApiControllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ProfileController(UserManager<User> userManager, SignInManager<User> signInManager, ApplicationDbContext context, IWebHostEnvironment environment) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly ApplicationDbContext _context = context;
    private readonly IWebHostEnvironment _environment = environment;

    [HttpGet("GetProfile")]
    public async Task<ActionResult> GetProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("یوزر ونه موندل سو.");
        }

        return Ok(new
        {
            user.FirstName,
            user.LastName,
            user.UserName,
            user.Email,
            user.PhoneNumber,
            user.ProfilePhoto
        });
    }

    [HttpGet("GetUserHistory")]
    public async Task<ActionResult> GetUserHistory()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("یوزر ونه موندل سو.");
        }

        var history = await _context.UserHistories
            .Where(x => x.CreatedByUserId == user.Id)
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

    [HttpGet("CheckDuplicate")]
    public async Task<ActionResult> CheckDuplicate(string userName, string firstName)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("یوزر ونه موندل سو.");
        }

        var normalizedUserName = _userManager.NormalizeName(userName?.Trim());
        var normalizedFirstName = firstName?.Trim().ToUpper();
        return Ok(new
        {
            UserNameExists = !string.IsNullOrWhiteSpace(normalizedUserName) && await _userManager.Users.AnyAsync(x => x.Id != user.Id && x.NormalizedUserName == normalizedUserName),
            FirstNameExists = !string.IsNullOrWhiteSpace(normalizedFirstName) && await _userManager.Users.AnyAsync(x => x.Id != user.Id && x.FirstName.ToUpper() == normalizedFirstName)
        });
    }

    [HttpPut("UpdateProfile")]
    public async Task<ActionResult> UpdateProfile([FromForm] ProfileUpdateViewModel request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("ټول اړین معلومات ولیکئ.");
        }
        else if (!new EmailAddressAttribute().IsValid(request.Email))
        {
            return BadRequest("صحیح ایمېل ولیکئ.");
        }
        else if (request.ImageFile != null && !IsImageFile(request.ImageFile))
        {
            return BadRequest("یوازي عکس قبول کیږي!");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("یوزر ونه موندل سو.");
        }

        var normalizedUserName = _userManager.NormalizeName(request.UserName.Trim());
        var normalizedFirstName = request.FirstName.Trim().ToUpper();
        if (await _userManager.Users.AnyAsync(x => x.Id != user.Id && x.NormalizedUserName == normalizedUserName))
        {
            return BadRequest("دغه یوزر نوم مخکې موجود دی.");
        }
        else if (await _userManager.Users.AnyAsync(x => x.Id != user.Id && x.FirstName.ToUpper() == normalizedFirstName))
        {
            return BadRequest("دغه نوم مخکې موجود دی.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UserName = request.UserName.Trim();
        user.Email = request.Email.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();

        string newFilePath = string.Empty;
        if (request.ImageFile != null)
        {
            var folder = Path.Combine(_environment.WebRootPath, "Profiles");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.ImageFile.FileName)}";
            newFilePath = Path.Combine(folder, fileName);
            await using var stream = new FileStream(newFilePath, FileMode.Create);
            await request.ImageFile.CopyToAsync(stream);
            user.ProfilePhoto = fileName;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            if (!string.IsNullOrEmpty(newFilePath) && System.IO.File.Exists(newFilePath))
            {
                System.IO.File.Delete(newFilePath);
            }
            return BadRequest(string.Join(" ", result.Errors.Select(x => x.Description)));
        }

        await _context.UserHistories.AddAsync(new UserHistory()
        {
            CreatedByUserId = user.Id,
            CreationDate = DateTime.Now,
            ModelName = "پروفایل",
            Details = "د پروفایل معلومات تغیر سول."
        });
        await _context.SaveChangesAsync();
        await _signInManager.RefreshSignInAsync(user);
        return Ok();
    }

    [HttpPut("ChangePassword")]
    public async Task<ActionResult> ChangePassword(ChangePasswordViewModel request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest("اوسنی او نوی پټ نوم ولیکئ.");
        }
        else if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest("نوی پټ نوم او تائید پټ نوم یو شان نه دي.");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("یوزر ونه موندل سو.");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            return BadRequest(string.Join(" ", result.Errors.Select(x => x.Description)));
        }

        await _context.UserHistories.AddAsync(new UserHistory()
        {
            CreatedByUserId = user.Id,
            CreationDate = DateTime.Now,
            ModelName = "پټ نوم",
            Details = "پټ نوم تغیر سو."
        });
        await _context.SaveChangesAsync();
        await _signInManager.RefreshSignInAsync(user);
        return Ok();
    }

    private static bool IsImageFile(IFormFile file)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        return file.Length > 0 && allowedExtensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant());
    }
}
