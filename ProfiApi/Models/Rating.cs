using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProfiApi.Models;

[Table("rating", Schema = "Profi1")]
public partial class Rating
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("competency_index")]
    [Precision(5, 2)]
    public decimal? CompetencyIndex { get; set; }

    [Column("trust_level")]
    [Precision(5, 2)]
    public decimal? TrustLevel { get; set; }

    [Column("confirms_count")]
    public int? ConfirmsCount { get; set; }

    [Column("calculate_at", TypeName = "timestamp without time zone")]
    public DateTime? CalculateAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Ratings")]
    public virtual User? User { get; set; }
}
