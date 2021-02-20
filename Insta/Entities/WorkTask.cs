using Insta.Working;
using System;

namespace Insta.Entities
{
    public class WorkTask
    {
        public int Id { get; set; }
        public Instagram Instagram { get; set; }
        public string Hashtag { get; set; }
        public int LowerDelay { get; set; }
        public int UpperDelay { get; set; }
        public Work.Mode Mode { get; set; }
        public DateTime StartTime { get; set; }
    }
}