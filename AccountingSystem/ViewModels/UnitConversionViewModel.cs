namespace AccountingSystem.ViewModels
{
    public class UnitConversionViewModel
    {
        public int Id { get; set; }
        public string SubUnitName { get; set; }
        public int SubUnitId { get; set; }
        public decimal MainUnitQuantity { get; set; }
        public decimal SubUnitQuantity { get; set; }
        public string Remarks { get; set; }
    }
}