using Microsoft.Azure.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBTuneKata.Schemas
{
    public class WishList : Document
    {
        public Wish[] Wishes;
    }
}
