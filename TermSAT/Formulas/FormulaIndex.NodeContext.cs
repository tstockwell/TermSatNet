using Microsoft.EntityFrameworkCore;

namespace TermSAT.Formulas;

public static partial class FormulaIndex
{
    public static void OnModelCreating(ModelBuilder modelBuilder, string tableName)
    {
        modelBuilder.Entity<Node>().Property(f => f.Id).IsRequired(); //.ValueGeneratedOnAdd();
        modelBuilder.Entity<Node>().Property(f => f.Parent).IsRequired();
        modelBuilder.Entity<Node>().Property(f => f.Key).IsRequired();
        modelBuilder.Entity<Node>().Property(f => f.Value).IsRequired();

        modelBuilder.Entity<Node>().HasKey(f => f.Id);
        modelBuilder.Entity<Node>().HasIndex(f => f.Parent);
        modelBuilder.Entity<Node>().HasIndex(_ => new { _.Parent, _.Key });

        modelBuilder.Entity<Node>(f => f.ToTable(tableName));
    }
    public class NodeContext : DbContext
    {
        public NodeContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<Node> Nodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            FormulaIndex.OnModelCreating(modelBuilder, nameof(Nodes));
        }

    }
}

