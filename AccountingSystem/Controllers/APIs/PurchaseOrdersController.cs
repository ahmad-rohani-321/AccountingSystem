using AccountingSystem.Data;
using AccountingSystem.Models.Purchase;
using AccountingSystem.ViewModels;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.APIs;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PurchaseOrdersController(ApplicationDbContext db) : ApiControllerBase
{
    private readonly ApplicationDbContext _db = db;

    private IQueryable<PurchaseOrderDetails> BuildDetailQuery(PurchaseOrder order)
    {
        return _db.PurchaseOrderDetails.Where(d =>
            d.CreatedByUserId == order.CreatedByUserId &&
            d.CreationDate == order.CreationDate);
    }

    private IQueryable<PurchaseOrder> BuildPurchaseOrderGridQuery()
    {
        return _db.PurchaseOrders
            .Include(x => x.Account)
            .AsQueryable();
    }

    private static IQueryable<PurchaseOrder> ApplyCreatedTodayFilter(IQueryable<PurchaseOrder> query)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        return query.Where(x => x.CreationDate >= today && x.CreationDate < tomorrow);
    }

    private static IQueryable<PurchaseOrderGridRow> ProjectPurchaseOrderGridRows(IQueryable<PurchaseOrder> query)
    {
        return query.Select(x => new PurchaseOrderGridRow
        {
            ID = x.ID,
            AccountName = x.Account != null ? x.Account.Name : string.Empty,
            AccountCode = x.Account != null ? x.Account.Code : string.Empty,
            DueDate = x.DueDate.ToDateTime(TimeOnly.MinValue),
            CreationDate = x.CreationDate,
            IsCompleted = x.IsCompleted,
            Remarks = x.Remarks
        });
    }

    [HttpGet("GetToday")]
    public async Task<object> GetToday(DateTime? fromDate, DateTime? toDate, int? accountId)
    {
        var query = BuildPurchaseOrderGridQuery();
        var hasAccountFilter = accountId.HasValue && accountId.Value > 0;
        var hasDateRangeFilter = fromDate.HasValue && toDate.HasValue;

        if (hasAccountFilter)
        {
            query = query.Where(x => x.AccountID == accountId.Value);
        }

        if (hasDateRangeFilter)
        {
            var from = fromDate.Value.Date;
            var toExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreationDate >= from && x.CreationDate < toExclusive);
        }
        else if (!hasAccountFilter)
        {
            query = ApplyCreatedTodayFilter(query);
        }

        return await ProjectPurchaseOrderGridRows(query).ToListAsync();
    }

    [HttpGet("GetCreatedToday")]
    public async Task<object> GetCreatedToday()
    {
        var query = ApplyCreatedTodayFilter(BuildPurchaseOrderGridQuery());
        return await ProjectPurchaseOrderGridRows(query).ToListAsync();
    }

    [HttpGet("GetPending")]
    public async Task<object> GetPending()
    {
        var query = await _db.PurchaseOrders
            .Include(x => x.Account)
            .Where(x => !x.IsCompleted)
            .OrderByDescending(x => x.CreationDate)
            .Select(x => new PurchaseOrderGridRow
            {
                ID = x.ID,
                AccountName = x.Account != null ? x.Account.Name : string.Empty,
                AccountCode = x.Account != null ? x.Account.Code : string.Empty,
                DueDate = x.DueDate.ToDateTime(TimeOnly.MinValue),
                CreationDate = x.CreationDate,
                IsCompleted = x.IsCompleted,
                Remarks = x.Remarks
            }).ToListAsync();

        return query;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var order = await _db.PurchaseOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id);
        if (order is null)
            return NotFound(new { Message = "د پېرود امر ونه موندل شو." });

        var details = await BuildDetailQuery(order)
            .AsNoTracking()
            .OrderBy(x => x.ID)
            .Select(x => new PurchaseOrderDetailResponse
            {
                ItemID = x.ItemID,
                UnitConversionID = x.UnitID,
                Quantity = x.Quantity,
                Remarks = x.Remarks ?? string.Empty
            })
            .ToListAsync();

        return Ok(new PurchaseOrderResponse
        {
            ID = order.ID,
            AccountID = order.AccountID,
            DueDate = order.DueDate.ToDateTime(TimeOnly.MinValue),
            IsCompleted = order.IsCompleted,
            Remarks = order.Remarks ?? string.Empty,
            Details = details
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] PurchaseOrderSaveRequest request)
    {
        if (request is null)
            return BadRequest(new { Message = "غوښتنه ناسمه ده." });

        if (request.AccountID <= 0)
            return BadRequest(new { Message = "حساب اړین دی." });

        if (request.Details is null || request.Details.Count == 0)
            return BadRequest(new { Message = "لږ تر لږه یو قطار اړین دی." });

        var accountExists = await _db.Accounts
            .AsNoTracking()
            .AnyAsync(a => a.ID == request.AccountID && (a.AccountTypeID == 4 || a.AccountTypeID == 5));
        if (!accountExists)
            return BadRequest(new { Message = "ټاکل شوی حساب ناسم دی." });

        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        await using var tx = await _db.Database.BeginTransactionAsync();
        var validatedRows = new List<(int ItemID, int UnitID, decimal Quantity, string Remarks)>();

        foreach (var row in request.Details)
        {
            if (row.ItemID <= 0)
                return BadRequest(new { Message = "جنس اړین دی." });
            if (row.UnitConversionID is null)
                return BadRequest(new { Message = "واحد اړین دی." });
            if (row.Quantity <= 0)
                return BadRequest(new { Message = "مقدار باید له صفر څخه زیات وي." });

            var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ID == row.ItemID && i.IsActive);
            if (item is null)
                return BadRequest(new { Message = "ټاکل شوی جنس ناسم دی." });

            int resolvedUnitId;

            if (row.UnitConversionID.Value == 0)
            {
                var mainUnitConversion = await _db.UnitConversion
                    .FirstOrDefaultAsync(uc => uc.ItemID == row.ItemID && uc.SubUnitID == item.UnitId);

                if (mainUnitConversion is null)
                {
                    mainUnitConversion = new Models.Inventory.UnitConversion
                    {
                        ItemID = row.ItemID,
                        MainUnitId = item.UnitId,
                        SubUnitID = item.UnitId,
                        MainAmount = 1,
                        SubAmount = 1,
                        ExchangedAmount = 1,
                        Remarks = string.Empty,
                        CreationDate = DateTime.UtcNow,
                        CreatedByUserId = userId
                    };
                    _db.UnitConversion.Add(mainUnitConversion);
                    await _db.SaveChangesAsync();
                }

                resolvedUnitId = mainUnitConversion.ID;
            }
            else
            {
                var conversion = await _db.UnitConversion
                    .AsNoTracking()
                    .FirstOrDefaultAsync(uc => uc.ID == row.UnitConversionID.Value && uc.ItemID == row.ItemID);
                if (conversion is null)
                    return BadRequest(new { Message = "ټاکل شوی واحد د دې جنس لپاره ناسم دی." });

                resolvedUnitId = conversion.ID;
            }

            validatedRows.Add((row.ItemID, resolvedUnitId, row.Quantity, row.Remarks?.Trim() ?? string.Empty));
        }

        PurchaseOrder order;
        var now = DateTime.UtcNow;
        var mode = "created";

        if (request.OrderID.HasValue && request.OrderID.Value > 0)
        {
            order = await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.ID == request.OrderID.Value);
            if (order is null)
                return NotFound(new { Message = "د پېرود امر ونه موندل شو." });

            order.AccountID = request.AccountID;
            order.DueDate = DateOnly.FromDateTime(request.DueDate.Date);
            mode = "updated";

            var oldDetails = await BuildDetailQuery(order).ToListAsync();
            if (oldDetails.Count > 0)
            {
                _db.PurchaseOrderDetails.RemoveRange(oldDetails);
                await _db.SaveChangesAsync();
            }
        }
        else
        {
            order = new PurchaseOrder
            {
                AccountID = request.AccountID,
                DueDate = DateOnly.FromDateTime(request.DueDate.Date),
                IsCompleted = false,
                CreationDate = now,
                CreatedByUserId = userId,
                Remarks = request.Remarks
            };
            _db.PurchaseOrders.Add(order);
            await _db.SaveChangesAsync();
        }

        var detailEntities = validatedRows.Select(d => new PurchaseOrderDetails
        {
            ItemID = d.ItemID,
            UnitID = d.UnitID,
            Quantity = d.Quantity,
            Remarks = d.Remarks,
            CreationDate = order.CreationDate,
            CreatedByUserId = order.CreatedByUserId
        }).ToList();

        _db.PurchaseOrderDetails.AddRange(detailEntities);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            OrderID = order.ID,
            Mode = mode,
            Message = mode == "created" ? "د پېرود امر په بریالیتوب جوړ شو." : "د پېرود امر په بریالیتوب نوي شو."
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.PurchaseOrders.FirstOrDefaultAsync(x => x.ID == id);
        if (order is null)
            return NotFound(new { Message = "د پېرود امر ونه موندل شو." });

        var details = await BuildDetailQuery(order).ToListAsync();
        if (details.Count > 0)
        {
            _db.PurchaseOrderDetails.RemoveRange(details);
        }

        _db.PurchaseOrders.Remove(order);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "د پېرود امر په بریالیتوب حذف شو." });
    }

}
