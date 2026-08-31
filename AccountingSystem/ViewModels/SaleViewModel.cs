namespace AccountingSystem.ViewModels;
public class SaleViewModel
{
    public bool IsHolded { get; set; }
    public bool EffectsStock { get; set; }
    public bool IsRefunded { get; set; }
    public int SaleId { get; set; }
    public int SaleNo { get; set; }
    public DateTime SaleDate { get; set; }
    public int PersonId { get; set; }
    public string PersonName { get; set; }
    public int CurrencyId { get; set; }
    public string CurrencyName { get; set; }
    public int BankId { get; set; }
    public string Remarks { get; set; }
    public decimal SaleTotal { get; set; }
    public decimal SaleRecieved { get; set; }
    public decimal SaleRemaining { get; set; }
    public List<SaleDetailsViewModel> SaleDetails { get; set; }
    public int SaleItemsCount { get; set; }
}