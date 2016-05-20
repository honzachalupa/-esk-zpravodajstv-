using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
namespace CeskeZpravodajstvi.Pages
{
    public sealed partial class SettingsPage : Page
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public SettingsPage()
        {
            InitializeComponent();

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += BackButton_Click;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if ((string)localSettings.Values["colorMode"] == "Tmavé")
                cmbColorMode.SelectedIndex = 1;
            else if ((string)localSettings.Values["colorMode"] == "Světlé")
                cmbColorMode.SelectedIndex = 2;
            else
                cmbColorMode.SelectedIndex = 0;

            sldFontSize.Value = (double)localSettings.Values["fontSize"];
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["colorMode"] = ((ComboBoxItem)cmbColorMode.SelectedValue).Content;
            localSettings.Values["fontSize"] = sldFontSize.Value;

            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.GoBack();
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
    }
}
