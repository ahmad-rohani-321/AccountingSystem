using AccountingSystem.Data;
using AccountingSystem.Models.Inventory;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.VisualBasic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Eventing.Reader;
using System.Net.NetworkInformation;
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
            // var data = allUnits.Select(x => new UnitConversionViewModel()
            // {
            //     Id = 0
            // }).ToList();
            // var data = await (
            //     from unit in _context.Units
            //     where unit.IsActive
            //     join conversion in _context.UnitConversion.Where(x => x.ItemID == id)
            //         on unit.ID equals conversion.SubUnitID into conversions
            //     from conversion in conversions.DefaultIfEmpty()
            //     select new UnitConversionViewModel
            //     {
            //         Id = conversion != null ? conversion.ID : 0,
            //         SubUnitId = unit.ID,
            //         SubUnitName = unit.Name,
            //         MainUnitQuantity = conversion != null ? conversion.MainAmount : 0,
            //         SubUnitQuantity = conversion != null ? conversion.SubAmount : 0,
            //         Remarks = conversion != null ? conversion.Remarks : null
            //     }
            // ).AsNoTracking().ToListAsync();

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
                        (unit.MainUnitQuantity == 0 && unit.SubUnitQuantity > 0) ||
                        (unit.MainUnitQuantity > 0 && unit.SubUnitQuantity == 0)
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
            else if (await _context.Items.AnyAsync(x => x.NativeName == request.Name))
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
                        (unit.MainUnitQuantity == 0 && unit.SubUnitQuantity > 0) ||
                        (unit.MainUnitQuantity > 0 && unit.SubUnitQuantity == 0)
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

        [HttpPut("ChangeItemActivation/{id}")]
        public async Task<ActionResult> ChangeItemActivation(int id)
        {
            var user = _accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (!(await _context.Items.AnyAsync(x => x.ID == id)))
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
        #endregion

    }
}
