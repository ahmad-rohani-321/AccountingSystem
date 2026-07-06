using Microsoft.AspNetCore.Identity;

namespace AccountingSystem.Models.Identity;

public class UserRole : IdentityUserRole<string>
{
    public DateTime CreationDate { get; set; }
}