using AccountingSystem.Data;
using AccountingSystem.Models.Identity;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AccountingSystem.Controllers.ApiControllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountsController(ApplicationDbContext context, IHttpContextAccessor accessor) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IHttpContextAccessor _contextAccessor = accessor;

        [HttpGet("NextCode")]
        public async Task<ActionResult> GetNextCode()
        {
            string prefix = "ACC";

            var existingCodes = await _context.Accounts
                .Where(a => a.Code.StartsWith(prefix))
                .Select(a => a.Code)
                .ToListAsync();

            var maxNumber = existingCodes
                .Select(code => code.Length > prefix.Length ? code.Substring(prefix.Length) : "0")
                .Select(fragment => int.TryParse(fragment, out var parsed) ? parsed : 0)
                .DefaultIfEmpty(0)
                .Max();

            return Ok($"{prefix}{(maxNumber + 1).ToString($"D{3}")}");
        }

        [HttpGet("GetAccountBalance/{id}")]
        public async Task<ActionResult> GetAccountBalance(int id)
        {
            var balance = await _context.AccountBalances
                .Include(ab => ab.Currency)
                .Where(ab => ab.AccountID == id)
                .Select(ab => new AccountBalanceViewModel
                {
                    CurrencyName = ab.Currency.CurrencyName,
                    Balance = ab.Balance,
                    CurrencyID = ab.CurrencyID,
                    Id = ab.ID
                })
                .ToListAsync();
            return Ok(balance);
        }

        [HttpGet("GetAccountCurrencyBalance/{currencyId}/{accountId}")]
        public async Task<ActionResult> GetAccountCurrencyBalance(int currencyId, int accountId)
        {
            var data = await _context.AccountBalances
                .Include(ab => ab.Currency)
                .FirstOrDefaultAsync(x => x.CurrencyID == currencyId && x.AccountID == accountId);
            var currency = await _context.Currencies.FirstOrDefaultAsync(x => x.ID == currencyId);
            AccountBalanceViewModel balance = new()
            {
                Balance = data == null ? 0 : data.Balance,
                CurrencyName = currency.CurrencyName
            };
            return Ok(balance);
        }

        [HttpGet("PeopleAccount")]
        public async Task<ActionResult> GetPeopleAccount()
        {
            int[] accountTypeLimits = [ 3, 4, 5, 9, 10 ];
            var data = (await _context
                        .AccountContacts
                        .Include(x => x.Account)
                        .ThenInclude(c => c.AccountType)
                        .Where(a => accountTypeLimits.Contains(a.Account.AccountTypeID)).ToArrayAsync())
                        .Select(a => new PeopleAccountViewModel()
                        {
                            AccountTypeId = a.Account.AccountTypeID,
                            AccountTypeName = a.Account.AccountType.Name,
                            Address = a.Address,
                            Code = a.Account.Code,
                            Email = a.Email,
                            FirstPhone = a.FirstPhone,
                            Id = a.Account.ID,
                            Name = a.Account.Name,
                            NIC = a.NIC,
                            SecondPhone = a.SecondPhone,
                            IsActive = a.Account.IsActive,
                            Balance = null
                        }).OrderBy(x => x.Id).ToList();
            return Ok(data);
        }

        [HttpGet("GetSuppliers")]
        public async Task<ActionResult> GetSuppliers()
        {
            int[] accountTypeLimits = [ 4, 5 ];
            var data = (await _context
                        .Accounts
                        .Include(c => c.AccountType)
                        .Where(a => a.IsActive && accountTypeLimits.Contains(a.AccountTypeID)).ToArrayAsync())
                        .Select(a => new PeopleAccountViewModel()
                        {
                            AccountTypeId = a.AccountTypeID,
                            AccountTypeName = a.AccountType.Name,
                            Code = a.Code,
                            Id = a.ID,
                            Name = a.Name,
                            IsActive = a.IsActive,
                            Balance = null
                        }).ToList();
            return Ok(data);
        }

        [HttpGet("GetBankAccounts")]
        public async Task<ActionResult> GetBankAccounts()
        {
            int[] accountTypeLimits = [ 1, 2 ];
            var data = (await _context
                        .Accounts
                        .Include(c => c.AccountType)
                        .Where(a => a.IsActive && accountTypeLimits.Contains(a.AccountTypeID)).ToArrayAsync())
                        .Select(a => new PeopleAccountViewModel()
                        {
                            AccountTypeId = a.AccountTypeID,
                            AccountTypeName = a.AccountType.Name,
                            Code = a.Code,
                            Id = a.ID,
                            Name = a.Name,
                            IsActive = a.IsActive,
                            Balance = null
                        }).ToList();
            return Ok(data);
        }

        [HttpGet("ContributorAccounts")]
        public async Task<ActionResult> GetContributorAccounts()
        {
            var data = (await _context
                        .AccountContacts
                        .Include(x => x.Account)
                        .ThenInclude(c => c.AccountType)
                        .Where(a => a.Account.AccountTypeID == 8).ToArrayAsync())
                        .Select(a => new PeopleAccountViewModel()
                        {
                            AccountTypeId = a.Account.AccountTypeID,
                            AccountTypeName = a.Account.AccountType.Name,
                            Address = a.Address,
                            Code = a.Account.Code,
                            Email = a.Email,
                            FirstPhone = a.FirstPhone,
                            Id = a.Account.ID,
                            Name = a.Account.Name,
                            NIC = a.NIC,
                            SecondPhone = a.SecondPhone,
                            IsActive = a.Account.IsActive,
                            Balance = null
                        }).ToList();
            return Ok(data);
        }

        [HttpGet("NormalAccounts")]
        public async Task<ActionResult> GetNormalAccounts()
        {
            int[] accountTypeLimits = [ 1, 2, 6, 7 ];
            var data = (await _context.Accounts
                        .Include(x => x.AccountType)
                        .Where(a => accountTypeLimits.Contains(a.AccountTypeID)).ToArrayAsync())
                        .Select(a => new AccountsViewModel()
                        {
                            AccountTypeId = a.AccountTypeID,
                            AccountTypeName = a.AccountType.Name,
                            Name = a.Name,
                            Code = a.Code,
                            IsActive = a.IsActive,
                            Id = a.ID,
                            Balance = null
                        }).ToList();
            return Ok(data);
        }

        [HttpGet("GetAccountsList")]
        public async Task<ActionResult> GetAccountsList()
        {
            var data = (await _context.Accounts.Include(x => x.AccountType).Where(x => x.IsActive).ToArrayAsync())
                        .Select(x => new AccountsViewModel
                        {
                            Id = x.ID,
                            Name = x.Name,
                            Code = x.Code,
                            AccountTypeName = x.AccountType.Name,
                            AccountTypeId = x.AccountTypeID
                        }).ToList();
            return Ok(data);
        }

        [HttpPost("CreatePersonAccount")]
        public async Task<ActionResult> CreatePeoplAccount(PeopleAccountViewModel personModel)
        {
            string user = _contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (personModel.Name == null || personModel.Name == string.Empty)
            {
                return BadRequest("نوم حتمی دی.");
            }
            else if (personModel.Code == null || personModel.Code == string.Empty)
            {
                return BadRequest("کوډ حتمی دی.");
            }
            else if (personModel.FirstPhone == null || personModel.FirstPhone == string.Empty)
            {
                return BadRequest("لومړی شمېره حتمی ده.");
            }
            else if (personModel.AccountTypeId == 0 )
            {
                return BadRequest("د حساب ډول حتمی دی.");
            }
            else if (await _context.Accounts.AnyAsync(n => n.Name == personModel.Name))
            {
                return BadRequest("لیکل سوی نوم تکراری دی.");
            }
            else if (await _context.Accounts.AnyAsync(c => c.Code == personModel.Name))
            {
                return BadRequest("لیکل سوی کوډ تکراری دی.");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var person = await _context.Accounts.AddAsync(
                        new Models.Accounts.Account()
                        {
                            Name = personModel.Name,
                            CreatedByUserId = user,
                            AccountTypeID = personModel.AccountTypeId,
                            Code = personModel.Code,
                            CreationDate = DateTime.Now,
                            IsActive = true
                        }
                        );
                    await _context.SaveChangesAsync();
                    await _context.AccountContacts.AddAsync(
                        new Models.Accounts.AccountContacts()
                        {
                            CreationDate = DateTime.Now,
                            FirstPhone = personModel.FirstPhone,
                            SecondPhone = personModel.SecondPhone,
                            Address = personModel.Address,
                            CreatedByUserId = user,
                            AccountID = person.Entity.ID,
                            Email = personModel.Email,
                            NIC = personModel.NIC
                        }
                        );
                    if (personModel.Balance != null || personModel.Balance.Count < 0)
                    {
                        foreach (var balance in personModel.Balance)
                        {
                            if(balance.Balance != 0)
                            {
                                await _context.AccountBalances.AddAsync(
                                new Models.Accounts.AccountBalance()
                                {
                                    Balance = balance.Balance,
                                    AccountID = person.Entity.ID,
                                    CreatedByUserId = user,
                                    CreationDate = DateTime.Now,
                                    CurrencyID = balance.CurrencyID
                                }
                                );
                                if (balance.Balance > 0)
                                {
                                    // do credit
                                    await _context.JournalEntries.AddAsync(
                                        new Models.Accounting.JournalEntry()
                                        {
                                            Credit = balance.Balance,
                                            Balance = balance.Balance,
                                            AccountBalanceID = person.Entity.ID,
                                            CreatedByUserId = user,
                                            Remarks = string.Empty,
                                            TransactionTypeID = 1,
                                            Debit = 0,
                                            ChequePhoto = string.Empty,
                                            CreationDate = DateTime.Now
                                        }
                                        );
                                }
                                else
                                {
                                    // do debit
                                    await _context.JournalEntries.AddAsync(
                                        new Models.Accounting.JournalEntry()
                                        {
                                            Credit = 0,
                                            Balance = balance.Balance,
                                            AccountBalanceID = person.Entity.ID,
                                            CreatedByUserId = user,
                                            Remarks = string.Empty,
                                            TransactionTypeID = 1,
                                            Debit = balance.Balance,
                                            ChequePhoto = string.Empty,
                                            CreationDate = DateTime.Now
                                        }
                                        );
                                }
                            }
                        }
                    }
                    await _context.UserHistories.AddAsync(
                        new Models.Identity.UserHistory()
                        {
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            Details = $"د {personModel.Name} په نوم حساب جوړ سو.",
                            ModelName = "حسابونه"
                        }
                        );
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

        [HttpPost("CreateAccount")]
        public async Task<ActionResult> CreateAccount(AccountsViewModel model)
        {
            string user = _contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (model.Name == null || model.Name == string.Empty)
            {
                return BadRequest("نوم حتمی دی.");
            }
            else if (model.Code == null || model.Code == string.Empty)
            {
                return BadRequest("کوډ حتمی دی.");
            }
            else if (model.AccountTypeId == 0 )
            {
                return BadRequest("د حساب ډول حتمی دی.");
            }
            else if (await _context.Accounts.AnyAsync(n => n.Name == model.Name))
            {
                return BadRequest("لیکل سوی نوم تکراری دی.");
            }
            else if (await _context.Accounts.AnyAsync(c => c.Code == model.Name))
            {
                return BadRequest("لیکل سوی کوډ تکراری دی.");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var person = await _context.Accounts.AddAsync(
                        new Models.Accounts.Account()
                        {
                            Name = model.Name,
                            CreatedByUserId = user,
                            AccountTypeID = model.AccountTypeId,
                            Code = model.Code,
                            CreationDate = DateTime.Now,
                            IsActive = true
                        }
                        );
                    await _context.SaveChangesAsync();
                    if (model.Balance != null || model.Balance.Count < 0)
                    {
                        foreach (var balance in model.Balance)
                        {
                            await _context.AccountBalances.AddAsync(
                                new Models.Accounts.AccountBalance()
                                {
                                    Balance = balance.Balance,
                                    AccountID = person.Entity.ID,
                                    CreatedByUserId = user,
                                    CreationDate = DateTime.Now,
                                    CurrencyID = balance.CurrencyID
                                }
                                );
                            if (balance.Balance > 0)
                            {
                                // do credit
                                await _context.JournalEntries.AddAsync(
                                    new Models.Accounting.JournalEntry()
                                    {
                                        Credit = balance.Balance,
                                        Balance = balance.Balance,
                                        AccountBalanceID = person.Entity.ID,
                                        CreatedByUserId = user,
                                        Remarks = string.Empty,
                                        TransactionTypeID = 1,
                                        Debit = 0,
                                        ChequePhoto = string.Empty,
                                        CreationDate = DateTime.Now
                                    }
                                    );
                            }
                            else
                            {
                                // do debit
                                await _context.JournalEntries.AddAsync(
                                    new Models.Accounting.JournalEntry()
                                    {
                                        Credit = 0,
                                        Balance = balance.Balance,
                                        AccountBalanceID = person.Entity.ID,
                                        CreatedByUserId = user,
                                        Remarks = string.Empty,
                                        TransactionTypeID = 1,
                                        Debit = balance.Balance,
                                        ChequePhoto = string.Empty,
                                        CreationDate = DateTime.Now
                                    }
                                    );
                            }
                        }
                    }
                    await _context.UserHistories.AddAsync(
                        new Models.Identity.UserHistory()
                        {
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            Details = $"د {model.Name} په نوم حساب جوړ سو.",
                            ModelName = "حسابونه"
                        }
                        );
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

        [HttpPut("UpdateAccountActivation/{id}")]
        public async Task<ActionResult> UpdateAccountActivation(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
            {
                return BadRequest("حساب ونه موندل سو.");
            }
            account.IsActive = !account.IsActive;
            string activeStatus = account.IsActive ? "فعال" : "غیر فعال";
            string user = _contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            await _context.UserHistories.AddAsync(
                new Models.Identity.UserHistory()
                {
                    CreatedByUserId = user,
                    CreationDate = DateTime.Now,
                    Details = $"د {account.Name} په نوم حساب {activeStatus} سو.",
                    ModelName = "حسابونه"
                }
                );
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPut("UpdatePersonAccount")]
        public async Task<ActionResult> UpdatePersonAccount(PeopleAccountViewModel personModel)
        {
            string user = _contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var account = await _context.Accounts.FindAsync(personModel.Id);
            if (account == null)
            {
                return BadRequest("حساب ونه موندل سو.");
            }
            else if (personModel.Name == null || personModel.Name == string.Empty)
            {
                return BadRequest("نوم حتمی دی.");
            }
            else if (personModel.Code == null || personModel.Code == string.Empty)
            {
                return BadRequest("کوډ حتمی دی.");
            }
            else if (personModel.FirstPhone == null || personModel.FirstPhone == string.Empty)
            {
                return BadRequest("لومړی شمېره حتمی ده.");
            }
            else if (personModel.AccountTypeId == 0)
            {
                return BadRequest("د حساب ډول حتمی دی.");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    account.Name = personModel.Name;
                    account.AccountTypeID = personModel.AccountTypeId;
                    account.Code = personModel.Code;
                    await _context.SaveChangesAsync();

                    var contact = await _context.AccountContacts.FirstOrDefaultAsync(c => c.AccountID == account.ID);
                    contact.FirstPhone = personModel.FirstPhone;
                    contact.SecondPhone = personModel.SecondPhone;
                    contact.Address = personModel.Address;
                    contact.Email = personModel.Email;
                    contact.NIC = personModel.NIC;


                    await _context.UserHistories.AddAsync(
                        new Models.Identity.UserHistory()
                        {
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            Details = $"د {personModel.Name} په نوم حساب تغیر سو.",
                            ModelName = "حسابونه"
                        }
                        );

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

        [HttpPut("UpdateAccount")]
        public async Task<ActionResult> UpdateAccount(AccountsViewModel model)
        {
            string user = _contextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var account = await _context.Accounts.FindAsync(model.Id);
            if (account == null)
            {
                return BadRequest("حساب ونه موندل سو.");
            }
            else if (model.Name == null || model.Name == string.Empty)
            {
                return BadRequest("نوم حتمی دی.");
            }
            else if (model.Code == null || model.Code == string.Empty)
            {
                return BadRequest("کوډ حتمی دی.");
            }
            else if (model.AccountTypeId == 0)
            {
                return BadRequest("د حساب ډول حتمی دی.");
            }
            else
            {
                try
                {
                    account.Name = model.Name;
                    account.AccountTypeID = model.AccountTypeId;
                    account.Code = model.Code;


                    await _context.UserHistories.AddAsync(
                        new Models.Identity.UserHistory()
                        {
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            Details = $"د {model.Name} په نوم حساب تغیر سو.",
                            ModelName = "حسابونه"
                        }
                        );

                    await _context.SaveChangesAsync();
                    return Ok();
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
        }

        #region Load Account types
        [HttpGet("GetPeopleAccountTypes")]
        public async Task<ActionResult> GetPeopleAccountTypes()
        {
            int[] typeIds = new int[] { 3, 4, 5, 9 };
            var list = await _context.AccountTypes.Where(x => typeIds.Contains(x.ID)).ToListAsync();
            return Ok(list);
        }
        [HttpGet("GetTreasureAccountTypes")]
        public async Task<ActionResult> GetTreasureAccountTypes()
        {
            int[] typeIds = new int[] { 1, 2, 6, 7 };
            var list = await _context.AccountTypes.Where(x => typeIds.Contains(x.ID)).ToListAsync();
            return Ok(list);
        }
        [HttpGet("GetContributorAccountTypes")]
        public async Task<ActionResult> GetContributorAccountTypes()
        {
            int[] typeIds = new int[] { 8 };
            var list = await _context.AccountTypes.Where(x => typeIds.Contains(x.ID)).ToListAsync();
            return Ok(list);
        }
        [HttpGet("GetPurchaseExpenseAccountTypes")]
        public async Task<ActionResult> GetPurchaseExpenseAccountTypes()
        {
            int[] typeIds = new int[] { 11 };
            var list = await _context.AccountTypes.Where(x => typeIds.Contains(x.ID)).ToListAsync();
            return Ok(list);
        }
        [HttpGet("GetTransactionTypes")]
        public async Task<ActionResult> GetTransactionTypes()
        {
            return Ok(await _context.JournalEntryTransactionTypes.ToListAsync());
        }
        #endregion
    }
}
