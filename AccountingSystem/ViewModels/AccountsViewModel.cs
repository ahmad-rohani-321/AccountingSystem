namespace AccountingSystem.ViewModels
{
    public class AccountsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public bool IsActive { get; set; }
        public int AccountTypeId { get; set; }
        public string AccountTypeName { get; set; } = string.Empty;
        public List<AccountBalanceViewModel> Balance { get; set; }
    }
}
