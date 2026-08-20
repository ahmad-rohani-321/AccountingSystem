namespace AccountingSystem.ViewModels;
public class PurchaseViewModel
{
    public bool IsHolded { get; set; }
    public bool EffectsStock { get; set; }
    public int PurchaseId { get; set; }
    public int PurchaseNo { get; set; }
    public DateTime PurchaseDate { get; set; }
    public int PersonId { get; set; }
    public string PersonName { get; set; }
    public int CurrencyId { get; set; }
    public string CurrencyName { get; set; }
    public int BankId { get; set; }
    public string Remarks { get; set; }
    public decimal PurchaseTotal { get; set; }
    public decimal PurchaseRecieved { get; set; }
    public decimal PurchaseRemaining { get; set; }
    public List<PurchaseDetailsViewModel> PurchaseDetails { get; set; }
}