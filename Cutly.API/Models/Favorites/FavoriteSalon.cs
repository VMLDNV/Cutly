using SalonEntity = Cutly.API.Models.Salon.Salon;
using Cutly.API.Models.Users;

namespace Cutly.API.Models.Favorites;

public class FavoriteSalon
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public int SalonId { get; set; }

    public SalonEntity Salon { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
