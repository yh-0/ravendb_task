﻿using System.Data.Common;
using System.Text;
// using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GetTvShowTotalLength
{
    internal class ShowWrapperDTO
    {
        [JsonPropertyName("show")]
        public required ShowDTO Show { get; set; }
    }

    internal class ShowDTO
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("id")]
        public required int Id { get; set; }

        [JsonPropertyName("ended")]
        public string? Ended { get; set; }
    }

    internal class ParsedShow
    {
        public int Id { get; set; }
        public DateTime Ended { get; set; }

        public ParsedShow(int id, DateTime ended)
        {
            Id = id;
            Ended = ended;
        }
    }

    internal class Program
    {
        private static readonly string urlBase = "https://api.tvmaze.com";

        private static async Task<int> getShowEpisodesLength(int showId)
        {
            // Build url
            StringBuilder sb = new StringBuilder();
            sb.Append(urlBase);
            sb.Append("/shows/");
            sb.Append(showId);
            sb.Append("/episodes");
            string url = sb.ToString();

            // Fetch data from API
            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode == false)
            {
                System.Console.Error.WriteLine($"Error: fetching episodes data from API failed with code {response.StatusCode}");
                Environment.Exit(10);
            }

            // Deserialize fetched data to dynamic object
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var searchResults = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonResponse);
            if (searchResults is null)
            {
                System.Console.Error.WriteLine("Error: no matching show episodes found.");
                Environment.Exit(10);
            }

            // Sum up runtimes of all episodes
            int showRuntime = 0;
            foreach (dynamic result in searchResults) // Dereference of a possibly null reference dealt with in eprint() 3 lines above
            {
                int? episodeRuntime = result.runtime;
                if (episodeRuntime != null) showRuntime += (int)episodeRuntime;
            }

            return showRuntime;
        }

        private static async Task<int> getShowId(string showName)
        {
            // Build url
            StringBuilder sb = new StringBuilder();
            sb.Append(urlBase);
            sb.Append("/search/shows?q=");
            sb.Append(showName);
            string url = sb.ToString();

            // Fetch data from API
            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode == false)
            {
                System.Console.Error.WriteLine($"Error: fetching shows data from API failed with code {response.StatusCode}");
                Environment.Exit(10);
            }

            // Deserialize fetched data to dynamic object
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var searchResults = JsonSerializer.Deserialize<List<ShowWrapperDTO>>(jsonResponse);
            if (searchResults is null)
            {
                System.Console.Error.WriteLine("Error: no matching shows found.");
                Environment.Exit(10);
            }

            // Unwrap ShowWrapperDTO to ShowDTO
            var showResults = searchResults
                .Select(result => result.Show)
                .ToList();
            if (searchResults.Count == 0)
            {
                System.Console.Error.WriteLine("Error: no matching shows found.");
                Environment.Exit(10);
            }

            // Parse and sort show results
            var sortedShowResults = showResults
                .Select(show => parseSearchResult(show, showName))
                .Where(parsedShow => parsedShow != null)
                .OrderByDescending(parsedShow => parsedShow?.Ended)
                .ToList();
            if (sortedShowResults is null)
            {
                System.Console.Error.WriteLine("Error: no matching shows found.");
                Environment.Exit(10);
            }

            // If there are no results with the exact same name, use first result
            if (sortedShowResults.Count == 0) return showResults[0].Id;

            // Return show with matching name and most recent ended date
            var selectedShow = sortedShowResults.FirstOrDefault();
            if (selectedShow is null)
            {
                System.Console.Error.WriteLine("Error: no matching shows found.");
                Environment.Exit(10);
            }
            return selectedShow.Id;
        }

        private static ParsedShow? parseSearchResult(ShowDTO show, string showName)
        {
            // Extract needed fields
            int id = show.Id;
            string name = show.Name;
            string? strEnded = show.Ended;

            // Prepare strings for comparison
            string nameLower = name.ToLower();
            string showNameLower = showName.ToLower();

            // If the result has different name than searched show, return null
            if (String.Equals(nameLower, showNameLower) == false) return null;

            // If the result has the exact same name as searched show but no end date, return it regardless
            // 0001-01-01 date gives the result lower priority if there are results with an actual date
            if (strEnded is null) return new ParsedShow(id, new DateTime(1, 1, 1));

            // Parse 'ended' string to DateTime
            int year = int.Parse(strEnded.Split("-")[0]);
            int month = int.Parse(strEnded.Split("-")[1]);
            int day = int.Parse(strEnded.Split("-")[2]);
            DateTime ended = new DateTime(year, month, day);

            return new ParsedShow(id, ended);
        }

        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("No arguments given.");
                Console.WriteLine("Usage: dotnet run <SHOW_NAME>");
            }

            string cleanShowName = args[0].Replace("\"", "");

            int id = await getShowId("girls");
            // int id = await getShowId(cleanShowName);
            int totalRuntime = await getShowEpisodesLength(id);
            System.Console.WriteLine(totalRuntime);
        }
    }
}