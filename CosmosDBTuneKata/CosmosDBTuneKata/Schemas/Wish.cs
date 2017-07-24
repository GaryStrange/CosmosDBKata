using CosmosDBTuneKata.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBTuneKata.Schemas
{
    public class Wish
    {
        [Index(IsIncluded = true, HasEqualtiyQueries = true)]
        public string Name { get; set; }
    }
}
