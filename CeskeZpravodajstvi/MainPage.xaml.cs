using BackgroundTask;
using CeskeZpravodajstvi.Model;
using CeskeZpravodajstvi.Pages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Store;
using Windows.Devices.Input;
using Windows.Foundation.Metadata;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace CeskeZpravodajstvi
{
    public sealed partial class MainPage : Page
    {
        DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        string serverRoot = "http://www.honzachalupa.cz/ceske-zpravodajstvi/";
        string localFolder = ApplicationData.Current.LocalFolder.Path;
        bool articleOpened = false;
        bool portraitOrientation = true;
        double[] pageDimensions;
        List<Source> sources;
        List<PivotMeta> pivotMeta = new List<PivotMeta>();
        Article selectedArticle;
        ListViewItem selectedItem;

        public MainPage()
        {
            InitializeComponent();

            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            InitializeAsync();

            if (!localSettings.Values.ContainsKey("fontSize"))
                localSettings.Values["fontSize"] = Convert.ToDouble(20);
        }

        public async void InitializeAsync()
        {
            gradientStop.Color = (Color)Application.Current.Resources["SystemAccentColor"];

            if (localSettings.Values["startupTimes"] == null) localSettings.Values["startupTimes"] = 0;

            localSettings.Values["startupTimes"] = (int)localSettings.Values["startupTimes"] + 1;

            if ((int)localSettings.Values["startupTimes"] == 1)
            {
                await new MessageDialog("Já, autor této aplikace, nejsem autorem ani vlastníkem zobrazovaného obsahu. Veškeré články i jejich doprovodný grafický materiál jsou duševním majetkem příslušných redakcí.\r\n\r\nDěkuji Vám za stažení této aplikace, Honza. :)", "Právní sdělení").ShowAsync();
            }
            else if ((int)localSettings.Values["startupTimes"] == 10 || (int)localSettings.Values["startupTimes"] % 60 == 0)
            {
                MessageDialog messageDonate = new MessageDialog("Pokud aplikaci aktivně používáte, moc Vás prosím o příspěvek na vývoj podobných aplikací. Své aplikace poskytu povětšinou zdarma - neznamená to však, že u jejich tvorby nestrávím několik stovek hodin dřiny.\r\n\r\nOceňte prosím mou práci.", "Příspěvek");
                messageDonate.Commands.Add(new UICommand("Přispět") { Id = 0 });
                messageDonate.Commands.Add(new UICommand("Možná později") { Id = 1 });
                messageDonate.DefaultCommandIndex = 0;
                messageDonate.CancelCommandIndex = 1;

                var result = await messageDonate.ShowAsync();

                if (Convert.ToInt32(result.Id) == 0) DonateRequest();
            }

            LoadNewArticles();
            InitBackgroundTasks();
        }

        private async void LoadNewArticles()
        {
            BeginLoading();

            try
            {
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.BackgroundColor = (Color)Resources["SystemAccentColor"];
                titleBar.ButtonBackgroundColor = (Color)Resources["SystemAccentColor"];
            }
            catch (Exception) { }

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                StatusBar statusBar = StatusBar.GetForCurrentView();
                statusBar.BackgroundOpacity = 1;
                statusBar.BackgroundColor = (Color)Resources["SystemAccentColor"];
                statusBar.ForegroundColor = (Color)Resources["SystemChromeWhiteColor"];
                await statusBar.ShowAsync();
            }

            string selectedSourcesTemp = (string)localSettings.Values["selectedSources"];
            string[] selectedSources = { };

            try
            {
                selectedSources = selectedSourcesTemp.Split(";".ToCharArray());
            }
            catch (Exception)
            {
                selectedSources = null;
            }

            if (IsStableConnectivityAvailable())
                OnlineInitialization(selectedSources);
            else
                OfflineInitialization(selectedSources);
        }

        public async Task<bool> InternetAvailable()
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(serverRoot + "sources.json");

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Warning: " + ex.Message);
                return false;
            }
        }

        public async void OnlineInitialization(string[] selectedSources)
        {
            txtLoadingMessage.Text = "Stahují se aktuální články";

            await DownloadFile(serverRoot + "sources.json", "\\sources.json");

            try
            {
                sources = await GetArticles(selectedSources);
            }
            catch (Exception)
            {
                await new MessageDialog("Nastala neočekávaná chyba. Zkuste operaci prosím zopakovat.", "Chyba").ShowAsync();
            }
            
            GenerateUI(sources);
        }

        public async void OfflineInitialization(string[] selectedSources)
        {
            txtLoadingMessage.Text = "Načítají se uložené články";

            try
            {
                sources = GetArticlesOffline(selectedSources);

                GenerateUI(sources);
            }
            catch (Exception)
            {
                await new MessageDialog("Nebyla nalezená žádná data dostupná offline. Připojte se prosím k internetu.", "Jste offline").ShowAsync();
            }
        }

        private void GenerateUI(List<Source> sources)
        {
            localSettings.Values["lastSyncTime"] = DateTime.Now.ToString();

            FixIcons();
            
            pvtMain.Items.Clear();

            List<PivotHeader> headers = new List<PivotHeader>();

            try
            {
                foreach (Source source in sources)
                    if (!headers.Any(x => x.Group == source.Group))
                        headers.Add(new PivotHeader() { Group = source.Group, Icon = source.Icon });
            }
            catch (Exception) { }

            foreach (var header in headers)
            {
                PivotItem pivotItemMain = new PivotItem();
                pivotItemMain.Header = header;
                pivotItemMain.Margin = new Thickness(0, -10, 0, 0);

                Pivot pivotSub = new Pivot();
                pivotSub.HeaderTemplate = (DataTemplate)Resources["subPivotTemplate"];
                pivotSub.SelectionChanged += PivotSub_SelectionChanged;

                foreach (Source source in sources)
                {
                    if (header.Group == source.Group)
                    {
                        try
                        {
                            PivotItem pivotItemSub = new PivotItem();
                            pivotItemSub.Header = source.Name;
                            pivotItemSub.Margin = new Thickness(0, -10, 0, 0);

                            ListView listView = new ListView();
                            listView.ItemsSource = source.Articles;
                            listView.ItemTemplate = (DataTemplate)Resources["listItemTemplate"];
                            listView.ItemClick += OpenArticle_ItemClick;
                            listView.SelectionMode = ListViewSelectionMode.Single;
                            listView.IsItemClickEnabled = true;
                            listView.Margin = new Thickness(0, 3, 0, 0);

                            pivotItemSub.Content = listView;
                            pivotSub.Items.Add(pivotItemSub);

                            /*PivotItem pivotItemSub = new PivotItem();
                            pivotItemSub.Header = source.Name;
                            pivotItemSub.Margin = new Thickness(0, -10, 0, 0);

                            ListView listView = new ListView();
                            //listView.ItemsSource = source.Articles;
                            listView.SelectionMode = ListViewSelectionMode.Single;
                            listView.IsItemClickEnabled = true;
                            listView.Margin = new Thickness(0, 3, 0, 0);

                            foreach (Article article in source.Articles)
                            {
                                ListViewItem listViewItem = new ListViewItem();
                                listViewItem.Template = (ControlTemplate)Resources["listItemTemplate"];
                                listViewItem.DataContext = article;
                                //listViewItem.Tapped += OpenArticle_ItemClick;
                                listView.Items.Add(listViewItem);
                            }

                            pivotItemSub.Content = listView;
                            pivotSub.Items.Add(pivotItemSub);*/
                        }
                        catch (Exception) { }
                    }
                }

                //pivotMeta.Add(new PivotMeta() { Name = header.Group, Min = 0, Max = pivotSub.Items.Count() });

                pivotItemMain.Content = pivotSub;
                pvtMain.Items.Add(pivotItemMain);
            }

            FinishLoading();
        }

        private void ListViewItem_Holding(object sender, HoldingRoutedEventArgs e)
        {
            if (e.HoldingState == HoldingState.Started) ShowContextMenu(sender);
        }

        private void ListViewItem_RightClick(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == PointerDeviceType.Mouse) ShowContextMenu(sender);
        }

        private void ShowContextMenu(object sender)
        {
            selectedArticle = (Article)((Grid)sender).DataContext;

            FrameworkElement flyoutBase = sender as FrameworkElement;

            MenuFlyout flyout = new MenuFlyout();

            MenuFlyoutItem flyoutItemShare = new MenuFlyoutItem();
            flyoutItemShare.Text = "Sdílet článek";
            flyoutItemShare.Click += FlyoutItemShare_Click;
            flyout.Items.Add(flyoutItemShare);

            MenuFlyoutItem flyoutItemBrowser = new MenuFlyoutItem();
            flyoutItemBrowser.Text = "Otevřít v internetovém prohlížeči";
            flyoutItemBrowser.Click += FlyoutItemBrowser_Click; ;
            flyout.Items.Add(flyoutItemBrowser);

            flyout.ShowAt(flyoutBase);
        }

        private void FlyoutItemShare_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        private async void FlyoutItemBrowser_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(selectedArticle.Url));
        }

        private void PivotSub_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*pivotMeta = pivotMeta;

            Debug.WriteLine(((PivotItem)((Pivot)sender).Parent).Header);
            Debug.WriteLine(((Pivot)sender).SelectedIndex);*/
        }

        private void FixIcons()
        {
            try
            {
                /*if (Application.Current.RequestedTheme == ApplicationTheme.Dark)*/
                    foreach (var source in sources)
                        source.Icon = source.Icon_Dark;
                /*else
                    foreach (var source in sources)
                        source.Icon = source.Icon_Light;*/
            }
            catch (Exception) { }
        }

        private void OpenArticle_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                selectedItem.IsSelected = false;
            }
            catch (Exception) { }

            ListView selectedList = sender as ListView;
            selectedItem = selectedList.ContainerFromItem(e.ClickedItem) as ListViewItem;

            OpenArticle(e);
        }

        private async void OpenArticle(ItemClickEventArgs e)
        {
            stpArticle.ScrollToVerticalOffset(0);
            articleOpened = true;

            selectedArticle = (Article)e.ClickedItem;

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                StatusBar statusBar = StatusBar.GetForCurrentView();
                await statusBar.HideAsync();
            }

            txtVideoError.Visibility = Visibility.Collapsed;

            if (selectedArticle.Video != "" && selectedArticle.Video != null)
            {
                metArticleVideo.Visibility = Visibility.Visible;
                imgArticleImage.Visibility = Visibility.Collapsed;
                metArticleVideo.Source = new Uri(selectedArticle.Video, UriKind.Absolute);
            }
            else
            {
                if (selectedArticle.Image != "" && selectedArticle.Image != null)
                {
                    metArticleVideo.Visibility = Visibility.Collapsed;
                    imgArticleImage.Visibility = Visibility.Visible;
                    imgArticleImage.Source = new BitmapImage(new Uri(selectedArticle.Image, UriKind.RelativeOrAbsolute));
                }
                else
                {
                    metArticleVideo.Visibility = Visibility.Collapsed;
                    imgArticleImage.Visibility = Visibility.Collapsed;
                }
            }

            txtArticleTitle.Text = selectedArticle.Title;
            txtArticleTitle.FontSize = (double)localSettings.Values["fontSize"] + 8;
            txtArticleDate.FontSize =  (double)localSettings.Values["fontSize"] - 4;
            txtArticleDate.Text = FormatDate(selectedArticle.Date);
            linkArticleUrl.Content = selectedArticle.Url;
            linkArticleUrl.NavigateUri = new Uri(selectedArticle.Url, UriKind.Absolute);

            txtArticleContent.Blocks.Clear();

            MatchCollection paragraphs = Regex.Matches(selectedArticle.Content, @"#(p|headline)#.+?#\/(p|headline)#");

            foreach (var item in paragraphs)
            {
                string value = item.ToString();

                Paragraph paragraph = new Paragraph();
                Run run = new Run();

                if (Regex.IsMatch(value, "#headline#"))
                {
                    paragraph.Foreground = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
                    paragraph.FontSize = (double)localSettings.Values["fontSize"] + 6;
                    paragraph.FontWeight = FontWeights.Normal;
                    paragraph.Margin = new Thickness(0, 20, 0, 5);
                }
                else
                {
                    paragraph.FontSize = (double)localSettings.Values["fontSize"];
                    paragraph.TextAlignment = TextAlignment.Justify;
                    paragraph.Margin = new Thickness(0, 0, 0, 10);
                }

                run.Text = Regex.Replace(value, "#.+?#", "");
                paragraph.Inlines.Add(run);
                txtArticleContent.Blocks.Add(paragraph);
            }

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

            btnDiscussion.Visibility = (string.IsNullOrEmpty(selectedArticle.Discussion_Url)) ? Visibility.Collapsed : Visibility.Visible;

            articleContainer.Visibility = Visibility.Visible;
            cmbArticleActions.Visibility = Visibility.Visible;
        }

        private string FormatDate(string value)
        {
            if (DateTime.Parse(value.ToString()).ToString("d.M.yyyy") == DateTime.Now.ToString("d.M.yyyy"))
            {
                value = "Dnes, " + DateTime.Parse(value.ToString()).ToString("H:mm");
            }
            else if (DateTime.Parse(value.ToString()).ToString("d.M.yyyy") == DateTime.Now.AddDays(-1).ToString("d.M.yyyy"))
            {
                value = "Včera, " + DateTime.Parse(value.ToString()).ToString("H:mm");
            }
            else
            {
                value = DateTime.Parse(value.ToString()).ToString("d.M.yyyy H:mm");
            }

            return value;
        }

        private async void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null) return;

            e.Handled = true;

            if (articleOpened)
            {
                //rootFrame.BackStack.Remove(rootFrame.BackStack.LastOrDefault());
                await CloseArticle();
            }
            else if (rootFrame.CanGoBack)
                rootFrame.GoBack();
        }

        private async Task CloseArticle()
        {
            selectedItem.IsSelected = false;

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                StatusBar statusBar = StatusBar.GetForCurrentView();
                await statusBar.ShowAsync();
            }

            imgArticleImage.Source = null;
            metArticleVideo.Source = null;
            txtArticleTitle.Text = "";
            txtArticleDate.Text = "";
            linkArticleUrl.Content = "";
            linkArticleUrl.NavigateUri = null;

            txtArticleContent.Blocks.Clear();

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            SystemNavigationManager.GetForCurrentView().BackRequested -= OnBackRequested;

            articleContainer.Visibility = Visibility.Collapsed;
            cmbArticleActions.Visibility = Visibility.Collapsed;

            articleOpened = false;
        }

        public async Task<List<Source>> GetArticles(string[] selectedSources)
        {
            List<Source> sources = new List<Source>();
            List<SourceDefinition> sourceDefitinions = GetSourceDefinitions();

            string selectedSourcesTemp = "";

            foreach (var sourceDefitinion in sourceDefitinions)
            {
                if (selectedSources != null)
                {
                    foreach (var selectedSource in selectedSources)
                    {
                        if (sourceDefitinion.Name == selectedSource)
                        {
                            string safeName = NormalizeValue(sourceDefitinion.Name + ".json");

                            await DownloadFile(serverRoot + "articles/" + safeName, "\\Articles\\" + safeName);
                            await DownloadFile(sourceDefitinion.Icon_Dark, "\\Graphics\\" + Regex.Match(sourceDefitinion.Icon_Dark, @"[a-z-_.,]*$"));
                            await DownloadFile(sourceDefitinion.Icon_Light, "\\Graphics\\" + Regex.Match(sourceDefitinion.Icon_Light, @"[a-z-_.,]*$"));

                            sources.Add(ReadArticleFile("\\Articles\\" + safeName));
                        }
                    }
                }
                else
                {
                    string safeName = NormalizeValue(sourceDefitinion.Name + ".json");

                    await DownloadFile(serverRoot + "articles/" + safeName, "\\Articles\\" + safeName);
                    await DownloadFile(sourceDefitinion.Icon_Dark, "\\Graphics\\" + Regex.Match(sourceDefitinion.Icon_Dark, @"[a-z-_.,]*$"));
                    await DownloadFile(sourceDefitinion.Icon_Light, "\\Graphics\\" + Regex.Match(sourceDefitinion.Icon_Light, @"[a-z-_.,]*$"));

                    sources.Add(ReadArticleFile("\\Articles\\" + safeName));

                    selectedSourcesTemp += sourceDefitinion.Name + ";";
                }
            }

            if (selectedSourcesTemp != "")
            {
                selectedSourcesTemp = selectedSourcesTemp.Substring(0, selectedSourcesTemp.Length - 1);

                localSettings.Values["selectedSources"] = selectedSourcesTemp;
            }

            return sources;
        }

        private List<Source> GetArticlesOffline(string[] selectedSources)
        {
            List<Source> articles = new List<Source>();
            List<SourceDefinition> sourceDefitinions = GetSourceDefinitions();

            foreach (var sourceDefitinion in sourceDefitinions)
            {
                foreach (var selectedSource in selectedSources)
                {
                    if (sourceDefitinion.Name == selectedSource)
                    {
                        string articleFileName = NormalizeValue(selectedSource + ".json");

                        articles.Add(ReadArticleFile("\\Articles\\" + articleFileName));
                    }
                }
            }

            return articles;
        }

        public Source ReadArticleFile(string articlesFile)
        {
            Stream stream = File.OpenRead(localFolder + articlesFile);

            string json;

            using (StreamReader streamReader = new StreamReader(stream))
            {
                json = streamReader.ReadToEnd();
            }

            Source source = JsonConvert.DeserializeObject<Source>(json);

            /*for (int i = 0; i < source.Articles.Count; i++)
            {
                Article article = source.Articles[i];

                if (article.Title == "" || article.Title == null || article.Content == "" || article.Content == null)
                {
                    source.Articles.RemoveAt(i);
                }
            }*/

            return source;
        }

        public List<SourceDefinition> GetSourceDefinitions()
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

        public async Task DownloadFile(string urlOnline, string urlLocal)
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

            if (!Directory.Exists(localFolder + "//Articles")) Directory.CreateDirectory(localFolder + "//Articles");
            if (!Directory.Exists(localFolder + "//Graphics")) Directory.CreateDirectory(localFolder + "//Graphics");

            if (File.Exists(urlLocal) && IsStableConnectivityAvailable()) File.Delete(urlLocal);

            FileStream fs = new FileStream(urlLocal, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(content);

            fs.Dispose();
            bw.Dispose();
        }

        private void navToggle_Click(object sender, RoutedEventArgs e)
        {
            spvMainView.IsPaneOpen = !spvMainView.IsPaneOpen;
        }

        private void btnSetSources_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(SetSourcesPage));
        }

        private void btnHelpTips_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(HelpTipsPage));
        }

        private void btnSettings_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPage));
        }

        private async void btnDiscussion_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(selectedArticle.Discussion_Url));
        }

        private async void btnOpenBrowser_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(selectedArticle.Url));
        }

        private void btnShare_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequestDeferral deferral = args.Request.GetDeferral();

            args.Request.Data.Properties.Title = selectedArticle.Title;
            args.Request.Data.Properties.ContentSourceWebLink = new Uri(selectedArticle.Url, UriKind.Absolute);
            args.Request.Data.SetUri(new Uri(selectedArticle.Url, UriKind.Absolute));
            args.Request.Data.SetHtmlFormat(selectedArticle.Title + "</a> (sdíleno z aplikace <a href='" + "www.xy.cz" + "'>" + "České zpravodajství</a> pro Windows 10)");
            args.Request.Data.SetText(selectedArticle.Title + " (sdíleno z aplikace České zpravodajství pro Windows 10)");

            deferral.Complete();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            spvMainView.IsPaneOpen = false;
        }

        private async void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            pageDimensions = new double[] { e.NewSize.Width, e.NewSize.Height };

            portraitOrientation = !(e.NewSize.Width > e.NewSize.Height);

            if (portraitOrientation)
            {
                spvMainView.Width = pageDimensions[0];
                spvMainView.HorizontalAlignment = HorizontalAlignment.Stretch;
                spvMainView.Padding = new Thickness(0, 0, 0, 0);
                articleContainer.Width = pageDimensions[0];
                articleContainer.HorizontalAlignment = HorizontalAlignment.Stretch;

                if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
                {
                    StatusBar statusBar = StatusBar.GetForCurrentView();
                    statusBar.BackgroundOpacity = 1;
                    statusBar.BackgroundColor = (Color)Resources["SystemAccentColor"];
                    statusBar.ForegroundColor = (Color)Resources["SystemChromeWhiteColor"];
                    await statusBar.ShowAsync();
                }

                txtHeader.Text = "ČESKÉ ZPRAVODAJSTVÍ";
                btnInfo.Visibility = Visibility.Visible;
            }
            else
            {
                spvMainView.Width = pageDimensions[0] / 2.5;
                spvMainView.HorizontalAlignment = HorizontalAlignment.Left;
                spvMainView.Padding = new Thickness(0, 0, 20, 0);
                articleContainer.Width = pageDimensions[0] - spvMainView.Width;
                articleContainer.HorizontalAlignment = HorizontalAlignment.Right;

                if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
                {
                    StatusBar statusBar = StatusBar.GetForCurrentView();
                    await statusBar.HideAsync();
                }

                txtHeader.Text = "ZPRAVODAJSTVÍ";
                btnInfo.Visibility = Visibility.Collapsed;
            }

            spvMainView.OpenPaneLength = spvMainView.Width;
            stpNavTop.Width = spvMainView.Width;
            stpNavBottom.Width = spvMainView.Width;
        }

        private void BeginLoading()
        {
            IconRotation.Begin();

            pgrLoading.IsActive = true;
            pvtMain.IsEnabled = false;
            rtpLoading.Visibility = Visibility.Visible;
        }

        private void FinishLoading()
        {
            rtpLoading.Visibility = Visibility.Collapsed;
            pgrLoading.IsActive = false;
            pvtMain.IsEnabled = true;

            IconRotation.Stop();
        }

        private async void InitBackgroundTasks()
        {
            await BackgroundExecutionManager.RequestAccessAsync();
            BackgroundAccessStatus accessStatus = BackgroundExecutionManager.GetAccessStatus();

            if (accessStatus == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity ||
                accessStatus == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity)
            {
                var existingTask = BackgroundTaskRegistration.AllTasks
                    .Where(x => x.Value.Name
                    .Equals(nameof(BackgroundSync)))
                    .Select(x => x.Value).FirstOrDefault();

                if (existingTask != null)
                    existingTask.Unregister(false);

                BackgroundTaskBuilder task = new BackgroundTaskBuilder
                {
                    Name = nameof(BackgroundSync),
                    CancelOnConditionLoss = false,
                    TaskEntryPoint = typeof(BackgroundSync).ToString()
                };

#if DEBUG
                task.SetTrigger(new SystemTrigger(SystemTriggerType.PowerStateChange, false));
#else
                task.SetTrigger(new TimeTrigger(15, false));
#endif
                task.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));

                task.Register();
            }
        }

        private string NormalizeValue(string value)
        {
            value = Regex.Replace(Regex.Replace(value, @"\s+", "-"), @"-+", "-").ToLower();

            value = value.Normalize(NormalizationForm.FormD);
            var chars = value.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            value = new string(chars).Normalize(NormalizationForm.FormC);

            return value;
        }

        private void stpArticle_ViewChanged()
        {
            rctReadingProgress.Width = rctReadingProgressCont.Width;

            double max = stpArticle.ScrollableHeight;
            double scrolled = stpArticle.VerticalOffset;
            double scrolledPerc = scrolled * 100 / max;
            double scrolledPx = rctReadingProgressCont.ActualWidth / 100 * scrolledPerc;

            rctReadingProgress.Width = scrolledPx;
        }

        private void stpArticle_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            stpArticle_ViewChanged();
        }

        private void stpArticle_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            stpArticle_ViewChanged();
        }

        private void btnDonate_Tapped(object sender, TappedRoutedEventArgs e)
        {
            DonateRequest();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadNewArticles();
        }

        private void btnAbout_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }

        private async void DonateRequest()
        {
            try
            {
                await CurrentApp.RequestProductPurchaseAsync("Příspěvek", false);
                await new MessageDialog("Mnohokrát Vám děkuji za pomoc!", "Příspěvek").ShowAsync();
            }
            catch (Exception)
            {
                await new MessageDialog("Nastal nějaký problém s odesláním příspěvku.", "Příspěvek").ShowAsync();
            }
        }

        public bool IsStableConnectivityAvailable()
        {
            ConnectionProfile connections = NetworkInformation.GetInternetConnectionProfile();
            bool available = connections != null && connections.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
            return available;

            /*try
            {
                ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();

                if (profile.IsWwanConnectionProfile)
                {
                    WwanDataClass connectionClass = profile.WwanConnectionProfileDetails.GetCurrentDataClass();
                    switch (connectionClass)
                    {
                        case WwanDataClass.Cdma1xEvdo:
                        case WwanDataClass.Cdma1xEvdoRevA:
                        case WwanDataClass.Cdma1xEvdoRevB:
                        case WwanDataClass.Cdma1xEvdv:
                        case WwanDataClass.Cdma1xRtt:
                        case WwanDataClass.Cdma3xRtt:
                        case WwanDataClass.CdmaUmb:
                        case WwanDataClass.Umts:
                        case WwanDataClass.Hsdpa:
                        case WwanDataClass.Hsupa:
                        case WwanDataClass.LteAdvanced:
                            return true;
                        default:
                            return false;
                    }
                }
                else if (profile.IsWlanConnectionProfile)
                    return true;

                return false;
            }
            catch (Exception)
            {
                return false;
            }*/
        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Image image = (Image)sender;
            image.Visibility = Visibility.Collapsed;
        }

        private void metArticleVideo_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MediaElement video = (MediaElement)sender;
            video.Visibility = Visibility.Collapsed;

            txtVideoError.Visibility = Visibility.Visible;
        }

        private void btnInfoClose_Click(object sender, RoutedEventArgs e)
        {
            btnInfo.Flyout.Hide();
        }
    }
}
