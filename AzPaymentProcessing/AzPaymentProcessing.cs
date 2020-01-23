
/// <summary>
/// -------------------------------------------------------------------------------------------------------------- 
/// *** For DEMO PURPOSES ONLY ***
/// -------------------------------------------------------------------------------------------------------------- 
/// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,  
/// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED  
/// WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE. 
/// 
///  Programmed by : Denis Rougeau 
///  Date          : January, 2020 
/// -------------------------------------------------------------------------------------------------------------- 
/// </summary>
/// 
/// Sample documentation: https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-create-first-csharp

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace AzPaymentProcessing
{
    [StorageAccount("PPAPIStorage")]
    public static class AzPaymentProcessing
    {

        // ***************************************************
        //  Get Secret from Azure Key Vault
        //  Reference:
        //    https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references
        // ***************************************************
        private static string AKVPPAPISecret = System.Environment.GetEnvironmentVariable("PPAPISecret");

        //// Setup blob Storage info 
        //// NOTE: this may be replaced by an Public FTP Endpoint
        //// Sample here: https://stackoverflow.com/questions/24236004/how-to-read-a-csv-file-from-ftp-using-c-sharp 

        [FunctionName("AzFunctionBlobTrigger")]
        public static async void Run(
            [BlobTrigger("%InputFolder%/{filename}")] Stream PayFileBlobIn,
            string filename,
            ILogger log,
            [DurableClient] IDurableOrchestrationClient starter)
        {

            // Open the file and stream the content in a text variable
            string text;
            byte[] bytes = new byte[PayFileBlobIn.Length];
            PayFileBlobIn.Read(bytes, 0, (int)PayFileBlobIn.Length);

            text = System.Text.Encoding.UTF8.GetString(bytes.ToArray());
            string encodedText = Convert.ToBase64String(bytes);

            // DEBUG ONLY 
            // log.LogInformation($"AKV Trigger: {AKVPPAPISecret}");

            // Call the Durable function orchestrator to process the request content.
            string instanceId = await starter.StartNewAsync("AzFunctionPayProcess", "", text);

            log.LogInformation($"PayProcess Blob Trigger a function Processed blob\n Name: {filename} \n Size: {PayFileBlobIn.Length} Bytes");

        }

        [FunctionName("AzFunctionPayProcess")]
        public static async Task<Object> RunOrchestration(
            IBinder binder, 
            ILogger log,
            [OrchestrationTrigger] IDurableOrchestrationContext AzFunctionPayProcesscontext)
        {
            var outputs = new List<string>();

            // Read the data passed from the trigger.
            string filecontent = AzFunctionPayProcesscontext.GetInput<string>();
            string fcontent = filecontent.Replace("\n", "");  //Remove Line Feed from CSV file
            string[] lines = fcontent.Split("\r");

            // Fan Out - Calling multiple instances in parallel processing a transaction line each
            var parallelTasks = new List<Task<string>>();
            int i = 0;
            foreach (string line in lines)
            {
                if(line.Length > 1)
                {
                    Task<string> tasks = AzFunctionPayProcesscontext.CallActivityAsync<string>("AzFunctionPayProcess_ExternalAPICall", line);
                    parallelTasks.Add(tasks);
                }
            }

            // Fan In: Wait for all Activity Functions to complete execution
            await Task.WhenAll(parallelTasks);

            // Send the list of outputs string to save them to Blob Storage.
            var outputsInst = parallelTasks.Select(x => x.Id).ToList();
            var outputslist = parallelTasks.Select(x => x.Result).ToList();

            // SaveProcessedPay: 
            //     Using the Storage Account Connection from [StorageAccount("...")] above
            //     binder:  Using binding definition in Host.json file or local.host.json (Local testing)
            string transaction_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
            using (var writer = binder.Bind<TextWriter>(new BlobAttribute($"%OutputFolder%/{transaction_dt}-Output.csv", FileAccess.Write)))
            {
                foreach (string outputline in outputslist)
                {
                    if (outputline.Trim().Length > 1)
                        writer.WriteLine(outputline);
                }
            };

            log.LogInformation($"PayProcess Durable function saved output blob\n Name: {transaction_dt}-Output.csv");

            return outputsInst;
        }

        [FunctionName("AzFunctionPayProcess_ExternalAPICall")]
        public static async Task<string> AzFunctionPayProcess_ExternalAPICall([ActivityTrigger] string transaction, ILogger log)
        {

            // Sample Value returned to the client
            string results = string.Empty;
            if (transaction.Trim().Length > 1)
            {
                // Split the transaction lines into columns (CSV value)
                string[] columns = transaction.Split(',');
                string transaction_dt = DateTime.Now.ToString("yyyyMMddHHmmss");

                // Sample Results including AKV Secret, First Column and a Transaction DateTime
                results = $"{AKVPPAPISecret},Payment Succeeded for {columns[0]}, {transaction_dt}, {transaction}";
            }

            //// SAMPLE EXTERNAL API CALL
            //// Format the API request with the coordinates and API key
            //var apiRequest =
            //    $"https://api.weather.com/v1/geocode/{c.Latitude}/{c.Longitude}/forecast/fifteenminute.json?language=en-US&units=e&apiKey={ApiKey}";

            //// Make the forecast request and read the response
            //var response = await Client.GetAsync(apiRequest);
            //var forecast = await response.Content.ReadAsStringAsync();
            //log.LogInformation(forecast);

            // DEBUG ONLY
            //log.LogInformation($"AKV PayProcess: {AKVPPAPISecret}");

            return results;
        }
    }
}