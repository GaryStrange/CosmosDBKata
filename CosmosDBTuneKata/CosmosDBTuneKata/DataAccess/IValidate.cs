using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosDBTuneKata.DataAccess
{
    public interface IValidate<T>
    {
        T Validate();
    }
}
