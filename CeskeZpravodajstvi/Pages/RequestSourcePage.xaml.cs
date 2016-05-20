using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CeskeZpravodajstvi.Pages
{
    public sealed partial class RequestSourcePage : Page
    {
        public RequestSourcePage()
        {
            this.InitializeComponent();

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += BackButton_Click;

            string color = (Application.Current.RequestedTheme == ApplicationTheme.Dark) ? color = "dark" : color = "light";

            wbvExternalSite.Source = new Uri(@"http://www.honzachalupa.cz/windows-apps/ceske-zpravodajstvi-hlasovani.html?theme=" + color, UriKind.Absolute);
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
