using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AccountingSystem.Models.Identity;

namespace AccountingSystem.Models
{
    public class BaseEntity
    {
        [Key]
        public int ID { get; set; }
        public DateTime CreationDate { get; set; }

        // user who created the entity
        public string CreatedByUserId { get; set; } = default!;
        [ForeignKey(nameof(CreatedByUserId))]
        public User CreatedByUser { get; set; } = default!;
    }
}