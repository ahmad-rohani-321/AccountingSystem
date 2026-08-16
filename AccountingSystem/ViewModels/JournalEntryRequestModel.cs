namespace AccountingSystem.ViewModels
{
    public class JournalEntryRequestModel
    {
        public DateTime Date { get; set; }
        public int CreditCurrencyId { get; set; }
        public int DebitCurrencyId { get; set; }
        public int CreditAccountId { get; set; }
        public int DebitAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Remarks { get; set; }
        public IFormFile Image { get; set; }
    }
}
