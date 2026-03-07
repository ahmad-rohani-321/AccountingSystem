using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using AccountingSystem.Models.Inventory;
using AccountingSystem.Repository.Inventory;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers.APIs
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WarehousesController(IHttpContextAccessor accessor, IWarehouseRepository warehouseRepository) : ControllerBase
    {
        private readonly IWarehouseRepository _warehouses = warehouseRepository;
        private readonly IHttpContextAccessor _accessor = accessor;

        [HttpGet]
        public async Task<object> Get(DataSourceLoadOptions loadOptions)
        {
            var data = await _warehouses.GetAllAsync();
            return DataSourceLoader.Load(data, loadOptions);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm] string values)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var entity = new WareHouse
            {
                CreationDate = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            ApplyValues(entity, values);

            if (!TryValidateModel(entity))
                return BadRequest(ModelState);

            await _warehouses.AddAsync(entity);
            return Ok(entity);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var entity = await _warehouses.GetByIdAsync(key);
            if (entity is null)
                return NotFound();

            ApplyValues(entity, values);

            if (!TryValidateModel(entity))
                return BadRequest(ModelState);

            await _warehouses.UpdateAsync(entity);
            return Ok(entity);
        }

        private string GetUserId()
        {
            var principal = _accessor.HttpContext?.User ?? User;
            return principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private static void ApplyValues(WareHouse entity, string values)
        {
            if (string.IsNullOrWhiteSpace(values))
                return;

            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(values);
            if (dict is null || dict.Count == 0)
                return;

            if (dict.TryGetValue(nameof(WareHouse.Name), out var nameEl) && nameEl.ValueKind != JsonValueKind.Null)
                entity.Name = nameEl.GetString() ?? string.Empty;

            if (dict.TryGetValue(nameof(WareHouse.Description), out var descEl) && descEl.ValueKind != JsonValueKind.Null)
                entity.Description = descEl.GetString() ?? string.Empty;

            if (dict.TryGetValue(nameof(WareHouse.IsActive), out var activeEl) && activeEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
                entity.IsActive = activeEl.GetBoolean();
        }
    }
}
