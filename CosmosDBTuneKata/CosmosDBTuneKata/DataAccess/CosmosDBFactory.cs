using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBTuneKata.DataAccess
{
    public static class CosmosDBFactory
    {
        public static DocumentClient CreateClient(DocumentDbClientConfig config)
        {
            return new DocumentClient(
                new Uri(config.endPointUrl),
                config.authKey,
                config.GetConnectionPolicy()
                );

        }
        public static DocumentClient CreateClient(string endpoint, string authKey)
        {
            return CreateClient(
                new DocumentDbClientConfig()
                {
                    endPointUrl = endpoint,
                    authKey = authKey
                }
                );
        }
    }
}
