using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using Newtonsoft.Json.Linq;

namespace shitfixer
{
    class Program
    {
        private const bool FixShit = false; // Set to false and give repository name in command line arguments for testing
        private const int MinutesBetweenUpdates = 15;
        private const int MillisecondsBetweenUpdate = MinutesBetweenUpdates * 60 * 1000;
        private const string GitHubPublicFeed = "https://github.com/timeline.json";

        private static Timer updateReposTimer;
        private static WebClient webClient; // I don't feel like making HTTP requests from scratch

        static void Main(string[] args)
        {
            GitHub.Login();
            GitHub.GetRepositories();
            if (FixShit)
            {
                webClient = new WebClient();
                updateReposTimer = new Timer(o => UpdateRepos(), null,
                                             MillisecondsBetweenUpdate, MillisecondsBetweenUpdate);
            }
            else
                ShitFixer.FixShit(args[0]);
            Console.WriteLine("Press q to exit.");
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey();
            } while (key.KeyChar != 'q');
        }

        // Grabs the public timeline and picks a repository out of it, then
        // clones it and fixes shit.
        // TODO: Avoid fixing the same repos several times
        private static void UpdateRepos()
        {
            Console.WriteLine("Fetching public timeline and updating repositories...");
            try
            {
                var rawFeed = webClient.DownloadString(GitHubPublicFeed); // TODO: Async?
                var feed = JObject.Parse(rawFeed);
            }
            catch { }
        }
    }
}
