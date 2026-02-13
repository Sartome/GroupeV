using Microsoft.EntityFrameworkCore;

namespace GroupeV
{
    /// <summary>
    /// Database context for vente_groupe database
    /// Configured for seller-focused heavy client application
    /// </summary>
    public class DatabaseContext : DbContext
    {
        // Main entities for sellers
        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Vendeur> Vendeurs { get; set; }
        public DbSet<Produit> Produits { get; set; }
        public DbSet<Categorie> Categories { get; set; }
        public DbSet<Prevente> Preventes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // En production, stocker la chaîne de connexion dans une variable d'environnement
                var connectionString = Environment.GetEnvironmentVariable("GROUPEV_CONNECTION_STRING")
                    ?? "Server=localhost;Uid=root;Pwd=;Database=vente_groupe;Connection Timeout=10;Default Command Timeout=30;";
                var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));

                optionsBuilder.UseMySql(connectionString, serverVersion, options =>
                {
                    // Enable retry on failure for transient errors
                    options.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    
                    // Set command timeout
                    options.CommandTimeout(30);
                });

                // Enable sensitive data logging in development (remove in production)
                #if DEBUG
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();
                #endif
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Utilisateur configuration
            modelBuilder.Entity<Utilisateur>(entity =>
            {
                entity.ToTable("utilisateur");
                entity.HasKey(e => e.IdUser);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();
            });

            // Vendeur configuration
            modelBuilder.Entity<Vendeur>(entity =>
            {
                entity.ToTable("vendeur");
                entity.HasKey(e => e.IdUser);
                
                entity.HasOne(e => e.Utilisateur)
                    .WithOne()
                    .HasForeignKey<Vendeur>(e => e.IdUser)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.IsCertified).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();
            });

            // Categorie configuration
            modelBuilder.Entity<Categorie>(entity =>
            {
                entity.ToTable("categorie");
                entity.HasKey(e => e.IdCategorie);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();
            });

            // Produit configuration
            modelBuilder.Entity<Produit>(entity =>
            {
                entity.ToTable("produit");
                entity.HasKey(e => e.IdProduit);

                entity.HasOne(e => e.Vendeur)
                    .WithMany()
                    .HasForeignKey(e => e.IdVendeur)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Categorie)
                    .WithMany()
                    .HasForeignKey(e => e.IdCategorie)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Prix).HasColumnType("decimal(10,2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();

                // Indexes
                entity.HasIndex(e => e.IdVendeur).HasDatabaseName("idx_produit_vendeur");
                entity.HasIndex(e => e.IdCategorie).HasDatabaseName("idx_produit_categorie");
            });

            // Prevente configuration
            modelBuilder.Entity<Prevente>(entity =>
            {
                entity.ToTable("prevente");
                entity.HasKey(e => e.IdPrevente);

                entity.HasOne(e => e.Produit)
                    .WithMany()
                    .HasForeignKey(e => e.IdProduit)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.PrixPrevente).HasColumnType("decimal(10,2)");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();
            });
        }
    }
}
