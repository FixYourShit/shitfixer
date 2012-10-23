﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using NGit;
using NGit.Api;
using Sharpen;
using NGit.Transport;
using System.Threading;

namespace shitfixer
{
    public static class ShitFixer
    {
        private const string CloneUrl = "git://github.com/{0}.git";
        public static bool Busy = false;

        private static string[] ValidExtensions = new []
            {
                "*.cs", "*.c", "*.asm", "*.S", "*.dasm", "*.vb", "*.py", "*.html", "*.css", "*.js", "*.xml",
                "*.json", "*.sh", "*.bat", "*.rb", "*.java", "*.php", "*.cpp", "*.pl", "*.in", "*.h", "*.m",
                "*.as", "*.d", "*.lua", "*.less"
            };

        private const string ExtendedCommitDescription = "Created by a bot\nChanges:\n{0}";

        private const string PullRequestMessage = "I am a bot that determines the most common formatting styles your project " +
            "uses and standardizes it throughout your entire project.\n\n**Change Summary**\n\n{0}\n" +
            "[My Source Code](https://github.com/FixYourShit/shitfixer) - [More Information](https://github.com/FixYourShit/shitfixer/blob/master/README.md) - [My Author](https://github.com/SirCmpwn)\n\n" +
            "If I did something wrong, or you want more features, pull requests on my own code are welcome.\n\n" +
            "**This is an automated tool. Please double-check that everything still works before merging.**\n\n" +
            "**This bot is in beta, please check that the changes are correct and [create an issue](https://github.com/FixYourShit/shitfixer/issues/new) if they are not.**";

        // Fixes shit async
        public static void FixShit(string repositoryName)
        {
            Task.Factory.StartNew(FixShitAsync, repositoryName);
        }

        private static void FixShitAsync(object _repositoryName)
        {
            Busy = true;
            try
            {
                var repositoryName = _repositoryName as string;
                Console.WriteLine("Cloning " + repositoryName + "...");
                var cloneDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString().Replace("-", ""));

                var repository = CloneRepository(string.Format(CloneUrl, repositoryName), cloneDir);

                var summary = ReformatRepository(cloneDir, repositoryName);

                var status = GetStatus(repository);
                var modified = status.GetModified();
                if (modified.Count != 0)
                {
                    Console.WriteLine("Committing changes...");
                    DoCommit(repository, "Fixed formatting issues", summary);
                    Console.WriteLine("Forking repository...");
                    dynamic fork = GitHub.Fork(repositoryName);

                    Console.WriteLine("Pushing to origin...");
                    var push = repository.Push();
                    push.SetCredentialsProvider(new UsernamePasswordCredentialsProvider(GitHub.Username, GitHub.Password));
                    push.SetRemote(fork.Remote);
                    push.SetRefSpecs(new RefSpec("refs/heads/master:refs/heads/master"));
                    push.Call();

                    // Create pull request
                    int requestNumber = -1;
                    int i;
                    for (i = 0; i < 10; i++)
                    {
                        try
                        {
                            Console.WriteLine("Sending pull request..."); // TODO: Use the default branch on the repo
                            var originOwner = repositoryName.Remove(repositoryName.IndexOf('/'));
                            requestNumber = GitHub.PullRequest(repositoryName, "FixYourShit:master",
                                originOwner + ":master", "Fixed your shit", string.Format(PullRequestMessage, summary));
                            break;
                        }
                        catch
                        {
                            Thread.Sleep(5000); // Most of the errors creating a pull request are from GitHub lagging beind
                        }
                    }
                    if (i == 10)
                    {
                        Program.RepositoriesToDelete.Add(new RepositoryToDelete
                        {
                            PullRequest = requestNumber,
                            Origin = repositoryName,
                            RepositoryName = fork.Name
                        });
                        throw new Exception("Unable to create pull request.");
                    }

                    Console.WriteLine("Finished cleaning " + repositoryName);
                    Program.RepositoriesToDelete.Add(new RepositoryToDelete
                    {
                        PullRequest = requestNumber,
                        Origin = repositoryName,
                        RepositoryName = fork.Name
                    });
                    Directory.Delete(cloneDir, true);
                }
                else
                    Console.WriteLine("No changes to commit.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                Busy = false;
            }
        }

        private static void DoCommit(Git repository, string message, string summary)
        {
            var commit = repository.Commit();
            commit.SetAuthor("FixYourShit", "shitfixer@sircmpwn.com");
            commit.SetMessage(message);
            commit.SetReflogComment(string.Format(ExtendedCommitDescription, summary));
            commit.SetAll(true);
            commit.Call();
        }

