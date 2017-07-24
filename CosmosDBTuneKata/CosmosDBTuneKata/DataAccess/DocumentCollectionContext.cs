using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBTuneKata.DataAccess
{
    public struct DocumentCollectionContext : ICollectionContext
    {
        private DocumentClient _client;
        private DocumentDbClientConfig _config;

        public DocumentClient Client { get { return _client; } }
        public DocumentDbClientConfig Config { get { return _config; } }

        public DocumentCollectionContext(DocumentClient client, DocumentDbClientConfig config)
        {
            _client = client;
            _config = config;

            CreateContextIfNotExists();
        }

        private void CreateContextIfNotExists()
        {
            CosmosDbHelper.CreateDatabaseIfNotExists(this.Client, this.Config.databaseName)
                .ConfigureAwait(false);

            CosmosDbHelper.CreateDocumentCollectionIfNotExists(
                this.Client
                , this.Config.databaseName
                , this.Config.collectionName
                , this.Config.collectionConfig.offerThroughput
                , this.Config.collectionConfig.indexingPolicy)
                .ConfigureAwait(false);

            Task.WaitAll();

        }

        public Uri CollectionUri
        {
            get
            { return UriFactory.CreateDocumentCollectionUri(Config.databaseName, Config.collectionName); }
        }

        public Uri DocumentUri(string documentId)
        {
            return UriFactory.CreateDocumentUri(this.Config.databaseName, this.Config.collectionConfig.collectionName, documentId);
        }

        public void ChangeThroughput(int newThroughput)
        {
            CosmosDbHelper.ReplaceOffer(
                this.Client,
                CosmosDbHelper.ReadOffer(this),
                newThroughput);
        }

        public int GetThroughput()
        {
            //Looking inside the Offer returned from the DocDB sdk it would appear that the retuned
            //readOffer object is actually a OfferV2. However the SDK seems to cast the object to Offer
            //before returning it. So I'm doing a cast back to OfferV2 to get the content.
            OfferV2 o = (CosmosDbHelper.ReadOffer(this) as OfferV2);
            var type = o
                .GetType();

            if (type.GetMember("Content") == null)
                throw new Exception("Unable to retrieve throughput!");

            return o
                .Content.OfferThroughput;
        }


    }

}
