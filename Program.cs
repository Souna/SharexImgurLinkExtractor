using Newtonsoft.Json.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace SharexImgurLinkExtractor
{
    internal class Program
    {
        //Go through the Backup folder and extract all imgur URL links from the json and xml files
        //Also save all dropbox links to a separate text file
        private const string Root = @"J:\bakup\ShareX Screenshots\Backup";
        private const string downloadDestination = $"{Root}\\goop";

        static void Main(string[] args)
        {
            List<string> imgurLinks = GetImageLinksFromXmlAndJsonFiles();

            DownloadImages(imgurLinks, downloadDestination);

            GetDropboxLinks(imgurLinks);
        }

        private static void GetDropboxLinks(List<string> links)
        {
            links = links.Where(entry =>
            entry.Contains("dropbox")).ToList();

            Console.WriteLine($"Found {links.Count} dropbox links in the list");

            //save links entries to a text file
            File.WriteAllLines($"{downloadDestination}\\dropbox.txt", links);
        }

        private static List<string> GetImageLinksFromXmlAndJsonFiles()
        {
            //count the number of json files in the directory whose name starts with "ApplicationConfig"
            var jsonCount = Directory.GetFiles(Root, "ApplicationConfig*.json", SearchOption.AllDirectories).Length;
            Console.WriteLine($"Found {jsonCount} json files inside {Root}");

            //count the number of xml files in the directory. Their names don't matter
            var xmlCount = Directory.GetFiles(Root, "*.xml", SearchOption.AllDirectories).Length;
            Console.WriteLine($"Found {xmlCount} xml files inside {Root}");

            List<string> imgurLinks = new List<string>();

            //for each json file whose name starts with "ApplicationConfig", loop through and extract the URL links
            var jsonFiles = Directory.GetFiles(Root, "ApplicationConfig*.json", SearchOption.AllDirectories);

            foreach (var jsonFile in jsonFiles)
            {
                var jsonText = File.ReadAllText(jsonFile);
                JObject json = JObject.Parse(jsonText);

                JArray recentTasks = (JArray)json["RecentTasks"];

                foreach (JObject urlAttribute in recentTasks)
                {
                    string url = (string)urlAttribute["URL"];
                    imgurLinks.Add(url);
                }
            }

            //for each xml file, loop through and extract the URL links
            var xmlFiles = Directory.GetFiles(Root, "*.xml", SearchOption.AllDirectories);

            foreach (var xmlFile in xmlFiles)
            {
                var xmlText = File.ReadAllText(xmlFile);
                //add a root tag to the xml file so that it can be parsed
                var temporaryRootTag = "<root>" + xmlText + "</root>";
                var doc = XDocument.Parse(temporaryRootTag);
                var xml = new XmlDocument();
                xml.LoadXml(doc.ToString());

                var nodes = xml.SelectNodes("//URL");

                foreach (XmlNode urlAttribute in nodes)
                {
                    imgurLinks.Add(urlAttribute.InnerText);
                }
            }

            Console.WriteLine($"Task is done. Found {imgurLinks.Count} imgur links in ShareX backup");

            //remove all duplicate items in the imgurLinks list, and keep only one of each
            imgurLinks = imgurLinks.Distinct().ToList();

            Console.WriteLine($"{imgurLinks.Count} links remaining after getting rid of dupes");

            return imgurLinks;
        }

        private static void DownloadImages(List<string> links, string downloadDestination)
        {
            Directory.CreateDirectory(downloadDestination);

            using (WebClient webClient = new WebClient())
            {
                foreach (string url in links)
                {
                    try
                    {
                        var fileName = Path.GetFileName(url);
                        var filePath = Path.Combine(downloadDestination, fileName);
                        webClient.DownloadFile(url, filePath);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Wasn't able to download {url}: {ex.Message}");
                    }
                }
            }
        }
    }
}