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
public class UnitsController(IUnitRepository unitRepository) : ApiControllerBase
{
    private readonly IUnitRepository _units = unitRepository;

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
        var userId = CurrentUserId;
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

    private static void ApplyValues(Unit entity, string values)
    {
        DevExtremeFormValueMapper.Apply(
            values,
            FormValueSetter.String(nameof(Unit.Name), value => entity.Name = value),
            FormValueSetter.String(nameof(Unit.Description), value => entity.Description = value),
            FormValueSetter.Boolean(nameof(Unit.IsActive), value => entity.IsActive = value));
    }
}
