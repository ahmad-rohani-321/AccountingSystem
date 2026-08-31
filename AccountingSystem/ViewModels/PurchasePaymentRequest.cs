    namespace AccountingSystem.ViewModels;
    public class PurchasePaymentRequest
    {
        public int PurchaseId { get; set; }
        public decimal RecieveAmount { get; set; }
        public string Description { get; set; } = string.Empty;
        public int FeesSource { get; set; }
    }