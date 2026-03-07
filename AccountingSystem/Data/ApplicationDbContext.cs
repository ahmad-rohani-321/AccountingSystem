using AccountingSystem.Models.Inventory;
using AccountingSystem.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace AccountingSystem.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<User>(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Change AspNet default Identity table names to remove 'AspNet'
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable(name: "User");
            });

            modelBuilder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable(name: "Role");
            });

            modelBuilder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("UserRole");
            });

            modelBuilder.Entity<IdentityUserClaim<string>>(entity =>
            {
                entity.ToTable("UserClaim");
            });

            modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.ToTable("UserLogin");
            });

            modelBuilder.Entity<IdentityRoleClaim<string>>(entity =>
            {
                entity.ToTable("RoleClaim");
            });

            modelBuilder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.ToTable("UserToken");
            });

            // Seed default admin user
            var adminUser = new User
            {
                Id = "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin",
                NormalizedEmail = "ADMIN",
                EmailConfirmed = true,
                FirstName = "admin",
                LastName = "admin",
                ProfilePhoto = string.Empty,
                SecurityStamp = "2c9a4d9b-4f5a-4b8b-9a7c-2b1c3d4e5f61",
                ConcurrencyStamp = "7a3c2e1d-9b8a-4f6e-8c2b-5d4f3a2b1c9e"
            };

            var hasher = new PasswordHasher<User>();
            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin@12345");

            modelBuilder.Entity<User>().HasData(adminUser);

            modelBuilder.Entity<StockTransactionType>().HasData(
                new StockTransactionType
                {
                    ID = 1,
                    Name = "ابتدایي موجودي",
                },
                new StockTransactionType
                {
                    ID = 2,
                    Name = "ګدام ته داخلول",
                },

                new StockTransactionType
                {
                    ID = 3,
                    Name = "له ګدام څخه ایستل",
                },

                new StockTransactionType
                {
                    ID = 4,
                    Name = "د ګدامونو ترمنځ انتقال",
                },

                new StockTransactionType
                {
                    ID = 5,
                    Name = "خرید",
                },

                new StockTransactionType
                {
                    ID = 6,
                    Name = "خرید واپسي",
                },

                new StockTransactionType
                {
                    ID = 7,
                    Name = "فروش",
                },

                new StockTransactionType
                {
                    ID = 8,
                    Name = "فروش واپسي",
                },

                new StockTransactionType
                {
                    ID = 9,
                    Name = "ضایعات",
                }
            );

            modelBuilder.Entity<WareHouse>().HasData(
                new WareHouse
                {
                    ID = 1,
                    Name = "عمومي ګدام",
                    Description = "اصلي ګدام د ټولو موادو لپاره دی.",
                    CreatedByUserId = "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                    CreationDate = DateTime.Now
                }
            );
        }
        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<Unit> Units { get; set; } = default!;
        public DbSet<WareHouse> WareHouses { get; set; } = default!;
        public DbSet<Item> Items { get; set; } = default!;
        public DbSet<UnitConversion> UnitConversion { get; set; }
        public DbSet<StockBalance> StockBalances { get; set; } = default!;
        public DbSet<StockTransactionType> StockTransactionTypes { get; set; } = default!;
        public DbSet<StockTransactions> StockTransactions { get; set; } = default!;
    }
}
