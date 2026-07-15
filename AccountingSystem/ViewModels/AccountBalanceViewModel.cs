namespace AccountingSystem.ViewModels
{
    public class AccountBalanceViewModel
    {
        public int Id { get; set; }
        public int CurrencyID { get; set; }
        public string CurrencyName { get; set; }
        public decimal Balance { get; set; }
    }
}
