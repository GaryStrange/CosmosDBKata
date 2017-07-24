using CosmosDBTuneKata.Schema;
using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBTuneKata.DataAccess
{
    public static class IndexingPolicyExtension
    {
        public static void AddRange(this IList<IncludedPath> collection, Dictionary<String, IndexAttribute> pathAndBehaviour)
        {
            foreach (var item in pathAndBehaviour)
            {
                var includedPath = new IncludedPath();
                includedPath.Path = String.Format("/{0}/?", item.Key);
                includedPath.Indexes.AddFromMetaData(item.Value);

                collection.Add(includedPath);
            }
        }

        private static void AddFromMetaData(this IList<Index> collection, IndexAttribute metaData)
        {
            if (metaData.HasRangeOrOrderByQueries)
                collection.Add(new RangeIndex(DataType.String) { Precision = -1 });
            else if (metaData.HasEqualtiyQueries)
                collection.Add(new HashIndex(DataType.String) { Precision = 3 });
        }

        public static void AddRange(this IList<IncludedPath> collection, String[] paths)
        {
            foreach (String path in paths)
            {
                collection.Add(new IncludedPath()
                {
                    Path = String.Format("/{0}/?", path)
                });
            }
        }
    }
}
