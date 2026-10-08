using System;

namespace AccountingSystem.Models.Shares;

public class ProfitDivider : BaseEntity
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Remarks { get; set; }
}
