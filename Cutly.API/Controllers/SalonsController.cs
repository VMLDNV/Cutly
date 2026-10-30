using Cutly.API.Data;
using Cutly.API.Models.Salon;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cutly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalonsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SalonsController(ApplicationDbContext context)
    {
        _context = context;
    }


    [HttpGet]
    public async Task<IActionResult> GetSalons()
    {
        var salons = await _context.Salons
            .AsNoTracking()
            .Select(salon => new
            {
                salon.Id,
                salon.Name,
                salon.Description,
                salon.Address,
                salon.City,
                salon.ImageUrl,
                salon.Location
            })
            .ToListAsync();

        return Ok(salons);
    }


    [HttpPost]
    public async Task<IActionResult> CreateSalon(
        [FromBody] CreateSalonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.City))
        {
            return BadRequest(new
            {
                message = "Името на салона и градът са задължителни."
            });
        }

        var businessExists = await _context.Businesses
            .AnyAsync(b => b.Id == request.BusinessId);

        if (!businessExists)
        {
            return BadRequest(new
            {
                message = "Посоченият бизнес не съществува."
            });
        }

        var salon = new Salon
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            City = request.City.Trim(),
            Address = request.Address,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            BusinessId = request.BusinessId
        };

        _context.Salons.Add(salon);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetSalons),
            new { id = salon.Id },
            new
            {
                salon.Id,
                salon.Name,
                salon.City,
                salon.Address,
                salon.BusinessId
            });
    }
}

public sealed class CreateSalonRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string City { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public int BusinessId { get; set; }
}
