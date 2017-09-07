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
            // Check to verify a database does not exist
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
            DocumentCollection collectionInfo = new DocumentCollection()
            {
                Id = collectionConfig.collectionName,
                PartitionKey = new PartitionKeyDefinition() { Paths = { collectionConfig.PartitionKeyPath } }
            };
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
            catch 
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


        public static T ReadDocument<T>(ICollectionContext context, String documentId
            , object partitionKeyValue = null
            ) where T : Document
        {
            Uri docUri = context.DocumentUri(documentId);

            var response = context.ProcessResourceResponse(
                String.Format("Read Document by id ({0}), partition ({1})", documentId, partitionKeyValue),
                context.Client.ReadDocumentAsync(
                    docUri
                    , new RequestOptions() { PartitionKey = new PartitionKey(partitionKeyValue) }
                    )
                .Result);

            return (dynamic)response.Resource;
        }

        private static string EqualityPredicate(this NameValueCollection attributes, string tableAlias = "c")
        {
            return string.Join("AND",
            Enumerable.Range(0, attributes.Count)
                .Select(i => attributes.GetKey(i) + " = " + attributes.GetValues(i).FirstOrDefault())
                );
        }

        public static FeedResponse<T> RequestDocumentByAttribute<T>(ICollectionContext context, object PartitionKeyValue, NameValueCollection equalityAttributes) where T : Resource, IPartitionedDocument
        {
            var request = context.Client.CreateDocumentQuery<T>(context.CollectionUri
                , String.Format("SELECT * FROM c WHERE c.id = \"{0}\"", 1)
                , new FeedOptions() { PartitionKey = new PartitionKey(PartitionKeyValue) }
                )
                .AsDocumentQuery();

            return request.ExecuteNextAsync<T>().Result;
        }

        public static FeedResponse<T> RequestDocument<T>(ICollectionContext context, string Id, object PartitionKeyValue) where T : Resource, IPartitionedDocument
        {
            var request = context.Client.CreateDocumentQuery<T>(context.CollectionUri
                , String.Format("SELECT * FROM c WHERE c.id = \"{0}\"", Id)
                //, new FeedOptions() { PartitionKey = new PartitionKey(PartitionKeyValue) }
                //, new FeedOptions() { EnableCrossPartitionQuery = true }
                , new FeedOptions() { EnableCrossPartitionQuery = true, MaxDegreeOfParallelism = 1 }
                
                )
                .AsDocumentQuery();

            return request.ExecuteNextAsync<T>().Result;
        }
        public static FeedResponse<T> RequestDocument<T>(ICollectionContext context, T doc) where T : Resource, IPartitionedDocument
        {
            return RequestDocument<T>(context, doc.Id, doc.PartitionKeyValue);
        }

        public static T GetDocument<T>(ICollectionContext context, String Id = null, object partitionKeyValue = null) where T : Resource
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

        public static void DeleteDocument<T>(ICollectionContext context, T doc, object partitionKeyValue) where T : Resource
        {
            var response = context.Client.DeleteDocumentAsync(doc.SelfLink, new RequestOptions() { PartitionKey = new PartitionKey(partitionKeyValue) }).Result;
            Debug.WriteLine(String.Format("Delete response request charge: {0}", response.RequestCharge));
        }



    }
}
