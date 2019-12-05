using Microsoft.EntityFrameworkCore;
using mycms_shared.Entities;

namespace mycms.Data
{
    public class MyCmsDbContext : DbContext
    {
        public MyCmsDbContext(DbContextOptions options)
            : base(options) { }

        public virtual DbSet<Article> Articles { get; set; }
        
    }
}