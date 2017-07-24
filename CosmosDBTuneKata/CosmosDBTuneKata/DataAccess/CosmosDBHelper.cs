using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBTuneKata.DataAccess
{
    public static class CosmosDbHelper
    {
        public static async Task CreateDatabaseIfNotExists(DocumentClient client, string databaseName)
        {
            // Check to verify a database with the id=FamilyDB does not exist
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(databaseName));
            }
            catch (DocumentClientException de)
            {
                // If the database does not exist, create a new database
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = databaseName });
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task DeleteDatabaseIfExists(DocumentClient client, string databaseName)
        {
            try
            {
                await client.DeleteDatabaseAsync(UriFactory.CreateDatabaseUri(databaseName));
            }
            catch (DocumentClientException de)
            {
                // If the database does not exist then we can ignore the exception
                if (de.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }
        }

        public static async Task DeleteCollectionIfExists(DocumentCollectionContext context)
        {
            try
            {
                await context.Client.DeleteDocumentCollectionAsync(context.CollectionUri);
            }
            catch (DocumentClientException de)
            {
                // If the collection does not exist then we can ignore the exception
                if (de.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            }
        }



        private static void CreateDocumentCollection(DocumentClient client, string databaseName, DocumentCollectionConfig collectionConfig)
        {
            DocumentCollection collectionInfo = new DocumentCollection();
            collectionInfo.Id = collectionConfig.collectionName;

            // Configure collections for maximum query flexibility including string range queries.
            collectionInfo.IndexingPolicy = collectionConfig.indexingPolicy ?? new IndexingPolicy(new RangeIndex(DataType.String) { Precision = -1 });

            try
            {
                // Here we create a collection with 400 RU/s.
                var result = client.CreateDocumentCollectionAsync(
                    UriFactory.CreateDatabaseUri(databaseName),
                    collectionInfo,
                    new RequestOptions { OfferThroughput = collectionConfig.offerThroughput }).Result;

                var x = result.Resource;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static async Task CreateDocumentCollectionIfNotExists(DocumentClient client, string databaseName, DocumentCollectionConfig collectionConfig)
        {
            HttpStatusCode? returnCode;
            try
            {
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collectionConfig.collectionName));
            }
            catch (DocumentClientException de)
            {
                returnCode = de.StatusCode;
                // If the document collection does not exist, create a new collection
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    CreateDocumentCollection(client, databaseName, collectionConfig);
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task CreateDocumentCollectionIfNotExists(DocumentClient client, string databaseName, string collectionName, int throughput = 400,
    IndexingPolicy indexingPolicy = null)
        {
            await CreateDocumentCollectionIfNotExists(client, databaseName
                , new DocumentCollectionConfig()
                {
                    collectionName = collectionName,
                    offerThroughput = throughput,
                    indexingPolicy = indexingPolicy
                }
                );
        }



        public static Database GetDatabaseIfExists(DocumentCollectionContext context)
        {
            return GetDatabaseIfExists(context.Client, context.Config.databaseName);

        }
        public static Database GetDatabaseIfExists(DocumentClient client, string databaseName)
        {
            return client.CreateDatabaseQuery().Where(d => d.Id == databaseName).AsEnumerable().FirstOrDefault();
        }

        public static DocumentCollection GetCollectionIfExists(DocumentCollectionContext context)
        {
            return GetCollectionIfExists(context.Client, context.Config.databaseName, context.Config.collectionName);
        }

        public static DocumentCollection GetCollectionIfExists(DocumentClient client, string databaseName, string collectionName)
        {
            if (GetDatabaseIfExists(client, databaseName) == null)
            {
                return null;
            }

            return client.CreateDocumentCollectionQuery(UriFactory.CreateDatabaseUri(databaseName))
                .Where(c => c.Id == collectionName).AsEnumerable().FirstOrDefault();
        }

        public static async Task<DocumentCollection> CreatePartitionedCollectionAsync(DocumentClient client, string databaseName, string collectionName, string partitionKey)
        {
            return
                await
                    CreatePartitionedCollectionAsync(client, databaseName, collectionName, partitionKey,
                        new RequestOptions { OfferThroughput = 400 });
        }

        public static async Task<DocumentCollection> CreatePartitionedCollectionAsync(DocumentClient client, string databaseName, string collectionName, string partitionKey, RequestOptions requestOptions)
        {
            DocumentCollection collection = new DocumentCollection { Id = collectionName };
            collection.PartitionKey.Paths.Add(partitionKey);

            return await client.CreateDocumentCollectionAsync(
                    UriFactory.CreateDatabaseUri(databaseName),
                    collection,
                    requestOptions);
        }

        public static async Task<DocumentCollection> CreatePartitionedCollectionWithIndexAsync(DocumentClient client, string databaseName, string collectionName, string partitionKey, IndexingPolicy indexingPolicy, RequestOptions requestOptions)
        {
            DocumentCollection collection = new DocumentCollection { Id = collectionName };
            collection.PartitionKey.Paths.Add(partitionKey);
            collection.IndexingPolicy = indexingPolicy;

            return await client.CreateDocumentCollectionAsync(
                    UriFactory.CreateDatabaseUri(databaseName),
                    collection,
                    requestOptions);
        }

        public static Offer ReadOffer(DocumentCollectionContext context)
        {
            return ReadOffer(
                context.Client,
                CosmosDbHelper.GetDatabaseIfExists(context),
                context.Config.collectionName);
        }
        public static Offer ReadOffer(DocumentClient client, Database db, string collectionName)
        {
            DocumentCollection collection = client.ReadDocumentCollectionAsync(string.Format("/dbs/{0}/colls/{1}", db.Id, collectionName)).Result.Resource;

            var response = client.ReadOffersFeedAsync().Result;
            Offer offer = response
                .Single(o => o.ResourceLink == collection.SelfLink);

            return offer;
        }

        public static Offer ReplaceOffer(DocumentClient client, Offer currentOffer, int newThroughput)
        {
            OfferV2 replacementOffer = new OfferV2(currentOffer, newThroughput);

            ResourceResponse<Offer> replaceOffer = client.ReplaceOfferAsync(replacementOffer).Result;

            return (Offer)replaceOffer;
        }

        public static List<PartitionKeyRange> GetPartitionKeyRange(DocumentCollectionContext context)
        {
            return GetPartitionKeyRange(context.Client, context.CollectionUri);
        }
        public static List<PartitionKeyRange> GetPartitionKeyRange(DocumentClient client, string databaseName, string collectionName)
        {
            return GetPartitionKeyRange(client, UriFactory.CreateDocumentCollectionUri(databaseName, collectionName));
        }
        public static List<PartitionKeyRange> GetPartitionKeyRange(DocumentClient client, Uri collectionUri)
        {
            string pkRangesResponseContinuation = null;
            List<PartitionKeyRange> partitionKeyRanges = new List<PartitionKeyRange>();

            do
            {
                FeedResponse<PartitionKeyRange> pkRangesResponse = client.ReadPartitionKeyRangeFeedAsync(
                    collectionUri,
                    new FeedOptions { RequestContinuation = pkRangesResponseContinuation }).Result;

                partitionKeyRanges.AddRange(pkRangesResponse);
                pkRangesResponseContinuation = pkRangesResponse.ResponseContinuation;
            }
            while (pkRangesResponseContinuation != null);

            return partitionKeyRanges;
        }

        public static T ReadDocument<T>(ICollectionContext context, String documentId, String partitionKeyValue = null) where T : Resource
        {
            Uri docUri = context.DocumentUri(documentId);

            var response = context.Client.ReadDocumentAsync(docUri
                , new RequestOptions() { PartitionKey = new PartitionKey(partitionKeyValue) })
                .Result;

            Debug.WriteLine(String.Format("Read response request charge: {0}", response.RequestCharge));

            return (dynamic)response.Resource;
        }

        public static T GetDocument<T>(ICollectionContext context, String Id = null, String partitionKeyValue = null) where T : Resource
        {
            var request = context.Client.CreateDocumentQuery<T>(context.CollectionUri
                , String.Format("SELECT * FROM c WHERE c.id = \"{0}\"", Id)
                , new FeedOptions() { PartitionKey = new PartitionKey(partitionKeyValue) }
                )
                .AsDocumentQuery();

            FeedResponse<T> response = request.ExecuteNextAsync<T>().Result;
            Debug.WriteLine(String.Format("Read response request charge: {0}", response.RequestCharge));

            return response.AsEnumerable().FirstOrDefault();
        }

        public static T CreateDocument<T>(ICollectionContext context, T doc) where T : Resource
        {
            var response = context.Client.CreateDocumentAsync(context.CollectionUri, doc).Result;
            Debug.WriteLine(String.Format("Create response request charge: {0}", response.RequestCharge));
            return (dynamic)response.Resource;
        }

        public static void ReplaceDocument<T>(ICollectionContext context, T doc) where T : Resource
        {
            var response = context.Client.ReplaceDocumentAsync(context.DocumentUri(doc.Id), doc).Result;
            Debug.WriteLine(String.Format("Replace response request charge: {0}", response.RequestCharge));
        }

        public static void UpsertDocument<T>(ICollectionContext context, T doc) where T : Resource
        {
            var response = context.Client.UpsertDocumentAsync(context.CollectionUri, doc).Result;
            Debug.WriteLine(String.Format("Upsert response request charge: {0}", response.RequestCharge));
        }

        public static async Task<DocumentChangeFeedBatch<T>> ReadChangeFeed<T>(DocumentCollectionContext context, DocumentChangeFeedBatch<T> batchContext)
        {
            return await ReadChangeFeed<T>(context.Client, context.CollectionUri, batchContext);
        }

        public static async Task<DocumentChangeFeedBatch<T>> ReadChangeFeed<T>(DocumentClient client, Uri collectionUri, DocumentChangeFeedBatch<T> batchContext)
        {
            //Dictionary<string, string> checkPoints = new Dictionary<string, string>();
            //List<T> feed = new List<T>();

            //string continuation = null;
            //checkPoints.TryGetValue(batchContext.KeyRange.Id, out continuation);

            DocumentChangeFeedBatch<T> newBatch = CosmosDbHelper.CreateDocumentChangeFeedBatch<T>(batchContext);
            IDocumentQuery<Document> query = client.CreateDocumentChangeFeedQuery(
                collectionUri,
                newBatch.Options
                );

            while (query.HasMoreResults)
            {
                FeedResponse<T> readChangesResponse = await query.ExecuteNextAsync<T>();

                foreach (T doc in readChangesResponse)
                {
                    newBatch.FeedData.Add(doc);
                }

                newBatch.ResponseContinuation = readChangesResponse.ResponseContinuation;
            }

            return newBatch;
        }


        public class DocumentChangeFeedBatch<T>
        {
            public DocumentChangeFeedBatch(String RangeKeyId, String RequestContinuation = null)
            {
                this.Options = new ChangeFeedOptions()
                {
                    PartitionKeyRangeId = RangeKeyId,
                    RequestContinuation = RequestContinuation,
                    StartFromBeginning = (RequestContinuation != null) ? false : true
                };

                FeedData = new List<T>();
            }

            public String ResponseContinuation { get; set; }
            public List<T> FeedData { get; set; }

            public ChangeFeedOptions Options;
        }

        public static DocumentChangeFeedBatch<T> CreateDocumentChangeFeedBatch<T>(PartitionKeyRange keyRange)
        {
            return new DocumentChangeFeedBatch<T>(keyRange.Id);
        }

        public static DocumentChangeFeedBatch<T> CreateDocumentChangeFeedBatch<T>(DocumentChangeFeedBatch<T> previousBatch)
        {
            return new DocumentChangeFeedBatch<T>(
                RangeKeyId: previousBatch.Options.PartitionKeyRangeId,
                RequestContinuation: previousBatch.ResponseContinuation // response is stored into request for new batch
                );
        }


    }
}
