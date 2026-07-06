using System;
using System.ComponentModel.DataAnnotations;

namespace AccountingSystem.Models.Accounts;

public class AccountType
{
    [Key]
    public int ID { get; set; }
    public string Name { get; set; }
}
