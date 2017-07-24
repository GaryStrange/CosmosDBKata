using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBTuneKata.DataAccess
{
    public struct DocumentCollectionConfig
    {
        public string collectionName;
        public int offerThroughput;
        public IndexingPolicy indexingPolicy;
    }
}
