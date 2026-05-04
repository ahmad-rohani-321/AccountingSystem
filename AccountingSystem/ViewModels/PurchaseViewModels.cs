using System;
using System.Collections.Generic;

namespace AccountingSystem.ViewModels;

public class PurchaseGridRow
{
    public int ID { get; set; }
    public int PurchaseNo { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountCode { get; set; } = string.Empty;
    public int AccountTypeID { get; set; }
    public string AccountTypeName { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int ItemCount { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
}

public class PurchaseSaveRequest
{
    public int? OrderID { get; set; }
    public int PurchaseNo { get; set; }
    public int AccountID { get; set; }
    public int? TreasureAccountID { get; set; }
    public int CurrencyID { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public List<PurchaseSaveDetailRequest> Details { get; set; } = [];
}

public class PurchaseSaveDetailRequest
{
    public int ItemID { get; set; }
    public int? UnitConversionID { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int WarehouseID { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class PurchaseResponse
{
    public int PurchaseID { get; set; }
    public int? OrderID { get; set; }
    public int PurchaseNo { get; set; }
    public int AccountID { get; set; }
    public int? TreasureAccountID { get; set; }
    public int CurrencyID { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public List<PurchaseDetailResponse> Details { get; set; } = [];
}

public class PurchaseDetailResponse
{
    public int ItemID { get; set; }
    public int? UnitConversionID { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int WarehouseID { get; set; }
    public string Remarks { get; set; } = string.Empty;
}
