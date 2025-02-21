using Elastic.Clients.Elasticsearch.Serverless;
using Elastic.Clients.Elasticsearch.Serverless.Nodes;
using Elastic.Transport;
using Elastic_Search.Indexing;
using Newtonsoft.Json;
using System.Xml.Linq;


namespace Elastic_Search
{
    public partial class Form1 : Form
    {
        ElasticSearchService service;
        public Form1(ElasticSearchService service)
        {
            InitializeComponent();
            this.service = service;
          // service.CreateIndex(new IndexDefinition());


        }
        public record DisplayItem
        {
            public string Title { get; set; }
            public string Categories { get; set; } // Строка для отображения категорий

            public string Text { get; set; }

            public string Url { get; set; }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var searchString = SearchBox.Text;

            if (string.IsNullOrEmpty(searchString))
            {
                MessageBox.Show("Please enter a search term.");
                return;
            }

            try
            {
                var query = $@"
        {{
            ""query"": {{
                ""multi_match"": {{
                    ""query"": ""{searchString}"",
                    ""fields"": [""title"", ""text"", ""category""]
                }}
            }}
        }}";


                // Выполнение поиска
                var searchResults = await service.Search(new ElasticSearch.CommonSearchRequest
                {
                    Query = searchString
                });


                // Отображение результатов в DataGridView
                dataGridView1.DataSource = searchResults.Items.Select(i => new DisplayItem()
                {
                    Categories = string.Join(" ", i.Category),
                    Title = i.Title,
                    Text = i.Text,
                    Url = i.Url,
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }


        }
    }
}
