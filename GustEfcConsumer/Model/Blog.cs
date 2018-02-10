using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GustEfcConsumer.Model
{
    [Table("Blogs")]
    public class Blog : EntityBase
    {
        public static Blog Example()
        {
            return new Blog
            {
                Url = "www.blog.com",
                CreatedAt = DateTime.UtcNow
            };
        }

        [Key]
        [Column("Id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public string Url { get; set; }

        public int? UserId { get; set; }

        public BlogSubject? Subject { get; set; }

        /// <summary>
        /// Used to test our library handeling of nullable properties
        /// story: This is a part of the old blog system. newer blogs do not have it, but we have to keep it for the old ones.
        /// </summary>
        public short? Code{ get; set; }

        public User User { get; set; }

        public List<Post> Posts { get; set; }
    }

    public enum BlogSubject
    {
        Lifestyle = 1,

        Sports = 2,

        Fashion = 3,

        Tech = 4
    }

    [NotMapped]
    public class BlogMetadata
    {
        public DateTime LoadedFromDatabase { get; set; }
    }
}