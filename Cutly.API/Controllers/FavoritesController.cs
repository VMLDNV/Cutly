using System.Security.Claims;
using Cutly.API.Data;
using Cutly.API.Models.Favorites;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cutly.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public FavoritesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/favorites/salons
    [HttpGet("salons")]
    public async Task<IActionResult> GetFavoriteSalons()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var favorites = await _context.FavoriteSalons
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                f.Salon.Id,
                f.Salon.Name,
                f.Salon.Description,
                f.Salon.City,
                f.Salon.Address,
                f.Salon.ImageUrl,

                AverageRating = _context.Reviews
                    .Where(r =>
                        r.Appointment.SalonEmployee.SalonId == f.SalonId &&
                        r.Appointment.Status ==
                            Cutly.API.Models.Appointments.AppointmentStatus.Completed)
                    .Select(r => (double?)r.Rating)
                    .Average(),

                ReviewCount = _context.Reviews.Count(r =>
                    r.Appointment.SalonEmployee.SalonId == f.SalonId &&
                    r.Appointment.Status ==
                        Cutly.API.Models.Appointments.AppointmentStatus.Completed)
            })
            .ToListAsync();

        return Ok(favorites);
    }

    // POST: api/favorites/salons/1
    [HttpPost("salons/{salonId:int}")]
    public async Task<IActionResult> AddFavoriteSalon(int salonId)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var salonExists = await _context.Salons
            .AnyAsync(s => s.Id == salonId && s.IsActive);

        if (!salonExists)
        {
            return NotFound(new
            {
                message = "Салонът не беше намерен."
            });
        }

        var alreadyFavorite = await _context.FavoriteSalons
            .AnyAsync(f =>
                f.UserId == userId &&
                f.SalonId == salonId);

        if (alreadyFavorite)
        {
            return Ok(new
            {
                message = "Салонът вече е в любими."
            });
        }

        var favorite = new FavoriteSalon
        {
            UserId = userId,
            SalonId = salonId
        };

        _context.FavoriteSalons.Add(favorite);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Салонът е добавен в любими."
        });
    }

    // DELETE: api/favorites/salons/1
    [HttpDelete("salons/{salonId:int}")]
    public async Task<IActionResult> RemoveFavoriteSalon(int salonId)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var favorite = await _context.FavoriteSalons
            .FirstOrDefaultAsync(f =>
                f.UserId == userId &&
                f.SalonId == salonId);

        if (favorite == null)
        {
            return NotFound(new
            {
                message = "Салонът не е в списъка с любими."
            });
        }

        _context.FavoriteSalons.Remove(favorite);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Салонът е премахнат от любими."
        });
    }
}
