using System;
using System.Collections.Generic;

namespace AccountingSystem.ViewModels;

public class SaleOrderGridRow
{
    public int ID { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public DateTime CreationDate { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}

public class SaleOrderSaveRequest
{
    public int? OrderID { get; set; }
    public int AccountID { get; set; }
    public DateTime DueDate { get; set; }
    public List<SaleOrderSaveDetailRequest> Details { get; set; } = [];
    public string Remarks { get; set; } = string.Empty;
}

public class SaleOrderSaveDetailRequest
{
    public int ItemID { get; set; }
    public int? UnitConversionID { get; set; }
    public decimal Quantity { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class SaleOrderResponse
{
    public int ID { get; set; }
    public int AccountID { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public List<SaleOrderDetailResponse> Details { get; set; } = [];
}

public class SaleOrderDetailResponse
{
    public int ItemID { get; set; }
    public int UnitConversionID { get; set; }
    public decimal Quantity { get; set; }
    public string Remarks { get; set; } = string.Empty;
}
