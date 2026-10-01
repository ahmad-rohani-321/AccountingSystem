using AccountingSystem.Data;
using AccountingSystem.Models.Accounting;
using AccountingSystem.Models.Accounts;
using AccountingSystem.Models.Shares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AccountingSystem.Controllers.ApiControllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SharesController(ApplicationDbContext context, IHttpContextAccessor accessor) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IHttpContextAccessor _accessor = accessor;

        public class CreateShareRequest
        {
            public int AccountId { get; set; }
            public int TreasureAccountId { get; set; }
            public int CurrencyId { get; set; }
            public decimal Amount { get; set; }
            public string Remarks { get; set; }
        }

        [HttpGet("GetShares/{accountId}")]
        public async Task<ActionResult> GetShares(int accountId)
        {
            if (!await _context.Accounts.AnyAsync(x => x.ID == accountId && x.AccountTypeID == 8))
            {
                return BadRequest("هیله ده شریک حساب انتخاب کړئ.");
            }

            var shares = await _context.Shares
                .Where(x => x.AccountId == accountId)
                .OrderByDescending(x => x.ID)
                .Select(x => new
                {
                    x.ID,
                    x.CreationDate,
                    x.Amount,
                    x.Remarks,
                    CurrencyName = x.Currency.CurrencyName,
                    CurrencyId = x.CurrencyId
                }).ToListAsync();
            return Ok(shares);
        }

        [HttpPost("Create")]
        public async Task<ActionResult> Create(CreateShareRequest request)
        {
            if (request == null)
            {
                return BadRequest("هیله ده د ونډې معلومات ولیکئ.");
            }
            if (!await _context.Accounts.AnyAsync(x => x.ID == request.AccountId && x.IsActive && x.AccountTypeID == 8))
            {
                return BadRequest("هیله ده فعال شریک حساب انتخاب کړئ.");
            }
            if (!await _context.Accounts.AnyAsync(x => x.ID == request.TreasureAccountId && x.IsActive && (x.AccountTypeID == 1 || x.AccountTypeID == 2)))
            {
                return BadRequest("هیله ده فعاله خزانه انتخاب کړئ.");
            }
            if (!await _context.Currencies.AnyAsync(x => x.ID == request.CurrencyId && x.IsActive))
            {
                return BadRequest("هیله ده فعال اسعار انتخاب کړئ.");
            }
            if (request.Amount <= 0)
            {
                return BadRequest("مبلغ باید له صفر څخه لوړ وي.");
            }
            if (string.IsNullOrWhiteSpace(request.Remarks))
            {
                return BadRequest("تشریحات ضروري دي.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                var date = DateTime.Now;
                var shareholder = await _context.Accounts.FirstAsync(x => x.ID == request.AccountId && x.IsActive && x.AccountTypeID == 8);
                var treasure = await _context.Accounts.FirstAsync(x => x.ID == request.TreasureAccountId && x.IsActive && (x.AccountTypeID == 1 || x.AccountTypeID == 2));
                var shareholderBalance = await _context.AccountBalances.FirstOrDefaultAsync(x => x.AccountID == request.AccountId && x.CurrencyID == request.CurrencyId);
                var treasureBalance = await _context.AccountBalances.FirstOrDefaultAsync(x => x.AccountID == request.TreasureAccountId && x.CurrencyID == request.CurrencyId);

                if (shareholderBalance == null)
                {
                    shareholderBalance = (await _context.AccountBalances.AddAsync(new AccountBalance()
                    {
                        AccountID = request.AccountId,
                        CurrencyID = request.CurrencyId,
                        CreatedByUserId = user,
                        CreationDate = date
                    })).Entity;
                }
                if (treasureBalance == null)
                {
                    treasureBalance = (await _context.AccountBalances.AddAsync(new AccountBalance()
                    {
                        AccountID = request.TreasureAccountId,
                        CurrencyID = request.CurrencyId,
                        CreatedByUserId = user,
                        CreationDate = date
                    })).Entity;
                }
                await _context.SaveChangesAsync();

                shareholderBalance.Balance -= request.Amount;
                treasureBalance.Balance += request.Amount;
                await _context.JournalEntries.AddAsync(new JournalEntry()
                {
                    AccountBalanceID = shareholderBalance.ID,
                    Balance = shareholderBalance.Balance,
                    Debit = request.Amount,
                    Remarks = request.Remarks.Trim(),
                    TransactionTypeID = 11,
                    ChequePhoto = string.Empty,
                    CreatedByUserId = user,
                    CreationDate = date
                });
                await _context.JournalEntries.AddAsync(new JournalEntry()
                {
                    AccountBalanceID = treasureBalance.ID,
                    Balance = treasureBalance.Balance,
                    Credit = request.Amount,
                    Remarks = request.Remarks.Trim(),
                    TransactionTypeID = 11,
                    ChequePhoto = string.Empty,
                    CreatedByUserId = user,
                    CreationDate = date
                });
                await _context.Shares.AddAsync(new Share()
                {
                    AccountId = request.AccountId,
                    CurrencyId = request.CurrencyId,
                    Amount = request.Amount,
                    Remarks = request.Remarks.Trim(),
                    CreatedByUserId = user,
                    CreationDate = date
                });
                await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                {
                    CreatedByUserId = user,
                    CreationDate = date,
                    ModelName = "ونډې",
                    Details = $"د {shareholder.Name} ونډه {request.Amount} د {treasure.Name} خزانې ته ثبت سوه."
                });
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
}
