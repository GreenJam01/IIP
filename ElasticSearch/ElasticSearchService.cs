using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Elastic_Search.Indexing;
using ElasticSearch;
using Newtonsoft.Json;

public class ElasticSearchService
{
    private ElasticsearchClient _client;
    private List<Data.Page> _pages;
    private Graph relations = new Graph();

    public ElasticSearchService()
    {
        // Чтение JSON-файла
        string json = File.ReadAllText("C:\\Users\\chaue\\source\\repos\\IIP\\IIP\\bin\\Debug\\net8.0\\wiki.json");
        _pages = JsonConvert.DeserializeObject<List<Data.Page>>(json).DistinctBy(i => i.Title).ToList();
        relations = Graph.ParseGraphFromFile("C:\\Users\\chaue\\Desktop\\OpenCV\\relations.txt");

        // Настройка клиента Elasticsearch
        var settings = new ElasticsearchClientSettings(new Uri("https://localhost:9200"))
            .CertificateFingerprint("45ec5987300eba1672135e3e01d9c273fc1b3ec928ed4855892d12087a16b7e4")
            .Authentication(new BasicAuthentication("elastic", "NY*gecx0XPeIVUGQaBvm"));

        _client = new ElasticsearchClient(settings);
    }

    public async Task Refresh(string indexName)
    {
        await _client.Indices.RefreshAsync(indexName);
    }

    public async Task DeleteIndexIfExists(string indexName)
    {
        var existsResponse = await _client.Indices.ExistsAsync(indexName);
        if (existsResponse.Exists)
        {
            await _client.Indices.DeleteAsync(indexName);
        }
    }

    public async Task CreateIndex(IndexDefinition index, bool autoReindex = true)
    {
        await DeleteIndexIfExists(IndexDefinition.Name);

        var createIndexResponse = await index.CreateIndexAsync(_client);

        if (!createIndexResponse.IsValidResponse)
        {
            throw new Exception("Failed to create index: " + createIndexResponse.DebugInformation);
        }

        if (autoReindex)
        {
            await Reindex(IndexDefinition.Name);
        }
    }

    public async Task Reindex(string indexName)
    {
        var bulkResponse = await _client.BulkAsync(b => b
            .Index(indexName)
            .IndexMany(_pages)
        );

        if (!bulkResponse.IsValidResponse)
        {
            throw new Exception("Failed to reindex: " + bulkResponse.DebugInformation);
        }

    }

    public async Task<SearchServiceResults> Search(CommonSearchRequest searchRequest)
    {
        const int titleBoost = 200;
        const int categoriesBoost = 15;
        const int textBoost = 100;

        var searchResponse = await _client.SearchAsync<SearchItemDocumentBase>(s => s
    .Index(IndexDefinition.Name)
    .Query(q => q
        .FunctionScore(fs => fs
            .Query(q2 => q2
                .Bool(b => b
                    .Should(
                        bs => bs.MultiMatch(m => m
                            .Query(searchRequest.Query)
                            .Fields(new[] { $"title^{titleBoost}" }) 
                            .Analyzer("character_synonym_analyzer")
      
                        ),
                        bs => bs.MultiMatch(m => m
                            .Query(searchRequest.Query)
                            .Fields(new[] { $"title^{400}" })
                            .Analyzer("relation_synonym_analyzer")
                        ),
                        bs => bs.MultiMatch(m => m
                            .Query(searchRequest.Query)
                            .Fields(new[] { $"text^{textBoost}" })
                            .Analyzer("keywords_wo_stopwords")
                        )
                    )
                )
            )
            .Functions(f => f
                .Filter(new TermQuery(new Field("category"))
                {
                    Value = "Главные персонажи",
                    Boost = 100
                }).Weight(100)
                .Filter(new TermQuery(new Field("category"))
                {
                    Value = "Персонажи",
                }).Weight(10)
                .Filter(new TermQuery(new Field("category"))
                {
                    Value = "Эпизоды"
                }).Weight(10)
                .Filter(new TermQuery(new Field("category"))
                {
                    Value = "Комиксы"
                }).Weight(0.01)
                 .Filter(new TermQuery(new Field("category"))
                 {
                     Value = "Галереи"
                 }).Weight(0.1)
            ).BoostMode(FunctionBoostMode.Multiply)
        )
    )
);

        //var searchResponse = await _client.SearchAsync<SearchItemDocumentBase>(s => s
        //    .Index(IndexDefinition.Name)
        //    .Query(q => q
        //        .FunctionScore(fs => fs
        //            .Query(q2 => q2
        //                .MultiMatch(m => m
        //                    .Query(searchRequest.Query)
        //                    .Fields(new[] { $"title.synonym^{titleBoost}", $"text.key^{textBoost}", $"category.keyword^{categoriesBoost}" })
        //                    .Fuzziness(new Fuzziness("1.0"))
        //                )
        //            )
        //            .Functions(f => f
        //            .Filter(new TermQuery(new Field("category"))
        //            {
        //                Value = "Главные персонажи",
        //                Boost = 100
        //            }).Weight(100)
        //                .Filter(new TermQuery(new Field("category"))
        //                {
        //                    Value = "Персонажи",
        //                }).Weight(10)
        //                .Filter(new TermQuery(new Field("category"))
        //                {
        //                    Value = "Эпизоды"
        //                }).Weight(10)
        //                .Filter(new TermQuery(new Field("category"))
        //                {
        //                    Value = "Галереи"
        //                }).Weight(0.5)

        //            ).BoostMode(FunctionBoostMode.Multiply)
        //        )
        //    )
        //    .Highlight(h => h
        //        .Fields(fields => fields
        //            .Add("title", new HighlightFieldDescriptor<SearchItemDocumentBase>()
        //                .NumberOfFragments(2)
        //                .FragmentSize(250)
        //                .NoMatchSize(200))
        //            .Add("text", new HighlightFieldDescriptor<SearchItemDocumentBase>()
        //                .NumberOfFragments(2)
        //                .FragmentSize(250)
        //                .NoMatchSize(200))
        //        )
        //    )
        //    .TrackScores(true)
        //);

        if (!searchResponse.IsValidResponse)
        {
            throw new Exception("Search request failed: " + searchResponse.DebugInformation);
        }

        var searchResult = new SearchServiceResults()
        {
            TotalResults = (int)searchResponse.Total,
            DebugInformation = searchResponse.DebugInformation,
            OriginalQuery = searchRequest.Query
        };

        foreach (var hit in searchResponse.Hits)
        {
            var relatedDocument = hit.Source;
            relatedDocument.Score = hit.Score;
            searchResult.Items.Add(relatedDocument);
        }

        return searchResult;
    }
}