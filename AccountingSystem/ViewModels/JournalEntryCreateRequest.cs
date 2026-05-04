using Microsoft.AspNetCore.Http;

namespace AccountingSystem.ViewModels;

public class JournalEntryCreateRequest
{
    public int DebitCurrencyId { get; set; }
    public int CreditCurrencyId { get; set; }
    public int DebitAccountId { get; set; }
    public int CreditAccountId { get; set; }
    public string Amount { get; set; } = string.Empty;
    public string ExchangeRate { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public IFormFile ChequePhoto { get; set; }
}
