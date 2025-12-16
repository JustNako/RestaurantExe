using System;
using System.ComponentModel.DataAnnotations;

namespace Restaurant.Entities
{
    public class Settings
    {
        public int Id { get; set; }
        public string? RestaurantName { get; set; }
        public decimal? ServiceChargePercentage { get; set; }
    }
}
