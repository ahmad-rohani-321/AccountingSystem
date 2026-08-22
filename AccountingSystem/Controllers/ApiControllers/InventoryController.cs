using AccountingSystem.Data;
using AccountingSystem.Models.Inventory;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.VisualBasic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace AccountingSystem.Controllers.ApiControllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController(ApplicationDbContext context, IHttpContextAccessor accessor, IWebHostEnvironment environment) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IHttpContextAccessor _accessor = accessor;
        private readonly IWebHostEnvironment _environemnt = environment;
        #region stock related
        [HttpPost("CreateStock")]
        public async Task<ActionResult> CreateStock(StockViewModel model)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (model.Name == null || model.Name.Equals(string.Empty))
            {
                return BadRequest("نوم حتمی لیکل حتمي دي..");
            }
            else if (await _context.WareHouses.AnyAsync(m => m.Name == model.Name))
            {
                return BadRequest("لیکل سوی نوم تکراري دی.");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.WareHouses.AddAsync(
                        new Models.Inventory.WareHouse()
                        {
                            Name = model.Name,
                            CreationDate = DateTime.Now,
                            CreatedByUserId = user,
                            Description = model.Description,
                            IsActive = true
                        }
                        );
                    await _context.UserHistories.AddAsync(
                        new Models.Identity.UserHistory()
                        {
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            Details = $"د {model.Name} په نوم ګدام جوړ سو.",
                            ModelName = "ګدامونه"
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

        [HttpPut("UpdateStock")]
        public async Task<ActionResult> UpdateStock(StockViewModel model)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (model.Id == 0)
            {
                return BadRequest("شخص نه دی انتخاب سوی");
            }
            else if (model.Name == null || model.Name.Equals(string.Empty))
            {
                return BadRequest("نوم حتمی لیکل حتمي دي.");
            }
            else if (await _context.WareHouses.Where(x => x.ID != model.Id).AnyAsync(m => m.Name == model.Name))
            {
                return BadRequest("لیکل سوی نوم تکراري دی.");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var getStock = await _context.WareHouses.FindAsync(model.Id);
                    getStock.Name = model.Name;
                    getStock.Description = model.Description;

                    await _context.UserHistories.AddAsync(
                        new Models.Identity.UserHistory()
                        {
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            Details = $"د {model.Name} په نوم ګدام تغیر سو.",
                            ModelName = "ګدامونه"
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

        [HttpPut("ChangeStockActivation/{id}")]
        public async Task<ActionResult> ChangeStockActivation(int id)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (id == 0)
            {
                return BadRequest("ګدام نه دی انتخاب سوی.");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var getStock = await _context.WareHouses.FindAsync(id);
                    getStock.IsActive = !getStock.IsActive;

                    string activeStatus = getStock.IsActive ? "فعال" : "غیر فعال";
                    await _context.UserHistories.AddAsync(
                        new Models.Identity.UserHistory()
                        {
                            CreatedByUserId = user,
                            CreationDate = DateTime.Now,
                            Details = $"د {getStock.Name} به نوم ګدام {activeStatus} سو.",
                            ModelName = "ګدامونه"
                        }
                        );
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return Ok();
                }
                catch (Exception ex)
                {
                    return BadRequest(ex.Message);
                }
            }
        }

        [HttpGet("GetStockList")]
        public async Task<ActionResult> GetStockList()
        {
            var list = (await _context.WareHouses.ToArrayAsync())
                        .Select(s => new StockViewModel()
                        {
                            Id = s.ID,
                            Name = s.Name,
                            Description = s.Description,
                            IsActive = s.IsActive
                        }).ToList();
            return Ok(list);
        }

        [HttpGet("GetActiveStockList")]
        public async Task<ActionResult> GetActiveStockList()
        {
            var list = (await _context.WareHouses.Where(x => x.IsActive).ToArrayAsync())
                        .Select(s => new StockViewModel()
                        {
                            Id = s.ID,
                            Name = s.Name
                        }).ToList();
            return Ok(list);
        }
        #endregion

        #region categories
        [HttpGet("GetActiveCatsList")]
        public async Task<ActionResult> GetActiveCatsList()
        {
            var data = (await _context.Categories.Where(x => x.IsActive).ToArrayAsync())
                .Select(c => new CategoryViewModel()
                {
                    Id = c.ID,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    Name = c.Name,
                }).ToList();
            return Ok(data);
        }

        [HttpGet("GetCatsList")]
        public async Task<ActionResult> GetCatsList()
        {
            var data = (await _context.Categories.ToArrayAsync())
                .Select(c => new CategoryViewModel()
                {
                    Id = c.ID,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    Name = c.Name,
                }).ToList();
            return Ok(data);
        }
        [HttpPost("CreateCategory")]
        public async Task<ActionResult> CreateCategory(CategoryViewModel model)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (model.Name == null || model.Name.Equals(string.Empty))
            {
                return BadRequest("د کټګوري نوم حتمي دی.");
            }
            else if (await _context.Categories.AnyAsync(x => x.Name == model.Name))
            {
                return BadRequest("د کټګوری نوم تکراري دی.");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.Categories.AddAsync(new Models.Inventory.Category()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        IsActive = true,
                        Description = model.Description,
                        Name = model.Name
                    });
                    await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Details = $"د {model.Name} په نوم کټیګوري جوړه سوه.",
                        ModelName = "کټیګوري"
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
        [HttpPut("UpdateCategory")]
        public async Task<ActionResult> UpdateCategory(CategoryViewModel model)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if(model == null || model.Id == 0)
            {
                return BadRequest("کټیګوري نه ده انتخاب سوې.");
            }
            else if (model.Name == null || model.Name.Equals(string.Empty))
            {
                return BadRequest("د کټګوري نوم حتمي دی.");
            }
            else if (await _context.Categories.Where(x => x.ID != model.Id).AnyAsync(x => x.Name == model.Name))
            {
                return BadRequest("د کټګوری نوم تکراري دی.");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var category = await _context.Categories.FindAsync(model.Id);
                    category.Name = model.Name;
                    category.Description = model.Description;

                    await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Details = $"د {model.Name} په نوم کټیګوري تغیر سوه.",
                        ModelName = "کټیګوري"
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
        [HttpPut("ChangeCategoryActivation/{id}")]
        public async Task<ActionResult> UpdateCategory(int id)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var cat = await _context.Categories.FindAsync(id);
            if(id == 0)
            {
                return BadRequest("کټیګوري نه ده انتخاب سوې.");
            }
            else if (cat == null)
            {
                return BadRequest("کټیګوري نه ده انتخاب سوې.");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    cat.IsActive = !cat.IsActive;

                    await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Details = $"د {cat.Name} په نوم کټیګوري فعالیت تغیر سو.",
                        ModelName = "کټیګوري"
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
        #endregion

        #region units
        [HttpGet("GetActiveUnitsList")]
        public async Task<ActionResult> GetActiveUnistList()
        {
            var list = (await _context.Units.Where(x => x.IsActive).ToArrayAsync())
                .Select(c => new UnitsViewModel()
                {
                    Id = c.ID,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    Name = c.Name
                }).ToList();
                return Ok(list);
        }
        [HttpGet("GetUnitsList")]
        public async Task<ActionResult> GetUnitsList()
        {
            var data = (await _context.Units.ToArrayAsync())
                .Select(c => new UnitsViewModel()
                {
                    Id = c.ID,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    Name = c.Name
                }).ToList();
            return Ok(data);
        }
        [HttpPost("CreateUnit")]
        public async Task<ActionResult> CreateUnit(UnitsViewModel model)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (model.Name == null || model.Name.Equals(string.Empty))
            {
                return BadRequest("د واحد نوم حتمي دی.");
            }
            else if (await _context.Categories.AnyAsync(x => x.Name == model.Name))
            {
                return BadRequest("د واحد نوم تکراري دی.");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.Units.AddAsync(new Models.Inventory.Unit()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        IsActive = true,
                        Description = model.Description,
                        Name = model.Name
                    });
                    await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Details = $"د {model.Name} په نوم واحد جوړ سو.",
                        ModelName = "واحد"
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
        [HttpPut("UpdateUnit")]
        public async Task<ActionResult> UpdateUnit(UnitsViewModel model)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if(model == null || model.Id == 0)
            {
                return BadRequest("واحد نه دی انتخاب سوې.");
            }
            else if (model.Name == null || model.Name.Equals(string.Empty))
            {
                return BadRequest("د واحد نوم حتمي دی.");
            }
            else if (await _context.Units.Where(x => x.ID != model.Id).AnyAsync(x => x.Name == model.Name))
            {
                return BadRequest("د واحد نوم تکراري دی.");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var unit = await _context.Units.FindAsync(model.Id);
                    unit.Name = model.Name;
                    unit.Description = model.Description;

                    await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Details = $"د {model.Name} په نوم واحد تغیر سو.",
                        ModelName = "واحد"
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
        [HttpPut("ChangeUnitActivation/{id}")]
        public async Task<ActionResult> UpdateUnit(int id)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            var unit = await _context.Units.FindAsync(id);
            if(id == 0)
            {
                return BadRequest("واحد نه دی انتخاب سوی.");
            }
            else if (unit == null)
            {
                return BadRequest("واحد نه دی انتخاب سوی.");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    unit.IsActive = !unit.IsActive;

                    await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Details = $"د {unit.Name} په نوم واحد فعالیت تغیر سو.",
                        ModelName = "واحد"
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

        [HttpGet("GetUnitConversions/{id}")]
        public async Task<ActionResult> GetUnitConversions(int id)
        {
            var allUnits = await _context.Units.Where(x => x.IsActive).ToListAsync();
            var itemUnits = await _context.UnitConversion.Where(x => x.ItemID == id).ToListAsync();

            var data = allUnits.Select(unit =>
                {
                    var conversion = itemUnits.FirstOrDefault(x =>
                        x.SubUnitID == unit.ID);

                    var mainUnit = allUnits.FirstOrDefault(x =>
                        x.ID == conversion?.MainUnitId);

                    return new UnitConversionViewModel
                    {
                        Id = conversion?.ID ?? 0,

                        SubUnitId = unit.ID,
                        SubUnitName = unit.Name,

                        MainUnitQuantity = conversion?.MainAmount ?? 0,
                        SubUnitQuantity = conversion?.SubAmount ?? 0,

                        Remarks = conversion?.Remarks
                    };
                }).ToList();
            return Ok(data);
        }

        [HttpGet("GetUnitConversionsOnly/{id}")]
        public async Task<ActionResult> GetUnitConversionsOnly(int id)
        {
            if (!await _context.Items.AnyAsync(x => x.ID == id))
            {
                return BadRequest("جنس نه دی موجود!");
            }
            else
            {
                var units = (await _context.UnitConversion
                        .Include(x => x.SubUnit)
                        .Where(x => x.ItemID == id && x.MainAmount > 0)
                        .ToArrayAsync())
                        .Select(x => new UnitConversionViewModel()
                        {
                            Id = x.ID,
                            SubUnitName = x.SubUnit.Name
                        }).ToList();
                return Ok(units);
            }
        }
        #endregion

        #region items
        [HttpGet("Next-Code")]
        public async Task<ActionResult> NextCode()
        {
            const string prefix = "ITM";
            const int pad = 3;

            var existing = await _context.Items
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
                var exists = await _context.Items.AsNoTracking().AnyAsync(i => i.SKU.ToLower() == candidate.ToLower());
                if (!exists)
                    return Ok(candidate);
            }

            // Fallback (should be unreachable).
            return Ok(prefix + (max + 1).ToString().PadLeft(pad, '0'));
        }

        [HttpPost("CreateItem")]
        public async Task<ActionResult> CreateItem(ItemsViewModel request)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            string extension = request.ImageFile != null ? Path.GetExtension(request.ImageFile.FileName).ToLowerInvariant() : "";
            

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            if (request.Name == null || request.Name.Equals(string.Empty))
            {
                return BadRequest("د جنس نوم حتمي دی");
            }
            else if (request.Code == null || request.Code.Equals(string.Empty))
            {
                return BadRequest("جنس کوډ حتمي دی.");
            }
            else if (request.CategoryId == 0)
            {
                return BadRequest("کټیګوري حتمي ده.");
            }
            else if (request.MainUnitId == 0)
            {
                return BadRequest("عمومي واحد حتمی دی.");
            }
            else if (request.ImageFile != null && !allowedExtensions.Contains(extension))
            {
                return BadRequest("یوازي عکس قبول کیږي!");
            }
            else if (await _context.Items.AnyAsync(x => x.NativeName == request.Name))
            {
                return BadRequest($"جنس نوم تکراري دی.");
            }
            else if (await _context.Items.AnyAsync(x => x.ID != request.Id && x.SKU == request.Name))
            {
                return BadRequest("جنس کوډ تکراري دی.");
            }
            else if (await _context.Items.AnyAsync(x => x.ID != request.Id && x.SerialNumber != null && x.SerialNumber == request.SerialNo))
            {
                return BadRequest("جنس سیریل نمبر حتمي دی.");
            }
            else if (request.UnitConversions.Count(u => 
                u.MainUnitQuantity != 0 && u.SubUnitQuantity != 0) > 0 && 
                !request.UnitConversions.Any(
                u => u.MainUnitQuantity < 0 || u.SubUnitQuantity < 0 ||
                (u.MainUnitQuantity >= 0 && u.SubUnitQuantity > 0) || 
                (u.MainUnitQuantity > 0 && u.SubUnitQuantity >= 0)
            ))
            {
                return BadRequest("واحدونه اصلاح کړئ");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    string fileName = "default.png";
                    if (request.ImageFile != null)
                    {
                        fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.ImageFile.FileName)}";
                        var path = Path.Combine(_environemnt.WebRootPath, "Items", fileName);

                        await using var stream = new FileStream(path, FileMode.Create);
                        await request.ImageFile.CopyToAsync(stream);
                    }

                    var newItem = await _context.Items.AddAsync(new Item()
                    {
                        NativeName = request.Name,
                        AliasName = request.SecondName,
                        CategoryId = request.CategoryId,
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Description = request.Description,
                        ImageName = fileName,
                        IsActive = true,
                        MinimumQuantity = request.MinQuantity,
                        SerialNumber = request.SerialNo,
                        SKU = request.Code,
                        UnitId = request.MainUnitId
                    });
                    await _context.SaveChangesAsync();

                    foreach (var unit in request.UnitConversions)
                    {
                        if (unit.SubUnitId == newItem.Entity.UnitId)
                        {
                            await _context.UnitConversion.AddAsync(new UnitConversion()
                            {
                                CreatedByUserId = user,
                                CreationDate = DateTime.Now,
                                ItemID = newItem.Entity.ID,
                                MainUnitId = newItem.Entity.UnitId,
                                SubUnitID = newItem.Entity.UnitId,
                                MainAmount = 1,
                                SubAmount = 1,
                                ExchangedAmount = 1,
                                Remarks = string.Empty
                            });
                        }
                        else
                        {
                            if (unit.MainUnitQuantity > 0 && unit.SubUnitQuantity > 0)
                            {
                                await _context.UnitConversion.AddAsync(new UnitConversion()
                                {
                                    CreatedByUserId = user,
                                    CreationDate = DateTime.Now,
                                    ItemID = newItem.Entity.ID,
                                    MainAmount = unit.MainUnitQuantity,
                                    SubAmount = unit.SubUnitQuantity,
                                    SubUnitID = unit.SubUnitId,
                                    MainUnitId = newItem.Entity.UnitId,
                                    Remarks = unit.Remarks,
                                    ExchangedAmount = unit.SubUnitQuantity / unit.MainUnitQuantity
                                });
                            }
                        }
                    }
                    await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Details = $"د {newItem.Entity.NativeName} په نوم جنس اضافه سو.",
                        ModelName = "اجناس"
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

        [HttpPut("UpdateItem")]
        public async Task<ActionResult> UpdateItem(ItemsViewModel request)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            string extension = request.ImageFile != null ? Path.GetExtension(request.ImageFile.FileName).ToLowerInvariant() : "";
            

            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            if (request.Name == null || request.Name.Equals(string.Empty))
            {
                return BadRequest("د جنس نوم حتمي دی");
            }
            else if (request.Code == null || request.Code.Equals(string.Empty))
            {
                return BadRequest("جنس کوډ حتمي دی.");
            }
            else if (request.CategoryId == 0)
            {
                return BadRequest("کټیګوري حتمي ده.");
            }
            else if (request.MainUnitId == 0)
            {
                return BadRequest("عمومي واحد حتمی دی.");
            }
            else if (request.ImageFile != null && !allowedExtensions.Contains(extension))
            {
                return BadRequest("یوازي عکس قبول کیږي!");
            }
            else if (await _context.Items.AnyAsync(x => x.ID != request.Id && x.NativeName == request.Name))
            {
                return BadRequest($"جنس نوم تکراري دی.");
            }
            else if (await _context.Items.AnyAsync(x => x.SKU == request.Name))
            {
                return BadRequest("جنس کوډ تکراري دی.");
            }
            else if (await _context.Items.AnyAsync(x => x.SerialNumber != null && x.SerialNumber == request.SerialNo))
            {
                return BadRequest("جنس سیریل نمبر حتمي دی.");
            }
            else if (!request.UnitConversions.Any(unit =>
                        // Negative values are never allowed.
                        unit.MainUnitQuantity < 0 ||
                        unit.SubUnitQuantity < 0 ||

                        // Exactly one is zero while the other is positive.
                        (unit.MainUnitQuantity >= 0 && unit.SubUnitQuantity > 0) ||
                        (unit.MainUnitQuantity > 0 && unit.SubUnitQuantity >= 0)
                    ))
            {
                return BadRequest("واحدونه اصلاح کړئ");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var getItem = await _context.Items.FindAsync(request.Id);

                    if (request.ImageFile != null)
                    {
                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.ImageFile.FileName)}";
                        var path = Path.Combine(_environemnt.WebRootPath, "Items", fileName);

                        await using var stream = new FileStream(path, FileMode.Create);
                        await request.ImageFile.CopyToAsync(stream);
                        getItem.ImageName = fileName;
                    }


                    getItem.NativeName = request.Name;
                    getItem.AliasName = request.SecondName;
                    getItem.CategoryId = request.CategoryId;
                    getItem.CreatedByUserId = user;
                    getItem.CreationDate = DateTime.Now;
                    getItem.Description = request.Description;
                    getItem.IsActive = true;
                    getItem.MinimumQuantity = request.MinQuantity;
                    getItem.SerialNumber = request.SerialNo;
                    getItem.SKU = request.Code;
                    getItem.UnitId = request.MainUnitId;
                    
                    await _context.SaveChangesAsync();

                    foreach (var unit in request.UnitConversions)
                    {
                        // Find this conversion only for the current item.
                        var conversion = await _context.UnitConversion
                            .FirstOrDefaultAsync(x =>
                                x.ID == unit.Id &&
                                x.ItemID == request.Id);

                        // Main unit must always be 1 : 1.
                        var isMainUnit = unit.SubUnitId == request.MainUnitId;

                        if (conversion == null)
                        {
                            // No existing record: add a new one when needed.
                            conversion = new UnitConversion
                            {
                                ItemID = request.Id,
                                SubUnitID = unit.SubUnitId,
                                MainUnitId = request.MainUnitId,
                                CreatedByUserId = user,
                                CreationDate = DateTime.Now
                            };

                            await _context.UnitConversion.AddAsync(conversion);
                        }

                        // Existing record or new record: update its values.
                        conversion.MainUnitId = request.MainUnitId;
                        conversion.Remarks = unit.Remarks;

                        if (isMainUnit)
                        {
                            conversion.MainAmount = 1;
                            conversion.SubAmount = 1;
                            conversion.ExchangedAmount = 1;
                        }
                        else
                        {
                            conversion.MainAmount = unit.MainUnitQuantity;
                            conversion.SubAmount = unit.SubUnitQuantity;

                            conversion.ExchangedAmount = unit.MainUnitQuantity == 0
                                ? 0
                                : unit.SubUnitQuantity / unit.MainUnitQuantity;
                        }
                    }
                    await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Details = $"د {getItem.NativeName} په نوم جنس تغیر سو.",
                        ModelName = "اجناس"
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

        [HttpGet("GetItemsList")]
        public async Task<ActionResult> GetItemsList()
        {
            var getData = (await _context.Items
                .Include(x => x.Category)
                .Include(x => x.Unit)
                .ToArrayAsync())
                    .Select(i => new ItemsViewModel()
                    {
                        Id = i.ID,
                        Name = i.NativeName,
                        SecondName = i.AliasName,
                        Code = i.SKU,
                        SerialNo = i.SerialNumber,
                        Description = i.Description,
                        MinQuantity = i.MinimumQuantity,
                        CategoryId = i.CategoryId,
                        CategoryName = i.Category.Name,
                        MainUnitId = i.UnitId,
                        MainUnitName = i.Unit.Name,
                        Image = i.ImageName,
                        IsActive = i.IsActive
                    }).ToList();
            return Ok(getData);
        }

        [HttpGet("GetActiveItemsList")]
        public async Task<ActionResult> GetActiveItemsList()
        {
            var getData = (await _context.Items
                .Include(x => x.Category)
                .Include(x => x.Unit)
                .Where(x => x.IsActive)
                .ToArrayAsync())
                    .Select(i => new 
                    {
                        Id = i.ID,
                        Name = i.NativeName
                    }).ToList();
            return Ok(getData);
        }

        [HttpPut("ChangeItemActivation/{id}")]
        public async Task<ActionResult> ChangeItemActivation(int id)
        {
            var user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (!await _context.Items.AnyAsync(x => x.ID == id))
            {
                return BadRequest("جنس نه دی موجود!");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var item = await _context.Items.FindAsync(id);
                    item.IsActive = !item.IsActive;
                    await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Details = $"د {item.NativeName} په نوم جنس فعالیت تغیر کړل سو.",
                        ModelName = "اجناس"
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

        [HttpGet("HasStock/{id}")]
        public async Task<ActionResult> HasStock(int id)
        {
            if (!await _context.Items.AnyAsync(x => x.ID == id))
            {
                return BadRequest("جنس نه دی موجود");
            }
            else
            {
                if (!await _context.StockBalances.AnyAsync(x => x.ItemID == id))
                {
                    return Ok(true);
                }
                else
                {
                    return BadRequest(false);
                }
            }
        }

        [HttpPost("NewItemStockEntry")]
        public async Task<ActionResult> NewItemStockEntry(List<StockItemRequestViewModel> request)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (request == null || request.Count == 0)
            {
                return BadRequest("خالي لیست نه ثبت کیږي");
            }
            else if (!await _context.Items.AnyAsync(x => request.Select(i => i.ItemId).Contains( x.ID)))
            {
                return BadRequest("هیله ده اجناس اصلاح کړئ!");
            }
            else if (!await _context.UnitConversion
                            .AnyAsync(u => 
                            request.Select(x => x.UnitId).Contains(u.ID) && 
                            request.Select(x => x.ItemId).Contains(u.ItemID)))
            {
                return BadRequest("هیله ده واحدونه اصلاح کړئ!");
            }
            else if (!await _context.WareHouses.AnyAsync(s => request.Select(w => w.StockId).Contains(s.ID)))
            {
                return BadRequest("هیله ده ګدامونه اصلاح کړی!");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    foreach (var item in request)
                    {
                        DateTime date = item.CreationDate.Date == DateTime.Now.Date ? DateTime.Now : item.CreationDate;
                        var itemUnit = await _context.UnitConversion.FindAsync(item.UnitId);
                        decimal calculatedQuantity = Math.Round(item.Quantity / itemUnit.ExchangedAmount, Defaults.DefaultDecimals);
                        var currentStockItem = await _context.StockBalances.FirstOrDefaultAsync(x => x.ItemID == item.ItemId && x.WarehouseID == item.StockId);
                        if (currentStockItem  == null)
                        {
                            var newEntry = await _context.StockBalances.AddAsync(new StockBalance()
                            {
                                CreatedByUserId = user,
                                ItemID = item.ItemId,
                                Quantity = calculatedQuantity,
                                CreationDate = date,
                                WarehouseID = item.StockId,
                                Remarks = item.Remarks
                            });
                            await _context.SaveChangesAsync();
                            currentStockItem = newEntry.Entity;
                        }
                        else
                        {
                            currentStockItem.Quantity += calculatedQuantity;
                        }
                        
                        await _context.StockTransactions.AddAsync(new StockTransactions()
                        {
                            CreatedByUserId = user,
                            CreationDate = date,
                            Quantity = item.Quantity,
                            Remarks = item.Remarks,
                            StockBalanceID = currentStockItem.ID,
                            TransactionID = 1,
                            UnitID = item.UnitId,
                        });
                    }
                    
                    await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Details = "د جنس لپاره ابتدائی موجودي، ثبت سوه.",
                        ModelName = "اجناس"
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

        [HttpGet("GetStockItems")]
        public async Task<ActionResult> GetStockItems()
        {
            var data = (await _context.StockBalances
                        .Include(x => x.Item)
                        .ThenInclude(x => x.Unit)
                        .Include(x => x.Warehouse)
                        .Where(x => x.Quantity > 0)
                        .ToArrayAsync())
                        .Select(x => new StockItemsViewModel()
                        {
                            Id = x.ID,
                            ItemID = x.Item.ID,
                            ItemName = x.Item.NativeName,
                            Quantity = x.Quantity,
                            StockID = x.WarehouseID,
                            StockName = x.Warehouse.Name,
                            UnitID = x.Item.Unit.ID,
                            UnitName = x.Item.Unit.Name
                        }).ToList();
            return Ok(data);
        }
        
        [HttpPost("SaveStockExchange")]
        public async Task<ActionResult> SaveStockExchange(StockItemsViewModel request)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (request == null)
            {
                return BadRequest("خالي لیست نه ثبت کیږي");
            }
            else if (!await _context.StockBalances.AnyAsync(x => x.ID == request.Id))
            {
                return BadRequest("هیله ده جنس انتخاب کړئ!");
            }
            else if(!await _context.UnitConversion.AnyAsync(u => u.ID == request.UnitID))
            {
                return BadRequest("هیله ده صحیح واحد انتخاب کړئ!");
            }
            else if (!await _context.WareHouses.AnyAsync(s => s.ID == request.StockID))
            {
                return BadRequest("هیله ده صحیح ګدام انتخاب کړئ!");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    DateTime date = request.CreationDate.Date == DateTime.Now.Date ? DateTime.Now : request.CreationDate;
                    var itemUnit = await _context.UnitConversion.FindAsync(request.UnitID);
                    decimal calculatedQuantity = Math.Round(request.Quantity / itemUnit.ExchangedAmount, Defaults.DefaultDecimals);
                    var stockItem = await _context.StockBalances.FindAsync(request.Id);

                    if (stockItem.Quantity < calculatedQuantity)
                    {
                        return BadRequest("د جنس د انتقال لپاره په کافي اندازه تعداد نسته");
                    }
                   
                    var toStock = await _context.StockBalances.FirstOrDefaultAsync(x => x.ItemID == stockItem.ItemID && x.WarehouseID == request.StockID);
                    stockItem.Quantity -= calculatedQuantity;
                    if (toStock == null)
                    {
                        var newEntry = await _context.StockBalances.AddAsync(new StockBalance()
                        {
                            CreatedByUserId = user,
                            ItemID = stockItem.ItemID,
                            Quantity = calculatedQuantity,
                            CreationDate = date,
                            WarehouseID = request.StockID,
                            Remarks = request.Remarks
                        });
                        await _context.SaveChangesAsync();
                        toStock = newEntry.Entity;
                    }
                    else
                    {
                        toStock.Quantity += calculatedQuantity;
                    }

                    await _context.StockTransactions.AddAsync(new StockTransactions()
                    {
                        CreatedByUserId = user,
                        CreationDate = date,
                        Quantity = request.Quantity,
                        Remarks = request.Remarks,
                        StockBalanceID = stockItem.ID,
                        TransactionID = 4, 
                        UnitID = request.UnitID,
                    });

                    await _context.StockTransactions.AddAsync(new StockTransactions()
                    {
                        CreatedByUserId = user,
                        CreationDate = date,
                        Quantity = request.Quantity,
                        Remarks = request.Remarks,
                        StockBalanceID = toStock.ID,
                        TransactionID = 12, 
                        UnitID = request.UnitID
                    });
                    
                    
                    await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Details = "د جنس لپاره موجودي تبادله، ثبت سوه.",
                        ModelName = "اجناس"
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
        
        [HttpPost("ExportOrDemageItem")]
        public async Task<ActionResult> ExportOrDemageItem(StockItemsViewModel request)
        {
            string user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (request == null)
            {
                return BadRequest("خالي لیست نه ثبت کیږي");
            }
            else if (!await _context.StockBalances.AnyAsync(s => s.ID == request.Id))
            {
                return BadRequest("هیله ده صحیح جنس انتخاب کړئ!");
            }
            else if(!await _context.UnitConversion.AnyAsync(u => u.ID == request.UnitID))
            {
                return BadRequest("هیله ده صحیح واحد انتخاب کړئ!");
            }
            else
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    DateTime date = request.CreationDate.Date == DateTime.Now.Date ? DateTime.Now : request.CreationDate;
                    var itemUnit = await _context.UnitConversion.FindAsync(request.UnitID);
                    decimal calculatedQuantity = Math.Round(request.Quantity / itemUnit.ExchangedAmount, Defaults.DefaultDecimals);
                    var stockItem = await _context.StockBalances.FindAsync(request.Id);

                    if (stockItem.Quantity < calculatedQuantity)
                    {
                        return BadRequest("د جنس د انتقال لپاره په کافي اندازه تعداد نسته");
                    }
                   
                    
                    stockItem.Quantity -= calculatedQuantity;

                    await _context.StockTransactions.AddAsync(new StockTransactions()
                    {
                        CreatedByUserId = user,
                        CreationDate = date,
                        Quantity = request.Quantity,
                        Remarks = request.Remarks,
                        StockBalanceID = stockItem.ID,
                        TransactionID = request.TransactionType, // Assuming 4 is the ID for stock exchange
                        UnitID = request.UnitID,
                    });
                    
                    
                    await _context.UserHistories.AddAsync(new Models.Identity.UserHistory()
                    {
                        CreatedByUserId = user,
                        CreationDate = DateTime.Now,
                        Details = "د جنس لپاره موجودي تبادله، ثبت سوه.",
                        ModelName = "اجناس"
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

        [HttpGet("StockTransactionsHistory")]
        public async Task<ActionResult> StockTransactionsHistory([FromQuery] int[] itemIds, [FromQuery] int[] stockIds, [FromQuery] int[] transactionTypeIds,[FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            if (itemIds?.Length > 0 &&
                !await _context.StockTransactions.Include(x => x.StockBalance).AnyAsync(x => itemIds.Contains(x.StockBalance.ItemID)))
            {
                return BadRequest("صحیح اجناس انتخاب کړئ");
            }
            else if(transactionTypeIds?.Length > 0 && 
                !await _context.StockTransactionTypes.AnyAsync(x => transactionTypeIds.Contains(x.ID)))
            {
                return BadRequest("هیله ده صحیح دعملېې ډول انتخاب کړئ");
            }
            else if (stockIds?.Length > 0 && !await _context.WareHouses.AnyAsync(x => stockIds.Contains(x.ID)))
            {
                return BadRequest("صحیح ګدام انتخاب کړئ");
            }
            else if (startDate > endDate)
            {
                return BadRequest("نېټې اصلاح کړئ");
            }
            else
            {
                List<StockTransactionsViewModel> data = null;
                // only items with dates
                if (itemIds?.Length > 0 && stockIds?.Length == 0 && transactionTypeIds.Length == 0)
                {
                    data = [.. (await _context.StockTransactions
                                    .Include(x => x.StockBalance)
                                    .ThenInclude(x => x.Item)
                                    .Include(x => x.StockBalance.Warehouse)
                                    .Include(x => x.Transaction)
                                    .Include(x => x.Unit)
                                    .ThenInclude(x => x.SubUnit)
                                    .Where(x => x.CreationDate.Date >= startDate && x.CreationDate.Date <= endDate && itemIds.Contains(x.StockBalance.Item.ID))
                                    .OrderByDescending(x => x.CreationDate)
                                    .ToArrayAsync())
                            .Select(x => new StockTransactionsViewModel(){
                                Date = x.CreationDate,
                                Description = x.Remarks,
                                Id = x.ID,
                                Name = x.StockBalance.Item.NativeName,
                                Quantity = x.Quantity,
                                TransactionTypeName = x.Transaction.Name,
                                UnitName = x.Unit.SubUnit.Name,
                                WarehouseName = x.StockBalance.Warehouse.Name
                            })];
                }
                // only stock with dates
                else if (itemIds?.Length == 0 && stockIds?.Length > 0 && transactionTypeIds.Length == 0)
                {
                    data = [.. (await _context.StockTransactions
                                    .Include(x => x.StockBalance)
                                    .ThenInclude(x => x.Item)
                                    .Include(x => x.StockBalance.Warehouse)
                                    .Include(x => x.Transaction)
                                    .Include(x => x.Unit)
                                    .ThenInclude(x => x.SubUnit)
                                    .Where(x => x.CreationDate.Date >= startDate && x.CreationDate.Date <= endDate && stockIds.Contains(x.StockBalance.Warehouse.ID))
                                    .OrderByDescending(x => x.CreationDate)
                                    .ToArrayAsync())
                            .Select(x => new StockTransactionsViewModel(){
                                Date = x.CreationDate,
                                Description = x.Remarks,
                                Id = x.ID,
                                Name = x.StockBalance.Item.NativeName,
                                Quantity = x.Quantity,
                                TransactionTypeName = x.Transaction.Name,
                                UnitName = x.Unit.SubUnit.Name,
                                WarehouseName = x.StockBalance.Warehouse.Name
                            })];
                }
                // only transaction type with dates
                else if (itemIds?.Length == 0 && stockIds?.Length == 0 && transactionTypeIds.Length > 0)
                {
                    data = [.. (await _context.StockTransactions
                                    .Include(x => x.StockBalance)
                                    .ThenInclude(x => x.Item)
                                    .Include(x => x.StockBalance.Warehouse)
                                    .Include(x => x.Transaction)
                                    .Include(x => x.Unit)
                                    .ThenInclude(x => x.SubUnit)
                                    .Where(x => x.CreationDate.Date >= startDate && x.CreationDate.Date <= endDate && transactionTypeIds.Contains(x.Transaction.ID))
                                    .OrderByDescending(x => x.CreationDate)
                                    .ToArrayAsync())
                            .Select(x => new StockTransactionsViewModel(){
                                Date = x.CreationDate,
                                Description = x.Remarks,
                                Id = x.ID,
                                Name = x.StockBalance.Item.NativeName,
                                Quantity = x.Quantity,
                                TransactionTypeName = x.Transaction.Name,
                                UnitName = x.Unit.SubUnit.Name,
                                WarehouseName = x.StockBalance.Warehouse.Name
                            })];
                }
                // items and stocks with dates
                else if (itemIds?.Length > 0 && stockIds?.Length > 0 && transactionTypeIds.Length == 0)
                {
                    data = [.. (await _context.StockTransactions
                                    .Include(x => x.StockBalance)
                                    .ThenInclude(x => x.Item)
                                    .Include(x => x.StockBalance.Warehouse)
                                    .Include(x => x.Transaction)
                                    .Include(x => x.Unit)
                                    .ThenInclude(x => x.SubUnit)
                                    .Where(x => x.CreationDate.Date >= startDate && x.CreationDate.Date <= endDate && itemIds.Contains(x.StockBalance.Item.ID) && stockIds.Contains(x.StockBalance.Warehouse.ID))
                                    .OrderByDescending(x => x.CreationDate)
                                    .ToArrayAsync())
                            .Select(x => new StockTransactionsViewModel(){
                                Date = x.CreationDate,
                                Description = x.Remarks,
                                Id = x.ID,
                                Name = x.StockBalance.Item.NativeName,
                                Quantity = x.Quantity,
                                TransactionTypeName = x.Transaction.Name,
                                UnitName = x.Unit.SubUnit.Name,
                                WarehouseName = x.StockBalance.Warehouse.Name
                            })];
                }
                // items and transactions with dates
                else if (itemIds?.Length > 0 && stockIds?.Length == 0 && transactionTypeIds.Length > 0)
                {
                    data = [.. (await _context.StockTransactions
                                    .Include(x => x.StockBalance)
                                    .ThenInclude(x => x.Item)
                                    .Include(x => x.StockBalance.Warehouse)
                                    .Include(x => x.Transaction)
                                    .Include(x => x.Unit)
                                    .ThenInclude(x => x.SubUnit)
                                    .Where(x => x.CreationDate.Date >= startDate && x.CreationDate.Date <= endDate && itemIds.Contains(x.StockBalance.Item.ID) && transactionTypeIds.Contains(x.Transaction.ID))
                                    .OrderByDescending(x => x.CreationDate)
                                    .ToArrayAsync())
                            .Select(x => new StockTransactionsViewModel(){
                                Date = x.CreationDate,
                                Description = x.Remarks,
                                Id = x.ID,
                                Name = x.StockBalance.Item.NativeName,
                                Quantity = x.Quantity,
                                TransactionTypeName = x.Transaction.Name,
                                UnitName = x.Unit.SubUnit.Name,
                                WarehouseName = x.StockBalance.Warehouse.Name
                            })];
                }
                // stocks and transactions with dates
                else if (itemIds?.Length == 0 && stockIds?.Length > 0 && transactionTypeIds.Length > 0)
                {
                    data = [.. (await _context.StockTransactions
                                    .Include(x => x.StockBalance)
                                    .ThenInclude(x => x.Item)
                                    .Include(x => x.StockBalance.Warehouse)
                                    .Include(x => x.Transaction)
                                    .Include(x => x.Unit)
                                    .ThenInclude(x => x.SubUnit)
                                    .Where(x => x.CreationDate.Date >= startDate && x.CreationDate.Date <= endDate && stockIds.Contains(x.StockBalance.Warehouse.ID) && transactionTypeIds.Contains(x.Transaction.ID))
                                    .OrderByDescending(x => x.CreationDate)
                                    .ToArrayAsync())
                            .Select(x => new StockTransactionsViewModel(){
                                Date = x.CreationDate,
                                Description = x.Remarks,
                                Id = x.ID,
                                Name = x.StockBalance.Item.NativeName,
                                Quantity = x.Quantity,
                                TransactionTypeName = x.Transaction.Name,
                                UnitName = x.Unit.SubUnit.Name,
                                WarehouseName = x.StockBalance.Warehouse.Name
                            })];
                }
                // stocks and transactions and items with dates
                else if (itemIds?.Length > 0 && stockIds?.Length > 0 && transactionTypeIds.Length > 0)
                {
                    data = [.. (await _context.StockTransactions
                                    .Include(x => x.StockBalance)
                                    .ThenInclude(x => x.Item)
                                    .Include(x => x.StockBalance.Warehouse)
                                    .Include(x => x.Transaction)
                                    .Include(x => x.Unit)
                                    .ThenInclude(x => x.SubUnit)
                                    .Where(x => x.CreationDate.Date >= startDate && x.CreationDate.Date <= endDate && itemIds.Contains(x.StockBalance.Item.ID) && stockIds.Contains(x.StockBalance.Warehouse.ID) && transactionTypeIds.Contains(x.Transaction.ID))
                                    .OrderByDescending(x => x.CreationDate)
                                    .ToArrayAsync())
                            .Select(x => new StockTransactionsViewModel(){
                                Date = x.CreationDate,
                                Description = x.Remarks,
                                Id = x.ID,
                                Name = x.StockBalance.Item.NativeName,
                                Quantity = x.Quantity,
                                TransactionTypeName = x.Transaction.Name,
                                UnitName = x.Unit.SubUnit.Name,
                                WarehouseName = x.StockBalance.Warehouse.Name
                            })];
                }
                else {
                    data = (await _context.StockTransactions
                                    .Include(x => x.StockBalance)
                                    .ThenInclude(x => x.Item)
                                    .Include(x => x.StockBalance.Warehouse)
                                    .Include(x => x.Transaction)
                                    .Include(x => x.Unit)
                                    .ThenInclude(x => x.SubUnit)
                                    .Where(x => x.CreationDate.Date >= startDate && x.CreationDate.Date <= endDate)
                                    .ToArrayAsync())
                            .Select(x => new StockTransactionsViewModel(){
                                Date = x.CreationDate,
                                Description = x.Remarks,
                                Id = x.ID,
                                Name = x.StockBalance.Item.NativeName,
                                Quantity = x.Quantity,
                                TransactionTypeName = x.Transaction.Name,
                                UnitName = x.Unit.SubUnit.Name,
                                WarehouseName = x.StockBalance.Warehouse.Name
                            }).ToList();
                }
                return Ok(data);
            }
        }
        
        [HttpGet("GetStockMinItems")]
        public async Task<ActionResult> GetStockMinItems()
        {
            var data = (await _context.StockBalances
                        .Include(x => x.Item)
                        .ThenInclude(x => x.Unit)
                        .Include(x => x.Warehouse)
                        .Where(x => x.Quantity > 0 && x.Item.MinimumQuantity > 0 && x.Item.MinimumQuantity >= x.Quantity)
                        .ToArrayAsync())
                        .Select(x => new StockItemsViewModel()
                        {
                            Id = x.ID,
                            ItemID = x.Item.ID,
                            ItemName = x.Item.NativeName,
                            Quantity = x.Quantity,
                            MinQuantity = x.Item.MinimumQuantity,
                            StockID = x.WarehouseID,
                            StockName = x.Warehouse.Name,
                            UnitID = x.Item.Unit.ID,
                            UnitName = x.Item.Unit.Name
                        }).ToList();
            return Ok(data);
        }
        #endregion

        [HttpGet("GetStockOutageActionTypes")]
        public async Task<ActionResult> GetStockOutageActionTypes()
        {
            int[] types = [3, 9];
            var actionTypes = await _context.StockTransactionTypes.Where(x => types.Contains(x.ID)).ToListAsync();
            return Ok(actionTypes);
        }

        [HttpGet("GetStockTransactionTypes")]
        public async Task<ActionResult> GetStockTransactionTypes()
        {
            return Ok(
                await _context.StockTransactionTypes.ToListAsync()
            );
        }
    }
}
