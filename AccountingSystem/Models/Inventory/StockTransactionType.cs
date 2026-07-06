using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Inventory;

public class StockTransactionType
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ID { get; set; }
    public string Name { get; set; }
}
