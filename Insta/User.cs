using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Insta
{
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        public virtual List<Instagram> Instagrams { get; set; } = new List<Instagram>();
        public virtual List<Subscribe> Subscribes { get; set; } = new List<Subscribe>();
        [NotMapped] public List<Work> Works { get; set; } = new List<Work>();
        public Work CurrentWork;
        public Instagram enterData;

        public enum State
        {
            main,
            login,
            password,
            twofactor,
            challengeRequired,
            challengeRequiredAccept,
            challengeRequiredPhoneCall,
            selectHashtag,
            setDuration,
            setTimeWork,
            setDate,
            entetCountToBuy,
            block
        }

        public State state;
    }
}