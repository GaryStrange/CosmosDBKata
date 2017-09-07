using System.Collections.Specialized;


namespace CosmosDBTuneKata.DataAccess
{
    public static class DocumentCollectionContextFactory
    {
        public static DocumentCollectionContext CreateCollectionContext(NameValueCollection appSettings)
        {
            return CreateCollectionContext(
                    CosmosDbClientConfig.CreateDocDbConfigFromAppConfig(appSettings)
                );
        }
        public static DocumentCollectionContext CreateCollectionContext(CosmosDbClientConfig config)
        {
            DocumentCollectionContext context = new DocumentCollectionContext(
                client: CosmosDBFactory.CreateClient(config),
                config: config
            );

            return context;
        }
    }
}
