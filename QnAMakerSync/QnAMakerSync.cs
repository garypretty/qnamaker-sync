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
            string uri = QnaMakerEndpoint;
            var client = new HttpClient
            {
                BaseAddress = new Uri(uri)
            };

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", SubscriptionKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = client.GetAsync($"knowledgebases/{KnowledgeBaseId}").Result;

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

            var faqItemsToAdd = faqsToSync.Where(f => !currentFaqItemIdsInKb.Contains(f.PageId)).ToList();
            qnaMakerUpdateModel.add = GenerateItemsToAddModel(faqItemsToAdd);

            qnaMakerUpdateModel.delete = GenerateItemsToDeleteModel(currentKnowledgeBase.qnaList.ToList(), faqsToSync);

            qnaMakerUpdateModel.update = GenerateItemsToUpdateModel(currentKnowledgeBase.qnaList.ToList(), faqsToSync);

            return qnaMakerUpdateModel;
        }

        private static List<int> CurrentFaqItemIdsInKb(QnAMakerKnowledgeBaseModel currentKnowledgeBase)
        {
            var currentFaqItemIdsInKb = new List<int>();

            foreach (var kbItem in currentKnowledgeBase.qnaList)
            {
                var kbPageIdMetaItem = kbItem.metadata.FirstOrDefault(m => m.name == "pageId");
                var kbPageId = kbPageIdMetaItem != null ? Convert.ToInt16(kbPageIdMetaItem.value) : -1;
                if (kbPageId != -1)
                {
                    currentFaqItemIdsInKb.Add(kbPageId);
                }
            }
            return currentFaqItemIdsInKb;
        }

        private static ItemsToDelete GenerateItemsToDeleteModel(IEnumerable<KbItem> currentKnowledgeBaseItems, List<FaqItem> faqItems)
        {
            var kbIdsToDelete = new List<int>();

            foreach (var kbItem in currentKnowledgeBaseItems)
            {
                var kbItemPageIdMetaDataItem = kbItem.metadata.FirstOrDefault(m => m.name == "pageId");
                var faqItemId = kbItemPageIdMetaDataItem != null ? Convert.ToInt16(kbItemPageIdMetaDataItem.value) : -1;
                var faqItem = faqItems.FirstOrDefault(f => f.PageId == faqItemId);

                if (faqItem == null)
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
                var kbItemPageIdMetaDataItem = kbItem.metadata.FirstOrDefault(m => m.name == "pageId");
                var faqPageId = kbItemPageIdMetaDataItem != null ? Convert.ToInt16(kbItemPageIdMetaDataItem.value) : -1;
                var faqPage = faqItems.FirstOrDefault(f => f.PageId == faqPageId);

                if (faqPage != null)
                {
                    var updatedKbItem = new KbItemToUpdate
                    {
                        qnaId = kbItem.qnaId,
                        answer = faqPage.Description,
                        questions = new QuestionsUpdateModel()
                    };

                    if (!kbItem.questions.Contains(faqPage.FAQQuestion))
                    {
                        updatedKbItem.questions.add = new[] { faqPage.FAQQuestion };
                        updatedKbItem.questions.delete = kbItem.questions;
                    }

                    var metadata = new List<MetaDataItem>();

                    var categoryMetadataItem = new MetaDataItem
                    {
                        name = "category",
                        value = faqPage.Category
                    };
                    var contentIdMetadataItem = new MetaDataItem
                    {
                        name = "pageId",
                        value = faqPage.PageId.ToString()
                    };

                    metadata.Add(contentIdMetadataItem);
                    metadata.Add(categoryMetadataItem);

                    updatedKbItem.metadata = new MetaDataUpdateModel
                    {
                        add = metadata.ToArray(),
                        delete = new MetaDataItem[] { }
                    };

                    kbItemsToUpdate.Add(updatedKbItem);
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

            foreach (var faqPage in faqItemsToAdd)
            {
                var kbItem = new KbItemToAdd
                {
                    answer = faqPage.Description,
                    metadata = new MetaDataItem[] { },
                    questions = new string[] { }
                };

                var questions = new List<string> { faqPage.FAQQuestion };
                kbItem.questions = questions.ToArray();

                var metadata = new List<MetaDataItem>();
                var categoryMetadataItem = new MetaDataItem
                {
                    name = "category",
                    value = faqPage.Category
                };
                var contentIdMetadataItem = new MetaDataItem
                {
                    name = "pageId",
                    value = faqPage.PageId.ToString()
                };
                metadata.Add(contentIdMetadataItem);
                metadata.Add(categoryMetadataItem);
                kbItem.metadata = metadata.ToArray();

                kbItemsToAdd.Add(kbItem);
            }

            itemsToAddModel.qnaList = kbItemsToAdd.ToArray();

            return itemsToAddModel;
        }
    }
}
