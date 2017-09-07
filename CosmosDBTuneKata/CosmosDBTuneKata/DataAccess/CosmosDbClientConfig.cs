using Microsoft.Azure.Documents.Client;
using System;

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBTuneKata.DataAccess
{
    public struct CosmosDbClientConfig : IValidate<CosmosDbClientConfig>
    {
        public string endPointUrl;
        public string authKey;
        public string databaseName;
        public DocumentCollectionConfig collectionConfig;

        public string collectionName { get { return this.collectionConfig.collectionName; } }
        public string PartionKeyField { get { return this.collectionConfig.PartitionKeyPath; } }
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

        public static CosmosDbClientConfig CreateDocDbConfigFromAppConfig(NameValueCollection appSettings)
        {
            return new CosmosDbClientConfig()
            {
                endPointUrl = appSettings["EndPointUrl"],
                authKey = appSettings["AuthorizationKey"],
                databaseName = appSettings["DatabaseName"],
                collectionConfig = new DocumentCollectionConfig()
                {
                    collectionName = appSettings["CollectionName"],
                    PartitionKeyPath = appSettings["PartitionKeyPath"]
                }
            }.Validate();
        }

        public CosmosDbClientConfig Validate()
        {
            if (this.endPointUrl == null) throw new NullReferenceException("endPointUrl null!");
            if (this.authKey == null) throw new NullReferenceException("authKey null!");
            if (this.databaseName == null) throw new NullReferenceException("databaseName null!");
            if (this.collectionName == null) throw new NullReferenceException("collectionName null!");
            if (this.PartionKeyField == null) throw new NullReferenceException("PartitionKeyField null!");
            return this;
        }
    };
}
