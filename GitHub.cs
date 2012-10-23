using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Web;
using System.Threading;

namespace shitfixer
{
    public class GitHub
    {
        private const string ForkUrl = "https://api.github.com/repos/{0}/forks";
        private const string ListRepositoriesUrl = "https://api.github.com/user/repos?sort=created";
        private const string PullRequestUrl = "https://api.github.com/repos/{0}/pulls";

        private static string AuthString;
        internal static string Username, Password;

        public static void Login()
        {
            // Fetch username and password from config
            var config = XDocument.Load("config.xml");
            var login = config.Root.Element("login");
            Username = login.Attribute("username").Value;
            Password = login.Attribute("password").Value;

            AuthString = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(
                Username + ":" + Password));
        }

        public static dynamic Fork(string repositoryName)
        {
            var request = CreatePost(string.Format(ForkUrl, repositoryName));
            request.GetRequestStream().Close();
            var response = request.GetResponse();
            var json = GetJson(response.GetResponseStream());
            var gitUrl = json["html_url"].Value<string>() + ".git";
            // Force a 5 second sleep
            Thread.Sleep(5000); // longer?
            DateTime timeout = DateTime.Now.AddMinutes(5);
            while (!GetRepositories().Contains(gitUrl) && DateTime.Now < timeout)
                Thread.Sleep(5000);
            if (DateTime.Now > timeout)
                return null;
            return new
            {
                Remote = gitUrl,
                Name = json["full_name"].Value<string>()
            };
        }

        public static List<string> GetRepositories()
        {
            var request = CreateGet(ListRepositoriesUrl);
            var response = request.GetResponse();
            var reader = new StreamReader(response.GetResponseStream());
            var json = reader.ReadToEnd();
            var repos = JArray.Parse(json);
            List<string> results = new List<string>();
            foreach (var repo in repos)
                results.Add(repo["html_url"].Value<string>() + ".git");
            return results;
        }

        public static void PullRequest(string targetRepository, string myBranch, string theirBranch, string title, string body)
        {
            var request = CreatePost(string.Format(PullRequestUrl, targetRepository));
            JObject json = new JObject();
            json.Add("base", theirBranch);
            json.Add("head", myBranch);
            json.Add("body", body);
            json.Add("title", title);
            var stream = request.GetRequestStream();
            WriteString(json.ToString(), stream);
            stream.Close();
            var response = request.GetResponse();
            response.Close(); // Discard
        }

        private static WebRequest CreateGet(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers.Add(HttpRequestHeader.Authorization, AuthString);
            request.ContentType = "application/x-www-form-urlencoded";
            return request;
        }

        private static WebRequest CreatePost(string url)
        {
            var request = CreateGet(url);
            request.Method = "POST";
            return request;
        }

        private static void WriteString(string content, Stream stream)
        {
            var payload = Encoding.UTF8.GetBytes(content);
            stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }

        private static JObject GetJson(Stream stream)
        {
            var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            return JObject.Parse(json);
        }
    }
}
