namespace AccountingSystem.ViewModels
{
    public class ItemsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SecondName { get; set; }
        public string Code { get; set; }
        public string SerialNo { get; set; }
        public string Description { get; set; }
        public decimal MinQuantity { get; set; }
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int MainUnitId { get; set; }
        public string MainUnitName { get; set; }
        public string Image { get; set; }
        public IFormFile ImageFile { get; set; }
        public List<UnitConversionViewModel> UnitConversions { get; set; }
    }
}
