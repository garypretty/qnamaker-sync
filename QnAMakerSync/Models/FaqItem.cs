using System.Collections.Generic;

namespace QnAMakerSync.Models
{
    public class FaqItem
    {
        public List<string> FaqQuestions { get; set; }

        public string Description { get; set; }

        public string ItemId { get; set; }

        public Dictionary<string,string> Metadata { get; set; }
    }
}