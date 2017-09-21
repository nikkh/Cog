using System;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Web;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using Cog;
using System.Linq;
using Microsoft.Azure;
using NAudio.Wave;
using System.Threading;

namespace NicksCognitiveTest
{
    static class Program
    {
        static List<SentimentBatchEntry> batch = new List<SentimentBatchEntry>();

        static void Main()
        {
            Console.Title = "Cognitive Services Testing";

            LoadConfiguration();

           


            
            ConCol("*****************************************************", ConsoleColor.Yellow);
            ConCol("****          Cognitive Services Demo           *****", ConsoleColor.Yellow);
            ConCol("*****************************************************", ConsoleColor.Yellow);



            ConCol("Looking in file drop location {0}", ConsoleColor.White, Global.FileDropPath);

            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = Global.FileDropPath;
            /* Watch for changes in LastAccess and LastWrite times, and
            the renaming of files or directories. */
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
            | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch text files.
            watcher.Filter = "*.*";

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.Created += new FileSystemEventHandler(OnChanged);
            watcher.Deleted += new FileSystemEventHandler(OnChanged);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Begin watching.
            watcher.EnableRaisingEvents = true;

            // Wait for the user to quit the program.
            Console.WriteLine("Press \'q\' to quit the sample.");
            while (Console.Read() != 'q') ;
            //Single x = 0;
            //NewMain();
            //return;



            //var negPath = @"C:\Users\nhill\Desktop\sentiment\review_polarity\txt_sentoken\neg";
            //var posPath = @"C:\Users\nhill\Desktop\sentiment\review_polarity\txt_sentoken\pos";

            //Console.WriteLine("calculating sentiment for a batch of negative reviews");
            //x = CalculateSentiment(PrepareBatch(negPath, 25)).Result;
            //Console.WriteLine("score was {0}", x);
            //Console.WriteLine();

            //Console.WriteLine("calculating sentiment for a batch of positive reviews");
            //x = CalculateSentiment(PrepareBatch(posPath, 25)).Result;
            //Console.WriteLine("score was {0}", x);
            //Console.WriteLine();

            //Console.WriteLine("retrieving a batch of HMRC-related tweets");
            //x = CalculateSentiment(PrepareTwitterBatch("#HMRC")).Result;
            //Console.WriteLine("score was {0}", x);
            //Console.WriteLine();

            //Console.WriteLine("retrieving a batch of positive-related tweets");
            //x = CalculateSentiment(PrepareTwitterBatch("#positive")).Result;
            //Console.WriteLine("score was {0}", x);
            //Console.WriteLine();

            //Console.WriteLine("retrieving a batch of idiot-related tweets");
            //x = CalculateSentiment(PrepareTwitterBatch("#idiot")).Result;
            //Console.WriteLine("score was {0}", x);
            //Console.WriteLine();

            //var audioPath = @"C:\Users\nhill\Documents\Sound recordings";
            //Console.WriteLine("processing audio files");
            //x = CalculateSentiment(PrepareBatchFromAudio(audioPath, 15)).Result;
            //Console.WriteLine("score was {0}", x);
            //Console.WriteLine();


            //Console.WriteLine("Hit ENTER to exit...");
            //Console.ReadLine();
        }

        private static void ConCol(string message, ConsoleColor colour = ConsoleColor.Gray, string one=null, string two= null, string three= null)
        {
            
            Console.ForegroundColor = colour;
            if (message.Contains("{2}"))
            {
                Console.WriteLine(String.Format(message, one, two, three));
            }
            else
            {
                if (message.Contains("{1}"))
                {
                    Console.WriteLine(String.Format(message, one, two));
                }
                else
                {
                    if (message.Contains("{0}"))
                    {
                        Console.WriteLine(String.Format(message, one));
                    }
                    else
                    {
                        Console.WriteLine(message);
                    }
                }
            }
            Console.ResetColor();

        }

        private static void NewMain()
        {
           

           
        }

