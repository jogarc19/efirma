using eFirmaApi.Model;
using Microsoft.EntityFrameworkCore;

namespace eFirmaApi.Context
{
    public class DBContextNotificacionAPI: DbContext
    {
        public DBContextNotificacionAPI(DbContextOptions options) : base(options) { }
        public DbSet<FIRMA_PFX> FIRMA_PFX { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
