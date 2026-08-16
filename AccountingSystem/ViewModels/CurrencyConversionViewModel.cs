namespace AccountingSystem.ViewModels;
public class CurrencyConversionViewModel
{
    public int CurrencyId { get; set; }
    public string SubCurrencyName { get; set; }
    public decimal MainCurrencyPrice { get; set; }
    public decimal SubCurrencyPrice { get; set; }
    public decimal ExchangedAmount { get; set; }
}