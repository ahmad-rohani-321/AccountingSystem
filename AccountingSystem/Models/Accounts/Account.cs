using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Accounts;

public class Account : BaseEntity
{
    public string Name { get; set; }
    public string Code { get; set; }
    public bool IsActive { get; set; }
    public int AccountTypeID { get; set; }
    [ForeignKey(nameof(AccountTypeID))]
    public AccountType AccountType { get; set; }
}