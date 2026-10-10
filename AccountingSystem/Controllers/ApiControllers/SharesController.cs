using AccountingSystem.Data;
using AccountingSystem.Models.Accounting;
using AccountingSystem.Models.Accounts;
using AccountingSystem.Models.Shares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AccountingSystem.Models.Identity;

namespace AccountingSystem.Controllers.ApiControllers
{
    [Authorize(Roles = AccountingSystem.Models.Identity.SystemRoles.Finance)]
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

        public class ShareDivisionAllocation
        {
            public int AccountId { get; set; }
            public string AccountName { get; set; }
            public string AccountCode { get; set; }
            public int SharesCount { get; set; }
            public decimal ShareAmount { get; set; }
            public decimal Weight { get; set; }
            public decimal Percentage { get; set; }
            public decimal Amount { get; set; }
        }

        public class ShareDivisionCalculation
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int MainCurrencyId { get; set; }
            public string MainCurrencyName { get; set; }
            public decimal Sales { get; set; }
            public decimal SaleProfit { get; set; }
            public decimal Purchases { get; set; }
            public decimal Expenses { get; set; }
            public decimal Payables { get; set; }
            public decimal Receivables { get; set; }
            public decimal Salaries { get; set; }
            public decimal DividableAmount { get; set; }
            public decimal TotalWeight { get; set; }
            public List<ShareDivisionAllocation> Allocations { get; set; } = [];
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

        [Authorize(Roles = SystemRoles.Administrator)]
        [HttpGet("DividerPreview")]
        public async Task<ActionResult> DividerPreview()
        {
            try
            {
                return Ok(await CalculateShareDivision());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = SystemRoles.Administrator)]
        [HttpPost("Divide")]
        public async Task<ActionResult> Divide()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var calculation = await CalculateShareDivision();
                if (calculation.Allocations.Count == 0)
                {
                    return BadRequest("د وېش لپاره ونډې نسته.");
                }

                var user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
                var date = DateTime.Now;
                var remarks = $"د {calculation.StartDate:yyyy/MM/dd} څخه تر {calculation.EndDate:yyyy/MM/dd} د ونډو وېش";
                var calculator = (await _context.SharesCalculators.AddAsync(new SharesCalculator()
                {
                    StartDate = calculation.StartDate,
                    EndDate = calculation.EndDate,
                    Expences = calculation.Expenses,
                    OurLoans = calculation.Receivables,
                    OthersLoans = calculation.Payables,
                    Purchases = calculation.Purchases,
                    Sales = calculation.Sales,
                    SaleProfit = calculation.SaleProfit,
                    Saleries = calculation.Salaries,
                    Payables = calculation.Payables,
                    Receivables = calculation.Receivables,
                    DividableAmount = calculation.DividableAmount,
                    MainCurrencyId = calculation.MainCurrencyId,
                    Remarks = remarks,
                    CreatedByUserId = user,
                    CreationDate = date
                })).Entity;
                await _context.SaveChangesAsync();

                foreach (var item in calculation.Allocations)
                {
                    var accountBalance = await _context.AccountBalances.FirstOrDefaultAsync(x =>
                        x.AccountID == item.AccountId && x.CurrencyID == calculation.MainCurrencyId);
                    if (accountBalance == null)
                    {
                        accountBalance = (await _context.AccountBalances.AddAsync(new AccountBalance()
                        {
                            AccountID = item.AccountId,
                            CurrencyID = calculation.MainCurrencyId,
                            CreatedByUserId = user,
                            CreationDate = date
                        })).Entity;
                        await _context.SaveChangesAsync();
                    }

                    accountBalance.Balance -= item.Amount;
                    await _context.SharesDividers.AddAsync(new SharesDivider()
                    {
                        AccountId = item.AccountId,
                        ShareAmount = item.ShareAmount,
                        Weight = item.Weight,
                        Percentage = item.Percentage,
                        Amount = item.Amount,
                        CurrencyId = calculation.MainCurrencyId,
                        SharesCalculatorID = calculator.ID,
                        Remarks = remarks,
                        CreatedByUserId = user,
                        CreationDate = date
                    });
                    await _context.JournalEntries.AddAsync(new JournalEntry()
                    {
                        AccountBalanceID = accountBalance.ID,
                        Balance = accountBalance.Balance,
                        Debit = item.Amount > 0 ? item.Amount : 0,
                        Credit = item.Amount < 0 ? Math.Abs(item.Amount) : 0,
                        TransactionTypeID = 16,
                        ChequePhoto = string.Empty,
                        Remarks = remarks,
                        CreatedByUserId = user,
                        CreationDate = date
                    });
                }

