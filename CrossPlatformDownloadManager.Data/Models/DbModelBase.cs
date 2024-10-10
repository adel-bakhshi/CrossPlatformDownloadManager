using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrossPlatformDownloadManager.Data.Models;

public abstract class DbModelBase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public abstract void UpdateDbModel(DbModelBase? model);
}