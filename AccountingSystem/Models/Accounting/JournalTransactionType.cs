using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AccountingSystem.Models.Accounting;

public class JournalTransactionType
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ID { get; set; }
    public string TypeName { get; set; }
}
