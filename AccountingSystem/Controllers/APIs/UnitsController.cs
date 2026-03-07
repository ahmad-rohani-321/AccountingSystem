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
    public class UnitsController(IHttpContextAccessor accessor, IUnitRepository unitRepository) : ControllerBase
    {
        private readonly IUnitRepository _units = unitRepository;
        private readonly IHttpContextAccessor _accessor = accessor;

        [HttpGet]
        public async Task<object> Get(DataSourceLoadOptions loadOptions)
        {
            var data = await _units.GetAllAsync();
            return DataSourceLoader.Load(data, loadOptions);
        }

        [HttpGet("Active")]
        public async Task<object> GetActive(DataSourceLoadOptions loadOptions)
        {
            var data = await _units.GetAllAsync();
            return DataSourceLoader.Load(data?.FindAll(u => u.IsActive), loadOptions);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromForm] string values)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var entity = new Unit
            {
                CreationDate = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            ApplyValues(entity, values);

            if (!TryValidateModel(entity))
                return BadRequest(ModelState);

            await _units.AddAsync(entity);
            return Ok(entity);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var entity = await _units.GetByIdAsync(key);
            if (entity is null)
                return NotFound();

            ApplyValues(entity, values);

            if (!TryValidateModel(entity))
                return BadRequest(ModelState);

            await _units.UpdateAsync(entity);
            return Ok(entity);
        }

        private string GetUserId()
        {
            var principal = _accessor.HttpContext?.User ?? User;
            return principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private static void ApplyValues(Unit entity, string values)
        {
            if (string.IsNullOrWhiteSpace(values))
                return;

            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(values);
            if (dict is null || dict.Count == 0)
                return;

            if (dict.TryGetValue(nameof(Unit.Name), out var nameEl) && nameEl.ValueKind != JsonValueKind.Null)
                entity.Name = nameEl.GetString() ?? string.Empty;

            if (dict.TryGetValue(nameof(Unit.Description), out var descEl) && descEl.ValueKind != JsonValueKind.Null)
                entity.Description = descEl.GetString() ?? string.Empty;

            if (dict.TryGetValue(nameof(Unit.IsActive), out var activeEl) && activeEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
                entity.IsActive = activeEl.GetBoolean();
        }
    }
}
