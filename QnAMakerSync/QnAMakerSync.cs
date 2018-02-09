using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QnAMakerSync.Models;

namespace QnAMakerSync
{
    public class QnAMakerSync
    {
        public string KnowledgeBaseId { get; set; }
        public string SubscriptionKey { get; set; }
        public string KnowledgeBaseName { get; set; }
        public string QnaMakerEndpoint { get; set; }

        public QnAMakerSync(string knowledgeBaseId, string subscriptionKey, string knowledgeBaseName, string qnaMakerEndpoint = "https://westus.api.cognitive.microsoft.com/qnamaker/v3.0")
        {
            KnowledgeBaseId = knowledgeBaseId;
            SubscriptionKey = subscriptionKey;
            KnowledgeBaseName = knowledgeBaseName;
            QnaMakerEndpoint = qnaMakerEndpoint;
        }

        public async Task UpdateKnowlegdeBase(List<FaqItem> faqPages)
        {
            var currentKnowledgeBase = GetCurrentKnowledgeBase();
            var qnaMakerUpdateModel = GenerateUpdateModel(faqPages, currentKnowledgeBase);
            await UpdateKnowledgeBase(qnaMakerUpdateModel);
        }

        public async Task PublishKnowledgeBase()
        {
            var uri = $"{QnaMakerEndpoint}/knowledgebases/{KnowledgeBaseId}";
            var method = new HttpMethod("PUT");
            var request = new HttpRequestMessage(method, uri);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
            var result = await client.SendAsync(request);
        }

        private async Task UpdateKnowledgeBase(QnAMakerUpdateModel qnaMakerUpdateModel)
        {
            var uri = $"{QnaMakerEndpoint}/knowledgebases/{KnowledgeBaseId}";
            var content = new StringContent(JsonConvert.SerializeObject(qnaMakerUpdateModel), Encoding.UTF8, "application/json");
            var method = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(method, uri)
            {
                Content = content
            };

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            await client.SendAsync(request);
        }

        private QnAMakerKnowledgeBaseModel GetCurrentKnowledgeBase()
        {
            var uri = QnaMakerEndpoint;
            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = client.GetAsync($"{uri}/knowledgebases/{KnowledgeBaseId}").Result;

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = response.Content.ReadAsStringAsync().Result;

            var knowledgeBase = JsonConvert.DeserializeObject<QnAMakerKnowledgeBaseModel>(result);
            return knowledgeBase;
        }

        private QnAMakerUpdateModel GenerateUpdateModel(List<FaqItem> faqsToSync, QnAMakerKnowledgeBaseModel currentKnowledgeBase)
        {
            var qnaMakerUpdateModel = new QnAMakerUpdateModel
            {
                add = new ItemsToAdd(),
                update = new ItemsToUpdate(),
                delete = new ItemsToDelete()
            };

            var currentFaqItemIdsInKb = CurrentFaqItemIdsInKb(currentKnowledgeBase);

            var faqItemsToAdd = faqsToSync.Where(f => !currentFaqItemIdsInKb.Contains(f.ItemId)).ToList();
            qnaMakerUpdateModel.add = GenerateItemsToAddModel(faqItemsToAdd);

            qnaMakerUpdateModel.delete = GenerateItemsToDeleteModel(currentKnowledgeBase.qnaList.ToList(), faqsToSync);

            qnaMakerUpdateModel.update = GenerateItemsToUpdateModel(currentKnowledgeBase.qnaList.ToList(), faqsToSync);

            return qnaMakerUpdateModel;
        }

        private static List<string> CurrentFaqItemIdsInKb(QnAMakerKnowledgeBaseModel currentKnowledgeBase)
        {
            var currentFaqItemIdsInKb = new List<string>();

            foreach (var kbItem in currentKnowledgeBase.qnaList)
            {
                var kbItemIdMetaItem = kbItem.metadata.FirstOrDefault(m => m.name == "itemId");
                if (kbItemIdMetaItem != null)
                {
                    currentFaqItemIdsInKb.Add(kbItemIdMetaItem.value);
                }
            }
            return currentFaqItemIdsInKb;
        }

