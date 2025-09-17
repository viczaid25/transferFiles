using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using transferFiles.Models;

namespace transferFiles.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TransferLinkLog> TransferLinkLogs => Set<TransferLinkLog>();
    }
}
