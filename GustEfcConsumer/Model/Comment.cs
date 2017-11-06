using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GustEfcConsumer.Model
{
    public class Comment
    {
        public static Comment Example()
        {
            return new Comment
            {
                Username = "userson",
                Text = "This is a good post"
            };
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Text { get; set; }

        [Required]
        public int PostId { get; set; }

        public int UserId { get; set; }

        public Post Post { get; set; }

        public User User { get; set; }
    }
}
