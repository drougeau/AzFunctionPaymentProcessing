/// <summary>
/// -------------------------------------------------------------------------------------------------------------- 
/// *** For DEMO PURPOSES ONLY ***
/// -------------------------------------------------------------------------------------------------------------- 
/// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,  
/// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED  
/// WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE. 
/// 
///  Programmed by : Denis Rougeau 
///  Date          : March, 2019 
/// -------------------------------------------------------------------------------------------------------------- 
/// </summary>
/// 
/// Sample documentation: https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-create-first-csharp

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Extensions.Logging;

namespace AzFunctionPayProcess
{

    public static class AzFunctionPayProcess
    {

        // ***************************************************
        //  Get Secret from Azure Key Vault
        //  Reference:
        //    New key vault and function capabilities - by Jeff Hollan
        //    https://medium.com/statuscode/getting-key-vault-secrets-in-azure-functions-37620fd20a0b
        // ***************************************************
        private static string AKVPPAPISecret = System.Environment.GetEnvironmentVariable("PPAPISecret");


        // Define Blob storage settings as Priv Constant
        private const string account = "<STORAGE_ACCOUNT_NAME";
        private const string key = "<STORAGE_KEY>";
        private const string connectionString = "DefaultEndpointsProtocol=https;AccountName=" + account + ";AccountKey=" + key + ";EndpointSuffix=core.windows.net";
        private const string inputcontainername = "inputfiles";
        private const string outputcontainername = "outputfiles";
 
        [FunctionName("AzFunctionPayProcess")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext AzFunctionPayProcesscontext)
        {
            // Read the data passed from the trigger.
            string filecontent = AzFunctionPayProcesscontext.GetInput<string>();
            string fcontent = filecontent.Replace("\n", "");  //Remove Line Feed from CSV file
            string[] lines = fcontent.Split("\r");

            // Fan Out - Calling multiple instances in parallel processing a transaction line each
            var tasks = new Task<string>[lines.Count()];
            int i = 0;
            foreach (string line in lines)
            {
                tasks[i] = AzFunctionPayProcesscontext.CallActivityAsync<string>("AzFunctionPayProcess_ExternalCall", line);
                i++;
            }

            // Fan In: Wait for all Activity Functions to complete execution
            await Task.WhenAll(tasks);

            // Send the list of outputs string to save them to Blob Storage.
            var outputslist = tasks.Select(x => x.Result).ToList();
            await AzFunctionPayProcesscontext.CallActivityAsync("SaveProcessedPay", outputslist);

            return outputslist;
        }

        [FunctionName("AzFunctionPayProcess_ExternalCall")]
        public static async Task<string> CallPayProviderAPI([ActivityTrigger] string transaction, ILogger log)
        {

            // Sample Value returned to the client
            string results = string.Empty;
            if (transaction.Trim().Length > 1)
            {
                // Split the transaction lines into columns (CSV value)
                string[] columns = transaction.Split(',');
                string transaction_dt = DateTime.Now.ToString("yyyyMMddHHmmss");

                // Sample Results including AKV Secret, First Column and a Transaction DateTime
                results = $"{AKVPPAPISecret}, Payment Succeeded for {columns[0]}, {transaction_dt}, {transaction}";
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

        [FunctionName("SaveProcessedPay")]
        public static async Task SaveProcessedPay([ActivityTrigger] DurableActivityContext context)
        {
            // retrieves a tuple from the Orchestrator function
            var parameters = context.GetInput<List<string>>();

            // Azure Blob storage settings + Output file name (Using DateTime)
            CloudStorageAccount storageAccount;
            CloudBlobClient blobClient;
            CloudBlobContainer outputcontainer;
            CloudBlockBlob blockBlobReference;
            string transaction_dt = DateTime.Now.ToString("yyyyMMddHHmmss");
            string FileName = $"{transaction_dt}-Output.csv";

            storageAccount = CloudStorageAccount.Parse(connectionString);
            blobClient = storageAccount.CreateCloudBlobClient();
            outputcontainer = blobClient.GetContainerReference(outputcontainername);
            blockBlobReference = outputcontainer.GetBlockBlobReference(FileName);

            // Save ALL results to a single Blob
            string resulttosave = string.Empty;
            foreach (string parameter in parameters)
            {
                if (parameter.Trim().Length > 1)
                    resulttosave = resulttosave + parameter + "\r\n";
            }
            await blockBlobReference.UploadTextAsync(resulttosave);
        }

        [FunctionName("BlobTrigger")]
        public static async void Run([BlobTrigger("inputfiles/{filename}", Connection = "PPAPIStorage")]Stream PayFileBlob,
            string filename, ILogger log, [OrchestrationClient] DurableOrchestrationClient starter)
        {

            // Open the file and stream the content in a text variable
            string text;
            byte[] bytes = new byte[PayFileBlob.Length];
            PayFileBlob.Read(bytes, 0, (int)PayFileBlob.Length);
            text = System.Text.Encoding.UTF8.GetString(bytes.ToArray());

            // DEBUG ONLY
            //log.LogInformation($"AKV Trigger: {AKVPPAPISecret}");

            // Call the Durable function orchestrator to process the request content.
            string instanceId = await starter.StartNewAsync("AzFunctionPayProcess", text);

            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{filename} \n Size: {PayFileBlob.Length} Bytes");

        }

        ////  Sampe using HTTP Trigger
        //[FunctionName("AzFunctionPayProcess_HttpStart")]
        //public static async Task<HttpResponseMessage> HttpStart(
        //    [HttpTrigger(AuthorizationLevel.Function, "post")]HttpRequestMessage req,
        //    [OrchestrationClient]DurableOrchestrationClient starter,
        //    ILogger log)
        //{

        //    // Azure Blob configuration settings
        //    CloudStorageAccount storageAccount;
        //    CloudBlobClient blobClient;
        //    CloudBlobContainer inputcontainer;
        //    //CloudBlobContainer outputcontainer;
        //    CloudBlockBlob blockBlobReference;
        //    string FileName = "demo1comma.csv";

        //    storageAccount = CloudStorageAccount.Parse(connectionString);
        //    blobClient = storageAccount.CreateCloudBlobClient();
        //    inputcontainer = blobClient.GetContainerReference(inputcontainername);
        //    //outputcontainer = blobClient.GetContainerReference(outputcontainername);
        //    blockBlobReference = inputcontainer.GetBlockBlobReference(FileName);

        //    // Open the file and stream the content in a text variable
        //    string text;
        //    using (var memoryStream = new MemoryStream())
        //    {
        //        // Downloads blob's content to a stream
        //        await blockBlobReference.DownloadToStreamAsync(memoryStream);

        //        // Convert the byte arrays to a string
        //        text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        //    }

        //    // Call the Durable function orchestrator to process the request content.
        //    string instanceId = await starter.StartNewAsync("AzFunctionPayProcess", text);

        //    log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

        //    return starter.CreateCheckStatusResponse(req, instanceId);
        //}

    }
}