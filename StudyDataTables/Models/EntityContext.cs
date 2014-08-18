using System.Data.Entity;

namespace StudyDataTables.Models
{
    public class EntityContext : DbContext
    {
        public EntityContext() : base("name=DefaultConnection") { }

        public DbSet<Order> Order { get; set; }
        public DbSet<OrderItem> OrderItem { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<Category> Category { get; set; }
        public DbSet<Company> Company { get; set; }

    }
}