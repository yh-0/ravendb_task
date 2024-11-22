using System.Data.Common;
using System.Text;
using Newtonsoft.Json;

namespace GetTvShowTotalLength
{
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
            if (response.IsSuccessStatusCode == false) eprint($"Error: fetching episodes data from API failed with code {response.StatusCode}", 10);

            // Deserialize fetched data to dynamic object
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var searchResults = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonResponse);
            if (searchResults is null) eprint("Error: no matching show episodes found.", 10);

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
            if (response.IsSuccessStatusCode == false) eprint($"Error: fetching shows data from API failed with code {response.StatusCode}", 10);

            // Deserialize fetched data to dynamic object
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var searchResults = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonResponse);
            if (searchResults is null) eprint("Error: no matching shows found.", 10);

            // Generate dictionary of found shows
            // dict[show_id] = show_end_date
            var showIdsByEndDate = new Dictionary<int, DateTime>();
            foreach (dynamic result in searchResults) // Dereference of a possibly null reference dealt with in eprint() 3 lines above
            {
                Tuple<int, DateTime>? tuple = parseSearchResult(result, showName);
                if (tuple is null) continue;
                showIdsByEndDate[tuple.Item1] = tuple.Item2;
            }

            // If there are no results with the exact same name, use first result
            string defaultStrId = searchResults[0].show.id;
            if (showIdsByEndDate.Count == 0) return int.Parse(defaultStrId);

            // If there are multiple results with the exact same name, select result with most recent 'ended' date
            var sortedShows = from show in showIdsByEndDate orderby show.Value descending select show;
            return sortedShows.FirstOrDefault().Key;
        }

        private static Tuple<int, DateTime>? parseSearchResult(dynamic result, string showName)
        {
            // Extract needed fields
            int id = result.show.id;
            string name = result.show.name;
            string? strEnded = result.show.ended;

            // Prepare strings for comparison
            string nameLower = name.ToLower();
            string showNameLower = showName.ToLower();

            // If the result has different name than searched show, return null
            if (String.Equals(nameLower, showNameLower) == false) return null;

            // If the result has the exact same name as searched show but no end date, return it regardless
            // 0001-01-01 date gives the result lower priority if there are results with an actual date
            if (strEnded is null) return new Tuple<int, DateTime>(id, new DateTime(1, 1, 1));

            // Parse 'ended' string to DateTime
            int year = int.Parse(strEnded.Split("-")[0]);
            int month = int.Parse(strEnded.Split("-")[1]);
            int day = int.Parse(strEnded.Split("-")[2]);
            DateTime ended = new DateTime(year, month, day);

            return new Tuple<int, DateTime>(id, ended);
        }

        private static void eprint(string errorStr, int exitCode)
        {
            System.Console.Error.WriteLine(errorStr);
            Environment.Exit(exitCode);
        }

        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("No arguments given.");
                Console.WriteLine("Usage: dotnet run <SHOW_NAME>");
            }

            string cleanShowName = args[0].Replace("\"", "");

            int id = await getShowId(cleanShowName);
            int totalRuntime = await getShowEpisodesLength(id);
            System.Console.WriteLine(totalRuntime);
        }
    }
}