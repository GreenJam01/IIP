using Data;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Nodes;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Elastic_Search.Indexing;
using ElasticSearch;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ElasticSearchService
{
    private ElasticsearchClient _client;
    private List<Data.Page> _pages;

    public ElasticSearchService()
    {
        // Чтение JSON-файла
        string json = File.ReadAllText("C:\\Users\\chaue\\source\\repos\\IIP\\IIP\\bin\\Debug\\net8.0\\wiki.json");
        _pages = JsonConvert.DeserializeObject<List<Data.Page>>(json).DistinctBy(i => i.Title).ToList();

        // Настройка клиента Elasticsearch
        var settings = new ElasticsearchClientSettings(new Uri("https://localhost:9200"))
            .CertificateFingerprint("45ec5987300eba1672135e3e01d9c273fc1b3ec928ed4855892d12087a16b7e4")
            .Authentication(new BasicAuthentication("elastic", "NY*gecx0XPeIVUGQaBvm"));
        

        _client = new ElasticsearchClient(settings);
    }

    public async void Refresh(string indexName)
    {
        await _client.Indices.RefreshAsync(indexName);
    }

    public async void DeleteIndexIfExists(string indexName)
    {
        if ( _client.Indices.ExistsAsync(indexName).Result.Exists)
        {
            await _client.Indices.DeleteAsync(indexName);
        }
    }

    public async void CreateIndex(IndexDefinition index, bool autoReindex = true)
    {
        DeleteIndexIfExists(IndexDefinition.Name);

        var createIndexResponse = await index.Create(_client);

        if (!createIndexResponse.IsValidResponse)
        {
            throw new Exception("Failed to create index: " + createIndexResponse.DebugInformation);
        }

        if (autoReindex)
        {
            Reindex(IndexDefinition.Name);
        }
    }

    public async void Reindex(string indexName)
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
        const int categoriesBoost = 45;
        const int textBoost = 45;
        var searchResponse = await _client.SearchAsync<SearchItemDocumentBase>(s => s
      .Index(IndexDefinition.Name)
      .Query(q => q
          .FunctionScore(fs => fs
              .Query(q2 => q2
                  .MultiMatch(m => m
                      .Query(searchRequest.Query)
                      .Fields(new[] { $"title.synonym^{titleBoost}", $"text.standard^{textBoost}", $"category^{categoriesBoost}" })
                      .Analyzer("synonym_analyzer")
                      .Analyzer("keywords_wo_stopwords")

                       .Fuzziness(new Fuzziness("Auto")) 
                  )
              )
              .Functions(f => f
                  .Filter(new TermQuery("сategory")
                  {
                      Value = "Персонажи"
                  })
                  .Weight(20.0)
                  .Filter(new TermQuery("сategory")
                  {
                      Value = "Эпизоды"
                  })
                  .Weight(10.0)
              )
          )
      )
  .Highlight(h => h
      .Fields(fields => fields
          .Add("title", new HighlightFieldDescriptor<SearchItemDocumentBase>()
              .NumberOfFragments(2)
              .FragmentSize(250)
              .NoMatchSize(200))
          .Add("text", new HighlightFieldDescriptor<SearchItemDocumentBase>()
              .NumberOfFragments(2)
              .FragmentSize(250)
              .NoMatchSize(200))
      )
  ).TrackScores(true)
  );



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
            searchResult.Items.Add(relatedDocument);
        }

        return searchResult;
    }
}