using System;
using System.IO;
using System.Net.Http;
using NUnit.Framework;
using HotReloadServer;

namespace HotReloadServerTests
{
    public class HttpFileServerTests
    {
        private HttpFileServer fileServer;
        private HttpClient client;
        private string tempDirPath;

        [SetUp]
        public void Setup()
        {
            this.tempDirPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(this.tempDirPath);

            var server = new HttpFileServer("localhost", 11111, this.tempDirPath);
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}