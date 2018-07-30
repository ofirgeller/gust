using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GustEfcConsumer.Model
{
    [Table("PostAudits")]
    public class PostAudit
    {
        [Key]
        public long AuditId { get; set; }

        public DateTime AuditAt { get; set; }

        /// <summary>
        /// The id of the post that was updated
        /// </summary>
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public long BlogId { get; set; }

        public Post Post { get; set; }
    }
}
