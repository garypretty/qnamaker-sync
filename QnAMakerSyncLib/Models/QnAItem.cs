using System.Collections.Generic;

namespace QnAMakerSyncLib.Models
{
    public class QnAItem
    {
        public List<string> Questions { get; set; }

        public string Answer { get; set; }

        public string ItemId { get; set; }

        public Dictionary<string,string> Metadata { get; set; }
    }
}