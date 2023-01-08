using System.Net;

namespace VintedImagegrabber
{
    internal class HttpHelper
    {
        readonly HttpClient httpClient;

        public HttpHelper()
        {
            CookieContainer cookies = new();
            HttpClientHandler handler = new()
            {
                CookieContainer = cookies
            };
            httpClient = new HttpClient(handler);

            //httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.4896.162 Safari/537.36");
        }

        public async Task DownloadImageAsync(string directoryPath, string fileName, Uri uri, bool verbose)
        {
            // Get the file extension
            var uriWithoutQuery = uri.GetLeftPart(UriPartial.Path);
            var fileExtension = Path.GetExtension(uriWithoutQuery);

            // Create file path and ensure directory exists
            var path = Path.Combine(directoryPath, $"{fileName}{fileExtension}");
            Directory.CreateDirectory(directoryPath);

            if (File.Exists(path))
            {
                if (verbose)
                    Console.WriteLine($"Skipping {fileName}");
                return;
            }

            Console.WriteLine($"DL {fileName}");
            // Download the image and write to the file
            var imageBytes = await httpClient.GetByteArrayAsync(uri);

            await File.WriteAllBytesAsync(path, imageBytes);
        }

        public async Task<string> GetUrl(string url)
        {
            return await httpClient.GetStringAsync(url);
        }
    }
}
