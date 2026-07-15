namespace AccountingSystem.ViewModels;
public class PeopleAccountViewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public string FirstPhone { get; set; }
    public string SecondPhone { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string NIC { get; set; }
    public int AccountTypeId { get; set; }
    public string AccountTypeName { get; set; }
    public bool IsActive { get; set; }
    public List<AccountBalanceViewModel> Balance { get; set; }
}