        private static Status GetStatus(Git repository)
        {
            var call = repository.Status();
            return call.Call();
        }

        private static Git CloneRepository(string url, string destination)
        {
            // TODO: Shallow clone
            var command = Git.CloneRepository();
            if (!Directory.Exists(destination))
                Directory.CreateDirectory(destination);
            command.SetDirectory(new FilePath(destination));
            command.SetURI(url);
            command.SetCloneSubmodules(false);
            return command.Call();
        }

        private static int CountStart(string s, char c)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] != c)
                    return i;
            }
            return s.Length;
        }

        public static string ReformatRepository(string cloneDir, string repositoryName)
        {
            // Process repository
            var files = new List<string>();
            foreach (var ext in ValidExtensions)
                files.AddRange(Directory.GetFiles(cloneDir, ext, SearchOption.AllDirectories));
            files = new List<string>(files.Where(f => !f.Contains(".git")));
            int tabCount = 0, spaceCount = 0;
            int crlfCount = 0, lfCount = 0;
            string contents;
            Console.WriteLine("Processing " + repositoryName + "...");
            foreach (var file in files)
            {
                try
                {
                    contents = File.ReadAllText(file);
                    lfCount += contents.Count(c => c == '\n');
                    crlfCount += contents.Count(c => c == '\r');
                    lfCount -= crlfCount;
                    foreach (var line in contents.Split('\n'))
                    {
                        if (line.StartsWith("\t"))
                            tabCount++;
                        else if (line.StartsWith(" "))
                            spaceCount++;
                    }
                }
                catch
                {
                }
            }
            if ((tabCount == 0 || spaceCount == 0) && (crlfCount == 0 || lfCount == 0))
            {
                Console.WriteLine(repositoryName + " is well formatted. No changes to apply.");
                return null;
            }
            Console.WriteLine("Applying changes to " + repositoryName);
            string text;
            string spacesString = "    ";
            foreach (var file in files)
            {
                StreamReader reader = new StreamReader(file);
                Encoding encoding = reader.CurrentEncoding;
                text = reader.ReadToEnd();
                reader.Close();
                if (!(crlfCount == 0 || lfCount == 0)) // Fix line breaks
                {
                    if (crlfCount < lfCount) // CRLF to LF
                        text = text.Replace("\r\n", "\n");
                    else // LF to CRLF
                    {
                        text = text.Replace("\r\n", "temporary_shit_to_fix")
                            .Replace("\n", "\r\n").Replace("temporary_shit_to_fix", "\r\n");
                    }
                }
                if (!(tabCount == 0 || spaceCount == 0)) // Fix indentation
                {
                    if (tabCount < spaceCount) // Tabs to spaces
                        text = text.Replace("\t", spacesString);
                    else // Spaces to tabs
                        text = SpacesToTabs(text, lfCount < crlfCount);
                }
                var writer = File.Create(file);
                var payload = encoding.GetBytes(text);
                writer.Write(payload, 0, payload.Length);
                writer.Flush();
                writer.Close();
            }

            // Create summary
            string summary = "";
            if (!(crlfCount == 0 || lfCount == 0))
            {
                if (crlfCount < lfCount) // CRLF to LF
                    summary += "* Converted CRLF to LF\n";
                else
                    summary += "* Converted LF to CRLF\n";
            }
            if (!(tabCount == 0 || spaceCount == 0))
            {
                if (tabCount < spaceCount) // Tabs to spaces
                    summary += "* Converted tabs to spaces (4 spaces to a tab)\n";
                else // Spaces to tabs
                    summary += "* Converted spaces to tabs\n";
            }
            return summary;
        }

        private static string SpacesToTabs(string text, bool crlf)
        {
            string[] lines = text.Replace("\r", "").Split('\n');
            text = string.Empty;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith(" "))
                {
                    int spaces = 0;
                    for (int j = 0; j < lines[i].Length; j++)
                    {
                        if (lines[i][j] != ' ')
                            break;
                        spaces++;
                    }
                    // TODO: Properly format this based on indent
                    spaces /= 4;
                    lines[i] = lines[i].TrimStart();
                    for (int j = 0; j < spaces; j++)
                        lines[i] = "\t" + lines[i];
                }
                text += lines[i];
                if (crlf)
                    text += "\r\n";
                else
                    text += "\n";
            }
            return text;
        }
    }
}
