using Microsoft.AspNetCore.Identity;

namespace Factlens.Core.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public int Age { get; set; }
        public string? Phone { get; set; }
    }
}
