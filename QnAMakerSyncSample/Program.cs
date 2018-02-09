using QnAMakerSyncLib.Models;
using QnAMakerSyncLib;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QnAMakerSyncSample
{
    class Program
    {
        static void Main(string[] args)
        {
            SampleKbSync().Wait();
        }

        public static async Task SampleKbSync()
        {
            // Get a list of QnA items to sync - the data for these could be stored anywhere
            // like a CMS or CRM system
            var qnaItemsToSync = GetQnAItems();

            // Create the QnAMakerSync object by passing in your KB and Subscription Id
            // and providing a name for your QnA Maker KB (which will be updated 
            // if it is different to current one)
            var qnaMakerSync = new QnAMakerSync("<YOUR_KNOWLEDGE_BASE_ID>",
                "<YOUR_SUBSCRIPTION_KEY>", @"You QnA Maker Service Name");

            // Pass your items in and have them pushed to the QnA Maker service
            await qnaMakerSync.UpdateKnowlegdeBase(qnaItemsToSync);

            await qnaMakerSync.PublishKnowledgeBase();
        }

        private static List<QnAItem> GetQnAItems()
        {
            var qnaItems = new List<QnAItem>();

            var item1 = new QnAItem()
            {
                Questions = new List<string>()
                {
                    "What is the meaning of life?",
                    "What's the answer to the meaning of life?"
                },
                Answer = "It is 42 obviously!",
                ItemId = "001",
                Metadata = new Dictionary<string, string>
                {
                    { "Category", "Movies" }
                }
            };
            qnaItems.Add(item1);

            var item2 = new QnAItem()
            {
                Questions = new List<string>() { "Where in the world is Walt Disney World?" },
                Answer = "Florida in the United States",
                ItemId = "002",
                Metadata = new Dictionary<string, string> { { "Category", "Travel" } }
            };
            qnaItems.Add(item2);

            return qnaItems;
        }
    }
}
