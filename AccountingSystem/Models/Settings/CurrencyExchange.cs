using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Settings;

public class CurrencyExchange : BaseEntity
{
    public int MainCurrencyID { get; set; }
    public int SubCurrencyID { get; set; }
    public decimal MainCurrencyAmount { get; set; }
    public decimal SubCurrencyAmount { get; set; }
    public decimal CurrencyExchangeRate { get; set; }
}