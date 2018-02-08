namespace QnAMakerSync.Models
{
    internal class ItemsToUpdate
    {
        public string name { get; set; }
        public KbItemToUpdate[] qnaList { get; set; }
        public string[] urls { get; set; }
    }
}