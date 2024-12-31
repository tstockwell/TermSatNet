using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TermSAT.Formulas;

namespace TermSAT.RuleDatabase;

public partial class MetaRecord
{
    [Key]
    public string Key { get; set; }

    [Required]
    public string Value { get; set; }

    public MetaRecord(string key, string value)
    {
        Key = key;
        Value = value;
    }
}

public partial class MetaRecord
{
    public static void OnModelCreating(ModelBuilder modelBuilder, string tableName)
    {
        modelBuilder.Entity<MetaRecord>().Property(f => f.Key).IsRequired();
        modelBuilder.Entity<MetaRecord>().Property(f => f.Value).IsRequired();

        modelBuilder.Entity<MetaRecord>().HasKey(f => f.Key);

        modelBuilder.Entity<MetaRecord>(f => f.ToTable(tableName));
    }

}

