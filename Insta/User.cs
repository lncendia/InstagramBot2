using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Insta
{
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        public List<Instagram> Instagrams { get; set; } = new();
        public List<Subscribe> Subscribes { get; set; } = new();
        [NotMapped] public List<Work> Works { get; set; } = new();
        public readonly List<Work> CurrentWorks = new();
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
            selectAccounts,
            selectMode,
            selectHashtag,
            setDuration,
            setTimeWork,
            setDate,
            enterCountToBuy,
            block,
            mailingAdmin
            
        }

        public State state;
    }
}