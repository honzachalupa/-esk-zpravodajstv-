using BackgroundTask.Model;
using NotificationsExtensions.Tiles;
using NotificationsExtensions.Toasts;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace BackgroundTasks
{
    class Notification
    {
        public static void CreateToastNotification(string articleTitle, string articleContent, string articleImage)
        {
            ToastContent content = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    TitleText = new ToastText()
                    {
                        Text = articleTitle
                    },
                    BodyTextLine1 = new ToastText()
                    {
                        Text = articleContent
                    },
                    AppLogoOverride = new ToastAppLogo()
                    {
                        Source = new ToastImageSource(articleImage)
                    }
                }
            };

            ToastNotification notification = new ToastNotification(content.GetXml());
            ToastNotificationManager.CreateToastNotifier().Show(notification);
        }

        public static void CreateTiles(List<Article> articles)
        {
            string imageUrl1 = "";
            string imageUrl2 = "";

            for (int i = 0; i < articles.Count; i++)
            {
                if (articles[i].Image != "")
                {
                    imageUrl1 = articles[i].Image;
                    break;
                }
            }

            for (int i = 0; i < articles.Count; i++)
            {
                if (articles[i].Image != "" && articles[i].Image != imageUrl1)
                {
                    imageUrl2 = articles[i].Image;
                    break;
                }
            }

            TileBindingContentAdaptive bindingContent = new TileBindingContentAdaptive()
            {
                BackgroundImage = new TileBackgroundImage()
                {
                    Source = new TileImageSource(imageUrl1),
                    Overlay = 60
                },

                PeekImage = new TilePeekImage()
                {
                    Source = new TileImageSource(imageUrl2)
                },

                Children =
                    {
                        CreateGroup(
                            from: articles[0].Title.Substring(0, 30),
                            body: articles[0].Content.Substring(0, 60)),

                        new TileText(),

                        CreateGroup(
                            from: articles[1].Title.Substring(0, 30),
                            body: articles[1].Content.Substring(0, 60)),

                        new TileText(),

                        CreateGroup(
                            from: articles[2].Title.Substring(0, 30),
                            body: articles[2].Content.Substring(0, 60)),

                        new TileText(),

                        CreateGroup(
                            from: articles[3].Title.Substring(0, 30),
                            body: articles[3].Content.Substring(0, 60))
                    }
            };

            TileBinding binding = new TileBinding()
            {
                Branding = TileBranding.NameAndLogo,
                Content = bindingContent
            };

            TileContent content = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = binding,
                    TileWide = binding,
                    TileLarge = binding
                }
            };

            TileNotification notification = new TileNotification(content.GetXml());
            TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
        }

        private static TileGroup CreateGroup(string from, string body)
        {
            return new TileGroup()
            {
                Children =
                {
                    new TileSubgroup()
                    {
                        Children =
                        {
                            new TileText()
                            {
                                Text = from,
                                Style = TileTextStyle.Base
                            },

                            new TileText()
                            {
                                Text = body,
                                Style = TileTextStyle.Caption
                            }
                        }
                    }
                }
            };
        }
    }
}