                await _context.UserHistories.AddAsync(new UserHistory()
                {
                    ModelName = "د ونډو وېش",
                    Details = $"{calculation.DividableAmount} {calculation.MainCurrencyName} پر {calculation.Allocations.Count} شریکانو ووېشل سول.",
                    CreatedByUserId = user,
                    CreationDate = date
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

        private async Task<ShareDivisionCalculation> CalculateShareDivision()
        {
            var mainCurrency = await _context.Currencies.AsNoTracking().FirstOrDefaultAsync(x => x.IsMainCurrency && x.IsActive)
                ?? throw new Exception("اصلي اسعار ونه موندل سول.");
            var endDate = DateTime.Now;
            var lastCalculator = await _context.SharesCalculators.AsNoTracking()
                .OrderByDescending(x => x.EndDate).ThenByDescending(x => x.ID).FirstOrDefaultAsync();
            var shares = await _context.Shares.AsNoTracking()
                .Where(x => x.CreationDate <= endDate)
                .Select(x => new { x.AccountId, x.CurrencyId, x.Amount, x.CreationDate, x.Account.Name, x.Account.Code })
                .ToListAsync();
            if (shares.Count == 0)
            {
                throw new Exception("د وېش لپاره ونډې نسته.");
            }

            var startDate = lastCalculator?.EndDate ?? shares.Min(x => x.CreationDate);
            var hasPreviousDivision = lastCalculator != null;
            var exchanges = await _context.CurrencyExchanges.AsNoTracking()
                .Where(x => x.MainCurrencyID == mainCurrency.ID && x.CreationDate <= endDate)
                .OrderByDescending(x => x.CreationDate).ToListAsync();

            decimal ConvertToMain(int currencyId, DateTime date, decimal amount)
            {
                if (currencyId == mainCurrency.ID) return amount;
                var exchange = exchanges.FirstOrDefault(x => x.SubCurrencyID == currencyId && x.CreationDate <= date);
                if (exchange == null || exchange.CurrencyExchangeRate <= 0)
                {
                    throw new Exception("د انتخاب سوو اسعارو لپاره د تبادلې نرخ نسته.");
                }
                return amount / exchange.CurrencyExchangeRate;
            }

            bool IsInPeriod(DateTime date) => hasPreviousDivision
                ? date > startDate && date <= endDate
                : date >= startDate && date <= endDate;

            var sales = await _context.Sales.AsNoTracking()
                .Where(x => !x.IsHolded && !x.IsRefunded && x.CreationDate <= endDate)
                .Select(x => new { x.ID, x.CurrencyID, x.TotalAmount, x.CreationDate }).ToListAsync();
            sales = sales.Where(x => IsInPeriod(x.CreationDate)).ToList();
            var saleIds = sales.Select(x => x.ID).ToList();
            var saleProfits = await _context.SalesDetails.AsNoTracking()
                .Where(x => saleIds.Contains(x.SaleID))
                .Select(x => new { x.Profit, x.Sale.CurrencyID, x.Sale.CreationDate }).ToListAsync();

            var purchases = await _context.Purchases.AsNoTracking()
                .Where(x => !x.IsHolded && !x.IsRefunded && x.CreationDate <= endDate)
                .Select(x => new { x.CurrencyID, x.TotalAmount, x.CreationDate }).ToListAsync();
            purchases = purchases.Where(x => IsInPeriod(x.CreationDate)).ToList();

            var expenses = await _context.JournalEntries.AsNoTracking()
                .Where(x => x.Credit > 0 && x.AccountBalance.Account.AccountTypeID == 7 && x.CreationDate <= endDate)
                .Select(x => new { x.Credit, x.AccountBalance.CurrencyID, x.CreationDate }).ToListAsync();
            expenses = expenses.Where(x => IsInPeriod(x.CreationDate)).ToList();

            int[] peopleAccountTypes = [3, 4, 5, 9];
            var peopleBalances = await _context.AccountBalances.AsNoTracking()
                .Where(x => x.Account.IsActive && peopleAccountTypes.Contains(x.Account.AccountTypeID) && x.Balance != 0)
                .Select(x => new { x.Balance, x.CurrencyID }).ToListAsync();
            var salaries = await _context.Salery.AsNoTracking()
                .Where(x => x.CreationDate <= endDate)
                .Select(x => new { x.GivenAmount, x.CreationDate }).ToListAsync();
            salaries = salaries.Where(x => IsInPeriod(x.CreationDate)).ToList();

            var result = new ShareDivisionCalculation()
            {
                StartDate = startDate,
                EndDate = endDate,
                MainCurrencyId = mainCurrency.ID,
                MainCurrencyName = mainCurrency.CurrencyName,
                Sales = sales.Sum(x => ConvertToMain(x.CurrencyID, x.CreationDate, x.TotalAmount)),
                SaleProfit = saleProfits.Sum(x => ConvertToMain(x.CurrencyID, x.CreationDate, x.Profit)),
                Purchases = purchases.Sum(x => ConvertToMain(x.CurrencyID, x.CreationDate, x.TotalAmount)),
                Expenses = expenses.Sum(x => ConvertToMain(x.CurrencyID, x.CreationDate, x.Credit)),
                Payables = peopleBalances.Where(x => x.Balance > 0).Sum(x => ConvertToMain(x.CurrencyID, endDate, x.Balance)),
                Receivables = peopleBalances.Where(x => x.Balance < 0).Sum(x => ConvertToMain(x.CurrencyID, endDate, x.Balance)),
                Salaries = salaries.Sum(x => x.GivenAmount)
            };
            result.DividableAmount = result.Sales + result.Receivables -
                (result.Salaries + result.Expenses + result.Purchases + result.Payables);

            var weightedShares = shares.Select(x =>
            {
                var effectiveDate = x.CreationDate < startDate ? startDate : x.CreationDate;
                var days = Math.Max(1, (endDate.Date - effectiveDate.Date).Days + 1);
                var amount = ConvertToMain(x.CurrencyId, x.CreationDate, x.Amount);
                return new { x.AccountId, x.Name, x.Code, Amount = amount, Weight = amount * days };
            }).ToList();
            result.TotalWeight = weightedShares.Sum(x => x.Weight);
            if (result.TotalWeight <= 0)
            {
                throw new Exception("د ونډو وزن د وېش لپاره صحیح نه دی.");
            }

            result.Allocations = weightedShares.GroupBy(x => new { x.AccountId, x.Name, x.Code })
                .Select(x => new ShareDivisionAllocation()
                {
                    AccountId = x.Key.AccountId,
                    AccountName = x.Key.Name,
                    AccountCode = x.Key.Code,
                    SharesCount = x.Count(),
                    ShareAmount = Math.Round(x.Sum(y => y.Amount), Defaults.DefaultDecimals),
                    Weight = Math.Round(x.Sum(y => y.Weight), Defaults.DefaultDecimals),
                    Percentage = Math.Round(x.Sum(y => y.Weight) / result.TotalWeight * 100, Defaults.DefaultDecimals),
                    Amount = Math.Round(result.DividableAmount * x.Sum(y => y.Weight) / result.TotalWeight, Defaults.DefaultDecimals)
                }).OrderByDescending(x => x.Amount).ToList();

            var roundingDifference = Math.Round(result.DividableAmount - result.Allocations.Sum(x => x.Amount), Defaults.DefaultDecimals);
            if (result.Allocations.Count > 0) result.Allocations[0].Amount += roundingDifference;
            return result;
        }

    }
}
