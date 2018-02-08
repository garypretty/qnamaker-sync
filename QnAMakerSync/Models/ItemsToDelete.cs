namespace QnAMakerSync.Models
{
    internal class ItemsToDelete
    {
        public int[] qnaIds { get; set; }
        public string[] sources { get; set; }
        public object[] users { get; set; }
    }
}