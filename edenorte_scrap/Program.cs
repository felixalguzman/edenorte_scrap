// See https://aka.ms/new-console-template for more information

using CommandLine;
using edenorte_scrap.Extensions;
using edenorte_scrap.Models;
using HtmlAgilityPack;
using System.Globalization;
using System.Net.Http.Headers;

namespace edenorte_scrap;

internal static class Program
{
    private const string BaseUrl = "https://ofv.edenorte.com.do";
    private const string LoginUrl = "https://ofv.edenorte.com.do/user/login";
    private const string ConsumptionUrl = "https://ofv.edenorte.com.do/teleconsumo";
    private static readonly HttpClient Client = new();
    private static readonly DateTimeFormatInfo dtfi = CultureInfo.GetCultureInfo("es-US").DateTimeFormat;

    private static Task Main(string[] args)
    {
        var result = Parser.Default.ParseArguments<Options>(args)
            .WithParsed(async o =>
            {
                await RunWithOptionsAsync(o);
            })
            .WithNotParsed(err =>
            {
                Console.WriteLine(err);
            });
        return Task.CompletedTask;
    }

    static async Task RunWithOptionsAsync(Options options)
    {
        var tokenHeaderMap = await CurrentToken();
        var headers = await Login(options.Email, options.Password, tokenHeaderMap);
        var contracts = await GetContracts(headers);
        var instance = Supabase.Client.Instance;

        await Supabase.Client.InitializeAsync(options.SupabaseUrl, options.SupabaseKey);

        foreach (var contract in contracts)
        {
            var existing = await instance.From<Contract>().Filter("number", Postgrest.Constants.Operator.Equals, contract.Number)
                .Single();
            if (existing == null)
            {
                var response = await instance.From<Contract>().Insert(contract);
                var newContract = response.Models.FirstOrDefault();

                if (newContract != null)
                {
                    contract.Id = newContract.Id;
                }

            }
            else
            {
                contract.Id = existing.Id;

            }
        }
        var consumptions = await GetContractsConsumption(headers, contracts);

        foreach (var consumption in consumptions)
        {
            var beginning = consumption.DateCreated.Date;
            var end = beginning.AddHours(23);

            var existing = await instance.From<Consumption>().Filter("contract_id", Postgrest.Constants.Operator.Equals, consumption.ContractId)
                .And(new List<Postgrest.QueryFilter>
                {
                    new Postgrest.QueryFilter("created_at", Postgrest.Constants.Operator.GreaterThanOrEqual, beginning),
                    new Postgrest.QueryFilter("created_at", Postgrest.Constants.Operator.LessThanOrEqual, end),

                })
                .Single();

            if (existing == null)
            {
                await instance.From<Consumption>().Insert(consumption);

            }

        }
    }

    static Task HandleParseError(IEnumerable<Error> errors)
    {

        Environment.Exit(1);
        return Task.CompletedTask;
    }

    private static async Task<List<Consumption>> GetContractsConsumption(HttpResponseHeaders headers,
        List<Contract> contracts)
    {
        var consumptions = new List<Consumption>();
        foreach (var contract in contracts)
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(contract.DetailUrl)
            };


            foreach (var (key, value) in headers.AsEnumerable())
            {
                if (key.Contains("Set-Cookie"))
                {
                    httpRequestMessage.Headers.Add(key, value);
                }
            }


            var response = await Client.SendAsync(httpRequestMessage);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(await response.Content.ReadAsStringAsync());
            var tableSpansExtension = new TableSpanExtension();

            var htmlBody = htmlDoc.DocumentNode.SelectNodes("//table")
                .ToList();

            var content = await response.Content.ReadAsStringAsync();

            if (content.Contains("No hay datos de teleconsumo disponibles por el momento para este contrato."))
                continue;

            var consumption = new Consumption
            {
                ContractId = contract.Id,
                DateCreated = DateTime.UtcNow,

            };


