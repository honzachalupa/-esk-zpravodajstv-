using BackgroundTask.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CeskeZpravodajstvi.Pages
{
    public sealed partial class SetSourcesPage : Page
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        string localFolder = ApplicationData.Current.LocalFolder.Path;

        public SetSourcesPage()
        {
            InitializeComponent();

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += BackButton_Click;

            List<SourceDefinition> sourceDefitinions = GetSourceDefinitions();

            GenerateUI(sourceDefitinions);
        }

        public List<SourceDefinition> GetSourceDefinitions()
        {
            Stream stream = File.OpenRead(localFolder + "\\sources.json");

            string json;

            using (StreamReader streamReader = new StreamReader(stream))
            {
                json = streamReader.ReadToEnd();
            }

            json = Regex.Replace(json, @"(\\r\\n|\s\s)", "");

            return JsonConvert.DeserializeObject<List<SourceDefinition>>(json);
        }

        private void GenerateUI(List<SourceDefinition> sourceDefitinions)
        {
            stpSourcesList.Children.Clear();
            List<string> groups = new List<string>();

            foreach (var source in sourceDefitinions)
            {
                if (!groups.Contains(source.Group))
                    groups.Add(source.Group);
            }

            foreach (var group in groups)
            {
                TextBlock label = new TextBlock();
                label.Text = group;
                label.FontSize = 20;
                stpSourcesList.Children.Add(label);

                foreach (var source in sourceDefitinions)
                {
                    if (group == source.Group)
                    {
                        CheckBox checkBox = new CheckBox();
                        checkBox.Content = source.Name;

                        stpSourcesList.Children.Add(checkBox);
                    }
                }
            }

            string selectedSourcesTemp = (string)localSettings.Values["selectedSources"];
            string[] selectedSources;

            try
            {
                selectedSources = selectedSourcesTemp.Split(";".ToCharArray());
            }
            catch (Exception)
            {
                selectedSources = null;
            }

            foreach (var item in stpSourcesList.Children)
            {
                if (item.GetType().ToString() == "Windows.UI.Xaml.Controls.CheckBox")
                {
                    if (selectedSources != null)
                    {
                        foreach (var source in selectedSources)
                        {
                            if (((CheckBox)item).Content as string == source)
                            {
                                ((CheckBox)item).IsChecked = true;
                            }
                        }
                    }
                    else
                    {
                        ((CheckBox)item).IsChecked = true;
                    }
                }
            }
        }

        private void btnRequestSource_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RequestSourcePage));
        }

        private void BackButton_Click(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame == null)
                return;

            if (rootFrame.CanGoBack && e.Handled == false)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }

        private void SaveChanges()
        {
            string selectedSourcesTemp = "";

            foreach (var item in stpSourcesList.Children)
            {
                if (item.GetType().ToString() == "Windows.UI.Xaml.Controls.CheckBox")
                {
                    if (((CheckBox)item).IsChecked == true)
                    {
                        selectedSourcesTemp += ((CheckBox)item).Content as string + ";";
                    }
                }
            }

            selectedSourcesTemp = selectedSourcesTemp.Substring(0, selectedSourcesTemp.Length - 1);

            localSettings.Values["selectedSources"] = selectedSourcesTemp;

            Frame.Navigate(typeof(MainPage));
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveChanges();

            Frame.BackStack.RemoveAt(Frame.BackStackDepth - 1);
            Frame.GoBack();
        }
    }
}
