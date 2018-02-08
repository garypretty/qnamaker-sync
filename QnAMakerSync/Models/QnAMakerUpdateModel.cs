namespace QnAMakerSync.Models
{
    internal class QnAMakerUpdateModel
    {
        public ItemsToAdd add { get; set; }
        public ItemsToDelete delete { get; set; }
        public ItemsToUpdate update { get; set; }
    }
}