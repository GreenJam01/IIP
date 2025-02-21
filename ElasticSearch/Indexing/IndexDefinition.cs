using Data;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Analysis;
using Elastic.Clients.Elasticsearch.Core.TermVectors;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using ElasticSearch;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elastic_Search.Indexing
{
    public class IndexDefinition
    {
        public static string Name { get; } = "pages";
        public static string EsType { get; } = "pagesSearchIndex";

        public async Task <CreateIndexResponse> Create(ElasticsearchClient client)
        {
            return await client.Indices.CreateAsync(Name, i => i
                .Settings(CommonIndexDescriptor)
                .Mappings(m => m
                                                    .Properties<Data.Page>(p => p
                                                        .Keyword(t => t.Title)
                                                        .Text(t => t.Text)
                                                        .Text(t => t.Category)
                                      
                                                        
                                                    ).Properties<SearchItemDocumentBase>(p => p
        .Text(t => t.Title, ta => ta
            .Fields(f => f
                .Text("synonym", ts => ts.Analyzer("synonym_analyzer"))
            )
        )
        .Text(t => t.Text, ta => ta
            .Fields(f => f
                .Text("standard", ts => ts.Analyzer("keywords_wo_stopwords"))
            )
        )

        .Text(t => t.Category)
    )
    )
                

            );
        }

        internal async void  PerformIndexing(ElasticsearchClient client, List<Data.Page> pages)
        {
            await PerformDocumentIndexing(client, pages.Select(SearchItemDocumentBase.Map).ToList());
        }
        protected async Task<int> PerformDocumentIndexing(ElasticsearchClient client, List<SearchItemDocumentBase> documents)
        {
            if (documents.Any())
            {
                var bulkIndexResponse = await client.BulkAsync(b => b
                    .IndexMany(documents, (op, item) => op
                        .Index(Name)
                        
                    )
                );

                if (bulkIndexResponse.Errors)
                {
                    // Handle error...
                }

                return bulkIndexResponse.Items.Count;
            }

            return 0;
        }

        protected static Action<IndexSettingsDescriptor> CommonIndexDescriptor => descriptor => descriptor
    .NumberOfReplicas(0)
    .NumberOfShards(1)
    .Analysis(a => a
        .Analyzers(analyzers => analyzers
            .Custom("html_stripper", cc => cc
                .Filter(new List<string>() { "rus_stopwords", "trim", "lowercase" })
                .CharFilter(new List<string>() { "html_strip" })
                .Tokenizer("autocomplete")
            )
            .Custom("keywords_wo_stopwords", cc => cc
                .Filter(new List<string>() { "rus_stopwords", "trim", "lowercase" })
                .CharFilter(new List<string>() { "html_strip" })
                .Tokenizer("key_tokenizer")
            )
            .Custom("synonym_analyzer", cc => cc
            .Filter(new List<string>() { "lowercase", "rus_stopwords", "russian_stemmer", "english_stemmer" })
            .Tokenizer("standard")
        )
        )
        .Tokenizers(tokenizers => tokenizers
            .Keyword("key_tokenizer")
            .EdgeNGram("autocomplete", e => e
                .MinGram(3)
                .MaxGram(15)
                .TokenChars(new List<TokenChar> { TokenChar.Letter, TokenChar.Digit })
            )
        )
        .TokenFilters(filters => filters
            .Stop("rus_stopwords", lang => lang
                .Stopwords(new List<string>() { "_russian_" })
            )
            .Stemmer("russian_stemmer", lang => lang
              .Language("russian")
          )
         .Stemmer("english_stemmer", lang => lang
              .Language("english")
          )
          .Synonym("synonym_analyzer", s => s
              .SynonymsPath("analysis/synonyms")
              .Format(SynonymFormat.Solr)
          )
        )
    );
    }
}