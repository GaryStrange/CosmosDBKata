using CosmosDBTuneKata.DataAccess;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBTuneKata.Schemas
{
    public class WishList : Document, IPartitionedDocument
    {
        [JsonProperty(PropertyName = "CustomerId")]
        public string CustomerId { get; set; }
        public Wish[] Wishes { get; set; }

        public object PartitionKeyValue => this.CustomerId;
    }
}
