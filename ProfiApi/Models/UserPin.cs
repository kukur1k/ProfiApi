using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("user_pins", Schema = "Profi1")]
public partial class UserPin
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("pin_hash")]
    [StringLength(255)]
    public string PinHash { get; set; } = null!;

    [Column("updated_at", TypeName = "timestamp without time zone")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserPin")]
    public virtual User User { get; set; } = null!;
}
