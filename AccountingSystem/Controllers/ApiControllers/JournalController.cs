using AccountingSystem.Data;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography.Xml;

namespace AccountingSystem.Controllers.ApiControllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class JournalController(ApplicationDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IHttpContextAccessor _accessor = accessor;
        private readonly IWebHostEnvironment _environemnt = environment;

        [HttpGet("GetTodayJournal")]
        public async Task<ActionResult> GetTodayJournal()
        {
            var data = (await _context.JournalEntries
                        .Include(x => x.AccountBalance.Account)
                        .Include(x => x.AccountBalance.Currency)
                        .Include(x => x.TransactionType)
                        .Where(x => x.CreationDate.Date == DateTime.Today.Date)
                        .ToArrayAsync())
                        .Select(x => new JournalViewModel()
                        {
                            AccountName = x.AccountBalance.Account.Name,
                            Balance = x.Balance,
                            Credit = x.Credit,
                            Debit = x.Debit,
                            CurrencyName = x.AccountBalance.Currency.CurrencyName,
                            Date = x.CreationDate,
                            Photo = x.ChequePhoto,
                            Remarks = x.Remarks,
                            TransactionTypeName = x.TransactionType.TypeName
                        }).ToList();
            return Ok(data);
        }

        [HttpGet("GetJournalReport")]
        public async Task<ActionResult> GetJournalReport(DateTime startDate, DateTime endDate, int? accountId, int? currencyId)
        {
            var data = new List<JournalViewModel>();
            if ((accountId.HasValue && accountId > 0) && currencyId == 0)
            {
                data = (await _context.JournalEntries
                        .Include(x => x.AccountBalance.Account)
                        .Include(x => x.AccountBalance.Currency)
                        .Include(x => x.TransactionType)
                        .Where(x => x.CreationDate.Date >= startDate.Date && x.CreationDate.Date <= endDate.Date && x.AccountBalance.AccountID == accountId)
                        .ToArrayAsync())
                        .Select(x => new JournalViewModel()
                        {
                            AccountName = x.AccountBalance.Account.Name,
                            Balance = x.Balance,
                            Credit = x.Credit,
                            Debit = x.Debit,
                            CurrencyName = x.AccountBalance.Currency.CurrencyName,
                            Date = x.CreationDate,
                            Photo = x.ChequePhoto,
                            Remarks = x.Remarks,
                            TransactionTypeName = x.TransactionType.TypeName
                        }).ToList();
            }
            else if ((currencyId.HasValue && currencyId > 0) && accountId == 0)
            {
                data = (await _context.JournalEntries
                        .Include(x => x.AccountBalance.Account)
                        .Include(x => x.AccountBalance.Currency)
                        .Include(x => x.TransactionType)
                        .Where(x => x.CreationDate.Date >= startDate.Date && x.CreationDate.Date <= endDate.Date && x.AccountBalance.CurrencyID == currencyId)
                        .ToArrayAsync())
                        .Select(x => new JournalViewModel()
                        {
                            AccountName = x.AccountBalance.Account.Name,
                            Balance = x.Balance,
                            Credit = x.Credit,
                            Debit = x.Debit,
                            CurrencyName = x.AccountBalance.Currency.CurrencyName,
                            Date = x.CreationDate,
                            Photo = x.ChequePhoto,
                            Remarks = x.Remarks,
                            TransactionTypeName = x.TransactionType.TypeName
                        }).ToList();
            }
            else if (accountId.HasValue && accountId > 0 && currencyId.HasValue && currencyId > 0)
            {
                data = (await _context.JournalEntries
                        .Include(x => x.AccountBalance.Account)
                        .Include(x => x.AccountBalance.Currency)
                        .Include(x => x.TransactionType)
                        .Where(x => x.CreationDate.Date >= startDate.Date && x.CreationDate.Date <= endDate.Date && x.AccountBalance.AccountID == accountId && x.AccountBalance.CurrencyID == currencyId)
                        .ToArrayAsync())
                        .Select(x => new JournalViewModel()
                        {
                            AccountName = x.AccountBalance.Account.Name,
                            Balance = x.Balance,
                            Credit = x.Credit,
                            Debit = x.Debit,
                            CurrencyName = x.AccountBalance.Currency.CurrencyName,
                            Date = x.CreationDate,
                            Photo = x.ChequePhoto,
                            Remarks = x.Remarks,
                            TransactionTypeName = x.TransactionType.TypeName
                        }).ToList();
            }
            else
            {
                data = (await _context.JournalEntries
                        .Include(x => x.AccountBalance.Account)
                        .Include(x => x.AccountBalance.Currency)
                        .Include(x => x.TransactionType)
                        .Where(x => x.CreationDate.Date >= startDate.Date && x.CreationDate.Date <= endDate.Date)
                        .ToArrayAsync())
                        .Select(x => new JournalViewModel()
                        {
                            AccountName = x.AccountBalance.Account.Name,
                            Balance = x.Balance,
                            Credit = x.Credit,
                            Debit = x.Debit,
                            CurrencyName = x.AccountBalance.Currency.CurrencyName,
                            Date = x.CreationDate,
                            Photo = x.ChequePhoto,
                            Remarks = x.Remarks,
                            TransactionTypeName = x.TransactionType.TypeName
                        }).ToList();
            }
            return Ok(data);
        }

        [HttpPost("NewJournalEntry")]
        public async Task<ActionResult> NewJournalEntry(JournalEntryRequestModel request)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            string extension = request.Image != null ? Path.GetExtension(request.Image.FileName).ToLowerInvariant() : "";
            string[] allowedExtensions = [".jpg", ".jpeg", ".png"];
            int[] debitAccountTypeIds = [1, 2, 3, 4, 5, 6, 8, 9];
            int[] creditAccountTypeIds = [1, 2, 3, 4, 5, 7, 8, 9];
            if (!await _context.Currencies.AnyAsync(x => x.ID == request.DebitCurrencyId))
            {
                return BadRequest("بردګي اسعار نه دي مو جود");
            }
            else if (!await _context.Currencies.AnyAsync(x => x.ID == request.CreditCurrencyId))
            {
                return BadRequest("رسیدګي اسعار نه دي مو جود");
            }
            else if (!await _context.Accounts.AnyAsync(x => x.ID == request.CreditAccountId))
            {
                return BadRequest("بردګي حساب نه دي مو جود");
            }
            else if (!await _context.Accounts.AnyAsync(x => x.ID == request.DebitAccountId))
            {
                return BadRequest("رسیدګي حساب نه دي مو جود");
            }
            else if(!await _context.Accounts.AnyAsync(x => debitAccountTypeIds.Contains(x.AccountTypeID)))
            {
                return BadRequest("هیله ده صحیح بردګي حساب انتخاب کړئ");
            }
            else if (!await _context.Accounts.AnyAsync(x => creditAccountTypeIds.Contains(x.AccountTypeID)))
            {
                return BadRequest("هیله ده صحیح رسیدګي حساب انتخاب کړئ");
            }
            else if (request.Amount <= 0)
            {
                return BadRequest("مبلغ باید له صفر څخه لوړ وي");
            }
            else if (request.Remarks == "" || request.Remarks == null)
            {
                return BadRequest("تشریحات ضروري دي");
            }
            else if (request.Image != null && !allowedExtensions.Contains(extension))
            {
                return BadRequest("یوازي عکس قبول کیږي!");
            }
            else if (request.DebitCurrencyId == request.CreditCurrencyId && request.CreditAccountId == request.DebitAccountId)
            {
                return BadRequest("ستاسو معامله ناسمه ده");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    DateTime date = request.Date == DateTime.Now.Date ? DateTime.Now : request.Date;
                    string fileName = "default.png";
                    if (request.Image != null)
                    {
                        fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.Image.FileName)}";
                        var path = Path.Combine(_environemnt.WebRootPath, "Journal", fileName);

                        await using var stream = new FileStream(path, FileMode.Create);
                        await request.Image.CopyToAsync(stream);
                    }
                    var creditAccount = await _context.Accounts.FirstOrDefaultAsync(x => x.ID == request.CreditAccountId);
                    var debitAccount = await _context.Accounts.FirstOrDefaultAsync(x => x.ID == request.DebitAccountId);

                    var creditAccountBalance = await _context.AccountBalances.FirstOrDefaultAsync(x => x.CurrencyID == request.CreditCurrencyId && x.AccountID == request.CreditAccountId);
                    var debitAccountBalance = await _context.AccountBalances.FirstOrDefaultAsync(x => x.CurrencyID == request.DebitCurrencyId && x.AccountID == request.DebitAccountId);

                    var creditCurrencyConversion = await _context.CurrencyExchanges.OrderByDescending(z => z.ID).FirstOrDefaultAsync(x => x.SubCurrencyID == request.CreditCurrencyId);
                    var debitCurrencyConversion = await _context.CurrencyExchanges.OrderByDescending(z => z.ID).FirstOrDefaultAsync(x => x.SubCurrencyID == request.DebitCurrencyId);

                    dynamic trnsactionType = DefineTransactionType(request.DebitAccountId, request.CreditAccountId, debitAccount.AccountTypeID, creditAccount.AccountTypeID);

                    decimal exchangeRate = (debitCurrencyConversion == null ? 1 : debitCurrencyConversion.CurrencyExchangeRate) / (creditCurrencyConversion == null ? 1 : creditCurrencyConversion.CurrencyExchangeRate);
                    decimal calcualtedCreditAmount = Math.Round(request.Amount / exchangeRate, Defaults.DefaultDecimals);

                    if (creditAccountBalance == null)
                    {
                        var newAccount  = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance()
                        {
                            AccountID = request.CreditAccountId,
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            CurrencyID = request.CreditCurrencyId
                        });
                        await _context.SaveChangesAsync();
                        creditAccountBalance = newAccount.Entity;
                    }
                    if (debitAccountBalance == null) {
                        var newAccount = await _context.AccountBalances.AddAsync(new Models.Accounts.AccountBalance()
                        {
                            AccountID = request.DebitAccountId,
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            CurrencyID = request.DebitCurrencyId
                        });
                        await _context.SaveChangesAsync();
                        debitAccountBalance = newAccount.Entity;
                    }
                    // entry for debit
                    debitAccountBalance.Balance -= request.Amount;
                    await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                    {
                        AccountBalanceID = debitAccountBalance.ID,
                        Balance = debitAccountBalance.Balance,
                        CreatedByUserId = user,
                        CreationDate = date,
                        Debit = request.Amount,
                        Remarks = request.Remarks,
                        TransactionTypeID = trnsactionType.DTT,
                        ChequePhoto = fileName
                    });

                    if(debitAccount.AccountTypeID == 6)
                    {
                        debitAccountBalance.Balance += request.Amount;
                        await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                        {
                            AccountBalanceID = debitAccountBalance.ID,
                            Balance = debitAccountBalance.Balance,
                            CreatedByUserId = user,
                            CreationDate = date,
                            Credit = request.Amount,
                            Remarks = request.Remarks,
                            TransactionTypeID = trnsactionType.DTT,
                            ChequePhoto = fileName
                        });
                    }

                    
                    // entry for credit
                    creditAccountBalance.Balance += calcualtedCreditAmount;
                    await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                    {
                        AccountBalanceID = creditAccountBalance.ID,
                        Balance = creditAccountBalance.Balance,
                        CreatedByUserId = user,
                        CreationDate = date,
                        Credit = calcualtedCreditAmount,
                        Remarks = request.Remarks,
                        TransactionTypeID = trnsactionType.CTT,
                        ChequePhoto = fileName
                    });

                    if(creditAccount.AccountTypeID == 7)
                    {
                        creditAccountBalance.Balance -= request.Amount;
                        await _context.JournalEntries.AddAsync(new Models.Accounting.JournalEntry()
                        {
                            AccountBalanceID = creditAccountBalance.ID,
                            Balance = creditAccountBalance.Balance,
                            CreatedByUserId = user,
                            CreationDate = date,
                            Debit = calcualtedCreditAmount,
                            Remarks = request.Remarks,
                            TransactionTypeID = trnsactionType.CTT,
                            ChequePhoto = fileName
                        });
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Ok();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(ex.Message);
                }
            }
        }

        private dynamic DefineTransactionType(int da, int ca, int dat, int cat)
        {
            int[] acts = [1, 2, 3, 4, 5, 8];
            int[] acts13 = [1, 2];
            int[] acts3 = [3, 4, 5, 8, 9];
            int[] act11 = [3, 4, 5, 8];
            if(acts13.Contains(dat) && acts13.Contains(cat) && da == ca)
            {
                return new
                {
                    DTT = 2, CTT = 2
                };
            }
            else if(acts13.Contains(dat) && acts13.Contains(cat) && da != ca)
            {
                if (dat == 1)
                {
                    return new
                    {
                        DTT = 4,
                        CTT = 3
                    };
                }
                else
                {
                    return new
                    {
                        DTT = 3,
                        CTT = 4
                    };
                }
            }
            else if (act11.Contains(dat) && act11.Contains(cat) && da == ca)
            {
                return new
                {
                    DTT = 2,
                    CTT = 2
                };
            }
            else if (act11.Contains(dat) && act11.Contains(cat) && da != ca)
            {
                return new
                {
                    DTT = 11,
                    CTT = 11
                };
            }
            else if(dat == 6 && acts13.Contains(cat))
            {
                return new
                {
                    DTT = 13 , CTT = 13
                };
            }
            else if(acts13.Contains(dat) && cat == 7)
            {
                return new
                {
                    DTT = 14 , CTT = 14
                };
            }
            else if(acts13.Contains(dat) && acts3.Contains(cat))
            {
                return new
                {
                    DTT = 4 , CTT = 3
                };
            }
            else if(acts3.Contains(dat) && acts13.Contains(cat))
            {
                return new
                {
                    DTT = 4, CTT = 3
                };
            }
            else
            {
                return null;
            }
        }
    }
}
