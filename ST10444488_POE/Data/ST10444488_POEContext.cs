using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ST10444488_POE.Models;

namespace ST10444488_POE.Data
{
    public class ST10444488_POEContext : IdentityDbContext<User>
    {
        public ST10444488_POEContext(DbContextOptions<ST10444488_POEContext> options) : base(options) { }
    }
}
