using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cog
{
    static class Global
    {
      
        public static string FileDropPath { get; set; }
        public static string FileDropArchivePath { get; set; }
        public static int BatchSize { get; set; }
        public static Single Sentiment_Indifferent { get; set; }

        public static Single Sentiment_Negative { get; set; }
        public static string TextAnalyticsSubscriptionKey { get; internal set; }
        public static string BingSpeechApiSubscriptionKey { get; internal set; }
        public static int MaxDocumentSizeInCharacters { get; internal set; }

        public static string TwitterConsumerKey { get; internal set; }
        public static string TwitterConsumerSecret { get; internal set; }
        public static string TwitterOAuthUrl { get; internal set; }
    }

    
}
