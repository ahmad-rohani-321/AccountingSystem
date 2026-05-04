using AccountingSystem.Models.Inventory;
using AccountingSystem.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AccountingSystem.Models.Settings;
using AccountingSystem.Models.Accounts;
using AccountingSystem.Models.Accounting;
using AccountingSystem.Models.Purchase;

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
                entity.ToTable("User");
            });

            modelBuilder.Entity<IdentityRole>(entity =>
            {
                entity.ToTable("Role");
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
                },

                new StockTransactionType()
                {
                    ID = 10,
                    Name = "خرید تغیر"
                },

                new StockTransactionType()
                {
                    ID = 11,
                    Name = "فروش تغیر"
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

            modelBuilder.Entity<AccountType>().HasData(
                new AccountType() // done
                {
                    ID = 1,
                    Name = "تجرۍ"
                },
                new AccountType() // done
                {
                    ID = 2,
                    Name = "بانک"
                },
                new AccountType() // done
                {
                    ID = 3,
                    Name = "پیریدونکی"
                },
                new AccountType() // done
                {
                    ID = 4,
                    Name = "عرضه کوونکی"
                },
                new AccountType() // done
                {
                    ID = 5,
                    Name = "معامله کوونکی"
                },
                new AccountType() // done
                {
                    ID = 6,
                    Name = "عواید"
                },
                new AccountType() // done
                {
                    ID = 7,
                    Name = "مصارف"
                },
                new AccountType()
                {
                    ID = 8,
                    Name = "شریک"
                },
                new AccountType() // done
                {
                    ID = 9,
                    Name = "کارمند"
                },
                new AccountType()  // just for walkin accounts
                {
                    ID = 10,
                    Name = "عادي"
                }
            );

            modelBuilder.Entity<Account>().HasData(
                new Account() // default walkin account has no journal
                {
                    ID = 1,
                    Name = "عادي",
                    AccountTypeID = 10,
                    CreatedByUserId = "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                    Code = "Walkin",
                    CreationDate = DateTime.Now,
                    IsActive = true
                }
                );

            modelBuilder.Entity<AccountContacts>().HasData(
                new AccountContacts()
                {
                    ID = 1,
                    AccountID = 1,
                    CreationDate = DateTime.Now,
                    CreatedByUserId = "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                    Address = "",
                    Email = "",
                    FirstPhone = "",
                    NIC = "",
                    SecondPhone = ""
                }
                );

            modelBuilder.Entity<Currency>().HasData(
                new Currency
                {
                    ID = 1,
                    CurrencyName = "افغانۍ",
                    CurrencySymbole = "AFN",
                    IsMainCurrency = true,
                    IsActive = true,
                    CreatedByUserId = "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                    CreationDate = DateTime.Now
                },
                new Currency
                {
                    ID = 2,
                    CurrencyName = "ډالر",
                    CurrencySymbole = "USD",
                    IsMainCurrency = false,
                    IsActive = true,
                    CreatedByUserId = "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                    CreationDate = DateTime.Now
                }
            );

            modelBuilder.Entity<JournalTransactionType>().HasData(
                new JournalTransactionType()
                {
                    ID = 1,
                    TypeName = "اولنی بلانس"
                },
                new JournalTransactionType()
                {
                    ID = 2,
                    TypeName = "د اسعارو تبادله"
                },
                new JournalTransactionType()
                {
                    ID = 3,
                    TypeName = "نقد جمع"
                },
                new JournalTransactionType()
                {
                    ID = 4,
                    TypeName = "نقد منفي"
                },
                new JournalTransactionType()
                {
                    ID = 5,
                    TypeName = "فروش"
                },
                new JournalTransactionType()
                {
                    ID = 6,
                    TypeName = "خرید"
                },
                new JournalTransactionType()
                {
                    ID = 7,
                    TypeName = "خرید تغیر"
                },
                new JournalTransactionType()
                {
                    ID = 8,
                    TypeName = "فروش تغیر"
                },
                new JournalTransactionType()
                {
                    ID = 9,
                    TypeName = "فروش واپسي"
                },
                new JournalTransactionType()
                {
                    ID = 10,
                    TypeName = "خرید واپسي"
                },
                new JournalTransactionType()
                {
                    ID = 11,
                    TypeName = "د حسابونو تبادله"
                }
            );
        }

        #region Inventory
        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<Unit> Units { get; set; } = default!;
        public DbSet<WareHouse> WareHouses { get; set; } = default!;
        public DbSet<Item> Items { get; set; } = default!;
        public DbSet<UnitConversion> UnitConversion { get; set; } = default!;
        public DbSet<StockBalance> StockBalances { get; set; } = default!;
        public DbSet<StockTransactionType> StockTransactionTypes { get; set; } = default!;
        public DbSet<StockTransactions> StockTransactions { get; set; } = default!;
        public DbSet<ItemPrice> ItemsPrices { get; set; } = default!;
        #endregion

        #region Accounting
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<JournalTransactionType> JournalEntryTransactionTypes { get; set; }
        #endregion

        #region Settings
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<CurrencyExchange> CurrencyExchanges { get; set; }
        #endregion

        #region Account
        public DbSet<AccountType> AccountTypes { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<AccountContacts> AccountContacts { get; set; }
        public DbSet<AccountBalance> AccountBalances { get; set; }
        #endregion

        #region Purchase
        public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
        public DbSet<PurchaseOrderDetails> PurchaseOrderDetails { get; set; }
        public DbSet<Purchase> Purchases { get; set; }
        public DbSet<PurchaseDetails> PurchaseDetails { get; set; }
        #endregion
    }
}
