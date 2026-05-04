using AccountingSystem.Models.Inventory;
using AccountingSystem.Repository.Inventory;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccountingSystem.Controllers.APIs;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WarehousesController(IWarehouseRepository warehouseRepository) : ApiControllerBase
{
    private readonly IWarehouseRepository _warehouses = warehouseRepository;

    [HttpGet]
    public async Task<object> Get(DataSourceLoadOptions loadOptions)
    {
        var data = await _warehouses.GetAllAsync();
        return DataSourceLoader.Load(data, loadOptions);
    }

    [HttpGet("Active")]
    public async Task<object> GetActive(DataSourceLoadOptions loadOptions)
    {
        var data = await _warehouses.GetAllAsync();
        return DataSourceLoader.Load(data?.FindAll(w => w.IsActive), loadOptions);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] string values)
    {
        var userId = CurrentUserId;
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

    private static void ApplyValues(WareHouse entity, string values)
    {
        DevExtremeFormValueMapper.Apply(
            values,
            FormValueSetter.String(nameof(WareHouse.Name), value => entity.Name = value),
            FormValueSetter.String(nameof(WareHouse.Description), value => entity.Description = value),
            FormValueSetter.Boolean(nameof(WareHouse.IsActive), value => entity.IsActive = value));
    }
}
