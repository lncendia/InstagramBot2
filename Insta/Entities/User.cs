using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Insta.Enums;
using Insta.Working;

namespace Insta.Entities
{
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        public List<Instagram> Instagrams { get; set; } = new();
        public List<Subscribe> Subscribes { get; set; } = new();
        [NotMapped] public List<Work> Works { get; set; } = new();
        [NotMapped] public readonly List<Work> CurrentWorks = new();
        [NotMapped] public Instagram EnterData;
        [NotMapped] public State State;
    }
}