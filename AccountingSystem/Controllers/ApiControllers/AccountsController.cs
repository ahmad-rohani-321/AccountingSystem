using AccountingSystem.Data;
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
        #endregion
    }
}
