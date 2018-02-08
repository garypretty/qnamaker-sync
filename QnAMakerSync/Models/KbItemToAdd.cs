namespace QnAMakerSync.Models
{
    internal class KbItemToAdd
    {
        public string answer { get; set; }
        public string[] questions { get; set; }
        public MetaDataItem[] metadata { get; set; }
    }
}