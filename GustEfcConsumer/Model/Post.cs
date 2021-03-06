﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GustEfcConsumer.Model
{
    [Table("Posts")]
    public class Post : EntityBase
    {
        public static Post Example()
        {
            return new Post
            {
                Title = "Go Fish!",
                Content = "This is how you fish a fish",
            };
        }

        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public long BlogId { get; set; }

        public Blog Blog { get; set; }

        public List<Comment> Comments { get; set; }

        public List<PostVote> PostVotes { get; set; }

        public List<PostAudit> Versions { get; set; }
    }
}
