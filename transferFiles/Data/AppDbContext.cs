using Microsoft.EntityFrameworkCore;
using transferFiles.Models;

namespace transferFiles.Data
{
    public class AppDbContext : DbContext
    {
        /// <summary>Esquema de la base donde viven las tablas de este sistema.</summary>
        public const string Schema = "it";

        /// <summary>
        /// Tabla de historial de migraciones propia del sistema, igual que el
        /// resto de los satélites de MeaxDB (__acceso_..., __solicitud_...).
        /// </summary>
        public const string MigrationsHistoryTable = "__transferfiles_migrations_history";

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TransferLinkLog> TransferLinkLogs => Set<TransferLinkLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema(Schema);

            // Estándar de la base: tabla <sistema>_<entidad en plural>, columnas
            // en snake_case, PK <entidad singular>_id, marcas de auditoría
            // created_at / updated_at / deleted_at y booleanos is_*.
            modelBuilder.Entity<TransferLinkLog>(e =>
            {
                e.ToTable("transferfiles_links", Schema);

                e.HasKey(x => x.LinkId);
                e.Property(x => x.LinkId).HasColumnName("link_id").ValueGeneratedOnAdd();

                e.Property(x => x.CreatedByLogin).HasColumnName("created_by_login").HasMaxLength(50).IsRequired();
                e.Property(x => x.CreatedByName).HasColumnName("created_by_name").HasMaxLength(150);
                e.Property(x => x.CreatedByDepartment).HasColumnName("created_by_department").HasMaxLength(50);

                e.Property(x => x.Link).HasColumnName("link_url").HasMaxLength(500).IsRequired();
                e.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(260).IsRequired();
                e.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
                e.Property(x => x.Password).HasColumnName("link_password").HasMaxLength(50);
                e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(1000);

                e.Property(x => x.CreatedAtUtc).HasColumnName("created_at").IsRequired();
                e.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at").IsRequired();
                e.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at");
                e.Property(x => x.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
                e.Property(x => x.DeletedAtUtc).HasColumnName("deleted_at");

                // "Mis links" filtra siempre por usuario y ordena por fecha.
                e.HasIndex(x => new { x.CreatedByLogin, x.CreatedAtUtc })
                 .HasDatabaseName("ix_transferfiles_links_created_by_login_created_at");
            });
        }
    }
}
