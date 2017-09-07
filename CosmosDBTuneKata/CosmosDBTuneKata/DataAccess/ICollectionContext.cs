using Microsoft.Azure.Documents.Client;
using System;


namespace CosmosDBTuneKata.DataAccess
{
    public interface ICollectionContext
    {
        DocumentClient Client { get; }
        Uri CollectionUri { get; }
        Uri DocumentUri(string documentId);

        T ProcessResourceResponse<T>(string requestInfo, T response) where T : IResourceResponseBase;
        T ProcessFeedResponse<T, K>(T response) where T : IFeedResponse<K>;
    }
}
