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
public class CategoriesController(ICategoryRepository category) : ApiControllerBase
{
    private readonly ICategoryRepository _cat = category;

    [HttpGet]
    public async Task<object> Get(DataSourceLoadOptions loadOptions)
    {
        var data = await _cat.GetAllAsync();
        return DataSourceLoader.Load(data, loadOptions);
    }

    [HttpGet("Active")]
    public async Task<object> GetActive(DataSourceLoadOptions loadOptions)
    {
        var data = await _cat.GetAllAsync();
        return DataSourceLoader.Load(data?.FindAll(c => c.IsActive), loadOptions);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromForm] string values)
    {
        var userId = CurrentUserId;
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var entity = new Category
        {
            CreationDate = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        ApplyValues(entity, values);

        if (!TryValidateModel(entity))
            return BadRequest(ModelState);

        await _cat.AddAsync(entity);
        return Ok(entity);
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
    {
        var entity = await _cat.GetByIdAsync(key);
        if (entity is null)
            return NotFound();

        ApplyValues(entity, values);

        if (!TryValidateModel(entity))
            return BadRequest(ModelState);

        await _cat.UpdateAsync(entity);
        return Ok(entity);
    }

    private static void ApplyValues(Category entity, string values)
    {
        DevExtremeFormValueMapper.Apply(
            values,
            FormValueSetter.String(nameof(Category.Name), value => entity.Name = value),
            FormValueSetter.String(nameof(Category.Description), value => entity.Description = value),
            FormValueSetter.Boolean(nameof(Category.IsActive), value => entity.IsActive = value));
    }
}
