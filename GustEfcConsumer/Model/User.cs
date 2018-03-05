using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GustEfcConsumer.Model
{
    [Table("Users")]
    public class User
    {
        public static User Example()
        {
            return new User
            {
                Name = "User Userson",
            };
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public List<Blog> Blogs { get; set; }

        public List<Comment> Comments { get; set; }
    }
}
