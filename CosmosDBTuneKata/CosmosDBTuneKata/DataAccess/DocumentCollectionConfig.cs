using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBTuneKata.DataAccess
{
    public class DocumentCollectionConfig
    {
        public string collectionName;
        public int offerThroughput = 10000;
        public IndexingPolicy indexingPolicy;
        public string PartitionKeyPath;
    }
}
