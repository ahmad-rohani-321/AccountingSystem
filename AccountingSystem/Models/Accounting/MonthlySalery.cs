using System;
using AccountingSystem.Models.Accounts;

namespace AccountingSystem.Models.Accounting;

public class MonthlySalery : BaseEntity
{
    public int EmployeeID { get; set; }
    public int Month { get; set; }
    public string Remarks { get; set; }
    public decimal GivenAmount { get; set; }

    public Account Employee { get; set; }
}
