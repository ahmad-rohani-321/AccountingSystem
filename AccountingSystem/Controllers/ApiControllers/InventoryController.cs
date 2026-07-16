using AccountingSystem.Data;
using AccountingSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Eventing.Reader;
using System.Net.NetworkInformation;
using System.Security.Claims;

namespace AccountingSystem.Controllers.ApiControllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController(ApplicationDbContext context, IHttpContextAccessor accessor) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IHttpContextAccessor _accessor = accessor;

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
            else if (await _context.WareHouses.AnyAsync(m => m.Name == model.Name))
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
    }
}
