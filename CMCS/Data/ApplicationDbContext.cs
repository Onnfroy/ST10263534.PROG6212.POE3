using Microsoft.EntityFrameworkCore;
using CMCS.Models;

namespace CMCS.Data
{
    // ApplicationDbContext class inheriting from DbContext
    public class ApplicationDbContext : DbContext
    {
        // Constructor accepting DbContextOptions and passing it to the base class
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // DbSet property for MonthlyClaim entities
        public DbSet<MonthlyClaim> Claims { get; set; }

        // DbSet property for UploadedDocument entities
        public DbSet<UploadedDocument> Documents { get; set; }
    }
}