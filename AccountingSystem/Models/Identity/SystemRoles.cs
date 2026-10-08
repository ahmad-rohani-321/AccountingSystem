namespace AccountingSystem.Models.Identity;

public static class SystemRoles
{
    public const string Administrator = "Administrator";
    public const string Seller = "Seller";
    public const string Purchaser = "Purchaser";
    public const string WarehouseMan = "Warehouse Man";
    public const string FinanceManager = "Finance Manager";

    public const string Sales = Administrator + "," + Seller;
    public const string Purchases = Administrator + "," + Purchaser;
    public const string Stock = Administrator + "," + WarehouseMan;
    public const string Finance = Administrator + "," + FinanceManager;
    public const string BusinessUsers = Administrator + "," + Seller + "," + Purchaser + "," + WarehouseMan + "," + FinanceManager;

    public static readonly string[] All =
    [
        Administrator,
        Seller,
        Purchaser,
        WarehouseMan,
        FinanceManager
    ];
}
