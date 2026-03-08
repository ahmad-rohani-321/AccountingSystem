using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using AccountingSystem.Data;
using AccountingSystem.Models.Inventory;
using AccountingSystem.Repository.Inventory;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Controllers.APIs
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ItemsController(
        IHttpContextAccessor accessor,
        ApplicationDbContext db,
        IItemsRepository itemsRepository,
        IWebHostEnvironment env) : ControllerBase
    {
        private readonly IHttpContextAccessor _accessor = accessor;
        private readonly ApplicationDbContext _db = db;
        private readonly IItemsRepository _items = itemsRepository;
        private readonly IWebHostEnvironment _env = env;

        [HttpGet]
        public async Task<object> Get(DataSourceLoadOptions loadOptions)
        {
            var data = await _db.Items
                .Include(i => i.Category)
                .Include(i => i.Unit)
                .Include(i => i.CreatedByUser)
                .AsNoTracking()
                .ToListAsync();

            var itemIdsWithOps = await _db.StockBalances
                .AsNoTracking()
                .Select(sb => sb.ItemID)
                .Distinct()
                .ToListAsync();

            var opsSet = itemIdsWithOps.ToHashSet();

            var shaped = data.Select(i => new
            {
                i.ID,
                i.ImageName,
                i.NativeName,
                i.AliasName,
                i.SKU,
                i.SerialNumber,
                i.Description,
                i.IsActive,
                i.MinimumQuantity,
                i.CategoryId,
                Category = i.Category,
                i.UnitId,
                Unit = i.Unit,
                i.CreationDate,
                CreatedByUser = i.CreatedByUser,
                HasStockOperations = opsSet.Contains(i.ID)
            });

            return DataSourceLoader.Load(shaped, loadOptions);
        }

        [HttpGet("{id:int}/UnitConversions")]
        public async Task<IActionResult> GetUnitConversions(int id)
        {
            var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ID == id);
            if (item is null)
                return NotFound();

            var existing = await _db.UnitConversion
                .AsNoTracking()
                .Where(uc => uc.ItemID == id)
                .ToListAsync();

            var existingUnitIds = existing.Select(x => x.SubUnitID).Distinct().ToList();

            var units = await _db.Units
                .AsNoTracking()
                .Where(u => u.IsActive || u.ID == item.UnitId || existingUnitIds.Contains(u.ID))
                .OrderBy(u => u.Name)
                .ToListAsync();

            var bySubUnit = existing.ToDictionary(x => x.SubUnitID, x => x);

            var rows = units.Select(u =>
            {
                bySubUnit.TryGetValue(u.ID, out var uc);
                return new
                {
                    SubUnitID = u.ID,
                    UnitName = u.Name,
                    MainUnitId = item.UnitId,
                    MainAmount = uc is null ? (decimal?)null : uc.MainAmount,
                    SubAmount = uc is null ? (decimal?)null : uc.SubAmount,
                    Remarks = uc is null ? string.Empty : (uc.Remarks ?? string.Empty)
                };
            });

            return Ok(rows);
        }

        [HttpGet("{id:int}/InitialStock/Allowed")]
        public async Task<IActionResult> InitialStockAllowed(int id)
        {
            var itemExists = await _db.Items.AsNoTracking().AnyAsync(i => i.ID == id);
            if (!itemExists)
                return NotFound();

            var hasOps = await _db.StockBalances.AsNoTracking().AnyAsync(sb => sb.ItemID == id);
            return Ok(new { Allowed = !hasOps });
        }

        [HttpGet("{id:int}/InitialStock/UnitOptions")]
        public async Task<IActionResult> GetInitialStockUnitOptions(int id)
        {
            var item = await _db.Items
                .AsNoTracking()
                .Include(i => i.Unit)
                .FirstOrDefaultAsync(i => i.ID == id);
            if (item is null)
                return NotFound();

            var conversions = await _db.UnitConversion
                .AsNoTracking()
                .Include(uc => uc.SubUnit)
                .Where(uc => uc.ItemID == id)
                .ToListAsync();

            var hasMainUnitConversion = conversions.Any(c => c.SubUnitID == item.UnitId);

            var options = new List<object>();

            if (!hasMainUnitConversion)
            {
                options.Add(new
                {
                    UnitConversionID = 0,
                    UnitID = item.UnitId,
                    UnitName = item.Unit?.Name ?? string.Empty,
                    ExchangedAmount = 1m
                });
            }

            options.AddRange(conversions
                .Select(c =>
                {
                    var exchanged = c.ExchangedAmount;
                    if (exchanged <= 0 && c.MainAmount > 0 && c.SubAmount > 0)
                        exchanged = c.SubAmount / c.MainAmount;

                    return new
                    {
                        UnitConversionID = c.ID,
                        UnitID = c.SubUnitID,
                        UnitName = c.SubUnit?.Name ?? string.Empty,
                        ExchangedAmount = exchanged
                    };
                })
                .Where(o => o.ExchangedAmount > 0));

            return Ok(options);
        }

        [HttpPut("{id:int}/InitialStock")]
        public async Task<IActionResult> SaveInitialStock(int id, [FromBody] List<InitialStockRowVm> rows)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var item = await _db.Items.AsNoTracking().FirstOrDefaultAsync(i => i.ID == id);
            if (item is null)
                return NotFound();

            var hasOps = await _db.StockBalances.AsNoTracking().AnyAsync(sb => sb.ItemID == id);
            if (hasOps)
                return BadRequest(new { errors = new { InitialStock = new[] { "Initial stock is only allowed when no operations exist." } } });

            rows ??= [];
            if (rows.Count == 0)
                return BadRequest(new { errors = new { InitialStock = new[] { "At least one row is required." } } });

            await using var tx = await _db.Database.BeginTransactionAsync();

            foreach (var row in rows)
            {
                if (row.WarehouseID <= 0)
                    return BadRequest(new { errors = new { WarehouseID = new[] { "Warehouse is required." } } });
                if (row.Quantity <= 0)
                    return BadRequest(new { errors = new { Quantity = new[] { "Quantity is required." } } });
                if (row.UnitConversionID is null)
                    return BadRequest(new { errors = new { UnitConversionID = new[] { "Unit is required." } } });

                decimal exchangedAmount;
                int unitId;

                if (row.UnitConversionID.Value == 0)
                {
                    exchangedAmount = 1m;
                    unitId = item.UnitId;
                }
                else
                {
                    var uc = await _db.UnitConversion.AsNoTracking().FirstOrDefaultAsync(u => u.ID == row.UnitConversionID.Value && u.ItemID == id);
                    if (uc is null)
                        return BadRequest(new { errors = new { UnitConversionID = new[] { "Invalid unit for this item." } } });
                    exchangedAmount = uc.ExchangedAmount;
                    unitId = uc.SubUnitID;
                }

                if (exchangedAmount <= 0)
                    return BadRequest(new { errors = new { UnitConversionID = new[] { "Invalid exchanged amount." } } });

                var balance = new StockBalance
                {
                    ItemID = id,
                    WarehouseID = row.WarehouseID,
                    BatchNo = row.BatchNo?.Trim() ?? string.Empty,
                    Remarks = row.Remarks?.Trim() ?? string.Empty,
                    Quantity = row.Quantity / exchangedAmount,
                    CreationDate = DateTime.UtcNow,
                    CreatedByUserId = userId
                };
                _db.StockBalances.Add(balance);
                await _db.SaveChangesAsync();

                var trx = new StockTransactions
                {
                    StockBalanceID = balance.ID,
                    Quantity = row.Quantity,
                    Remarks = row.Remarks?.Trim() ?? string.Empty,
                    UnitID = unitId,
                    TransactionID = 1,
                    CreationDate = DateTime.UtcNow,
                    CreatedByUserId = userId
                };
                _db.StockTransactions.Add(trx);
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return Ok();
        }

        [HttpPut("{id:int}/UnitConversions")]
        public async Task<IActionResult> SaveUnitConversions(int id, [FromBody] List<UnitConversionRow> rows)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var item = await _db.Items.FirstOrDefaultAsync(i => i.ID == id);
            if (item is null)
                return NotFound();

            rows ??= [];

            foreach (var r in rows)
            {
                var hasMain = r.MainAmount.HasValue;
                var hasSub = r.SubAmount.HasValue;
                if (!hasMain && !hasSub)
                    continue;

                if (hasMain != hasSub)
                    return BadRequest(new { errors = new { UnitConversions = new[] { "Both main and sub values must be filled." } } });

                if (r.MainAmount <= 0 || r.SubAmount <= 0)
                    return BadRequest(new { errors = new { UnitConversions = new[] { "Unit conversions must be greater than zero." } } });
            }

            await using var tx = await _db.Database.BeginTransactionAsync();

            var existing = await _db.UnitConversion.Where(uc => uc.ItemID == id).ToListAsync();
            var existingBySub = existing.ToDictionary(x => x.SubUnitID, x => x);

            var activeUnitIds = await _db.Units.AsNoTracking()
                .Where(u => u.IsActive)
                .Select(u => u.ID)
                .ToListAsync();
            var activeUnitSet = activeUnitIds.ToHashSet();

            var rowBySub = rows.ToDictionary(r => r.SubUnitID, r => r);

            foreach (var (subUnitId, uc) in existingBySub)
            {
                if (!rowBySub.TryGetValue(subUnitId, out var row) || (!row.MainAmount.HasValue && !row.SubAmount.HasValue))
                    _db.UnitConversion.Remove(uc);
            }

            foreach (var row in rows)
            {
                var hasMain = row.MainAmount.HasValue;
                var hasSub = row.SubAmount.HasValue;
                if (!hasMain && !hasSub)
                    continue;

                var isExistingConversion = existingBySub.ContainsKey(row.SubUnitID);
                var isItemMainUnit = row.SubUnitID == item.UnitId;
                if (!isExistingConversion && !isItemMainUnit && !activeUnitSet.Contains(row.SubUnitID))
                    return BadRequest(new { errors = new { UnitConversions = new[] { "Unit is inactive and cannot be newly used." } } });

                if (!existingBySub.TryGetValue(row.SubUnitID, out var uc))
                {
                    uc = new UnitConversion
                    {
                        ItemID = id,
                        CreationDate = DateTime.UtcNow,
                        CreatedByUserId = userId
                    };
                    _db.UnitConversion.Add(uc);
                }

                uc.MainUnitId = item.UnitId;
                uc.SubUnitID = row.SubUnitID;
                uc.MainAmount = row.MainAmount!.Value;
                uc.SubAmount = row.SubAmount!.Value;
                uc.ExchangedAmount = row.SubAmount.Value / row.MainAmount.Value;
                uc.Remarks = row.Remarks?.Trim() ?? string.Empty;
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok();
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var entity = await _db.Items.FirstOrDefaultAsync(i => i.ID == key);
            if (entity is null)
                return NotFound();

            ApplyValues(entity, values);

            if (entity.MinimumQuantity < 0)
                ModelState.AddModelError(nameof(Item.MinimumQuantity), "د کمښت خبرتیا باید د صفر څخه کمه نه وي.");

            var uniqueErrors = await GetUniqueErrorsAsync(entity.NativeName, entity.SKU, entity.SerialNumber, key);
            foreach (var err in uniqueErrors)
                ModelState.AddModelError(err.Field, err.Message);

            var categoryActive = await _db.Categories.AsNoTracking().AnyAsync(c => c.ID == entity.CategoryId && c.IsActive);
            if (!categoryActive)
                ModelState.AddModelError(nameof(Item.CategoryId), "کټیګوري باید فعاله وي.");

            var unitActive = await _db.Units.AsNoTracking().AnyAsync(u => u.ID == entity.UnitId && u.IsActive);
            if (!unitActive)
                ModelState.AddModelError(nameof(Item.UnitId), "واحد باید فعال وي.");

            if (!TryValidateModel(entity) || !ModelState.IsValid)
                return BadRequest(ModelState);

            await _items.UpdateItemAsync(entity);
            return Ok(entity);
        }

	        [HttpPost("CreateWithConversions")]
        public async Task<IActionResult> CreateWithConversions([FromBody] CreateItemRequest request)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

	            var item = new Item
	            {
	                NativeName = request.NativeName?.Trim() ?? string.Empty,
	                AliasName = request.AliasName?.Trim() ?? string.Empty,
	                SKU = request.SKU?.Trim() ?? string.Empty,
	                SerialNumber = request.SerialNumber?.Trim() ?? string.Empty,
	                Description = request.Description?.Trim() ?? string.Empty,
                    ImageName = string.IsNullOrWhiteSpace(request.ImageName) ? "default.png" : request.ImageName.Trim(),
	                MinimumQuantity = request.MinimumQuantity,
	                CategoryId = request.CategoryId,
	                UnitId = request.UnitId,
                    IsActive = true,
	                CreationDate = DateTime.UtcNow,
	                CreatedByUserId = userId
	            };

	            if (string.IsNullOrWhiteSpace(item.NativeName))
	                ModelState.AddModelError(nameof(Item.NativeName), "NativeName is required.");
	            // AliasName is optional.
	            if (string.IsNullOrWhiteSpace(item.SKU))
	                item.SKU = await GenerateNextSkuAsync();
	            if (item.MinimumQuantity < 0)
	                ModelState.AddModelError(nameof(Item.MinimumQuantity), "MinimumQuantity cannot be less than zero.");
	            if (item.CategoryId <= 0)
	                ModelState.AddModelError(nameof(Item.CategoryId), "CategoryId is required.");
                if (item.UnitId <= 0)
                ModelState.AddModelError(nameof(Item.UnitId), "UnitId is required.");

                var categoryActive = await _db.Categories.AsNoTracking().AnyAsync(c => c.ID == item.CategoryId && c.IsActive);
                if (!categoryActive)
                    ModelState.AddModelError(nameof(Item.CategoryId), "Category must be active.");

                var unitActive = await _db.Units.AsNoTracking().AnyAsync(u => u.ID == item.UnitId && u.IsActive);
                if (!unitActive)
                    ModelState.AddModelError(nameof(Item.UnitId), "Unit must be active.");

            var uniqueErrors = await GetUniqueErrorsAsync(item.NativeName, item.SKU, item.SerialNumber, null);
            foreach (var err in uniqueErrors)
                ModelState.AddModelError(err.Field, err.Message);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

	            var conversions = new List<UnitConversion>();
	            if (request.UnitConversions is { Count: > 0 })
	            {
	                foreach (var row in request.UnitConversions)
                {
                    var hasMain = row.MainAmount.HasValue;
                    var hasSub = row.SubAmount.HasValue;
                    if (!hasMain && !hasSub)
                        continue;

                    if (hasMain != hasSub)
                    {
                        ModelState.AddModelError("UnitConversions", "Both main and sub values must be filled for unit conversions.");
                        return BadRequest(ModelState);
                    }

                    if (row.MainAmount <= 0 || row.SubAmount <= 0)
                    {
                        ModelState.AddModelError("UnitConversions", "Unit conversions must be greater than zero.");
                        return BadRequest(ModelState);
                    }

	                    conversions.Add(new UnitConversion
	                    {
	                        MainUnitId = item.UnitId,
	                        SubUnitID = row.SubUnitID,
	                        MainAmount = row.MainAmount.Value,
	                        SubAmount = row.SubAmount.Value,
	                        ExchangedAmount = row.SubAmount.Value / row.MainAmount.Value,
	                        Remarks = row.Remarks?.Trim() ?? string.Empty,
	                        CreationDate = DateTime.UtcNow,
	                        CreatedByUserId = userId
	                    });
	                }
	            }

	            // Ensure at least one unit conversion row exists for the main unit.
	            if (conversions.Count == 0)
	            {
	                conversions.Add(new UnitConversion
	                {
	                    MainUnitId = item.UnitId,
	                    SubUnitID = item.UnitId,
	                    MainAmount = 1,
	                    SubAmount = 1,
	                    ExchangedAmount = 1,
	                    Remarks = string.Empty,
	                    CreationDate = DateTime.UtcNow,
	                    CreatedByUserId = userId
	                });
	            }

	            await _items.AddItemAsync(item, conversions);
	            return Ok(item);
	        }

            [HttpPost("UploadImage")]
            public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
            {
                if (file is null || file.Length == 0)
                    return BadRequest("File is required.");

                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
                ext = ext == ".jpeg" ? ".jpg" : ext;
                if (ext is not ".png" and not ".jpg")
                    return BadRequest("Only .png and .jpg files are allowed.");

                var imagesRoot = Path.Combine(_env.WebRootPath, "images", "itemsfolder");
                Directory.CreateDirectory(imagesRoot);

                var fileName = $"{Guid.NewGuid():D}{ext}";
                var fullPath = Path.Combine(imagesRoot, fileName);

                await using (var stream = System.IO.File.Create(fullPath))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { fileName });
            }

            [HttpPut("{id:int}/Image")]
            public async Task<IActionResult> UpdateImage(int id, [FromBody] UpdateItemImageRequest request)
            {
                var item = await _db.Items.FirstOrDefaultAsync(i => i.ID == id);
                if (item is null)
                    return NotFound();

                var rawName = request?.ImageName?.Trim();
                var imageName = string.IsNullOrWhiteSpace(rawName) ? "default.png" : rawName;
                var fileNameOnly = Path.GetFileName(imageName);
                if (!string.Equals(fileNameOnly, imageName, StringComparison.Ordinal))
                    return BadRequest("Invalid image name.");

                var ext = Path.GetExtension(fileNameOnly).ToLowerInvariant();
                if (ext is not ".png" and not ".jpg")
                    return BadRequest("Only .png and .jpg images are allowed.");

                var oldImageName = item.ImageName?.Trim() ?? string.Empty;
                item.ImageName = fileNameOnly;
                await _db.SaveChangesAsync();

                var oldFileNameOnly = Path.GetFileName(oldImageName);
                if (!string.IsNullOrWhiteSpace(oldFileNameOnly) &&
                    !string.Equals(oldFileNameOnly, "default.png", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(oldFileNameOnly, fileNameOnly, StringComparison.OrdinalIgnoreCase))
                {
                    var oldFilePath = Path.Combine(_env.WebRootPath, "images", "itemsfolder", oldFileNameOnly);
                    if (System.IO.File.Exists(oldFilePath))
                        System.IO.File.Delete(oldFilePath);
                }

                return Ok(new { item.ID, item.ImageName });
            }

	        private async Task<string> GenerateNextSkuAsync()
	        {
	            const string prefix = "ITM";
	            const int pad = 3;

	            var existing = await _db.Items
	                .AsNoTracking()
	                .Where(i => i.SKU != null && i.SKU.StartsWith(prefix))
	                .Select(i => i.SKU)
	                .ToListAsync();

	            var max = 0;
	            foreach (var sku in existing)
	            {
	                if (string.IsNullOrWhiteSpace(sku))
	                    continue;

	                var trimmed = sku.Trim();
	                if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
	                    continue;

	                var numberPart = trimmed[prefix.Length..];
	                if (numberPart.Length == 0)
	                    continue;

	                if (int.TryParse(numberPart, out var n) && n > max)
	                    max = n;
	            }

	            // Find the first unused candidate to be safe under concurrent inserts.
	            for (var n = max + 1; n < max + 10000; n++)
	            {
	                var candidate = prefix + n.ToString().PadLeft(pad, '0');
	                var exists = await _db.Items.AsNoTracking().AnyAsync(i => i.SKU.ToLower() == candidate.ToLower());
	                if (!exists)
	                    return candidate;
	            }

	            // Fallback (should be unreachable).
	            return prefix + (max + 1).ToString().PadLeft(pad, '0');
	        }

	        [HttpGet("Unique")]
	        public async Task<IActionResult> Unique(
	            [FromQuery] string nativeName,
	            [FromQuery] string sku,
	            [FromQuery] string serialNumber,
	            [FromQuery] int? itemId)
	        {
	            var errors = await GetUniqueErrorsAsync(nativeName, sku, serialNumber, itemId);
	            return Ok(new
            {
                NativeNameUnique = errors.All(e => e.Field != nameof(Item.NativeName)),
                SKUUnique = errors.All(e => e.Field != nameof(Item.SKU)),
                SerialNumberUnique = errors.All(e => e.Field != nameof(Item.SerialNumber))
	            });
	        }

	        [HttpGet("NextSku")]
	        public async Task<IActionResult> NextSku()
	        {
	            var sku = await GenerateNextSkuAsync();
	            return Ok(new { SKU = sku });
	        }

        private string GetUserId()
        {
            var principal = _accessor.HttpContext?.User ?? User;
            return principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private async Task<List<(string Field, string Message)>> GetUniqueErrorsAsync(string nativeName, string sku, string serialNumber, int? excludeId)
        {
            var errors = new List<(string Field, string Message)>();

            nativeName = (nativeName ?? string.Empty).Trim();
            sku = (sku ?? string.Empty).Trim();
            serialNumber = (serialNumber ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(nativeName))
            {
                var exists = await _db.Items.AnyAsync(i =>
                    i.NativeName.ToLower() == nativeName.ToLower() &&
                    (!excludeId.HasValue || i.ID != excludeId.Value));
                if (exists)
                    errors.Add((nameof(Item.NativeName), "NativeName must be unique."));
            }

            if (!string.IsNullOrWhiteSpace(sku))
            {
                var exists = await _db.Items.AnyAsync(i =>
                    i.SKU.ToLower() == sku.ToLower() &&
                    (!excludeId.HasValue || i.ID != excludeId.Value));
                if (exists)
                    errors.Add((nameof(Item.SKU), "SKU must be unique."));
            }

            if (!string.IsNullOrWhiteSpace(serialNumber))
            {
                var exists = await _db.Items.AnyAsync(i =>
                    i.SerialNumber.ToLower() == serialNumber.ToLower() &&
                    (!excludeId.HasValue || i.ID != excludeId.Value));
                if (exists)
                    errors.Add((nameof(Item.SerialNumber), "SerialNumber must be unique."));
            }

            return errors;
        }

        private static void ApplyValues(Item entity, string values)
        {
            if (string.IsNullOrWhiteSpace(values))
                return;

            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(values);
            if (dict is null || dict.Count == 0)
                return;

            if (dict.TryGetValue(nameof(Item.NativeName), out var nativeEl) && nativeEl.ValueKind != JsonValueKind.Null)
                entity.NativeName = nativeEl.GetString() ?? string.Empty;

            if (dict.TryGetValue(nameof(Item.AliasName), out var aliasEl) && aliasEl.ValueKind != JsonValueKind.Null)
                entity.AliasName = aliasEl.GetString() ?? string.Empty;

            if (dict.TryGetValue(nameof(Item.SKU), out var skuEl) && skuEl.ValueKind != JsonValueKind.Null)
                entity.SKU = skuEl.GetString() ?? string.Empty;

            if (dict.TryGetValue(nameof(Item.SerialNumber), out var serialEl) && serialEl.ValueKind != JsonValueKind.Null)
                entity.SerialNumber = serialEl.GetString() ?? string.Empty;

            if (dict.TryGetValue(nameof(Item.Description), out var descEl) && descEl.ValueKind != JsonValueKind.Null)
                entity.Description = descEl.GetString() ?? string.Empty;

            if (dict.TryGetValue(nameof(Item.MinimumQuantity), out var minEl) && minEl.ValueKind != JsonValueKind.Null)
            {
                if (minEl.TryGetDecimal(out var min))
                    entity.MinimumQuantity = min;
            }

            if (dict.TryGetValue(nameof(Item.IsActive), out var activeEl) && activeEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
                entity.IsActive = activeEl.GetBoolean();

            if (dict.TryGetValue(nameof(Item.CategoryId), out var catEl) && catEl.ValueKind != JsonValueKind.Null)
            {
                if (catEl.TryGetInt32(out var cat))
                    entity.CategoryId = cat;
            }

            if (dict.TryGetValue(nameof(Item.UnitId), out var unitEl) && unitEl.ValueKind != JsonValueKind.Null)
            {
                if (unitEl.TryGetInt32(out var unit))
                    entity.UnitId = unit;
            }
        }
    }

    public class CreateItemRequest
    {
        public string NativeName { get; set; } = string.Empty;
        public string AliasName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageName { get; set; } = "default.png";
        public decimal MinimumQuantity { get; set; }
        public int CategoryId { get; set; }
        public int UnitId { get; set; }
        public List<UnitConversionRow> UnitConversions { get; set; } = [];
    }

    public class UnitConversionRow
    {
        public int SubUnitID { get; set; }
        public decimal? MainAmount { get; set; }
        public decimal? SubAmount { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }

    public class UpdateItemImageRequest
    {
        public string ImageName { get; set; } = "default.png";
    }

    public class InitialStockRowVm
    {
        public int ItemID { get; set; }
        public int WarehouseID { get; set; }
        public decimal Quantity { get; set; }
        public int? UnitConversionID { get; set; }
        public string BatchNo { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
    }
}