        private static void LoadConfiguration()
        {
            var fileDropPath = CloudConfigurationManager.GetSetting("FileDropPath");
            if (String.IsNullOrEmpty(fileDropPath)) throw new Exception("FileDropPath is not set in configuration");
            if (!Directory.Exists(fileDropPath)) throw new Exception(String.Format("FileDropPath - Directory {0} does not exist!", fileDropPath));
            Global.FileDropPath = fileDropPath;

            var textAnalyticsSubscriptionKey = CloudConfigurationManager.GetSetting("TextAnalyticsSubscriptionKey");
            if (String.IsNullOrEmpty(textAnalyticsSubscriptionKey)) throw new Exception("TextAnalyticsSubscriptionKey is not set in configuration");
            Global.TextAnalyticsSubscriptionKey = textAnalyticsSubscriptionKey;

            var bingSpeechApiSubscriptionKey = CloudConfigurationManager.GetSetting("BingSpeechApiSubscriptionKey");
            if (String.IsNullOrEmpty(bingSpeechApiSubscriptionKey)) throw new Exception("BingSpeechApiSubscriptionKey is not set in configuration");
            Global.BingSpeechApiSubscriptionKey = bingSpeechApiSubscriptionKey;

            var fileDropArchivePath = CloudConfigurationManager.GetSetting("FileDropArchivePath");
            if (String.IsNullOrEmpty(fileDropArchivePath)) throw new Exception("FileDropArchivePath is not set in configuration");
            if (!Directory.Exists(fileDropArchivePath)) throw new Exception(String.Format("FileDropArchivePath - Directory {0} does not exist!", fileDropPath));
            Global.FileDropArchivePath = fileDropArchivePath;

            var batchSizeString = CloudConfigurationManager.GetSetting("BatchSize");
            Int32.TryParse(batchSizeString, out int batchSize);
            if (batchSize == 0) batchSize = 10;
            Global.BatchSize = batchSize;

            var maxDocumentSizeInCharactersString = CloudConfigurationManager.GetSetting("MaxDocumentSizeInCharacters");
            Int32.TryParse(maxDocumentSizeInCharactersString, out int maxDocumentSizeInCharacters);
            if (maxDocumentSizeInCharacters == 0) maxDocumentSizeInCharacters = 5000;
            Global.MaxDocumentSizeInCharacters = maxDocumentSizeInCharacters;

            var NegativeString = CloudConfigurationManager.GetSetting("Sentiment_Negative");
            Single.TryParse(NegativeString, out Single negative);
            if (negative == 0) negative = (Single)0.4;
            Global.Sentiment_Negative = negative;

            var IndifferentString = CloudConfigurationManager.GetSetting("Sentiment_Indifferent");
            Single.TryParse(IndifferentString, out Single indifferent);
            if (indifferent == 0) indifferent = (Single)0.6;
            Global.Sentiment_Indifferent = indifferent;

        }

        private static bool ShouldProcessFile(string fileFullPath)
        {
            bool result = false;
            if (Path.GetExtension(fileFullPath) == ".m4a") result = true;
            if (Path.GetExtension(fileFullPath) == ".txt") result = true;
            
            return result;
        }

        // Define the event handlers.
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            if(e.ChangeType == WatcherChangeTypes.Created)
            {
                if (!ShouldProcessFile(e.FullPath))
                {
                    ConCol("Skipping file {0} due to its extension", ConsoleColor.DarkGray, e.Name);
                    return;
                }
                ProcessFile(e.FullPath);
            }
           
            
        }

        public static void ProcessFile(string fullFilePath)
        {
            if (Path.GetExtension(fullFilePath) == ".m4a") ProcessAudio(fullFilePath);
            FileInfo f = new FileInfo(fullFilePath);
            while (IsFileLocked(f))
            {
                Thread.Sleep(500);
            }
            if (Path.GetExtension(fullFilePath) == ".txt") ProcessText(fullFilePath);
        }

        static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private static void ProcessText(string fullFilePath)
        {
            ConCol("Processing text file: {0}", ConsoleColor.Gray, Path.GetFileName(fullFilePath));

            string textToProcess = File.ReadAllText(fullFilePath);
            if(textToProcess.Length > Global.MaxDocumentSizeInCharacters) textToProcess = textToProcess.Substring(0,Global.MaxDocumentSizeInCharacters);

            SentimentBatchEntry sbe = new SentimentBatchEntry(SentimentBatchEntryType.Audio, textToProcess, Path.GetFileName(fullFilePath));
            batch.Add(sbe);
            ProcessBatchIfEnoughEntries();

        }

