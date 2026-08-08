using AccountingSystem.Models.Inventory;
using AccountingSystem.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AccountingSystem.Models.Settings;
using AccountingSystem.Models.Accounts;
using AccountingSystem.Models.Accounting;
using AccountingSystem.Models.Purchase;
using AccountingSystem.Models.Sales;

namespace AccountingSystem.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : 
        IdentityDbContext<User, 
            Role, 
            string, 
            IdentityUserClaim<string>, 
            UserRole, 
            IdentityUserLogin<string>, 
            IdentityRoleClaim<string>, 
            IdentityUserToken<string>> (options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Change AspNet default Identity table names to remove 'AspNet'
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("User");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Role");
            });

            modelBuilder.Entity<UserRole>(entity =>
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
                Email = "admin@admin.com",
                NormalizedEmail = "ADMIN@ADMIN.COM",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "admin",
                ProfilePhoto = string.Empty,
                SecurityStamp = "2c9a4d9b-4f5a-4b8b-9a7c-2b1c3d4e5f61",
                ConcurrencyStamp = "7a3c2e1d-9b8a-4f6e-8c2b-5d4f3a2b1c9e"
            };
            var hasher = new PasswordHasher<User>();
            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin@12345");
            modelBuilder.Entity<User>().HasData(adminUser);

            // seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role() { 
                    Id = "65a02658-9b8d-4505-95af-5edd8634bb35",
                    ConcurrencyStamp = "085747f8-ab1e-4c43-9d7a-9c3f1b7c8d65",
                    Name = "Administrator",
                    NormalizedName = "ADMINISTRATOR",
                    PashtoName = "مدیر"
                },
                new Role()
                {
                    Id = "4877847e-e120-4d16-bb07-d37ae91afbf2",
                    ConcurrencyStamp = "0aad0b13-7d71-4b23-bc8b-bb6c89dfa078",
                    Name = "Seller",
                    NormalizedName = "SELLER",
                    PashtoName = "فروش کوونکی"
                },
                new Role()
                {
                    Id = "1be9d138-e703-4532-ac94-d6fb3639e96e",
                    ConcurrencyStamp = "c4f762eb-8c37-48f7-835a-0d3bb4220f87",
                    Name = "Purchaser",
                    NormalizedName = "PURCHASER",
                    PashtoName = "خرید کوونکی"
                },
                new Role()
                {
                    Id = "e7073afe-407b-44c3-bf9d-b6c53728204a",
                    ConcurrencyStamp = "d1398197-d8c0-48cf-bc4e-dde4d600a64b",
                    Name = "Warehouse Man",
                    NormalizedName = "WAREHOUSE MAN",
                    PashtoName = "ګدام دار"
                },
                new Role()
                {
                    Id = "19e49db9-95a7-4e5d-bfcb-e7bd0ff1ad8d",
                    ConcurrencyStamp = "325efddc-5b32-438e-84b8-dbce6879b8d2",
                    Name = "Finance Manager",
                    NormalizedName = "FINANCE MANAGER",
                    PashtoName = "مالي مدیر"
                }
            );

            // seed role for admin user
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole() { 
                    RoleId = "65a02658-9b8d-4505-95af-5edd8634bb35",
                    UserId = "f5b9b7e7-2d3a-4b4d-a1b5-1b3f2a7a9e01",
                    CreationDate = DateTime.Now
                }
                );

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
                    Name = "له ګدام څخه انتقال",
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

                new StockTransactionType
                {
                    ID = 10,
                    Name = "خرید تغیر"
                },

                new StockTransactionType
                {
                    ID = 11,
                    Name = "فروش تغیر"
                },
                new StockTransactionType
                {
                    ID = 12,
                    Name = "ګدام ته انتقال"
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
                },

                new AccountType()  // just for walkin accounts
                {
                    ID = 11,
                    Name = "خرید مصرف"
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
                },

                new JournalTransactionType()
                {
                    ID = 12,
                    TypeName = "خرید مصرف"
                }
            );
        }
        public DbSet<UserHistory> UserHistories { get; set; }

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
        public DbSet<PurchaseExpences> PurchaseExpenses { get; set; }
        public DbSet<PurchaseExpenseDetails> PurchaseExpenseDetails { get; set; }
        public DbSet<PurchaseVariousExpenses> PurchaseVariousExpenses { get; set; }
        #endregion

        #region Sales
        public DbSet<SaleOrder> SalesOrders { get; set; }
        public DbSet<SaleOrderDetails> SalesOrderDetails { get; set; }
        public DbSet<Sales> Sales { get; set; }
        public DbSet<SaleDetails> SalesDetails { get; set; }
        #endregion
    }
}
