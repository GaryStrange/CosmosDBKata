using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBTuneKata.DataAccess
{
    public struct DocumentDbClientConfig
    {
        public string endPointUrl;
        public string authKey;
        public string databaseName;
        public DocumentCollectionConfig collectionConfig;

        public string collectionName { get { return this.collectionConfig.collectionName; } }
        public ConnectionPolicy GetConnectionPolicy()
        {
            return new ConnectionPolicy
            {
                ConnectionMode = ConnectionMode.Direct,
                ConnectionProtocol = Protocol.Tcp,
                RequestTimeout = TimeSpan.FromMinutes(2),
                RetryOptions = new RetryOptions
                {
                    MaxRetryAttemptsOnThrottledRequests = 5,
                    MaxRetryWaitTimeInSeconds = 2
                }
            };
        }

        public static DocumentDbClientConfig DocDbConfigFromAppConfig(NameValueCollection appSettings)
        {
            return new DocumentDbClientConfig()
            {
                endPointUrl = appSettings["EndPointUrl"],
                authKey = appSettings["AuthorizationKey"],
                databaseName = appSettings["DatabaseName"],
                collectionConfig = new DocumentCollectionConfig()
                {
                    collectionName = appSettings["CollectionName"]
                }
            };
        }

    };
}
