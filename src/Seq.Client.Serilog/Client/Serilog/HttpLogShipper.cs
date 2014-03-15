using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using Serilog.Debugging;

namespace Seq.Client.Serilog
{
    class HttpLogShipper
    {
        readonly string _apiKey;
        readonly int _batchPostingLimit;
        readonly Thread _worker;
        readonly AutoResetEvent _written;
        volatile bool _stopping, _stopped;
        readonly string _bookmarkFilename;
        readonly string _logFolder;
        readonly HttpClient _httpClient;
        readonly string _candidateSearchPath;

        const string ApiKeyHeaderName = "X-Seq-ApiKey";

        // The Serilog-style wait-for-stragglers algorithm hasn't been implemented here yet.
        // ReSharper disable once NotAccessedField.Local
        readonly TimeSpan _period;

        const string BulkUploadResource = "/api/events/raw";

        public HttpLogShipper(string serverUrl, string bufferBaseFilename, string apiKey, int batchPostingLimit, TimeSpan period)
        {
            _apiKey = apiKey;
            _batchPostingLimit = batchPostingLimit;
            _period = period;
            _httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };
            _bookmarkFilename = Path.GetFullPath(bufferBaseFilename + ".bookmark");
            _logFolder = Path.GetDirectoryName(_bookmarkFilename);
            _candidateSearchPath = Path.GetFileName(bufferBaseFilename) + "*.json";
            _written = new AutoResetEvent(false);
            _worker = new Thread(Run);
            _worker.Start();
        }

        public void Dispose()
        {
            if (_stopped)
                return;

            _stopping = true;
            _written.Set();
            try
            {
                if (!_worker.Join(TimeSpan.FromSeconds(10)))
                    _worker.Abort();
            }
            catch (ThreadStateException tsx)
            {
                SelfLog.WriteLine("Error while stopping HttpLogShipper: {0}", tsx);
            }
            _httpClient.Dispose();
        }

        public void EventWritten()
        {
            _written.Set();
        }

        void Run()
        {
            while (!_stopping)
            {
                try
                {
                    using (var bookmark = File.Open(_bookmarkFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                    {
                        long nextLineBeginsAtOffset;
                        string currentFile;

                        TryReadBookmark(bookmark, out nextLineBeginsAtOffset, out currentFile);

                        var fileSet = GetFileSet();

                        if (currentFile == null || !File.Exists(currentFile))
                        {
                            nextLineBeginsAtOffset = 0;
                            currentFile = fileSet.FirstOrDefault();
                        }

                        if (currentFile != null)
                        {
                            var payload = new StringWriter();
                            payload.Write("{\"events\":[");
                            var count = 0;
                            var delimStart = "";

                            using (var current = File.Open(currentFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                current.Position = nextLineBeginsAtOffset;

                                string nextLine;
                                while(count < _batchPostingLimit &&
                                    TryReadLine(current, ref nextLineBeginsAtOffset, out nextLine))
                                {
                                    ++count;
                                    payload.Write(delimStart);
                                    payload.Write(nextLine);
                                    delimStart = ",";
                                }

                                payload.Write("]}");
                            }
                            
                            if (count > 0)
                            {
                                var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
                                if (!string.IsNullOrWhiteSpace(_apiKey))
                                    content.Headers.Add(ApiKeyHeaderName, _apiKey);

                                var result = _httpClient.PostAsync(BulkUploadResource, content).Result;
                                if (result.IsSuccessStatusCode)
                                {
                                    WriteBookmark(bookmark, nextLineBeginsAtOffset, currentFile);
                                }
                                else
                                {
                                    SelfLog.WriteLine("Received failed HTTP shipping result {0}: {1}", result.StatusCode, result.Content.ReadAsStringAsync().Result);
                                }                                
                            }
                            else
                            {
                                if (fileSet.Length == 2 && fileSet.First() == currentFile)
                                {
                                    WriteBookmark(bookmark, 0, fileSet[1]);
                                }

                                if (fileSet.Length > 2)
                                {
                                    File.Delete(fileSet[0]);
                                }
                            }
                        }
                    }

                    _written.WaitOne(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("Error shipping logs, pausing for 10s: {0}", ex);
                    for (var i = 0; i < 1000 && !_stopping; i++)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(10));
                    }
                }
            }

            _stopped = true;
        }

        static void WriteBookmark(FileStream bookmark, long nextLineBeginsAtOffset, string currentFile)
        {
            using (var writer = new StreamWriter(bookmark))
            {
                writer.WriteLine("{0}:::{1}", nextLineBeginsAtOffset, currentFile);
            }
        }

        // The weakest link in this scheme, currently.
        // More effort's required - we don't want simple whitespace in this file to
        // cause us to get the offset wrong (and thus output invalid JSON)
        static bool TryReadLine(Stream current, ref long nextStart, out string nextLine)
        {
            var includesBom = nextStart == 0;

            if (current.Length <= nextStart)
            {
                nextLine = null;
                return false;
            }

            current.Position = nextStart;

            // Allocates a buffer each time; some major perf improvements are possible here;
            using (var reader = new StreamReader(current, Encoding.UTF8, false, 128, true))
            {
                nextLine = reader.ReadLine();
            }

            if (nextLine == null)
                return false;

            nextStart += Encoding.UTF8.GetByteCount(nextLine) + Encoding.UTF8.GetByteCount(Environment.NewLine);
            if (includesBom)
                nextStart += 3;

            return true;
        }

        static void TryReadBookmark(Stream bookmark, out long nextLineBeginsAtOffset, out string currentFile)
        {
            nextLineBeginsAtOffset = 0;
            currentFile = null;

            if (bookmark.Length != 0)
            {
                string current;
                using (var reader = new StreamReader(bookmark, Encoding.UTF8, false, 128, true))
                {
                    current = reader.ReadLine();
                }

                if (current != null)
                {
                    bookmark.Position = 0;
                    var parts = current.Split(new[] { ":::" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        nextLineBeginsAtOffset = long.Parse(parts[0]);
                        currentFile = parts[1];
                    }
                }
                
            }
        }

        string[] GetFileSet()
        {
            return Directory.GetFiles(_logFolder, _candidateSearchPath)
                .OrderBy(n => n)
                .ToArray();
        }
    }
}