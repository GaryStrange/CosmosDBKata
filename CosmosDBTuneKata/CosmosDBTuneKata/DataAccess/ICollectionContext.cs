using Microsoft.Azure.Documents.Client;
using System;


namespace CosmosDBTuneKata.DataAccess
{
    public interface ICollectionContext
    {
        DocumentClient Client { get; }
        Uri CollectionUri { get; }
        Uri DocumentUri(string documentId);
    }
}