        private static void ProcessAudio(string fullFilePath)
        {
            // Convert file to WAV
            ConCol("Processing audio file: {0}", ConsoleColor.Yellow, Path.GetFileName(fullFilePath));
            var wavFileName = Convertm4aWav(fullFilePath);
            ConCol("Converted to .wav format {0}", ConsoleColor.Yellow, Path.GetFileName(wavFileName));

            // Transcribe File
            string trancribedAudio = TranscribeAudio(wavFileName);
            var textFileName = String.Format(@"{0}\{1}{2}", Path.GetDirectoryName(fullFilePath), Path.GetFileNameWithoutExtension(fullFilePath), ".txt");
            // Save Transcribed File (as .txt) - will trigger normal file processing
            ConCol("Saving text transcription of audio file {0}", ConsoleColor.Yellow, Path.GetFileName(textFileName));
            File.WriteAllText(textFileName, trancribedAudio);

           
            


            // Move Processed file to archive
        }

        private static void ProcessBatchIfEnoughEntries()
        {
            
            
            ConCol("Batch contains {0} items", ConsoleColor.Gray, batch.Count.ToString());
            
            if (batch.Count < Global.BatchSize)
            {
                ConCol("Sentiment calculation deferred. There are {0} items in batch.  {1} items are needed to trigger calculation", ConsoleColor.Gray, batch.Count.ToString(), Global.BatchSize.ToString());
                return;
            }
            batch = CalculateSentiment(batch).Result;
            // Write results
            foreach (var item in batch)
            {
                ProcessResults(item);
                
            }
            ConCol("Archiving batch to {0}", ConsoleColor.Gray, Global.FileDropArchivePath);
            // move files to archive and empty batch
            foreach (var item in batch)
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item.FileName);
                var fileName = String.Format("{0}{1}", fileNameWithoutExtension, ".txt");
                ArchiveFile(fileName);
                fileName = String.Format("{0}{1}", fileNameWithoutExtension, ".m4a");
                ArchiveFile(fileName);
                fileName = String.Format("{0}{1}", fileNameWithoutExtension, ".wav");
                ArchiveFile(fileName);

            }
            batch.Clear();
            ConCol("Batch processed successfully and archived", ConsoleColor.Gray);
        }

        private static void ProcessResults(SentimentBatchEntry item)
        {
            ConsoleColor appropriateColour = ConsoleColor.Red;
            if (item.Sentiment == Sentiment.Neutral) appropriateColour = ConsoleColor.DarkYellow;
            if (item.Sentiment == Sentiment.Positive) appropriateColour = ConsoleColor.Green;
            if (item.Sentiment == Sentiment.Negative) appropriateColour = ConsoleColor.Cyan;
            ConCol("{0} was analysed. Sentiment Score was {1}. Sentiment is {2}", appropriateColour, item.FileName, item.SentimentScore.ToString(), item.Sentiment.ToString().ToUpper());
            if (item.Sentiment == Sentiment.Error)
            {
                if (item.Error == null) item.Error = "Error field unexpectedly null";
                ConCol(item.Error, appropriateColour);
            }
            else
            {
                string trimmedTextToAnalyse = item.TextToAnalyse;
                if (item.TextToAnalyse.Length > 100) trimmedTextToAnalyse = item.TextToAnalyse.Substring(0, 99);
                ConCol("Text: {0}", appropriateColour, trimmedTextToAnalyse);
            }

        }

        private static void ArchiveFile(string fileName)
        {
            
           
            string currentFile = Path.Combine(Global.FileDropPath,  fileName);
            string archiveFile = Path.Combine(Global.FileDropArchivePath, fileName);
            if (File.Exists(currentFile))
            {
                if (File.Exists(archiveFile))
                {
                    File.Delete(archiveFile);
                    File.Copy(currentFile, archiveFile);
                    File.Delete(currentFile);
                    ConCol("File {0} was replaced in archive", ConsoleColor.DarkGray, archiveFile);
                }
                else
                {
                    File.Copy(currentFile, archiveFile);
                    File.Delete(currentFile);
                    ConCol("File {0} was archived", ConsoleColor.DarkGray, currentFile);
                }
            }


        }

        private static string Convertm4aWav(string fullFilePath)
        {
            var wavFileName = String.Format(@"{0}\{1}{2}", Path.GetDirectoryName(fullFilePath), Path.GetFileNameWithoutExtension(fullFilePath), ".wav");
            // convert "back" to WAV
            // create media foundation reader to read the AAC encoded file
            using (MediaFoundationReader reader = new MediaFoundationReader(fullFilePath)) {
                // resample the file to PCM with same sample rate, channels and bits per sample
                using (ResamplerDmoStream resampledReader = new ResamplerDmoStream(reader,
                    new WaveFormat(reader.WaveFormat.SampleRate, reader.WaveFormat.BitsPerSample, reader.WaveFormat.Channels))) {
                    // create WAVe file



                    using (WaveFileWriter waveWriter = new WaveFileWriter(wavFileName, resampledReader.WaveFormat))
                    {
                        // copy samples
                        resampledReader.CopyTo(waveWriter);
                        waveWriter.Flush(); ;
                    }
                }
            }
            return wavFileName;

        }

        private static void OnRenamed(object source, RenamedEventArgs e)
        {
            // Specify what is done when a file is renamed.
            Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        }

        //private static List<SentimentBatchEntry> ProcessConsolidated(List<SentimentBatchEntry> consolidated)
        //{
        //    int i = 0;
        //    foreach (var item in consolidated)
        //    {
        //        i++;
        //        item.SentimentScore = i;
        //    }
        //    return consolidated;
        //}

        //private static List<SentimentBatchEntry> GetAudioBatch(string path, int batchSize)
        //{
        //    List<SentimentBatchEntry> batch = new List<SentimentBatchEntry>();
        //    int count = 0;
            
        //    foreach (string file in Directory.EnumerateFiles(path, "*.wav"))
        //    {

        //        if (count == batchSize) break;
                
        //        string transcription = TranscribeAudio(file);
        //        SentimentBatchEntry sbe = new SentimentBatchEntry(SentimentBatchEntryType.Audio, transcription, file);
        //        batch.Add(sbe);
        //        count++;
        //    }
        //    return batch; ;
        //}

        //private static List<SentimentBatchEntry> GetTwitterBatch(string hashTag, int batchSize )
        //{
        //    List<SentimentBatchEntry> batch = new List<SentimentBatchEntry>();
        //    // You need to set your own keys and screen name
        //    var oAuthConsumerKey = "BDcTJ3692JpJPeC43sSmlPfFy";
        //    var oAuthConsumerSecret = "yOPC22Ok5iTh9kljm3gBWIPOhcWhRhnfuqOT2YrpipsHYsXQ8w";
        //    var oAuthUrl = "https://api.twitter.com/oauth2/token";

        //    // Do the Authenticate
        //    var authHeaderFormat = "Basic {0}";

        //    var authHeader = string.Format(authHeaderFormat,
        //        Convert.ToBase64String(Encoding.UTF8.GetBytes(Uri.EscapeDataString(oAuthConsumerKey) + ":" +
        //        Uri.EscapeDataString((oAuthConsumerSecret)))
        //    ));

        //    var postBody = "grant_type=client_credentials";

        //    HttpWebRequest authRequest = (HttpWebRequest)WebRequest.Create(oAuthUrl);
        //    authRequest.Headers.Add("Authorization", authHeader);
        //    authRequest.Method = "POST";
        //    authRequest.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
        //    authRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

        //    using (Stream stream = authRequest.GetRequestStream())
        //    {
        //        byte[] content = ASCIIEncoding.ASCII.GetBytes(postBody);
        //        stream.Write(content, 0, content.Length);
        //    }

        //    authRequest.Headers.Add("Accept-Encoding", "gzip");

        //    WebResponse authResponse = authRequest.GetResponse();
        //    // deserialize into an object
        //    TwitAuthenticateResponse twitAuthResponse;
        //    using (authResponse)
        //    {
        //        using (var reader = new StreamReader(authResponse.GetResponseStream()))
        //        {

        //            var objectText = reader.ReadToEnd();
        //            twitAuthResponse = JsonConvert.DeserializeObject<TwitAuthenticateResponse>(objectText);
        //        }
        //    }
        //    var searchFormat = "https://api.twitter.com/1.1/search/tweets.json?q={0}";
        //    // Do the timeline

        //    var timelineUrl = string.Format(searchFormat, HttpUtility.UrlEncode(hashTag));

        //    HttpWebRequest timeLineRequest = (HttpWebRequest)WebRequest.Create(timelineUrl);
        //    var timelineHeaderFormat = "{0} {1}";
        //    timeLineRequest.Headers.Add("Authorization", string.Format(timelineHeaderFormat, twitAuthResponse.token_type, twitAuthResponse.access_token));
        //    timeLineRequest.Method = "Get";
        //    WebResponse timeLineResponse = timeLineRequest.GetResponse();
        //    var timeLineJson = string.Empty;
        //    using (timeLineResponse)
        //    {
        //        using (var reader = new StreamReader(timeLineResponse.GetResponseStream()))
        //        {
        //            timeLineJson = reader.ReadToEnd();
        //        }
        //    }
            
        //    JObject tweets = JObject.Parse(timeLineJson);
        //    int i = 0;
        //    foreach (var item in tweets["statuses"])
        //    {
        //        var tweet = item["text"].ToString();
        //        SentimentBatchEntry sbe = new SentimentBatchEntry(SentimentBatchEntryType.Tweet, tweet, "Add source");
        //        batch.Add(sbe);
        //        i++;
        //        if (i > batchSize) break;
        //    }


        //    return batch;
        //}

        //private static List<SentimentBatchEntry> GetTextBatch(string path, int batchSize)
        //{
        //    int count = 0;
        //    List<SentimentBatchEntry> batch = new List<SentimentBatchEntry>();
        //    foreach (string file in Directory.EnumerateFiles(path, "*.txt"))
        //    {

        //        if (count == batchSize) break;
                
        //        var contents = File.ReadAllText(file);
        //        SentimentBatchEntry e = new SentimentBatchEntry(SentimentBatchEntryType.Text, contents, file);
        //        batch.Add(e);
        //        count++;
        //    }
        //    return batch;
        //}

        //private static List<string> PrepareTwitterBatch(string hashTag)
        //{
        //    // You need to set your own keys and screen name
        //    var oAuthConsumerKey = "BDcTJ3692JpJPeC43sSmlPfFy";
        //    var oAuthConsumerSecret = "yOPC22Ok5iTh9kljm3gBWIPOhcWhRhnfuqOT2YrpipsHYsXQ8w";
        //    var oAuthUrl = "https://api.twitter.com/oauth2/token";
            
        //    // Do the Authenticate
        //    var authHeaderFormat = "Basic {0}";

        //    var authHeader = string.Format(authHeaderFormat,
        //        Convert.ToBase64String(Encoding.UTF8.GetBytes(Uri.EscapeDataString(oAuthConsumerKey) + ":" +
        //        Uri.EscapeDataString((oAuthConsumerSecret)))
        //    ));

        //    var postBody = "grant_type=client_credentials";

        //    HttpWebRequest authRequest = (HttpWebRequest)WebRequest.Create(oAuthUrl);
        //    authRequest.Headers.Add("Authorization", authHeader);
        //    authRequest.Method = "POST";
        //    authRequest.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
        //    authRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

        //    using (Stream stream = authRequest.GetRequestStream())
        //    {
        //        byte[] content = ASCIIEncoding.ASCII.GetBytes(postBody);
        //        stream.Write(content, 0, content.Length);
        //    }

        //    authRequest.Headers.Add("Accept-Encoding", "gzip");

        //    WebResponse authResponse = authRequest.GetResponse();
        //    // deserialize into an object
        //    TwitAuthenticateResponse twitAuthResponse;
        //    using (authResponse)
        //    {
        //        using (var reader = new StreamReader(authResponse.GetResponseStream()))
        //        {
                    
        //            var objectText = reader.ReadToEnd();
        //            twitAuthResponse = JsonConvert.DeserializeObject<TwitAuthenticateResponse>(objectText);
        //        }
        //    }
        //    var searchFormat = "https://api.twitter.com/1.1/search/tweets.json?q={0}";
        //    // Do the timeline
            
        //    var timelineUrl = string.Format(searchFormat, HttpUtility.UrlEncode(hashTag));
            
        //    HttpWebRequest timeLineRequest = (HttpWebRequest)WebRequest.Create(timelineUrl);
        //    var timelineHeaderFormat = "{0} {1}";
        //    timeLineRequest.Headers.Add("Authorization", string.Format(timelineHeaderFormat, twitAuthResponse.token_type, twitAuthResponse.access_token));
        //    timeLineRequest.Method = "Get";
        //    WebResponse timeLineResponse = timeLineRequest.GetResponse();
        //    var timeLineJson = string.Empty;
        //    using (timeLineResponse)
        //    {
        //        using (var reader = new StreamReader(timeLineResponse.GetResponseStream()))
        //        {
        //            timeLineJson = reader.ReadToEnd();
        //        }
        //    }
        //    List<string> results = new List<string>();
        //    JObject tweets = JObject.Parse(timeLineJson);
        //    foreach (var item in tweets["statuses"])
        //    {
        //        var tweet = item["text"].ToString();
        //        results.Add(tweet);
        //    }

     
        //    return results;
        //}
        //private static List<string> PrepareBatch(string path, int batchSize)
        //{
        //    int count = 0;
        //    List<string> batch = new List<string>();
        //    foreach (string file in Directory.EnumerateFiles(path, "*.txt"))
        //    {
                
        //        if (count == batchSize) break;
        //        var contents = File.ReadAllText(file);
        //        batch.Add(contents);
        //        count++;
        //    }
        //    return batch;
        //}

        //static async Task<Single> CalculateSentiment(List<string> strings)
        //{
        //    HttpResponseMessage response;
        //    Single averageScore = 0;
        //    Single totalScore = 0;
        //    var client = new HttpClient();
        //    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "00c8154911914da794fd3bf4f67065e5");
        //    var queryString = HttpUtility.ParseQueryString(string.Empty);
        //    queryString["numberOfLanguagesToDetect"] = "4";
        //    var uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment?" + queryString;

        //    int i = 0;
        //    JArray ja = new JArray();
        //    foreach (var textToAnalyse in strings)
        //    {
        //        ja.Add(new JObject(
        //        new JProperty("language", "en"),
        //        new JProperty("id", i),
        //        new JProperty("text", textToAnalyse)));
        //        i++;
        //    }

        //    JObject jBody = new JObject(new JProperty("documents", ja));
        //    var body = JsonConvert.SerializeObject(jBody);
        //    byte[] byteData = Encoding.UTF8.GetBytes(body);
        //    JObject sentimentResults = null;
        //    using (var content = new ByteArrayContent(byteData))
        //    {
        //        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        //        response = await client.PostAsync(uri, content);
        //        var responseBody = await response.Content.ReadAsStringAsync();
        //        sentimentResults = JObject.Parse(responseBody);
        //    }
        //    int count = 0;
        //    foreach (var item in sentimentResults["documents"])
        //    {
        //        Single.TryParse(item["score"].ToString(), out Single score);
        //        Console.WriteLine("Discrete score is {0}", score);
        //        totalScore += score;
        //        count++;
        //    }


        //    averageScore = (totalScore / count);

        //    return averageScore;
        //}
        static async Task<List<SentimentBatchEntry>> CalculateSentiment(List<SentimentBatchEntry> entries)
        {
            HttpResponseMessage response;
           
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Global.TextAnalyticsSubscriptionKey);
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["numberOfLanguagesToDetect"] = "4";
            var uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment?" + queryString;

            int i = 0;
            JArray ja = new JArray();
            foreach (var entry in entries)
            {
                entry.Id = i;
                ja.Add(new JObject(
                new JProperty("language", "en"),
                new JProperty("id", i),
                new JProperty("text", entry.TextToAnalyse)));
                i++;
            }

            JObject jBody = new JObject(new JProperty("documents", ja));
            var body = JsonConvert.SerializeObject(jBody);
            byte[] byteData = Encoding.UTF8.GetBytes(body);
            JObject sentimentResults = null;
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                sentimentResults = JObject.Parse(responseBody);
            }
           
            foreach (var item in sentimentResults["documents"])
            {
                Single.TryParse(item["score"].ToString(), out Single score);
                Int32.TryParse(item["id"].ToString(), out int id);
                
                entries.Where(x => x.Id == id).FirstOrDefault().SentimentScore = score;
                entries.Where(x => x.Id == id).FirstOrDefault().Sentiment = GetSentiment(score);
            }

            foreach (var item in sentimentResults["errors"])
            {
                
                Int32.TryParse(item["id"].ToString(), out int id);
                entries.Where(x => x.Id == id).FirstOrDefault().Sentiment = Sentiment.Error;
                entries.Where(x => x.Id == id).FirstOrDefault().Error = item["message"].ToString();
                
            }



            return entries;
        }

        private static Sentiment GetSentiment(float score)
        {
            Sentiment result = Sentiment.Unknown;
            if (score > Global.Sentiment_Negative)
            {
                if (score < Global.Sentiment_Indifferent)
                {
                    result = Sentiment.Neutral;
                }
                else
                {
                    result = Sentiment.Positive;
                }
            }
            else
            {
                result = Sentiment.Negative;
            }
            return result;
        }

        private static List<string> PrepareBatchFromAudio(string path, int batchSize)
        {
            int count = 0;
            List<string> batch = new List<string>();
            foreach (string file in Directory.EnumerateFiles(path, "*.wav"))
            {

                if (count == batchSize) break;
                string transcription = TranscribeAudio(file);
                batch.Add(transcription);
                count++;
            }
            return batch;

        }

        private static string TranscribeAudio(string file)
        {
            BingAuthentication auth = new BingAuthentication(Global.BingSpeechApiSubscriptionKey);
            string requestUri = "https://speech.platform.bing.com/speech/recognition/interactive/cognitiveservices/v1?language=en-US";
            string host = @"speech.platform.bing.com";
            string contentType = @"audio/wav; codec=""audio/pcm""; samplerate=16000";

            /*
             * Input your own audio file or use read from a microphone stream directly.
             */
            string audioFile = file;
            string responseString="";
            FileStream fs = null;

            try
            {
                var token = auth.GetAccessToken();
              
                HttpWebRequest request = null;
                request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
                request.SendChunked = true;
                request.Accept = @"application/json;text/xml";
                request.Method = "POST";
                request.ProtocolVersion = HttpVersion.Version11;
                request.Host = host;
                request.ContentType = contentType;
                request.Headers["Authorization"] = "Bearer " + token;

                using (fs = new FileStream(audioFile, FileMode.Open, FileAccess.Read))
                {

                    /*
                     * Open a request stream and write 1024 byte chunks in the stream one at a time.
                     */
                    byte[] buffer = null;
                    int bytesRead = 0;
                    using (Stream requestStream = request.GetRequestStream())
                    {
                        /*
                         * Read 1024 raw bytes from the input audio file.
                         */
                        buffer = new Byte[checked((uint)Math.Min(1024, (int)fs.Length))];
                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            requestStream.Write(buffer, 0, bytesRead);
                        }

                        // Flush
                        requestStream.Flush();
                    }

                    /*
                     * Get the response from the service.
                     */
                    
                    using (WebResponse response = request.GetResponse())
                    {
                        

                        using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                        {
                            responseString = sr.ReadToEnd();
                        }

                        
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
            JObject responseObject = JObject.Parse(responseString);
            return responseObject["DisplayText"].ToString();
        }

       
    }

    public class TwitAuthenticateResponse
    {
        public string token_type { get; set; }
        public string access_token { get; set; }
    }
}
