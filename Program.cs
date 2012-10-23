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
        private const bool FixShit = true, FixPublicShit = false, FixRequestedShit = true; // Set to false and give repository name in command line arguments for testing
        private const int MinutesBetweenUpdates = 1;
        private const int MillisecondsBetweenUpdate = MinutesBetweenUpdates * 60 * 1000;
        private const string GitHubPublicFeed = "https://github.com/timeline.json";

        private static Timer updateReposTimer;
        private static WebClient webClient; // I don't feel like making HTTP requests from scratch
        internal static List<RepositoryToDelete> RepositoriesToDelete;

        static void Main(string[] args)
        {
            RepositoriesToDelete = new List<RepositoryToDelete>();

            GitHub.Login();
            GitHub.GetRepositories();
            if (FixShit)
            {
                webClient = new WebClient();
                UpdateRepos();
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
            try
            {
                if (RepositoriesToDelete.Count != 0) // Delete repositories as needed
                {
                    Console.WriteLine("Checking outstanding pull requests...");
                    foreach (var request in RepositoriesToDelete)
                    {
                        if (GitHub.GetPullRequestStatus(request.PullRequest, request.Origin) == "closed")
                        {
                            Console.WriteLine("Deleting " + request.RepositoryName);
                            GitHub.DeleteRepository(request.RepositoryName);
                            RepositoriesToDelete.Remove(request);
                        }
                    }
                }
                if (ShitFixer.Busy)
                    return;
                if (FixRequestedShit) // Requested shit takes a higher priority
                {
                    Console.WriteLine("Checking fix requests...");
                    var issues = GitHub.GetActiveIssues("FixYourShit/shitfixer").Where(i => i.Title.StartsWith("Fix "));
                    if (issues.Count() != 0)
                    {
                        var issue = issues.First();
                        string repository = issue.Title.Substring(4).Trim();
                        bool valid = false;
                        try
                        {
                            // Validate repository
                            webClient.DownloadString("https://github.com/" + repository);
                            valid = true;
                        }
                        catch
                        {
                            GitHub.CommentOnIssue(issue.IssueNumber, "FixYourShit/shitfixer", "That repository is either private or does not exist.");
                            GitHub.CloseIssue(issue.IssueNumber, "FixYourShit/shitfixer");
                        }
                        if (valid)
                        {
                            if (repository.Remove(repository.IndexOf('/')).ToLower() == "fixyourshit")
                            {
                                GitHub.CommentOnIssue(issue.IssueNumber, "FixYourShit/shitfixer", "Haha, very funny.");
                                GitHub.CloseIssue(issue.IssueNumber, "FixYourShit/shitfixer");
                            }
                            else
                            {
                                GitHub.CommentOnIssue(issue.IssueNumber, "FixYourShit/shitfixer", "Working on it.");
                                GitHub.CloseIssue(issue.IssueNumber, "FixYourShit/shitfixer");
                                ShitFixer.FixShit(repository);
                            }
                        }
                    }
                }
                if (FixPublicShit)
                {
                    Console.WriteLine("Looking for public repositories to fix...");
                    var rawFeed = webClient.DownloadString(GitHubPublicFeed); // TODO: Async?
                    var feed = JObject.Parse(rawFeed);
                }
            }
            catch { }
        }
    }
}
