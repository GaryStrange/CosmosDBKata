using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CosmosDBTuneKata.DataAccess;
using CosmosDBTuneKata.Schemas;
using Microsoft.Azure.Documents.Client;
using System.Diagnostics;

namespace CosmosDBTuneKata.UnitTests
{
    [TestClass]
    public class CosmosDbHelperTest
    {
        CosmosDbClientConfig config;
        DocumentCollectionContext testContext;
        WishList doc;

        internal WishList CreateWishList()
        {
            return new WishList()
            {
                CustomerId = Guid.NewGuid().ToString(),
                Wishes = new Wish[1]
                {
                    new Wish() { Name = "Wish upon a star", Size = "Big" }
                }

            };
        }

        [TestInitialize]
        public void Initialise()
        {
            config = CosmosDbClientConfig.CreateDocDbConfigFromAppConfig(System.Configuration.ConfigurationManager.AppSettings);

            testContext =
                DocumentCollectionContextFactory.CreateCollectionContext(
                    config
                );

            doc = CosmosDbHelper.CreateDocument(testContext, CreateWishList());
        }

        [TestCleanup]
        public void Teardown()
        {
            CosmosDbHelper.DeleteDocument(this.testContext, this.doc, this.doc.CustomerId);
        }

        [TestMethod]
        public void Document_Read_Success()
        {
            WishList Doc = CosmosDbHelper.ReadDocument<WishList>(testContext, this.doc.Id, this.doc.CustomerId);

        }

        [TestMethod]
        public void RequestDocument_Success()
        {
            FeedResponse<WishList> response = CosmosDbHelper.RequestDocument<WishList>(testContext, this.doc);

            Debug.WriteLine(String.Format("RequestDocument response request charge: {0}", response.RequestCharge));
            Assert.IsTrue(response.RequestCharge < 50, string.Format("Request charge ({0} RU) wooh! what did you do? way too much RU.", response.RequestCharge));
            Assert.IsTrue(response.RequestCharge < 3, string.Format("Request charge ({0} RU) is really expensive!", response.RequestCharge));
            Assert.IsTrue(response.RequestCharge < 1.1, string.Format( "Request charge ({0} RU) to expensive!", response.RequestCharge));
        }
    }
}