            foreach (var arr in htmlBody.Select(node => TableSpanExtension.ToArray(node)))
            {
                for (var y = 0; y < arr.GetLength(1); y++)
                {
                    var title = arr[0, y]?.Trim();
                    var value = arr[1, y]?.Trim();

                    if (value != null && !value.Contains("No"))
                    {
                        switch (title)
                        {
                            case "Activa Entregada(Kwh)":
                                consumption.ReadingDelivered = Convert.ToDouble(value);
                                break;
                            case "Medidor":
                                break;
                            case "Medidio actual":
                                consumption.CurrentMeasure = Convert.ToDouble(value);

                                break;
                            case "Bidireccional":
                                break;
                            case "Tarifa":
                                break;
                            case "Fecha cambio medidor":
                                break;
                            case "M&uacute;ltiplo actual":
                                break;
                            case "Fecha &uacute;ltima Factura":
                                consumption.LastInvoice = DateTime.Parse(value, dtfi);
                                break;

                            case "Datos disponibles hasta el d&iacute;a":
                                consumption.DataAvailableUpTo = DateTime.Parse(value, dtfi);
                                break;

                            case "Consumo hasta la fecha (kWh)":
                                consumption.ConsumptionTillNow = Convert.ToDouble(value);

                                break;
                            case "Proyecci&oacute;n de consumo (kWh)":
                                consumption.ProjectedConsumption = Convert.ToDouble(value);

                                break;
                            case "D&iacute;a de mayor consumo":
                                consumption.MaxConsumptionDate = DateTime.Parse(value, dtfi);

                                break;
                            case "Valor de consumo (kWh)":
                                consumption.ConsumptionValue = Convert.ToDouble(value);
                                break;
                        }
                    }


                }
            }

            consumptions.Add(consumption);

        }

        return consumptions;
    }

    private static async Task<List<Contract>> GetContracts(HttpResponseHeaders headers)
    {
        var httpRequestMessage = new HttpRequestMessage()
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(ConsumptionUrl),
        };

        foreach (var (key, value) in headers.AsEnumerable())
        {
            if (key.Contains("Set-Cookie"))
            {
                httpRequestMessage.Headers.Add(key, value);
            }
        }


        var response = await Client.SendAsync(httpRequestMessage);
        var htmlDoc = new HtmlDocument();
        string html = await response.Content.ReadAsStringAsync();
        htmlDoc.LoadHtml(html);
        var htmlBody = htmlDoc.DocumentNode.SelectNodes("//tr")
            .ToList();

        var contracts = new List<Contract>();
        foreach (var row in htmlBody)
        {
            var number = row.FirstChild.SelectSingleNode("//th").FirstChild.InnerHtml;
            var link = row.SelectSingleNode("//td/a").Attributes["href"].Value;

            contracts.Add(new Contract
            {
                Number = number.Trim(),
                DetailUrl = link,
            });
        }

        return contracts;
    }

    private static async Task<HttpResponseHeaders> Login(string email, string password,
        (string, HttpResponseHeaders) tokenHeaderMap)
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
            RequestUri = new Uri(LoginUrl),
            Content = requestContent
        };

        foreach (var (key, value) in tokenHeaderMap.Item2.AsEnumerable())
        {
            if (key.Contains("Set-Cookie"))
            {
                httpRequestMessage.Headers.Add(key, value);
            }
        }


        var loginResponse = await Client.SendAsync(httpRequestMessage);
        var headers = loginResponse.Headers;

        return headers;
    }

    private static async Task<(string, HttpResponseHeaders)> CurrentToken()
    {
        var response = await Client.GetAsync(BaseUrl);
        // From Doc
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(await response.Content.ReadAsStringAsync());
        var htmlBody = htmlDoc.DocumentNode
            .SelectNodes("//input")
            .First(x => x.Attributes["type"].Value == "hidden");


        return (htmlBody.Attributes["value"].Value, response.Headers);
    }
}