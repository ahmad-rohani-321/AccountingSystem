using Microsoft.AspNetCore.Identity;

namespace AccountingSystem.Models.Identity;

public class Role : IdentityRole
{
    public string PashtoName { get; set; }
}
