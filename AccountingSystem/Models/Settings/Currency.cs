namespace AccountingSystem.Models.Settings;

public class Currency : BaseEntity
{
    public string CurrencyName { get; set; } = default;
    public string CurrencySymbole { get; set; } = default;
    public bool IsMainCurrency { get; set; }
    public bool IsActive { get; set; } = true;
}
