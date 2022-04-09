// See https://aka.ms/new-console-template for more information
using edenorte_scrap.Models;
using HtmlAgilityPack;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Web;

class Program
{
    const string baseUrl = "https://ofv.edenorte.com.do";
    const string loginUrl = "https://ofv.edenorte.com.do/user/login";
    const string consumptionUrl = "https://ofv.edenorte.com.do/teleconsumo";
    static readonly HttpClient client = new();

    static async Task Main(string[] args)
    {
        var productValue = new ProductInfoHeaderValue("ScraperBot", "1.0");
        var commentValue = new ProductInfoHeaderValue("(+http://www.example.com/ScraperBot.html)");
 
        client.DefaultRequestHeaders.UserAgent.Add(productValue);
        client.DefaultRequestHeaders.UserAgent.Add(commentValue);


        var email = Environment.GetEnvironmentVariable("email");
        var password = Environment.GetEnvironmentVariable("password");

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            throw new Exception("Faltan datos");
        }


        try
        {
            var tokenHeaderMap = await CurrentToken();
            var headers = await Login(email, password, tokenHeaderMap);
            var contracts = await GetContracts(headers);

        }
        catch (Exception)
        {

            throw;
        }


    }

    private static async Task<List<Contract>> GetContracts(HttpResponseHeaders headers)
    {

        var httpRequestMessage = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(consumptionUrl),
        };

        foreach (var header in headers.AsEnumerable())
        {

            if (header.Key.Contains("Set-Cookie"))
            {
                httpRequestMessage.Headers.Add(header.Key, header.Value);
            }
        }


        var response = await client.SendAsync(httpRequestMessage);
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(await response.Content.ReadAsStringAsync());
        var htmlBody = htmlDoc.DocumentNode.SelectNodes("//tr")
            .ToList();

        var contracts = new List<Contract>();
        foreach (var row in htmlBody)
        {

           var number = row.FirstChild.SelectSingleNode("//th").FirstChild.InnerHtml;
            var link = row.SelectSingleNode("//td/a").Attributes["href"].Value;
            
            contracts.Add(new Contract
            {
                Number = Convert.ToInt32(number),
                DetailUrl = link,
            });
           
        }

        return contracts;
    }

    private static async Task<HttpResponseHeaders> Login(string email, string password, (string, HttpResponseHeaders) tokenHeaderMap)
    {
        var requestContent = new FormUrlEncodedContent(new[]
                    {
                new KeyValuePair<string, string>("login-form[login]", email),
                new KeyValuePair<string, string>("login-form[password]", password),
                new KeyValuePair<string, string>("_csrf", tokenHeaderMap.Item1),
            });

        var httpRequestMessage = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(loginUrl),
            Content = requestContent
        };

        foreach (var header in tokenHeaderMap.Item2.AsEnumerable())
        {
            if (header.Key.Contains("Set-Cookie"))
            {
                httpRequestMessage.Headers.Add(header.Key, header.Value);
            }
        }


        var loginResponse = await client.SendAsync(httpRequestMessage);
        var headers = loginResponse.Headers;

        return headers;
    }

    private static async Task<(string, HttpResponseHeaders)> CurrentToken()
    {
        var response = await client.GetAsync(baseUrl);
        // From Doc
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(await response.Content.ReadAsStringAsync());
        var htmlBody = htmlDoc.DocumentNode.SelectNodes("//input")
            .Where(x => x.Attributes["type"].Value == "hidden")
            .First();



        return (htmlBody.Attributes["value"].Value, response.Headers);
    }
}