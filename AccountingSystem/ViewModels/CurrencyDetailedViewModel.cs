namespace AccountingSystem.ViewModels
{
    public class CurrencyDetailedViewModel
    {
        public int CurrencyId { get; set; }
        public string CurrencyName { get; set; }
        public string CurrencySymbole { get; set; }
        public bool IsMainCurrency { get; set; }
        public bool IsActive { get; set; }
    }
}
