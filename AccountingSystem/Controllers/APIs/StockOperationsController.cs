using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AccountingSystem.Data;
using AccountingSystem.Models.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.APIs;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StockOperationsController(ApplicationDbContext db) : ControllerBase
{
    private readonly ApplicationDbContext _db = db;

    [HttpGet("TransactionTypes")]
    public async Task<IActionResult> GetTransactionTypes()
    {
        var ids = new[] { 2, 3, 9 };
        var rows = await _db.StockTransactionTypes
            .AsNoTracking()
            .Where(t => ids.Contains(t.ID))
            .OrderBy(t => t.ID)
            .Select(t => new { t.ID, t.Name })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("Items")]
    public async Task<IActionResult> GetItems()
    {
        var rows = await _db.Items
            .AsNoTracking()
            .Include(i => i.Unit)
            .OrderBy(i => i.NativeName)
            .Select(i => new
            {
                i.ID,
                i.NativeName,
                i.AliasName,
                MainUnitName = i.Unit != null ? i.Unit.Name : string.Empty,
                DisplayName = (i.NativeName ?? string.Empty) + " - " + (i.AliasName ?? string.Empty)
            })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("StockBalanceItems")]
    public async Task<IActionResult> GetStockBalanceItems()
    {
        var rows = await _db.StockBalances
            .AsNoTracking()
            .Include(sb => sb.Item)
            .ThenInclude(i => i.Unit)
            .Include(sb => sb.Warehouse)
            .Where(sb => sb.Quantity > 0)
            .OrderByDescending(sb => sb.ID)
            .Select(sb => new
            {
                sb.ID,
                sb.ItemID,
                sb.WarehouseID,
                NativeName = sb.Item != null ? sb.Item.NativeName : string.Empty,
                AliasName = sb.Item != null ? sb.Item.AliasName : string.Empty,
                MainUnitName = sb.Item != null && sb.Item.Unit != null ? sb.Item.Unit.Name : string.Empty,
                WarehouseName = sb.Warehouse != null ? sb.Warehouse.Name : string.Empty,
                sb.BatchNo,
                sb.Quantity,
                DisplayName =
                    (sb.Item != null ? sb.Item.NativeName : string.Empty)
                    + " - "
                    + (sb.Warehouse != null ? sb.Warehouse.Name : string.Empty)
                    + " - "
                    + (sb.BatchNo ?? string.Empty)
            })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpPost("RemainingStock")]
    public async Task<IActionResult> SaveRemainingStock([FromBody] RemainingStockOperationVm request)
    {
        try
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            if (request is null)
                return BadRequest(new { errors = new { Request = new[] { "غلط درخواست." } } });

            if (request.TransactionTypeID is not (2 or 3 or 9))
                return BadRequest(new { errors = new { TransactionTypeID = new[] { "د عملياتو نوع ناسم دی." } } });

            if (request.Quantity <= 0)
                return BadRequest(new { errors = new { Quantity = new[] { "تعداد ضروري دی." } } });

            if (request.UnitConversionID is null)
                return BadRequest(new { errors = new { UnitConversionID = new[] { "واحد ضروري دی." } } });

            if (request.TransactionTypeID is 3 or 9)
            {
                if (string.IsNullOrWhiteSpace(request.Notes))
                    return BadRequest(new { errors = new { Notes = new[] { "ملاحظات ضروري دي." } } });
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            if (request.TransactionTypeID == 2)
            {
                if (request.ItemID is null || request.ItemID <= 0)
                    return BadRequest(new { errors = new { ItemID = new[] { "جنس ضروري دی." } } });

                if (request.WarehouseID is null || request.WarehouseID <= 0)
                    return BadRequest(new { errors = new { WarehouseID = new[] { "ګدام ضروري دی." } } });

                var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ID == request.ItemID.Value);
                if (item is null)
                    return NotFound();

                var (exchangedAmount, unitId, err) = await ResolveUnit(request.UnitConversionID.Value, item.ID, item.UnitId);
                if (!string.IsNullOrWhiteSpace(err))
                    return BadRequest(new { errors = new { UnitConversionID = new[] { err } } });

                var batchNo = (request.BatchNo ?? string.Empty).Trim();
                var notes = (request.Notes ?? string.Empty).Trim();

                var inMainQty = request.Quantity / exchangedAmount;
                if (inMainQty <= 0)
                    return BadRequest(new { errors = new { Quantity = new[] { "ØªØ¹Ø¯Ø§Ø¯ Ù†Ø§Ø³Ù… Ø¯ÛŒ." } } });

                // TransactionID=2: add/update by (ItemID, WarehouseID, BatchNo)
                var balance = await _db.StockBalances.FirstOrDefaultAsync(sb =>
                    sb.ItemID == item.ID &&
                    sb.WarehouseID == request.WarehouseID.Value &&
                    (sb.BatchNo ?? string.Empty) == batchNo);

                if (balance is null)
                {
                    balance = new StockBalance
                    {
                        ItemID = item.ID,
                        WarehouseID = request.WarehouseID.Value,
                        BatchNo = batchNo,
                        Remarks = notes,
                        Quantity = inMainQty,
                        CreationDate = DateTime.UtcNow,
                        CreatedByUserId = userId
                    };
                    _db.StockBalances.Add(balance);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    balance.Quantity += inMainQty;
                    if (!string.IsNullOrWhiteSpace(notes))
                        balance.Remarks = notes;
                    _db.StockBalances.Update(balance);
                    await _db.SaveChangesAsync();
                }

                var trx = new StockTransactions
                {
                    StockBalanceID = balance.ID,
                    Quantity = request.Quantity,
                    Remarks = notes,
                    UnitID = unitId,
                    TransactionID = 2,
                    CreationDate = DateTime.UtcNow,
                    CreatedByUserId = userId
                };
                _db.StockTransactions.Add(trx);
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return Ok();
            }

            if (request.StockBalanceID is null || request.StockBalanceID <= 0)
                return BadRequest(new { errors = new { StockBalanceID = new[] { "جنس ضروري دی." } } });

            var stockBalance = await _db.StockBalances
                .Include(sb => sb.Item)
                .FirstOrDefaultAsync(sb => sb.ID == request.StockBalanceID.Value);

            if (stockBalance is null)
                return NotFound();
            if (stockBalance.Item is null)
                return BadRequest(new { errors = new { StockBalanceID = new[] { "ناسم انتخاب." } } });

            var (outExchangedAmount, outUnitId, outErr) = await ResolveUnit(request.UnitConversionID.Value, stockBalance.ItemID, stockBalance.Item.UnitId);
            if (!string.IsNullOrWhiteSpace(outErr))
                return BadRequest(new { errors = new { UnitConversionID = new[] { outErr } } });

            var mainQty = request.Quantity / outExchangedAmount;
            if (mainQty <= 0)
                return BadRequest(new { errors = new { Quantity = new[] { "تعداد ناسم دی." } } });

            if (stockBalance.Quantity - mainQty < 0)
                return BadRequest(new { errors = new { Quantity = new[] { "په ګدام کې کافي موجودي نشته." } } });

            stockBalance.Quantity -= mainQty;
            stockBalance.Remarks = string.IsNullOrWhiteSpace(request.Notes) ? (stockBalance.Remarks ?? string.Empty) : request.Notes.Trim();
            _db.StockBalances.Update(stockBalance);
            await _db.SaveChangesAsync();

            var outTrx = new StockTransactions
            {
                StockBalanceID = stockBalance.ID,
                Quantity = request.Quantity,
                Remarks = (request.Notes ?? string.Empty).Trim(),
                UnitID = outUnitId,
                TransactionID = request.TransactionTypeID,
                CreationDate = DateTime.UtcNow,
                CreatedByUserId = userId
            };
            _db.StockTransactions.Add(outTrx);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();
            return Ok();
        }
        catch
        {
            return StatusCode(500, new { errors = new { Save = new[] { "د ثبت پر مهال خطا وشوه. بیا هڅه وکړئ." } } });
        }
    }

    private async Task<(decimal exchangedAmount, int unitId, string error)> ResolveUnit(int unitConversionId, int itemId, int mainUnitId)
    {
        if (unitConversionId == 0)
            return (1m, mainUnitId, string.Empty);

        var uc = await _db.UnitConversion
            .FirstOrDefaultAsync(u => u.ID == unitConversionId && u.ItemID == itemId);

        if (uc is null)
            return (0m, 0, "د دې جنس لپاره واحد ناسم دی.");

        var exchanged = uc.ExchangedAmount;
        if (exchanged <= 0 && uc.MainAmount > 0 && uc.SubAmount > 0)
        {
            exchanged = uc.SubAmount / uc.MainAmount;
            if (exchanged > 0)
            {
                uc.ExchangedAmount = exchanged;
                await _db.SaveChangesAsync();
            }
        }

        if (exchanged <= 0)
            return (0m, 0, "د واحد تبادله ناسم ده.");

        return (exchanged, uc.SubUnitID, string.Empty);
    }

    private string GetUserId()
    {
        return User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    public class RemainingStockOperationVm
    {
        public int TransactionTypeID { get; set; }
        public int? ItemID { get; set; }
        public int? StockBalanceID { get; set; }
        public int? UnitConversionID { get; set; }
        public string BatchNo { get; set; } = string.Empty;
        public int? WarehouseID { get; set; }
        public decimal Quantity { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
