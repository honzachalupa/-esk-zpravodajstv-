using BackgroundTask.Model;
using BackgroundTasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;

namespace BackgroundTask
{
    public sealed class BackgroundSync : IBackgroundTask
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        string serverRoot = "http://www.honzachalupa.cz/ceske-zpravodajstvi/";
        string localFolder = ApplicationData.Current.LocalFolder.Path;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            Debug.WriteLine("BackgroundSync triggered at " + DateTime.Now.ToString("H:mm"));

            string lastSyncTime = (string)localSettings.Values["lastSyncTime"];
            string selectedSourcesTemp = (string)localSettings.Values["selectedSources"];
            string[] selectedSources = selectedSourcesTemp.Split(";".ToCharArray());

            await DownloadFile(serverRoot + "sources.json", "\\sources.json");

            List<Source> sources = await GetArticles(selectedSources);

            if (sources[0].Articles.Count > 0)
            {
                List<Article> allArticles = new List<Article>();

                foreach (Source source in sources)
                {
                    for (int i = 0; i < source.Articles.Count; i++)
                    {
                        if (source.Articles[i].Title != "" || source.Articles[i].Title != null || source.Articles[i].Content != "" || source.Articles[i].Content != null)
                        {
                            allArticles.Add(new Article() { Title = source.Articles[i].Title, Date = source.Articles[i].Date, Author = source.Articles[i].Author, Image = source.Articles[i].Image, Content = Regex.Replace(source.Articles[i].Content, "#.+?#", ""), Url = source.Articles[i].Url });
                        }
                    }
                }

                localSettings.Values["lastSyncTime"] = DateTime.Now.ToString();

                int diffCount = 0;

                foreach (Article article in allArticles)
                {
                    if (DateTime.Parse(article.Date) >= DateTime.Parse(lastSyncTime))
                    {
                        diffCount++;
                    }
                }

                string notificationText;
                string textPart;

                if (diffCount > 0)
                {
                    switch (diffCount)
                    {
                        case 1:
                            textPart = "nový článek";
                            break;
                        case 2:
                        case 3:
                        case 4:
                            textPart = "nové články";
                            break;
                        default:
                            textPart = "nových článků";
                            break;
                    }

                    notificationText = "Máte " + diffCount + " " + textPart + ".";

                    Notification.CreateToastNotification(allArticles[0].Title, notificationText, allArticles[0].Image);
                }

                Notification.CreateTiles(allArticles);
            }

            deferral.Complete();
        }

        private async Task DownloadFile(string urlOnline, string urlLocal)
        {
            // Download source file
            byte[] content;
            WebRequest request = WebRequest.Create(urlOnline);
            WebResponse response = await request.GetResponseAsync();

            Stream stream = response.GetResponseStream();

            using (BinaryReader br = new BinaryReader(stream))
            {
                content = br.ReadBytes(1000000);
                br.Dispose();
            }

            response.Dispose();

            // Save source file
            urlLocal = localFolder + urlLocal;

            if (!Directory.Exists(localFolder + "//Articles"))
            {
                Directory.CreateDirectory(localFolder + "//Articles");
            }

            if (File.Exists(urlLocal))
            {
                File.Delete(urlLocal);
            }

            FileStream fs = new FileStream(urlLocal, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(content);

            fs.Dispose();
            bw.Dispose();
        }

        private async Task<List<Source>> GetArticles(string[] selectedSources)
        {
            List<Source> articles = new List<Source>();
            List<SourceDefinition> sourceDefitinions = GetSourceDefinitions();

            foreach (var sourceDefitinion in sourceDefitinions)
            {
                foreach (var selectedSource in selectedSources)
                {
                    if (sourceDefitinion.Name == selectedSource)
                    {
                        string safeName = NormalizeValue(sourceDefitinion.Name + ".json");

                        await DownloadFile(serverRoot + "articles/" + safeName, "\\Articles\\" + safeName);
                        await DownloadFile(sourceDefitinion.Icon_Dark, "\\Graphics\\" + Regex.Match(sourceDefitinion.Icon_Dark, @"[a-z-_.,]*$"));
                        //await DownloadFile(sourceDefitinion.Icon_Light, "\\Graphics\\" + Regex.Match(sourceDefitinion.Icon_Light, @"[a-z-_.,]*$"));

                        articles.Add(ReadArticleFile("\\Articles\\" + safeName));
                    }
                }
            }

            return articles;
        }

        private List<SourceDefinition> GetSourceDefinitions()
        {
            Stream stream = File.OpenRead(localFolder + "\\sources.json");

            string json;

            using (StreamReader streamReader = new StreamReader(stream))
            {
                json = streamReader.ReadToEnd();
            }

            json = Regex.Replace(json, @"(\\r\\n|\s\s|\t)", "");

            return JsonConvert.DeserializeObject<List<SourceDefinition>>(json);
        }

        private Source ReadArticleFile(string articlesFile)
        {
            Stream stream = File.OpenRead(localFolder + articlesFile);

            string json;

            using (StreamReader streamReader = new StreamReader(stream))
            {
                json = streamReader.ReadToEnd();
            }

            return JsonConvert.DeserializeObject<Source>(json);
        }

        private string NormalizeValue(string value)
        {
            value = Regex.Replace(Regex.Replace(value, @"\s+", "-"), @"-+", "-").ToLower();

            value = value.Normalize(NormalizationForm.FormD);
            var chars = value.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            value = new string(chars).Normalize(NormalizationForm.FormC);

            return value;
        }
    }
}
