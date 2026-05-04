using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers.APIs;

public abstract class ApiControllerBase : ControllerBase
{
    protected string CurrentUserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
