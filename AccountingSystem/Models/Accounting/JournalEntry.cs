using System;
using System.ComponentModel.DataAnnotations.Schema;
using AccountingSystem.Models.Accounts;

namespace AccountingSystem.Models.Accounting;

public class JournalEntry : BaseEntity
{
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
    public string ChequePhoto { get; set; }
    public string Remarks { get; set; }
    public int TransactionTypeID { get; set; }
    [ForeignKey(nameof(TransactionTypeID))]
    public JournalTransactionType TransactionType { get; set; }
    public int AccountBalanceID { get; set; }
    [ForeignKey(nameof(AccountBalanceID))]
    public AccountBalance AccountBalance { get; set; }
}
