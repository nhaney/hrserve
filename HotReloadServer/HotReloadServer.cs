using System;

namespace HotReloadServer
{
    /// <summary>
    /// HotReloadServer
    ///
    /// Serve a file directory as HTTP and hot reload it with a custom command when
    /// a file changes in the directory or a different one.
    /// 
    /// </summary>
    public class HotReloadServer: IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="address">The address to bind the HTTP Server to</param>
        /// <param name="port">The port to bind the HTTP Server to</param>
        /// <param name="command">The command to run when a file change is detected</param>
        /// <param name="watchDir">The directory to watch</param>
        /// <param name="serveDir">The directory that the HTTP Server serves</param>
        public HotReloadServer(string address, int port, string command, string sourceDir, string serveDir)
        {
        }

        public void Dispose()
        {

        }
    }
}
