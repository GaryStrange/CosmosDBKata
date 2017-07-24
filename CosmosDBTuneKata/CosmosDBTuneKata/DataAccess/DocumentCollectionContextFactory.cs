using System.Collections.Specialized;


namespace CosmosDBTuneKata.DataAccess
{
    public static class DocumentCollectionContextFactory
    {
        public static DocumentCollectionContext CreateCollectionContext(NameValueCollection appSettings)
        {
            return CreateCollectionContext(
                    DocumentDbClientConfig.DocDbConfigFromAppConfig(appSettings)
                );
        }
        public static DocumentCollectionContext CreateCollectionContext(DocumentDbClientConfig config)
        {
            DocumentCollectionContext context = new DocumentCollectionContext(
                client: CosmosDBFactory.CreateClient(config),
                config: config
            );

            return context;
        }
    }
}
