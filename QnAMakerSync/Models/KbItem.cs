namespace QnAMakerSync.Models
{
    internal class KbItem
    {
        public int qnaId { get; set; }
        public string answer { get; set; }
        public string source { get; set; }
        public string[] questions { get; set; }
        public MetaDataItem[] metadata { get; set; }
    }
}