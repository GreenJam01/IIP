
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearch
{
    public class ReindexCustomResponse
    {
        public bool Success { get; set; }
        public int TotalProcessed { get; set; }

        public ReindexCustomResponse(bool success = true)
        {
            Success = success;
            TotalProcessed = 0;
        }

        public ReindexCustomResponse MergeWith(ReindexCustomResponse other)
        {
            Success &= other.Success;
            TotalProcessed += other.TotalProcessed;

            return this;
        }
    }

    public class CommonSearchRequest
    {

        public string Query { get; set; }

       
    }

    public class SearchServiceResults
    {
        public List<SearchItemDocumentBase> Items { get; set; }

        public int TotalResults { get; set; }
        public string OriginalQuery { get; set; }

        // Usefull while debugging
        public string DebugInformation { get; set; }

        public SearchServiceResults()
        {
            Items = new List<SearchItemDocumentBase>();
        }
    }

    public class SearchItemDocumentBase
    {

    
        public string Title { get; set; }
        public List<string> Category { get; set; }
 
        public string Url { get; set; }
        public string Text { get; set; }

        public double? Score { get; set; }
        
        internal static SearchItemDocumentBase Map(Data.Page page)
        {
            var result = new SearchItemDocumentBase()
            {
                Category = page.Category,
                Title = page.Title,
                Text = page.Text
            };

            return result;
        }

    }
}
