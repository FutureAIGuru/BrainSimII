using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Windows;

namespace BrainSimulator
{
    public partial class Help : Window
    {
        private const string HelpHost = "brainsimulator-help.invalid";
        private string requestedPage;
        private bool closed;

        public Help(string page = "GettingStarted0.html")
        {
            InitializeComponent();
            requestedPage = page;
        }

        public void NavigateTo(string page)
        {
            requestedPage = page;
            if (theBrowser.CoreWebView2 != null)
                theBrowser.CoreWebView2.Navigate($"https://{HelpHost}/{page}");
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string helpFolder = Path.Combine(AppContext.BaseDirectory, "Help");
                if (!File.Exists(Path.Combine(helpFolder, requestedPage)))
                    throw new FileNotFoundException("The local help files are missing.", requestedPage);

                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Future AI Society", "Brain Simulator II", "WebView2");
                var environment = await CoreWebView2Environment.CreateAsync(
                    userDataFolder: userDataFolder);
                if (closed) return;

                await theBrowser.EnsureCoreWebView2Async(environment);
                if (closed) return;

                var browser = theBrowser.CoreWebView2;
                browser.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Auto;
                browser.SetVirtualHostNameToFolderMapping(
                    HelpHost, helpFolder, CoreWebView2HostResourceAccessKind.Deny);
                browser.NavigationStarting += Browser_NavigationStarting;
                browser.NewWindowRequested += Browser_NewWindowRequested;
                browser.HistoryChanged += Browser_HistoryChanged;
                browser.NavigationCompleted += Browser_NavigationCompleted;
                NavigateTo(requestedPage);
            }
            catch (WebView2RuntimeNotFoundException)
            {
                if (closed) return;
                theBrowser.Visibility = Visibility.Collapsed;
                statusText.Text = "Help needs the Microsoft Edge WebView2 Runtime. Install it, then reopen Help.";
                runtimeButton.Visibility = Visibility.Visible;
            }
            catch (Exception exception)
            {
                if (closed) return;
                theBrowser.Visibility = Visibility.Collapsed;
                statusText.Text = "Help could not be opened. " + exception.Message;
            }
        }

        private static bool IsHelpUri(string address)
        {
            return Uri.TryCreate(address, UriKind.Absolute, out Uri uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && uri.Host == HelpHost
                && uri.IsDefaultPort;
        }

        private static void OpenExternalLink(string address)
        {
            if (Uri.TryCreate(address, UriKind.Absolute, out Uri uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
                MainWindow.OpenApp(address);
        }

        private void Browser_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (IsHelpUri(e.Uri)) return;
            e.Cancel = true;
            OpenExternalLink(e.Uri);
        }

        private void Browser_NewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            if (IsHelpUri(e.Uri))
                theBrowser.CoreWebView2.Navigate(e.Uri);
            else
                OpenExternalLink(e.Uri);
        }

        private void Browser_HistoryChanged(object sender, object e)
        {
            backButton.IsEnabled = theBrowser.CanGoBack;
            forwardButton.IsEnabled = theBrowser.CanGoForward;
        }

        private void Browser_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled) return;
            statusPanel.Visibility = e.IsSuccess ? Visibility.Collapsed : Visibility.Visible;
            statusText.Text = e.IsSuccess ? "" : "This help page could not be loaded. Choose Getting Started or Contents to try again.";
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (theBrowser.CanGoBack) theBrowser.GoBack();
        }

        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            if (theBrowser.CanGoForward) theBrowser.GoForward();
        }

        private void GettingStarted_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("GettingStarted0.html");
        }

        private void Contents_Click(object sender, RoutedEventArgs e)
        {
            NavigateTo("UI.html");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Runtime_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.OpenApp("https://developer.microsoft.com/microsoft-edge/webview2/#download");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            closed = true;
            theBrowser.Dispose();
        }
    }
}