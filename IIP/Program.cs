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
using Data;
using System.Text.RegularExpressions;
using System.Net;
using System.Text;
namespace SimpleWebScraper
{
    public class Program
    {
        public static string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";

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

        public static void SynonimsGatering(string? url, List<string> synonims)
        {
            var web = new HtmlWeb();
            web.UserAgent = UserAgent;
            var currentDocument = web.Load(url);
            var synonimsList = new List<string>();

            var nameNode = currentDocument.DocumentNode.SelectSingleNode("//p[b]");
            if (nameNode != null)
            {
                string name = WebUtility.HtmlDecode(nameNode.SelectSingleNode(".//b")?.InnerText.Trim()).Trim();
                string fullText = nameNode.InnerText;
                var match = Regex.Match(fullText, @"\(англ\.\s*([^)]+)\)");
                if (match.Success)
                {
                    string englishSynonym = WebUtility.HtmlDecode(match.Groups[1].Value.Trim()).Replace("&#160;", "").Trim();

                    if (!string.IsNullOrEmpty(name))
                        synonimsList.Add(name);
                    if (!string.IsNullOrEmpty(englishSynonym))
                        synonimsList.Add(englishSynonym);
                }
            }


            var nicknamesSection = currentDocument.DocumentNode.SelectSingleNode("//section[contains(@class, 'pi-smart-group') and .//h3[@data-source='Прозвище']]");
            if (nicknamesSection != null)
            {
                var nicknameNodes = nicknamesSection.SelectNodes(".//li");
                if (nicknameNodes != null)
                {
                    foreach (var node in nicknameNodes)
                    {
                        string nickname = node.InnerText.Trim();
                        nickname = nickname.Split('(')[0].Trim();
                        if (!string.IsNullOrEmpty(nickname))
                            synonimsList.Add(nickname);
                    }
                }
            }

            // Извлекаем псевдоним
            var pseudonymSection = currentDocument.DocumentNode.SelectSingleNode("//section[contains(@class, 'pi-smart-group') and .//h3[@data-source='Псевдоним']]");
            if (pseudonymSection != null)
            {
                var pseudonymNode = pseudonymSection.SelectSingleNode(".//div[@class='pi-smart-data-value']");
                if (pseudonymNode != null)
                {
                    string pseudonym = pseudonymNode.InnerText.Trim();
                    if (!string.IsNullOrEmpty(pseudonym))
                        synonimsList.Add(pseudonym);
                }
            }
            string str = string.Join(",", synonimsList);
            synonims.Add(string.Join(",", str));
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(str);
        }

        private static void WriteSynonimsToFile(List<string> synonims, string filePath)
        {
            try
            {
                File.WriteAllLines(filePath, synonims.Where(i => !string.IsNullOrEmpty(i)));
                Console.WriteLine($"Синонимы успешно записаны в файл: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи в файл: {ex.Message}");
            }
        }
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
                page.Url = url;
              
                string textContent = contentNode.InnerText;
                page.Text = textContent;
                textContent = System.Text.RegularExpressions.Regex.Replace(textContent, @"\s+", " ").Trim();

                var titleNode = currentDocument.DocumentNode.SelectSingleNode("//meta[@property='og:title']");

                if (titleNode != null)
                {
                    string title = titleNode.GetAttributeValue("content", "Заголовок не найден");
                    page.Title = title;
                }
                List<string> categories = new List<string>();
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
            var web = new HtmlWeb();
            web.UserAgent = UserAgent;
            var pages = new List<Page>();
            var firstPageToScrape = "/ru/wiki/%D0%A1%D0%BB%D1%83%D0%B6%D0%B5%D0%B1%D0%BD%D0%B0%D1%8F:%D0%92%D1%81%D0%B5_%D1%81%D1%82%D1%80%D0%B0%D0%BD%D0%B8%D1%86%D1%8B";
            var pagesDiscovered = new List<string> { firstPageToScrape };
            var pagesToScrape = new Queue<string>();
            pagesToScrape.Enqueue(firstPageToScrape);
            int i = 1;
            int limit = 15;
            List<string> synonims = new List<string>();
            while (pagesToScrape.Count != 0 && i < limit)
            {
                var currentPage = pagesToScrape.Dequeue();
                var currentDocument = web.Load("https://steven-universe.fandom.com" + currentPage);
                var paginationHTMLElements = currentDocument.DocumentNode.QuerySelectorAll("div.mw-allpages-nav");
                foreach (var paginationHTMLElement in paginationHTMLElements)
                {
                    var newPaginationLink = paginationHTMLElement.ChildNodes.Count() == 1 ? paginationHTMLElement.FirstChild.Attributes["href"].Value : paginationHTMLElement.ChildNodes[2].Attributes["href"].Value;
                    if (!pagesDiscovered.Contains(newPaginationLink))
                    {
                        if (!pagesToScrape.Contains(newPaginationLink))
                        {
                            pagesToScrape.Enqueue(newPaginationLink);
                        }
                        pagesDiscovered.Add(newPaginationLink);
                    }
                }
                var productHTMLElements = currentDocument.DocumentNode.QuerySelectorAll("ul.mw-allpages-chunk > li > a");
                Parallel.ForEach(productHTMLElements,
                 new ParallelOptions { MaxDegreeOfParallelism = 4 },
                currentPage =>
                {
                    Page_Wiki_Load("https://steven-universe.fandom.com" + currentPage.Attributes["href"].Value);
                    SynonimsGatering("https://steven-universe.fandom.com" + currentPage.Attributes["href"].Value, synonims);
                });
                i++;
            }
            WriteSynonimsToFile(synonims.Distinct().ToList(), "synonims");

            //using FileStream createStream = File.Create(@"wiki.json");
            //var options1 = new JsonSerializerOptions
            //{
            //    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            //    WriteIndented = true
            //};
            //JsonSerializer.Serialize(createStream, pages.DistinctBy(i => i.Title), options1);
        }
    }
}