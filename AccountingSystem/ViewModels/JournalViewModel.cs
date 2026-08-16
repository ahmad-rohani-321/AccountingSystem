namespace AccountingSystem.ViewModels;

public class JournalViewModel
{
    public DateTime Date { get; set; }
    public string AccountName { get; set; }
    public string CurrencyName { get; set; }
    public string TransactionTypeName { get; set; }
    public decimal Credit { get; set; }
    public decimal Debit { get; set; }
    public decimal Balance { get; set; }
    public string Remarks { get; set; }
    public string Photo { get; set; }
}
