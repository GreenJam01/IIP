using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Analysis;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;
using ElasticSearch;

namespace Elastic_Search.Indexing
{
    public class IndexDefinition
    {
        public static string Name { get; } = "pages";
        public static string EsType { get; } = "pagesSearchIndex";

        public async Task<CreateIndexResponse> CreateIndexAsync(ElasticsearchClient client)
        {
            return await client.Indices.CreateAsync(Name, i => i
                .Settings(CommonIndexDescriptor)
             .Mappings(m => m
    .Properties<Data.Page>(p => p

        .Text(t => t.Title, t =>
            t.Fields(f => f
                .Text("character", ts => ts.Analyzer("character_synonym_analyzer"))
                .Text("relation", ts => ts.Analyzer("relation_synonym_analyzer"))
        ))
        .Text(t => t.Text, ta => ta.Fields(f => f.Text("key", ts => ts
            .Analyzer("keywords_wo_stopwords")
        ))
        )
        .Keyword(t => t.Category, t => t.Fields(f => f.Text("keyword")))
        .DenseVector(t => t.ImageEmbedding,
        ts => ts.
            Dims(512).
            Similarity(DenseVectorSimilarity.Cosine)
        )
        .DenseVector(t => t.TextEmbedding,
        ts => ts.
            Dims(512).
            Similarity(DenseVectorSimilarity.Cosine)
        )

    )

));
        }

        public async Task PerformIndexingAsync(ElasticsearchClient client, List<Data.Page> pages)
        {
            var documents = pages.Select(SearchItemDocumentBase.Map).ToList();
            await PerformDocumentIndexingAsync(client, documents);
        }

        protected async Task<int> PerformDocumentIndexingAsync(ElasticsearchClient client, List<SearchItemDocumentBase> documents)
        {
            if (!documents.Any())
                return 0;

            var bulkIndexResponse = await client.BulkAsync(b => b
                .IndexMany(documents, (op, item) => op
                    .Index(Name)
            ));

            if (bulkIndexResponse.Errors)
            {
                // Логирование ошибок
                Console.WriteLine("Ошибка при индексации документов");
            }

            return bulkIndexResponse.Items.Count;
        }

        protected static Action<IndexSettingsDescriptor> CommonIndexDescriptor => descriptor => descriptor
            .NumberOfReplicas(1)
            .NumberOfShards(1)
        .Analysis(a => a
    .Analyzers(analyzers => analyzers
        .Custom("character_synonym_analyzer", cc => cc
            .Filter(new List<string> { "character_synonym_filter", "lowercase", "russian_stemmer" })
            .Tokenizer("standard")
        )
        .Custom("relation_synonym_analyzer", cc => cc
            .Filter(new List<string> { "relation_synonym_filter", "lowercase", "russian_stemmer" })
            .Tokenizer("standard")
        )
     .Custom("keywords_wo_stopwords", cc => cc
                        .Filter(new List<string> { "rus_stopwords", "trim", "russian_stemmer" })
                        .CharFilter(new List<string> { "html_strip" })
                        .Tokenizer("standard")
                    )
    )
    .TokenFilters(filters => filters

                    .Stop("rus_stopwords", lang => lang
                        .Stopwords(new List<string> { "_russian_" })
                    )
                        .Stemmer("russian_stemmer", lang => lang
                        .Language("russian")
                    )

                    .Stemmer("english_stemmer", lang => lang
                        .Language("english")
                    )
        .Synonym("character_synonym_filter", s => s
            .SynonymsPath("synonims")
            .Format(SynonymFormat.Solr)
        )
        .Synonym("relation_synonym_filter", s => s
            .SynonymsPath("synonyms_relations")
            .Format(SynonymFormat.Solr)
        )

    )
);

        //    .Analysis(a => a
        //        .Analyzers(analyzers => analyzers
        //            .Custom("html_stripper", cc => cc
        //                .Filter(new List<string> { "rus_stopwords", "trim", "lowercase" })
        //                .CharFilter(new List<string> { "html_strip" })
        //                .Tokenizer("autocomplete")
        //            )
        //            .Custom("keywords_wo_stopwords", cc => cc
        //                .Filter(new List<string> { "rus_stopwords", "trim", "lowercase", "russian_stemmer", "english_stemmer" })
        //                .CharFilter(new List<string> { "html_strip" })
        //                .Tokenizer("standard")
        //            )
        //            .Custom("synonym_analyzer", cc => cc
        //                .Filter(new List<string> { "synonym_analyz", "synonym_relations" })
        //                .Tokenizer("standard")
        //            )
        //        )
        //        .Tokenizers(tokenizers => tokenizers
        //            .Keyword("key_tokenizer")
        //            .EdgeNGram("autocomplete", e => e
        //                .MinGram(3)
        //                .MaxGram(15)
        //                .TokenChars(new List<TokenChar> { TokenChar.Letter, TokenChar.Digit })
        //            )
        //        )
        //.TokenFilters(filters => filters
        //    .Stop("rus_stopwords", lang => lang
        //        .Stopwords(new List<string> { "_russian_" })
        //    )
        //.Stemmer("russian_stemmer", lang => lang
        //    .Language("russian")
        //)
        //.Stemmer("english_stemmer", lang => lang
        //    .Language("english")
        //)
        //            .Synonym("synonym_analyz", s => s
        //                .SynonymsPath("synonims")
        //                .Format(SynonymFormat.Solr)
        //            )
        //            .Synonym("synonym_relations", s => s
        //            .SynonymsPath("synonyms_relations")

        //        .Format(SynonymFormat.Solr)
        //        )
        //    )
        //);
    }
}