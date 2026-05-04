using System.Collections.Generic;

namespace AccountingSystem.ViewModels;

public class CreateItemRequest
{
    public string NativeName { get; set; } = string.Empty;
    public string AliasName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageName { get; set; } = "default.png";
    public decimal MinimumQuantity { get; set; }
    public int CategoryId { get; set; }
    public int UnitId { get; set; }
    public List<UnitConversionRow> UnitConversions { get; set; } = [];
}

public class UnitConversionRow
{
    public int SubUnitID { get; set; }
    public decimal? MainAmount { get; set; }
    public decimal? SubAmount { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class UpdateItemImageRequest
{
    public string ImageName { get; set; } = "default.png";
}

public class InitialStockRowVm
{
    public int ItemID { get; set; }
    public int WarehouseID { get; set; }
    public decimal Quantity { get; set; }
    public int? UnitConversionID { get; set; }
    public string Remarks { get; set; } = string.Empty;
}
