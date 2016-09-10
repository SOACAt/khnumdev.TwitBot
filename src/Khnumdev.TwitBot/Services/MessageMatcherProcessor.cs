﻿namespace Khnumdev.TwitBot.Services
{
    using Core.TextAnalyzer.Model;
    using Data.Model;
    using Data.Repositories;
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    public class MessageMatcherProcessor : IMessageMatcherProcessor
    {
        const string USER_ID = "TwitterUserId";

        static List<Tweet> _messages;

        readonly ITwitterRepository _twitterRepository;
        readonly long _userId;

        public MessageMatcherProcessor(ITwitterRepository twitterRepository)
        {
            _twitterRepository = twitterRepository;
            _userId = long.Parse(ConfigurationManager.AppSettings[USER_ID]);
        }

        public async Task<MatchedMessage> ProcessAsync(AnalysisResult result, string input)
        {
            if (_messages == null)
            {
                _messages = await _twitterRepository
                    .GetTweetContentFromAsync(_userId);
            }

            var random = new Random();

            var keyPhrases = string.Join(" ", result.KeyPhrases);

            var messages = _messages
                .Where(i => i.KeyPhrases != null)
                .Select(i =>
                new
                {
                    Message = SanitizeTweet(i.Text, true),
                    Sentiment = i.Sentiment,
                    Value = CalculatePharseCoincidence(keyPhrases, i.KeyPhrases)
                })
                .GroupBy(i => i.Value, i => i)
                .OrderByDescending(i => i.Key)
                .First();

            var selectedMessage = messages.ElementAt(random.Next(0, messages.Count() - 1));

            return new MatchedMessage
            {
                Message = selectedMessage.Message,
                Sentiment = selectedMessage.Sentiment
            };
        }

        /// <summary>
        /// Best match returns 1
        /// </summary>
        /// <param name="input"></param>
        /// <param name="tweetKeyPhrases"></param>
        /// <returns></returns>
        int CalculatePharseCoincidence(string keyPhrases, string tweetKeyPhrases)
        {
            var splitedTweetPhrases = SanitizeTweet(tweetKeyPhrases, false)
                .ToLowerInvariant()
                .Replace(',', ' ')
                .Split(' ')
                .Distinct();

            var splittedKeyPhrases = keyPhrases.ToLowerInvariant().Split(' ').Distinct();

            var numberOfMatchedWords = splitedTweetPhrases
                .Where(i => splittedKeyPhrases.Contains(i))
                .Count();

            return numberOfMatchedWords;
        }

        string SanitizeTweet(string tweet, bool removeUsername)
        {
            string result = tweet;

            if (removeUsername)
            {
                var regex = @"@(\w+)";
                var regexMatch = new Regex(regex);
                result = regexMatch.Replace(tweet, string.Empty);
            }

            return result.Replace("@", " ")
                .Replace("_", " ")
                .TrimStart();
        }
    }
}