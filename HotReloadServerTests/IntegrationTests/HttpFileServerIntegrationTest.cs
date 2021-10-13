using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;

using HotReloadServer;

namespace HotReloadServerTests
{
    /// <summary>
    /// Integration tests that test how the HttpFileServer works
    /// with real Http requests and file system operations.
    /// </summary>
    public class HttpFileServerIntegrationTest
    {
        private HttpFileServer _fileServer;
        private HttpClient _client;
        private string _tempDirPath;
        private string _serverUrl;

        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var address = "localhost";
            var port = 11111;
            this._tempDirPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(this._tempDirPath);

            this._fileServer = new HttpFileServer(address, port, this._tempDirPath);
            this._serverUrl = $"http://{address}:{port}";

            var token = this._tokenSource.Token;
            Task.Run(() => this._fileServer.Run(token));
            this._client = new HttpClient();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            this._fileServer.Stop();
            this._client.Dispose();
        }

        [TearDown]
        public void TearDown()
        {
            var dirInfo = new DirectoryInfo(this._tempDirPath);

            foreach (FileInfo file in dirInfo.GetFiles())
            {
                file.Delete(); 
            }
            foreach (DirectoryInfo dir in dirInfo.GetDirectories())
            {
                dir.Delete(true); 
            }
        }

        [Test]
        public async Task TestFetchFile()
        {
            string fileName = "test.txt";
            string filePath = Path.Combine(_tempDirPath, fileName);
            string fileContents = "Sample Text";
            using (var fw = File.OpenWrite(filePath))
            {
                await fw.WriteAsync(Encoding.UTF8.GetBytes(fileContents));
            }
            var requestUrl = $"{_serverUrl}/{fileName}";

            var response = await this._client.GetAsync(requestUrl);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(fileContents, await response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task TestFetchDefaultFile()
        {
            string fileName = "index.html";
            string filePath = Path.Combine(_tempDirPath, fileName);
            string fileContents = "<h1>Sample HTML Page</h1>";
            using (var fw = File.OpenWrite(filePath))
            {
                await fw.WriteAsync(Encoding.UTF8.GetBytes(fileContents));
            }
            var requestUrl = _serverUrl;

            var response = await this._client.GetAsync(requestUrl);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual(fileContents, await response.Content.ReadAsStringAsync());
        }

        [Test]
        public async Task TestFileNotFound()
        {
            var requestUrl = $"{_serverUrl}/filethatdoesntexist.txt";
            var response = await this._client.GetAsync(requestUrl);

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Test]
        public async Task TestInvalidMethod()
        {
            var response = await this._client.PostAsync(_serverUrl, null);
            Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            response = await this._client.PutAsync(_serverUrl, null);
            Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            response = await this._client.PatchAsync(_serverUrl, null);
            Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            response = await this._client.DeleteAsync(_serverUrl);
            Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        }
    }
}