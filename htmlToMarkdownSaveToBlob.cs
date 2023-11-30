using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HtmlAgilityPack;
using Markdig;
using Azure.Storage.Blobs;

namespace Neptune.HtmlToMd
{
    public static class HtmlToMdTest
    {
        [FunctionName("HtmlToMdTest")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string url = req.Query["url"];
            string filePath = req.Query["filePath"];

            // Download the HTML content from the URL
            var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(url);

            // Extract the inner HTML content
            var htmlNode = doc.DocumentNode.SelectSingleNode("//body");
            var htmlContent = htmlNode.InnerHtml;

            // Convert HTML to Markdown
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var markdownContent = Markdown.ToMarkdown(htmlContent, pipeline);

            // Save markdown content to a blob
            BlobServiceClient blobServiceClient = new BlobServiceClient("YourConnectionString");
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("YourContainerName");
            BlobClient blobClient = containerClient.GetBlobClient(filePath);

            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(markdownContent)))
            {
                await blobClient.UploadAsync(stream, true);
            }

            return new OkObjectResult(blobClient.Uri.AbsoluteUri);
        }
    }
}