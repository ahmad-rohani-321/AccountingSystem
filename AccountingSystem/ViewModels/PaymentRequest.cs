    namespace AccountingSystem.ViewModels;
    public class PaymentRequest
    {
        public int Id { get; set; }
        public decimal RecieveAmount { get; set; }
        public string Description { get; set; } = string.Empty;
        public int FeesSource { get; set; }
    }