using Microsoft.AspNetCore.Identity;

namespace ST10444488_POE.Models
{
    public class User : IdentityUser
    {
        public string Role { get; set; }
    }
}
