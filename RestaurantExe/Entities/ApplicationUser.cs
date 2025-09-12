using Microsoft.AspNetCore.Identity;

namespace Restaurant.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
    }
}