        private static ItemsToDelete GenerateItemsToDeleteModel(IEnumerable<KbItem> currentKnowledgeBaseItems, List<FaqItem> faqItems)
        {
            var kbIdsToDelete = new List<int>();

            foreach (var kbItem in currentKnowledgeBaseItems)
            {
                var kbItemIdMetaDataItem = kbItem.metadata.FirstOrDefault(m => m.name == "itemId");

                if (kbItemIdMetaDataItem != null)
                {
                    var faqItem = faqItems.FirstOrDefault(f => f.ItemId == kbItemIdMetaDataItem.value);

                    if (faqItem == null)
                    {
                        kbIdsToDelete.Add(kbItem.qnaId);
                    }
                }
                else
                {
                    kbIdsToDelete.Add(kbItem.qnaId);
                }
            }

            var itemsToDeleteModel = new ItemsToDelete()
            {
                qnaIds = kbIdsToDelete.ToArray(),
                sources = new string[] { },
                users = new object[] { }
            };

            return itemsToDeleteModel;
        }

        private ItemsToUpdate GenerateItemsToUpdateModel(IEnumerable<KbItem> currentKnowledgeBaseItems, List<FaqItem> faqItems)
        {
            var itemsToUpdateModel = new ItemsToUpdate
            {
                name = KnowledgeBaseName,
                urls = new string[] { }
            };

            var kbItemsToUpdate = new List<KbItemToUpdate>();

            foreach (var kbItem in currentKnowledgeBaseItems)
            {
                var kbItemIdMetaDataItem = kbItem.metadata.FirstOrDefault(m => m.name == "itemId");

                if (kbItemIdMetaDataItem != null)
                {
                    var faqItem = faqItems.FirstOrDefault(f => f.ItemId == kbItemIdMetaDataItem.value);

                    if (faqItem != null)
                    {
                        var updatedKbItem = new KbItemToUpdate
                        {
                            qnaId = kbItem.qnaId,
                            answer = faqItem.Description,
                            questions = new QuestionsUpdateModel()
                        };

                        var questionsToAdd = faqItem.FaqQuestions.ToList();
                        updatedKbItem.questions.add = questionsToAdd.ToArray();

                        var questionsToDelete = kbItem.questions.ToList().Where(q => !questionsToAdd.Contains(q));
                        updatedKbItem.questions.delete = questionsToDelete.ToArray();

                        var metaDataItemsToDelete = kbItem.metadata
                            .Where(m => !faqItem.Metadata.Select(f => f.Key).Contains(m.name)).ToList();

                        var metaDataItemsToAddOrUpdate = new List<MetaDataItem>();
                        metaDataItemsToAddOrUpdate.Add(new MetaDataItem()
                        {
                            name = "itemId",
                            value = faqItem.ItemId
                        });

                        foreach (var metadataItem in faqItem.Metadata)
                        {
                            metaDataItemsToAddOrUpdate.Add(new MetaDataItem()
                            {
                                name = metadataItem.Key,
                                value = metadataItem.Value
                            });
                        }

                        updatedKbItem.metadata = new MetaDataUpdateModel
                        {
                            add = metaDataItemsToAddOrUpdate.ToArray(),
                            delete = metaDataItemsToDelete.ToArray()
                        };

                        kbItemsToUpdate.Add(updatedKbItem);
                    }
                }
            }

            itemsToUpdateModel.qnaList = kbItemsToUpdate.ToArray();
            return itemsToUpdateModel;
        }

        private static ItemsToAdd GenerateItemsToAddModel(List<FaqItem> faqItemsToAdd)
        {
            var itemsToAddModel = new ItemsToAdd
            {
                qnaList = new KbItemToAdd[] { },
                urls = new string[] { },
                users = new object[] { }
            };

            var kbItemsToAdd = new List<KbItemToAdd>();

            foreach (var faqItem in faqItemsToAdd)
            {
                var kbItem = new KbItemToAdd
                {
                    answer = faqItem.Description,
                    metadata = new MetaDataItem[] { },
                    questions = new string[] { }
                };

                var questions = faqItem.FaqQuestions;
                kbItem.questions = questions.ToArray();

                var metadata = new List<MetaDataItem>();

                metadata.Add(new MetaDataItem()
                {
                    name = "itemId",
                    value = faqItem.ItemId
                });

                foreach (var metaDataItem in faqItem.Metadata)
                {
                    var metaDataItemToAdd = new MetaDataItem
                    {
                        name = metaDataItem.Key,
                        value = metaDataItem.Value
                    };
                    metadata.Add(metaDataItemToAdd);
                }

                kbItem.metadata = metadata.ToArray();

                kbItemsToAdd.Add(kbItem);
            }

            itemsToAddModel.qnaList = kbItemsToAdd.ToArray();

            return itemsToAddModel;
        }
    }
}
