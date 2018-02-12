# QnA Maker Sync Library

NuGet Package -> <https://www.nuget.org/packages/QnAMakerSync/1.0.0>

## Summary

C# library to allow you to sync QnA items to the Microsoft QnA Maker service from anywhere.

Implemented for v3 of the QnA Maker Service (https://westus.dev.cognitive.microsoft.com/docs/services/597029932bcd590e74b648fb/operations/597037798a8bb5031800bf5b), the library allows you to build up a list of QnAItems (for example you might have existing FAQ pages in your CMS or in a CRM system) and sync them with the QnA Maker service.  You could use the library within a scheduled job to keep all of your items up to date in QnA Maker without the need of duplicating effort maintaining your items in more than one place (e.g. FAQ pages on your web site and in the QnA Maker portal).

Each item that you sync has the following properties;

* One or more questions
* An answer
* A unique Id (string) that you use to identify the QnA Item locally (so that when you sync again we update it instead of add a new one)
* Metadata - key value pairs you can add to QnA Items which can be used to either strictly filter or boost certain answers you get back from QnA Maker service when you query it for answers later on.

**One major advantage of using this library right now is that the QnA Maker Portal does not currently support adding metadata to QnA Items through the UI**

Right now the sync is one-way, from your respository up to the QnA Maker service and your respository is expected to be the single source of truth for your QnA items. Any items found within your QnA Maker knowledgebase will be removed if a matching item is not found within your local repository (using the ItemId). A future update will provide additional capabilities, such as ignoring items in your knowledgebase that were not inserted using this library (i.e. those that you manually add within the portal) and also the ability to sync items created within the portal back down to your own knowledgebase.

## Usage and samples 

You can create an instance of the QnAMakerSync class and pass your QnA Items to the UpdateKnowledgeBase method to have them syncronised to your QnA Maker service.  I have also included a PublishKnowledgeBase, which does exactly what it suggests, publishes your knowledge base.

```cs

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

```

You will need to build a list of QnAItems to pass to the service and in the example above I am calling the GetQnAItems method to retrive this list.Below is a simplistic example of how that method (also included in the repo in the sample project) which creates 2 QnA Items, but here is where you would potentially go and loop through all of the FAQ pages in your site or existing repository and build them up from there.  

```cs

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

```

The **ItemId** property should be a unique identifier for the QnA Item wherever you are storing them outside of QnA Maker, e.g. if you have FAQ pages in your CMS then you might use the ID of the page. Here we are also adding a metadata item called 'Category', but this could be absolutely anything you want to filter your answer on later.

*Note: When using this package, "itemId" is a reserved metadata value that is used to store the ItemId you provide on the QnAItem to allow updates to take place. Please avoid using  "itemId" as a metadata value.*