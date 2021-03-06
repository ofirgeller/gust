﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GustEfcConsumer.Model
{
    [Table("PostVotes")]
    public class PostVote : EntityBase
    {
        public static PostVote Example()
        {
            return new PostVote
            {
                Positive = true
            };
        }

        [Key]
        public int Id { get; set; }
                
        public int PostId { get; set; }

        public bool Positive { get; set; }

        public Post Post { get; set; }
    }
}
