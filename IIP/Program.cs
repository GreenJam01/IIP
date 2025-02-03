using HtmlAgilityPack;
using System.Globalization;
using CsvHelper;
using System.Formats.Asn1;
using System.Xml;
using Fizzler.Systems.HtmlAgilityPack;
using System.Reflection.Metadata;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
namespace SimpleWebScraper
{
    public class Program
    {
        public static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
        // defining a custom class to store 
        // the scraped data 
        public class Page
        {
            public string? Url { get; set; }
            public string? Name { get; set; }
            public string? Text { get; set; }

            public List  <string>  Category { get; set; } = new List <string> ();

            public List <string> Images { get; set; } = new List<string>();
        }

        public static List<string> Categories = new List<string>()
        {
            "Галереи",
            "Игры",
            "Места",
            "Музыка",
            "Неканон",
            "Объекты",
            "Отношения",
            "Персонажи",
            "Продукция",
            "Сериал",
            "События",
            "Сотрудники",
            "Цитаты",
            "Эпизоды"
        };
        public static Page Page_Wiki_Load(string? url)
        {
            var web = new HtmlWeb();
            // setting a global User-Agent header 
            web.UserAgent = UserAgent;
            var currentDocument = web.Load(url);
            var contentNode = currentDocument.DocumentNode.SelectSingleNode("//div[@class='mw-parser-output']");
            if (contentNode != null)
            {
                foreach (var navbox in contentNode.SelectNodes("//table[@class='mw-collapsible mw-collapsed nav']")
                             ?? Enumerable.Empty<HtmlNode>())
                {
                    navbox.Remove();
                }
                Page page = new Page();
              
                string textContent = contentNode.InnerText;
                page.Text = textContent;
                textContent = System.Text.RegularExpressions.Regex.Replace(textContent, @"\s+", " ").Trim();

                var titleNode = currentDocument.DocumentNode.SelectSingleNode("//meta[@property='og:title']");

                if (titleNode != null)
                {
                    // Извлечение значения атрибута content
                    string title = titleNode.GetAttributeValue("content", "Заголовок не найден");
                    page.Name = title;
                }
                List<string> categories = new List<string>();

                // Поиск всех элементов <a> внутри div.page-header__categories
                var categoryNodes = currentDocument.DocumentNode.SelectNodes("//div[@class='page-header__categories']//a");

                if (categoryNodes != null)
                {
                    foreach (var node in categoryNodes)
                    {
                        if (!node.InnerText.EndsWith("других"))
                        {
                            string categoryText = node.InnerText.Trim();
                            categories.Add(categoryText);
                        }
                      
                    }
                }
                page.Category = categories;
                var imageNodes = contentNode.SelectNodes(".//img");
                
                if (imageNodes != null)
                {
                    foreach (var imgNode in imageNodes)
                    {
                        string imageUrl = imgNode.GetAttributeValue("src", null);
                
                        if (!string.IsNullOrEmpty(imageUrl) && imageUrl.StartsWith("https"))
                        {
                            page.Images.Add(imageUrl);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Изображения не найдены.");
                }
                
                return page;
            }
            else
            {
                Console.WriteLine("Элемент с классом 'mw-parser-output' не найден.");
                return new Page { };
            }
         
        }
        public static void Main()
        {
            // initializing HAP 
            var web = new HtmlWeb();
            // setting a global User-Agent header 
            web.UserAgent = UserAgent;
            // creating the list that will keep the scraped data 

            var pages = new List<Page>();
            // the URL of the first pagination web page 
            var firstPageToScrape = "/ru/wiki/%D0%A1%D0%BB%D1%83%D0%B6%D0%B5%D0%B1%D0%BD%D0%B0%D1%8F:%D0%92%D1%81%D0%B5_%D1%81%D1%82%D1%80%D0%B0%D0%BD%D0%B8%D1%86%D1%8B";
            // the list of pages discovered during the crawling task 
            var pagesDiscovered = new List<string> { firstPageToScrape };
            // the list of pages that remains to be scraped 
            var pagesToScrape = new Queue<string>();
            // initializing the list with firstPageToScrape 
            pagesToScrape.Enqueue(firstPageToScrape);
            // current crawling iteration 
            int i = 1;
            // the maximum number of pages to scrape before stopping 
            int limit = 15;
            // until there is a page to scrape or limit is hit 
            while (pagesToScrape.Count != 0 && i < limit)
            {
                // getting the current page to scrape from the queue 
                var currentPage = pagesToScrape.Dequeue();
                // loading the page 
                var currentDocument = web.Load("https://steven-universe.fandom.com" + currentPage);
                // selecting the list of pagination HTML elements 
                var paginationHTMLElements = currentDocument.DocumentNode.QuerySelectorAll("div.mw-allpages-nav");
                // to avoid visiting a page twice 
                foreach (var paginationHTMLElement in paginationHTMLElements)
                {
                    // extracting the current pagination URL 
                    var newPaginationLink = paginationHTMLElement.ChildNodes.Count() == 1 ? paginationHTMLElement.FirstChild.Attributes["href"].Value : paginationHTMLElement.ChildNodes[2].Attributes["href"].Value;
                    // if the page discovered is new 
                    if (!pagesDiscovered.Contains(newPaginationLink))
                    {
                        // if the page discovered needs to be scraped 
                        if (!pagesToScrape.Contains(newPaginationLink))
                        {
                            pagesToScrape.Enqueue(newPaginationLink);
                        }
                        pagesDiscovered.Add(newPaginationLink);
                    }
                }
                // getting the list of HTML page nodes 
                var productHTMLElements = currentDocument.DocumentNode.QuerySelectorAll("ul.mw-allpages-chunk > li > a");
                // iterating over the list of page HTML elements 
                Parallel.ForEach(productHTMLElements,
    new ParallelOptions { MaxDegreeOfParallelism = 4 },
    currentPage =>
    {
        var page = Page_Wiki_Load("https://steven-universe.fandom.com" + currentPage.Attributes["href"].Value);
        pages.Add(page);
    } );
                // incrementing the crawling counter 
                i++;
            }

            using FileStream createStream = File.Create(@"wiki.json");
            var options1 = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                WriteIndented = true
            };
            JsonSerializer.Serialize(createStream, pages, options1);
        }
    }
}