using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace webapiTester
{
    class Program
    {
        static void Main(string[] args)
        {
            StartMockServer();

            var tester = new WebApiTester();

            tester.WorkingFolder = @"C:\workspace\tests\webapiTest\service1";

            tester.Run();
        }

        private static void StartMockServer()
        {
            var mockSrv = FluentMockServer.Start(new FluentMockServerSettings
            {
                Urls = new[] { "http://+:5001" },
                StartAdminInterface = true
            });

            mockSrv.Given(Request.Create()
                .WithPath("/service1/api/users")
                .UsingGet()
                .WithBody(new { age = 30 }))
                .RespondWith(Response.Create()
                            .WithStatusCode(HttpStatusCode.OK)
                            .WithHeader("Content-Type", "application/json")
                            .WithBodyAsJson(new
                            {
                                name = "joe doe",
                                age = 30,
                                address = "1 rue de Paris"
                            }));
        }
    }

    class ServiceConfig
    {
        public string Url { get; set; }

    }

    class InputPayload
    {
        public HttpMethod Method { get; set; }
        public string Content { get; set; }
    }

    public class OutputPaylod
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Content { get; set; }
    }

    public class TestExecutionItem
    {
        public string InputFile { get; set; }
        public string OutputFile { get; set; }
        public bool Result { get; set; }
        public string ResultDetails { get; set; }
    }

    public class WebApiTester
    {
        public string WorkingFolder { get; set; }

        ServiceConfig Config;
        private char[] Separators => new char[] { '.', '_', '-' };

        public WebApiTester()
        {
        }

        public void Run()
        {
            LoadConfig();

            var inputPath = Path.Combine(WorkingFolder, "input");
            var outputPath = Path.Combine(WorkingFolder, "output");

            var inputFiles = Directory.GetFiles(inputPath);
            var outputFiles = Directory.GetFiles(outputPath);

            var testItems = MatchFiles(inputFiles, outputFiles);

            foreach (var testItem in testItems)
            {
                var actualOutput = ExecuteInput(testItem.InputFile).Result;
                var expectedOutput = LoadJsonFile<OutputPaylod>(testItem.OutputFile);
                var result = Compare(expectedOutput, actualOutput);

                testItem.Result = string.IsNullOrEmpty(result);
                testItem.ResultDetails = result;

                LogResult(testItem);
            }  
        }

        private void LogResult(TestExecutionItem testItem)
        {
            //throw new NotImplementedException();
        }

        private async Task<OutputPaylod> ExecuteInput(string inputFile)
        {
            var srvInput = ReadInputPayload(inputFile);

            return await CallService(Config.Url, srvInput.Method, srvInput.Content);
        }

        private async Task<OutputPaylod> CallService(string url, HttpMethod method, string payload)
        {
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(method, url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var result = await httpClient.SendAsync(request);

            var output = new OutputPaylod
            {
                StatusCode = result.StatusCode,
                Content = await result.Content.ReadAsStringAsync()
            };

            return output;
        }

        private InputPayload ReadInputPayload(string inputFile)
        {
            return LoadJsonFile<InputPayload>(inputFile);
        }

        protected virtual string Compare(OutputPaylod expected, OutputPaylod actual)
        {
            return "";
        }

        public List<TestExecutionItem> MatchFiles(string[] inputFiles, string[] outputFiles)
        {
            var inputPairs = inputFiles
                .Select(i =>
                {
                    var fileName = Path.GetFileName(i);
                    var pair = fileName.Split(Separators);
                    return new { key = Convert.ToInt32(pair[0]), value = i };
                }).ToList();

            var outputPairs = outputFiles
                .Select(i =>
                {
                    var fileName = Path.GetFileName(i);
                    var pair = fileName.Split(Separators);
                    return new { key = Convert.ToInt32(pair[0]), value = i };
                }).ToList();

            var result = new List<TestExecutionItem>();
            foreach (var i in inputPairs)
            {
                var outputMatch = outputPairs.FirstOrDefault(o => o.key == i.key);
                if (outputMatch == null)
                    continue;

                result.Add(new TestExecutionItem
                {
                    InputFile = i.value,
                    OutputFile = outputMatch.value
                });
            }

            return result;
        }

        private void LoadConfig()
        {
            var config = File.ReadAllText(Path.Combine(WorkingFolder, "config.json"));

            Config = LoadJsonFile<ServiceConfig>(config);
        }

        private T LoadJsonFile<T>(string filePath)
        {
            var jsonContent = File.ReadAllText(filePath);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonContent);
        }
    }
}
