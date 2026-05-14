using System;
using System.Collections.Generic;

namespace AccountingSystem.ViewModels;

public class SaleGridRow
{
    public int ID { get; set; }
    public int SaleNo { get; set; }
    public bool IsHolded { get; set; }
    public bool IsRefunded { get; set; }
    public int AccountID { get; set; }
    public int CurrencyID { get; set; }
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

public class SaleSaveRequest
{
    public int? OrderID { get; set; }
    public int SaleNo { get; set; }
    public bool IsHolded { get; set; }
    public int AccountID { get; set; }
    public int? TreasureAccountID { get; set; }
    public int CurrencyID { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public List<SaleSaveDetailRequest> Details { get; set; } = [];
}

public class SaleSaveDetailRequest
{
    public int ItemID { get; set; }
    public int? UnitConversionID { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int WarehouseID { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class SaleResponse
{
    public int SaleID { get; set; }
    public int? OrderID { get; set; }
    public int SaleNo { get; set; }
    public bool IsHolded { get; set; }
    public int AccountID { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public int? TreasureAccountID { get; set; }
    public int CurrencyID { get; set; }
    public string CurrencyName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public List<SaleDetailResponse> Details { get; set; } = [];
}

public class SaleDetailResponse
{
    public int ItemID { get; set; }
    public int? UnitConversionID { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int WarehouseID { get; set; }
    public string Remarks { get; set; } = string.Empty;
}

public class SaleRefundRequest
{
    public int? TreasureAccountID { get; set; }
    public decimal RefundAmount { get; set; }
}

public class SaleReceiveRequest
{
    public int? TreasureAccountID { get; set; }
    public decimal ReceiveAmount { get; set; }
}
