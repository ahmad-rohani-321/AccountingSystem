using System;

namespace AccountingSystem.Models.Settings;

public class CurrencyExchangeVM
{
    public string SubCurrencyName { get; set; } = default!;
    public int SubCurrencyID { get; set; }
    public decimal SubCurrencyAmount { get; set; }
    public decimal MainCurrencyAmount { get; set; }
    public decimal ExchangeRate { get; set; }
}
