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

namespace NicksCognitiveTest
{
    static class Program
    {
        static void Main()
        {
            Single x = 0;
            NewMain();
            return;


            var negPath = @"C:\Users\nhill\Desktop\sentiment\review_polarity\txt_sentoken\neg";
            var posPath = @"C:\Users\nhill\Desktop\sentiment\review_polarity\txt_sentoken\pos";

            Console.WriteLine("calculating sentiment for a batch of negative reviews");
            x = CalculateSentiment(PrepareBatch(negPath, 25)).Result;
            Console.WriteLine("score was {0}", x);
            Console.WriteLine();

            Console.WriteLine("calculating sentiment for a batch of positive reviews");
            x = CalculateSentiment(PrepareBatch(posPath, 25)).Result;
            Console.WriteLine("score was {0}", x);
            Console.WriteLine();

            Console.WriteLine("retrieving a batch of HMRC-related tweets");
            x = CalculateSentiment(PrepareTwitterBatch("#HMRC")).Result;
            Console.WriteLine("score was {0}", x);
            Console.WriteLine();

            Console.WriteLine("retrieving a batch of positive-related tweets");
            x = CalculateSentiment(PrepareTwitterBatch("#positive")).Result;
            Console.WriteLine("score was {0}", x);
            Console.WriteLine();

            Console.WriteLine("retrieving a batch of idiot-related tweets");
            x = CalculateSentiment(PrepareTwitterBatch("#idiot")).Result;
            Console.WriteLine("score was {0}", x);
            Console.WriteLine();

            var audioPath = @"C:\Users\nhill\Documents\Sound recordings";
            Console.WriteLine("processing audio files");
            x = CalculateSentiment(PrepareBatchFromAudio(audioPath, 15)).Result;
            Console.WriteLine("score was {0}", x);
            Console.WriteLine();


            Console.WriteLine("Hit ENTER to exit...");
            Console.ReadLine();
        }

        private static void NewMain()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            const string PATH_TO_NEGATIVE_REVIEWS = @"C:\Users\nhill\Desktop\sentiment\review_polarity\txt_sentoken\neg";
            const string PATH_TO_POSITIVE_REVIEWS = @"C:\Users\nhill\Desktop\sentiment\review_polarity\txt_sentoken\pos";
            const string PATH_TO_AUDIO = @"C:\Users\nhill\Documents\Sound recordings";
            Console.WriteLine("*****************************************************");
            Console.WriteLine("****          Cognitive Services Demo           *****");
            Console.WriteLine("*****************************************************");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine("Building batch of texts to analyse....");
            Console.WriteLine("Adding negative film reviews");
            List<SentimentBatchEntry> negative = GetTextBatch(PATH_TO_NEGATIVE_REVIEWS, 10);
            Console.WriteLine("Adding positive film reviews");
            List<SentimentBatchEntry> positive = GetTextBatch(PATH_TO_POSITIVE_REVIEWS, 10);
            Console.WriteLine("Adding tweets with hashtag #HMRC");

            List<SentimentBatchEntry> tweets = GetTwitterBatch("#HMRC", 10);
            List<SentimentBatchEntry> audio = GetAudioBatch(PATH_TO_AUDIO, 10);
            Console.WriteLine("Converting Audio Files to Text and adding to batch");
            
            var consolidated = negative.Union(positive).Union(tweets).Union(audio).ToList();
            consolidated = CalculateSentiment(consolidated).Result;
            foreach (var item in consolidated)
            {
                Console.WriteLine(item);
            }
            Console.ReadLine();
        }

        private static List<SentimentBatchEntry> ProcessConsolidated(List<SentimentBatchEntry> consolidated)
        {
            int i = 0;
            foreach (var item in consolidated)
            {
                i++;
                item.SentimentScore = i;
            }
            return consolidated;
        }

        private static List<SentimentBatchEntry> GetAudioBatch(string path, int batchSize)
        {
            List<SentimentBatchEntry> batch = new List<SentimentBatchEntry>();
            int count = 0;
            
            foreach (string file in Directory.EnumerateFiles(path, "*.wav"))
            {

                if (count == batchSize) break;
                
                string transcription = TranscribeAudio(file);
                SentimentBatchEntry sbe = new SentimentBatchEntry(SentimentBatchEntryType.Audio, transcription, file);
                batch.Add(sbe);
                count++;
            }
            return batch; ;
        }

