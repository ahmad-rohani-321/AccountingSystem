using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Accounts;

public class AccountContacts : BaseEntity
{
    public string FirstPhone { get; set; }
    public string SecondPhone { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string NIC { get; set; }

    public int AccountID { get; set; }
    [ForeignKey(nameof(AccountID))]
    public Account Account { get; set; }
}
