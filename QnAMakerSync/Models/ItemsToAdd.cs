namespace QnAMakerSync.Models
{
    internal class ItemsToAdd
    {
        public KbItemToAdd[] qnaList { get; set; }
        public string[] urls { get; set; }
        public object[] users { get; set; }
    }
}