        private static List<SentimentBatchEntry> GetTwitterBatch(string hashTag, int batchSize )
        {
            List<SentimentBatchEntry> batch = new List<SentimentBatchEntry>();
            // You need to set your own keys and screen name
            var oAuthConsumerKey = "BDcTJ3692JpJPeC43sSmlPfFy";
            var oAuthConsumerSecret = "yOPC22Ok5iTh9kljm3gBWIPOhcWhRhnfuqOT2YrpipsHYsXQ8w";
            var oAuthUrl = "https://api.twitter.com/oauth2/token";

            // Do the Authenticate
            var authHeaderFormat = "Basic {0}";

            var authHeader = string.Format(authHeaderFormat,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(Uri.EscapeDataString(oAuthConsumerKey) + ":" +
                Uri.EscapeDataString((oAuthConsumerSecret)))
            ));

            var postBody = "grant_type=client_credentials";

            HttpWebRequest authRequest = (HttpWebRequest)WebRequest.Create(oAuthUrl);
            authRequest.Headers.Add("Authorization", authHeader);
            authRequest.Method = "POST";
            authRequest.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            authRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (Stream stream = authRequest.GetRequestStream())
            {
                byte[] content = ASCIIEncoding.ASCII.GetBytes(postBody);
                stream.Write(content, 0, content.Length);
            }

            authRequest.Headers.Add("Accept-Encoding", "gzip");

            WebResponse authResponse = authRequest.GetResponse();
            // deserialize into an object
            TwitAuthenticateResponse twitAuthResponse;
            using (authResponse)
            {
                using (var reader = new StreamReader(authResponse.GetResponseStream()))
                {

                    var objectText = reader.ReadToEnd();
                    twitAuthResponse = JsonConvert.DeserializeObject<TwitAuthenticateResponse>(objectText);
                }
            }
            var searchFormat = "https://api.twitter.com/1.1/search/tweets.json?q={0}";
            // Do the timeline

            var timelineUrl = string.Format(searchFormat, HttpUtility.UrlEncode(hashTag));

            HttpWebRequest timeLineRequest = (HttpWebRequest)WebRequest.Create(timelineUrl);
            var timelineHeaderFormat = "{0} {1}";
            timeLineRequest.Headers.Add("Authorization", string.Format(timelineHeaderFormat, twitAuthResponse.token_type, twitAuthResponse.access_token));
            timeLineRequest.Method = "Get";
            WebResponse timeLineResponse = timeLineRequest.GetResponse();
            var timeLineJson = string.Empty;
            using (timeLineResponse)
            {
                using (var reader = new StreamReader(timeLineResponse.GetResponseStream()))
                {
                    timeLineJson = reader.ReadToEnd();
                }
            }
            
            JObject tweets = JObject.Parse(timeLineJson);
            int i = 0;
            foreach (var item in tweets["statuses"])
            {
                var tweet = item["text"].ToString();
                SentimentBatchEntry sbe = new SentimentBatchEntry(SentimentBatchEntryType.Tweet, tweet, "Add source");
                batch.Add(sbe);
                i++;
                if (i > batchSize) break;
            }


