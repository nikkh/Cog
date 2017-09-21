using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cog
{
    public class SentimentBatchEntry
    {
        public SentimentBatchEntry(SentimentBatchEntryType type, string text, string source)
        {
            Id = 0;
            SentimentBatchEntryType = type;
            TextToAnalyse = text;
            Source = source;
            SentimentScore = 0;
        }

        public SentimentBatchEntryType SentimentBatchEntryType { get; set; }

        public string TextToAnalyse { get; set; }

        public int  Id { get; set; }

        public string Source { get; set; }

        public Single SentimentScore { get; set; }
        public override string ToString()
        {
            return string.Format("Id = {0}, Entry Type = {1}, Source={2} Text to Analyse ={3}, Score = {4}", Id, SentimentBatchEntryType, Source, TextToAnalyse.Substring(0, 20), SentimentScore);
        }
    }

  

    public enum SentimentBatchEntryType { Audio, Text, Tweet}
}
