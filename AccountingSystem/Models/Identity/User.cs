using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AccountingSystem.Models.Identity
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ProfilePhoto { get; set; } = string.Empty;
    }
}