            return batch;
        }

        private static List<SentimentBatchEntry> GetTextBatch(string path, int batchSize)
        {
            int count = 0;
            List<SentimentBatchEntry> batch = new List<SentimentBatchEntry>();
            foreach (string file in Directory.EnumerateFiles(path, "*.txt"))
            {

                if (count == batchSize) break;
                
                var contents = File.ReadAllText(file);
                SentimentBatchEntry e = new SentimentBatchEntry(SentimentBatchEntryType.Text, contents, file);
                batch.Add(e);
                count++;
            }
            return batch;
        }

        private static List<string> PrepareTwitterBatch(string hashTag)
        {
            // You need to set your own keys and screen name
            var oAuthConsumerKey = "BDcTJ3692JpJPeC43sSmlPfFy";
            var oAuthConsumerSecret = "yOPC22Ok5iTh9kljm3gBWIPOhcWhRhnfuqOT2YrpipsHYsXQ8w";
            var oAuthUrl = "https://api.twitter.com/oauth2/token";
            
            // Do the Authenticate
            var authHeaderFormat = "Basic {0}";

            var authHeader = string.Format(authHeaderFormat,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(Uri.EscapeDataString(oAuthConsumerKey) + ":" +
                Uri.EscapeDataString((oAuthConsumerSecret)))
            ));

            var postBody = "grant_type=client_credentials";

            HttpWebRequest authRequest = (HttpWebRequest)WebRequest.Create(oAuthUrl);
            authRequest.Headers.Add("Authorization", authHeader);
            authRequest.Method = "POST";
            authRequest.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            authRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (Stream stream = authRequest.GetRequestStream())
            {
                byte[] content = ASCIIEncoding.ASCII.GetBytes(postBody);
                stream.Write(content, 0, content.Length);
            }

            authRequest.Headers.Add("Accept-Encoding", "gzip");

            WebResponse authResponse = authRequest.GetResponse();
            // deserialize into an object
            TwitAuthenticateResponse twitAuthResponse;
            using (authResponse)
            {
                using (var reader = new StreamReader(authResponse.GetResponseStream()))
                {
                    
                    var objectText = reader.ReadToEnd();
                    twitAuthResponse = JsonConvert.DeserializeObject<TwitAuthenticateResponse>(objectText);
                }
            }
            var searchFormat = "https://api.twitter.com/1.1/search/tweets.json?q={0}";
            // Do the timeline
            
            var timelineUrl = string.Format(searchFormat, HttpUtility.UrlEncode(hashTag));
            
            HttpWebRequest timeLineRequest = (HttpWebRequest)WebRequest.Create(timelineUrl);
            var timelineHeaderFormat = "{0} {1}";
            timeLineRequest.Headers.Add("Authorization", string.Format(timelineHeaderFormat, twitAuthResponse.token_type, twitAuthResponse.access_token));
            timeLineRequest.Method = "Get";
            WebResponse timeLineResponse = timeLineRequest.GetResponse();
            var timeLineJson = string.Empty;
            using (timeLineResponse)
            {
                using (var reader = new StreamReader(timeLineResponse.GetResponseStream()))
                {
                    timeLineJson = reader.ReadToEnd();
                }
            }
            List<string> results = new List<string>();
            JObject tweets = JObject.Parse(timeLineJson);
            foreach (var item in tweets["statuses"])
            {
                var tweet = item["text"].ToString();
                results.Add(tweet);
            }

     
            return results;
        }
        private static List<string> PrepareBatch(string path, int batchSize)
        {
            int count = 0;
            List<string> batch = new List<string>();
            foreach (string file in Directory.EnumerateFiles(path, "*.txt"))
            {
                
                if (count == batchSize) break;
                var contents = File.ReadAllText(file);
                batch.Add(contents);
                count++;
            }
            return batch;
        }

        static async Task<Single> CalculateSentiment(List<string> strings)
        {
            HttpResponseMessage response;
            Single averageScore = 0;
            Single totalScore = 0;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "00c8154911914da794fd3bf4f67065e5");
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["numberOfLanguagesToDetect"] = "4";
            var uri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment?" + queryString;

            int i = 0;
            JArray ja = new JArray();
            foreach (var textToAnalyse in strings)
            {
                ja.Add(new JObject(
                new JProperty("language", "en"),
                new JProperty("id", i),
                new JProperty("text", textToAnalyse)));
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
            int count = 0;
            foreach (var item in sentimentResults["documents"])
            {
                Single.TryParse(item["score"].ToString(), out Single score);
                Console.WriteLine("Discrete score is {0}", score);
                totalScore += score;
                count++;
            }


            averageScore = (totalScore / count);

            return averageScore;
        }
        static async Task<List<SentimentBatchEntry>> CalculateSentiment(List<SentimentBatchEntry> entries)
        {
            HttpResponseMessage response;
            Single averageScore = 0;
            Single totalScore = 0;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "00c8154911914da794fd3bf4f67065e5");
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
                Console.WriteLine("Discrete score is {0}", score);
                Int32.TryParse(item["id"].ToString(), out int id);
                entries.Where(x => x.Id == id).FirstOrDefault().SentimentScore = score;



            }


         

            return entries;
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
            BingAuthentication auth = new BingAuthentication("981fdb85c36e44f89263a28d24267ff2");
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
