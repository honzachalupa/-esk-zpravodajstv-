using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CeskeZpravodajstvi.Pages
{
    public sealed partial class HelpTipsPage : Page
    {
        public HelpTipsPage()
        {
            this.InitializeComponent();

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += BackButton_Click;
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
    }
}
