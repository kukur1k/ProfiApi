using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("role", Schema = "Profi1")]
public partial class Role
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("title")]
    [StringLength(20)]
    public string? Title { get; set; }

    [InverseProperty("IdRoleNavigation")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
