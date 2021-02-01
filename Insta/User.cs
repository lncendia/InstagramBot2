using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Insta
{
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        public List<Instagram> Instagrams { get; set; } = new List<Instagram>();
        public List<Subscribe> Subscribes { get; set; } = new List<Subscribe>();
        [NotMapped] public List<Work> Works { get; set; } = new List<Work>();
        public Work CurrentWork;
        public Instagram EnterData;

        public enum State
        {
            main,
            login,
            password,
            twoFactor,
            challengeRequired,
            challengeRequiredAccept,
            challengeRequiredPhoneCall,
            selectMode,
            selectHashtag,
            setDuration,
            setTimeWork,
            setDate,
            enterCountToBuy,
            block
        }

        public State state;
    }
}