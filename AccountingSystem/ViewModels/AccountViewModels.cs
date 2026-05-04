using System.Collections.Generic;

namespace AccountingSystem.ViewModels;

public static class AccountDefinitions
{
public static readonly int[] AllowedAccountTypeIds = { 3, 4, 5, 9 };
    public const string DefaultCodePrefix = "ACC";
}

public class AccountTypeOption
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AccountListItem
{
    public int AccountID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int AccountTypeID { get; set; }
    public string AccountTypeName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int? AccountContactID { get; set; }
    public string NIC { get; set; } = string.Empty;
    public string FirstPhone { get; set; } = string.Empty;
    public string SecondPhone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class AccountBalancePayload
{
    public int CurrencyID { get; set; }
    public decimal Amount { get; set; }
}

public class AccountCreateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NIC { get; set; } = string.Empty;
    public string FirstPhone { get; set; } = string.Empty;
    public string SecondPhone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int AccountTypeID { get; set; }
    public List<AccountBalancePayload> Balances { get; set; } = new();
}
