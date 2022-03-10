using System;
using Insta.Enums;

namespace Insta.Model
{
    public class WorkTask
    {
        public int Id { get; set; }
        public Instagram Instagram { get; set; }
        public string Hashtag { get; set; }
        public int LowerDelay { get; set; }
        public int UpperDelay { get; set; }
        public int Offset { get; set; }
        public Mode Mode { get; set; }
        public HashtagType HashtagType { get; set; }
        public DateTime StartTime { get; set; }
    }
}