namespace QnAMakerSync.Models
{
    internal class MetaDataUpdateModel
    {
        public MetaDataItem[] add { get; set; }
        public MetaDataItem[] delete { get; set; }
    }
}