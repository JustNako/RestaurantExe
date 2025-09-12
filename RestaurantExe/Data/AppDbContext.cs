using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Restaurant.Entities; // если будут свои сущности

namespace Restaurant.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }

        // Добавим позже:
        // public DbSet<Employee> Employees { get; set; }
        // public DbSet<Category> Categories { get; set; }
        // public DbSet<Product> Products { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<ShiftNote> ShiftNotes { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<RecipeItem> RecipeItems { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<QrLink> QrLinks { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Global query filters
            modelBuilder.Entity<Table>()
                .HasQueryFilter(t => !t.IsDeleted);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId);
            modelBuilder.Entity<Order>()
               .HasOne(o => o.Shift)
               .WithMany(s => s.Orders)
               .HasForeignKey(o => o.ShiftId)
               .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<RecipeItem>()
                .HasOne(ri => ri.Product)
                .WithMany(p => p.RecipeItems)
                .HasForeignKey(ri => ri.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RecipeItem>()
                .HasOne(ri => ri.Ingredient)
                .WithMany(i => i.RecipeItems)
                .HasForeignKey(ri => ri.IngredientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Category-Product relationship
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure QrLink-Table relationship
            modelBuilder.Entity<QrLink>()
                .HasOne(q => q.Table)
                .WithMany()
                .HasForeignKey(q => q.TableId)
                .OnDelete(DeleteBehavior.SetNull);

        }




    }
}
