﻿using InteropTools.CorePages;
using InteropTools.Providers;
using InteropTools.RemoteClasses.Server;
using InteropTools.Resources;
using Microsoft.HockeyApp;
using Microsoft.Services.Store.Engagement;
using Microsoft.Toolkit.Uwp.Notifications;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.System.Display;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Shell = InteropTools.CorePages.Shell;

namespace InteropTools
{
    public sealed partial class App : Application
    {
        public static readonly TextResources textResources = new();

        public static readonly string RemoteLoc = ResourceManager.Current.MainResourceMap.GetValue(
              "Resources/The_following_Remote_device_wants_to_access_your_phone_Registry",
              ResourceContext.GetForCurrentView()).ValueAsString;
        public static readonly string RemoteAllowLoc = ResourceManager.Current.MainResourceMap.GetValue("Resources/Allow", ResourceContext.GetForCurrentView()).ValueAsString;
        public static readonly string RemoteDenyLoc = ResourceManager.Current.MainResourceMap.GetValue("Resources/Deny", ResourceContext.GetForCurrentView()).ValueAsString;

        public static readonly ObservableRangeCollection<Session> Sessions = new();

        public static bool Fancyness = false;

        public static int? CurrentSession;

        public static IRegistryProvider RegistryHelper
        {
            get => CurrentSession != null ? Sessions[(int)CurrentSession].Helper : null;

            set
            {
                if (CurrentSession != null)
                {
                    Sessions[(int)CurrentSession].Helper = value;
                }
            }
        }

        public static IRegistryProvider MainRegistryHelper = new MainRegistryProvider();

        public static readonly DisplayRequest DisplayRequest = new();

        // External stuff
        public static readonly RemoteServer Server = new();

        private static readonly Random Random = new();
        public static readonly string SessionId = RandomString(10);
        public static readonly List<Remote> AllowedRemotes = new();
        public static readonly List<Remote> DeniedRemotes = new();
        // End of external stuff

        private static readonly Rect bounds = ApplicationView.GetForCurrentView().VisibleBounds;
        private static readonly double scaleFactor = DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
        public static readonly Size size = new(bounds.Width * scaleFactor, bounds.Height * scaleFactor);

        public App()
        {
            UnhandledException += async (s, args) =>
            {
                args.Handled = true;

                try
                {
                    await new MessageDialog(args.Exception?.ToString() ?? string.Empty, "Unhandled exception").ShowAsync();
                }
                catch { }
            };

            HockeyClient.Current.Configure("8f0c0303cc9b40648d6f3962bbac2b40", new TelemetryConfiguration()
            {
                Collectors = WindowsCollectors.Metadata | WindowsCollectors.Session | WindowsCollectors.UnhandledException,
                EnableDiagnostics = true
            });

            InitializeComponent();
        }

        public static SshClient SshClient { get; set; }

        public static UIElement AppContent
        {
            get
            {
                if (Window.Current.Content == null)
                {
                    Window.Current.Content = new CoreFrame();
                }

                CoreFrame frame = Window.Current.Content as CoreFrame;
                return frame.FrameContent;
            }

            set
            {
                if (Window.Current.Content == null)
                {
                    Window.Current.Content = new CoreFrame();
                }

                CoreFrame frame = Window.Current.Content as CoreFrame;
                frame.FrameContent = value;
            }
        }
        public static async Task<bool> IsCMDSupported()
        {
            IRegistryProvider helper = MainRegistryHelper;
            RegTypes regtype;
            string regvalue;
            GetKeyValueReturn ret = await helper.GetKeyValue(RegHives.HKEY_LOCAL_MACHINE, @"SYSTEM\ControlSet001\Services\MpsSvc", "Start", RegTypes.REG_DWORD); _ = ret.regtype; _ = ret.regvalue;

            //if (regvalue != "2") return false;

            if ((SshClient != null) && (SshClient.IsConnected))
            {
                return true;
            }

            await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, @"system\CurrentControlSet\control\ssh", "default-shell",
                               RegTypes.REG_SZ, @"%SystemRoot%\system32\cmd.exe");
            await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, @"system\CurrentControlSet\control\ssh", "default-env",
                               RegTypes.REG_SZ, "currentdir,async,autoexec");
            ret = await helper.GetKeyValue(RegHives.HKEY_LOCAL_MACHINE, @"system\CurrentControlSet\control\ssh", "user-list",
                               RegTypes.REG_SZ); _ = ret.regtype; regvalue = ret.regvalue;

            if ((regvalue == null) || (regvalue == ""))
            {
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, @"system\CurrentControlSet\control\ssh", "user-list",
                                   RegTypes.REG_SZ, "Sirepuser");
            }

            bool add
                  = true;

            string username = "InteropTools";
            ret = await helper.GetKeyValue(RegHives.HKEY_LOCAL_MACHINE, @"system\CurrentControlSet\control\ssh", "user-list",
                               RegTypes.REG_SZ); _ = ret.regtype; regvalue = ret.regvalue;

            if (regvalue.Contains(";"))
            {
                foreach (string user in regvalue.Split(';'))
                {
                    if (user.ToLower() == username.ToLower())
                    {
                        add
                              = false;
                    }
                }
            }

            else
            {
                if (regvalue.ToLower() == username.ToLower())
                {
                    add
                          = false;
                }
            }

            if (add)
            {
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, @"system\CurrentControlSet\control\ssh", "user-list",
                                   RegTypes.REG_SZ, regvalue + ";" + username);
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, @"system\CurrentControlSet\control\ssh\" + username,
                                   "user-name", RegTypes.REG_SZ, "LocalSystem");
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, @"system\CurrentControlSet\control\ssh\" + username,
                                   "auth-method", RegTypes.REG_SZ, "password");
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, @"system\CurrentControlSet\control\ssh\" + username,
                                   "user-pin", RegTypes.REG_SZ, SessionId);
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, @"system\CurrentControlSet\control\ssh\" + username,
                                   "subsystems", RegTypes.REG_SZ, "default,sftp");
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, @"system\CurrentControlSet\control\ssh\" + username,
                                   "default-home-dir", RegTypes.REG_SZ, @"%SystemRoot%\system32\");
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, "System\\Currentcontrolset\\control\\ssh\\" + username,
                                   "default-shell", RegTypes.REG_SZ, @"%SystemRoot%\system32\cmd.exe");
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, "System\\Currentcontrolset\\control\\ssh\\" + username,
                                   "sftp-home-dir", RegTypes.REG_SZ, "C:\\");
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, "System\\Currentcontrolset\\control\\ssh\\" + username,
                                   "sftp-mkdir-rex", RegTypes.REG_SZ, ".*");
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, "System\\Currentcontrolset\\control\\ssh\\" + username,
                                   "sftp-open-dir-rex", RegTypes.REG_SZ, ".*");
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, "System\\Currentcontrolset\\control\\ssh\\" + username,
                                   "sftp-read-file-rex", RegTypes.REG_SZ, ".*");
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, "System\\Currentcontrolset\\control\\ssh\\" + username,
                                   "sftp-remove-file-rex", RegTypes.REG_SZ, ".*");
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, "System\\Currentcontrolset\\control\\ssh\\" + username,
                                   "sftp-rmdir-rex", RegTypes.REG_SZ, ".*");
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, "System\\Currentcontrolset\\control\\ssh\\" + username,
                                   "sftp-stat-rex", RegTypes.REG_SZ, ".*");
                await helper.SetKeyValue(RegHives.HKEY_LOCAL_MACHINE, "System\\Currentcontrolset\\control\\ssh\\" + username,
                                   "sftp-write-file-rex", RegTypes.REG_SZ, ".*");
            }

            ret = await helper.GetKeyValue(RegHives.HKEY_LOCAL_MACHINE, @"system\CurrentControlSet\control\ssh\" + username,
                               "user-pin", RegTypes.REG_SZ); regtype = ret.regtype; regvalue = ret.regvalue;

            try
            {
                string Server = helper.GetHostName();
                string Username = "InteropTools";
                string Password = regvalue;
                PasswordConnectionInfo coninfo = new(Server, Username, Password)
                {
                    Timeout = new TimeSpan(0, 0, 5),
                    RetryAttempts = 1
                };
                SftpClient sclient = new(coninfo)
                {
                    OperationTimeout = new TimeSpan(0, 0, 5)
                };
                sclient.Connect();
                sclient.BufferSize = 4 * 1024;
                IAsyncOperation<StorageFile> op = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///SSH//cmd.exe", UriKind.Absolute));

                while (op.Status == AsyncStatus.Started)
                {
                }

                Task<Stream> op2 = op.GetResults().OpenStreamForReadAsync();

                while (op2.Status == TaskStatus.Running)
                {
                }

                Stream cmd = op2.Result;
                op =
                  StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///SSH//en-US//cmd.exe.mui",
                      UriKind.Absolute));

                while (op.Status == AsyncStatus.Started)
                {
                }

                op2 = op.GetResults().OpenStreamForReadAsync();

                while (op2.Status == TaskStatus.Running)
                {
                }

                Stream cmdmui = op2.Result;
                op = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///SSH//reg.exe", UriKind.Absolute));

                while (op.Status == AsyncStatus.Started)
                {
                }

                op2 = op.GetResults().OpenStreamForReadAsync();

                while (op2.Status == TaskStatus.Running)
                {
                }

                Stream reg = op2.Result;
                op =
                  StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///SSH//en-US//reg.exe.mui",
                      UriKind.Absolute));

                while (op.Status == AsyncStatus.Started)
                {
                }

                op2 = op.GetResults().OpenStreamForReadAsync();

                while (op2.Status == TaskStatus.Running)
                {
                }

                Stream regmui = op2.Result;
                op =
                  StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///SSH//CheckNetIsolation.exe",
                      UriKind.Absolute));

                while (op.Status == AsyncStatus.Started)
                {
                }

                op2 = op.GetResults().OpenStreamForReadAsync();

                while (op2.Status == TaskStatus.Running)
                {
                }

                Stream netisol = op2.Result;
                op =
                  StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///SSH//en-US//CheckNetIsolation.exe.mui",
                      UriKind.Absolute));

                while (op.Status == AsyncStatus.Started)
                {
                }

                op2 = op.GetResults().OpenStreamForReadAsync();

                while (op2.Status == TaskStatus.Running)
                {
                }

                Stream netisolmui = op2.Result;
                sclient.UploadFile(cmd, "/C/Windows/System32/cmd.exe");
                sclient.UploadFile(cmdmui, "/C/Windows/System32/en-US/cmd.exe.mui");
                sclient.UploadFile(reg, "/C/Windows/System32/reg.exe");
                sclient.UploadFile(regmui, "/C/Windows/System32/en-US/reg.exe.mui");
                sclient.UploadFile(netisol, "/C/Windows/System32/CheckNetIsolation.exe");
                sclient.UploadFile(netisolmui, "/C/Windows/System32/en-US/CheckNetIsolation.exe.mui");
                sclient.Disconnect();
                SshClient = new SshClient(coninfo);
                SshClient.Connect();
                SshClient.KeepAliveInterval = new TimeSpan(0, 0, 10);
                return true;
            }

            catch
            {
                SshClient = null;
                return false;
            }
        }

        public static void AddNewSession(object args)
        {
            Session session = new()
            {
                Helper = null,
                WindowContent = new Shell(args),//new SelectProviderPage(args),
                CreationDate = DateTime.Now
            };
            Sessions.Add(session);
            SwitchSession(session);
        }

        public static async void SwitchSession(Session session)
        {
            if (CurrentSession != null)
            {
                Sessions[(int)CurrentSession].WindowContent = AppContent;
                RenderTargetBitmap renderTargetBitmap = new();
                await renderTargetBitmap.RenderAsync(Sessions[(int)CurrentSession].WindowContent);
                Sessions[(int)CurrentSession].Preview = renderTargetBitmap;
            }

            if (session.WindowContent is Shell)
            {
                Shell shell = (Shell)session.WindowContent;
                AppViewBackButtonVisibility visibility = AppViewBackButtonVisibility.Collapsed;

                if (shell.RootFrame.CanGoBack)
                {
                    visibility = AppViewBackButtonVisibility.Visible;
                }

                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = visibility;
            }

            else
            {
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                  AppViewBackButtonVisibility.Collapsed;
            }

            CurrentSession = Sessions.IndexOf(session);
            AppContent = session.WindowContent;
            RenderTargetBitmap renderTargetBitmap_ = new();
            await renderTargetBitmap_.RenderAsync(Sessions[(int)CurrentSession].WindowContent);
            Sessions[(int)CurrentSession].Preview = renderTargetBitmap_;
            Window.Current.Activate();

            if (AppContent is Shell)
            {
                Shell shell = (Shell)session.WindowContent;
                shell.ReSetupTitlebar();
            }
        }

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        protected override async void OnFileActivated(FileActivatedEventArgs args)
        {
            // TODO: Handle file activation
            // The number of files received is args.Files.Size
            // The name of the first file is args.Files[0].Name
            StorageFile file = args.Files[0] as StorageFile;
            RefreshTile();

            if (CurrentSession == null)
            {
                //  Display an extended splash screen if app was not previously running.
                if (args.PreviousExecutionState != ApplicationExecutionState.Running)
                {
                    bool loadState = (args.PreviousExecutionState == ApplicationExecutionState.Terminated);
                    ExtendedSplashScreen extendedSplash = new(args.SplashScreen, loadState, file);
                    AppContent = extendedSplash;
                    Window.Current.Activate();
                }

                else
                {
                    AddNewSession(file);
                }
            }

            else if (AppContent as Shell == null)
            {
                //  Display an extended splash screen if app was not previously running.
                if (args.PreviousExecutionState != ApplicationExecutionState.Running)
                {
                    bool loadState = (args.PreviousExecutionState == ApplicationExecutionState.Terminated);
                    ExtendedSplashScreen extendedSplash = new(args.SplashScreen, loadState, file);
                    AppContent = extendedSplash;
                    Window.Current.Activate();
                }

                else
                {
                    AddNewSession(file);
                }
            }

            else
            {
                Shell currentContent = AppContent as Shell;
                await currentContent.HandleFileActivatedEvent(file);
            }
        }

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Protocol)
            {
                base.OnActivated(args);

                try
                {
                    if (args is ToastNotificationActivatedEventArgs)
                    {
                        ToastNotificationActivatedEventArgs toastActivationArgs = args as ToastNotificationActivatedEventArgs;

                        StoreServicesEngagementManager engagementManager = StoreServicesEngagementManager.GetDefault();
                        string originalArgs = engagementManager.ParseArgumentsAndTrackAppLaunch(
                            toastActivationArgs.Argument);

                        // Use the originalArgs variable to access the original arguments
                        // that were passed to the app.
                    }
                }
                catch
                {

                }

                ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;

                if (!ApiInformation.IsMethodPresent("Windows.UI.Composition.Compositor", "CreateHostBackdropBrush"))
                {
                    Resources.MergedDictionaries.Add
                    (
                        new ResourceDictionary { Source = new Uri("ms-appx:///Themes/rs2_neon.xaml") }
                    );
                }
                else
                {
                    Resources.MergedDictionaries.Add
                    (
                        new ResourceDictionary { Source = new Uri("ms-appx:///Themes/rs1_neon.xaml") }
                    );
                }

                try
                {
                    StoreServicesEngagementManager engagementManager = StoreServicesEngagementManager.GetDefault();
                    await engagementManager.RegisterNotificationChannelAsync();
                }
                catch
                {

                }


#if DEBUG
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    // disabled, obscures the hamburger button, enable if you need it
                    DebugSettings.EnableFrameRateCounter = true;
                }
#endif

                RefreshTile();

                if (CurrentSession == null)
                {
                    //  Display an extended splash screen if app was not previously running.
                    if (eventArgs.PreviousExecutionState != ApplicationExecutionState.Running)
                    {
                        bool loadState = (eventArgs.PreviousExecutionState == ApplicationExecutionState.Terminated);
                        ExtendedSplashScreen extendedSplash = new(eventArgs.SplashScreen, loadState, "");
                        AppContent = extendedSplash;
                        Window.Current.Activate();
                    }

                    else
                    {
                        AddNewSession("");
                    }
                }
            }
            else if (args.Kind == ActivationKind.ProtocolForResults && Window.Current.Content == null && ((ProtocolForResultsActivatedEventArgs)args).Uri.AbsoluteUri.Contains("interoptools-appextensionregistrar"))
            {
                // Setup temp frame, ask for provider, show dialog saying hey do you want to allow app, and close / suspend.
                // Also save the app pfn that asked.

                ProtocolForResultsActivatedEventArgs protocolForResultsArgs = (ProtocolForResultsActivatedEventArgs)args;

                // Window management
                if (Window.Current.Content is not Frame rootFrame)
                {
                    rootFrame = new Frame();
                    Window.Current.Content = rootFrame;
                }

                // Open the page that we created to handle activation for results.
                rootFrame.Navigate(typeof(LaunchedForResultsPage), protocolForResultsArgs);

                // Ensure the current window is active.
                Window.Current.Activate();
            }
            else
            {
                base.OnActivated(args);

                try
                {
                    if (args is ToastNotificationActivatedEventArgs)
                    {
                        ToastNotificationActivatedEventArgs toastActivationArgs = args as ToastNotificationActivatedEventArgs;

                        StoreServicesEngagementManager engagementManager = StoreServicesEngagementManager.GetDefault();
                        string originalArgs = engagementManager.ParseArgumentsAndTrackAppLaunch(
                            toastActivationArgs.Argument);

                        // Use the originalArgs variable to access the original arguments
                        // that were passed to the app.
                    }
                }
                catch
                {

                }
            }
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            if (ApiInformation.IsMethodPresent("Windows.UI.Composition.Compositor", "CreateHostBackdropBrush"))
            {
                ApplicationData applicationData = ApplicationData.Current;
                ApplicationDataContainer localSettings = applicationData.LocalSettings;

                if ((localSettings.Values["useMDL2"] == null) || (localSettings.Values["useMDL2"].GetType() != typeof(bool)))
                {
                    localSettings.Values["useMDL2"] = false;
                }

                bool useMDL2 = (bool)localSettings.Values["useMDL2"];

                if (!useMDL2)
                {
                    Resources.MergedDictionaries.Add
                    (
                        new ResourceDictionary { Source = new Uri("ms-appx:///Themes/rs2_neon.xaml") }
                    );
                }
                else
                {
                    Resources.MergedDictionaries.Add
                    (
                        new ResourceDictionary { Source = new Uri("ms-appx:///Themes/rs1_neon.xaml") }
                    );
                }
            }
            else
            {
                Resources.MergedDictionaries.Add
                (
                    new ResourceDictionary { Source = new Uri("ms-appx:///Themes/rs1_neon.xaml") }
                );
            }

            try
            {
                StoreServicesEngagementManager engagementManager = StoreServicesEngagementManager.GetDefault();
                await engagementManager.RegisterNotificationChannelAsync();
            }
            catch
            {

            }


#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // disabled, obscures the hamburger button, enable if you need it
                DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            RefreshTile();

            if (CurrentSession == null)
            {
                //  Display an extended splash screen if app was not previously running.
                if (e.PreviousExecutionState != ApplicationExecutionState.Running)
                {
                    bool loadState = (e.PreviousExecutionState == ApplicationExecutionState.Terminated);
                    ExtendedSplashScreen extendedSplash = new(e.SplashScreen, loadState, e.Arguments);
                    AppContent = extendedSplash;
                    Window.Current.Activate();
                }

                else
                {
                    AddNewSession(e.Arguments);
                }
            }

            string args = e.Arguments;

            if (AppContent is not Shell currentContent)
            {
                return;
            }

            Shell shell = currentContent;
            shell.HandleLaunchedEvent(args);
        }

        private async void RefreshTile()
        {
            string devicefamily = AnalyticsInfo.VersionInfo.DeviceFamily;
            string tileimg;
            switch (devicefamily.ToLower())
            {
                case "windows.desktop":
                    {
                        tileimg = "desktop";
                        break;
                    }

                case "windows.xbox":
                    {
                        tileimg = "xbox";
                        break;
                    }

                case "windows.holographic":
                    {
                        tileimg = "holographic";
                        break;
                    }

                case "windows.team":
                    {
                        tileimg = "team";
                        break;
                    }

                case "windows.iot":
                    {
                        tileimg = "iot";
                        break;
                    }

                case "windows.mobile":
                    {
                        tileimg = "phone";
                        break;
                    }

                default:
                    {
                        tileimg = "generic";
                        break;
                    }
            }

            TileContent content = new()
            {
                Visual = new TileVisual
                {
                    TileSmall = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            BackgroundImage = new TileBackgroundImage()
                            {
                                Source = "Assets/Tiles/Small/" + tileimg + ".png"
                            }
                        }
                    },
                    TileMedium = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            BackgroundImage = new TileBackgroundImage()
                            {
                                Source = "Assets/Tiles/Medium/" + tileimg + ".png"
                            }
                        }
                    },
                    TileWide = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            BackgroundImage = new TileBackgroundImage()
                            {
                                Source = "Assets/Tiles/Wide/" + tileimg + ".png"
                            }
                        }
                    },
                    TileLarge = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            BackgroundImage = new TileBackgroundImage()
                            {
                                Source = "Assets/Tiles/Large/" + tileimg + ".png"
                            }
                        }
                    }
                }
            };
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            TileUpdateManager.CreateTileUpdaterForApplication().Update(new TileNotification(content.GetXml()));

            if (ApiInformation.IsTypePresent("Windows.UI.StartScreen.JumpList"))
            {
                if (JumpList.IsSupported())
                {
                    JumpList jumpList = await JumpList.LoadCurrentAsync();
                    jumpList.Items.Clear();
                    JumpListItem item1 = JumpListItem.CreateWithArguments("RegistryEditorPage", TextResources.Shell_RegistryEditorTitle);
                    item1.Description = TextResources.Shell_RegistryEditorDesc;
                    item1.Logo = new Uri("ms-appx:///Assets/JumpList/registryeditor.png");
                    JumpListItem item2 = JumpListItem.CreateWithArguments("RegistryBrowserPage", TextResources.Shell_RegistryBrowserTitle);
                    item2.Description = TextResources.Shell_RegistryBrowserDesc;
                    item2.Logo = new Uri("ms-appx:///Assets/JumpList/registrybrowser.png");
                    JumpListItem item3 = JumpListItem.CreateWithArguments("RegistrySearchPage", TextResources.Shell_RegistrySearchTitle);
                    item3.Description = TextResources.Shell_RegistrySearchDesc;
                    item3.Logo = new Uri("ms-appx:///Assets/JumpList/registrysearch.png");
                    JumpListItem item4 = JumpListItem.CreateWithArguments("TweaksPage", TextResources.Shell_TweaksTitle);
                    item4.Description = TextResources.Shell_TweaksDesc;
                    item4.Logo = new Uri("ms-appx:///Assets/JumpList/tweaks.png");
                    JumpListItem item6 = JumpListItem.CreateWithArguments("AppManagerPage", TextResources.Shell_ApplicationsTitle);
                    item6.Description = TextResources.Shell_ApplicationsDescription;
                    item6.Logo = new Uri("ms-appx:///Assets/JumpList/apps.png");
                    JumpListItem item7 = JumpListItem.CreateWithArguments("CertificatesPage", TextResources.Shell_CertificatesTitle);
                    item7.Description = TextResources.Shell_CertificatesDesc;
                    item7.Logo = new Uri("ms-appx:///Assets/JumpList/certs.png");
                    JumpListItem item8 = JumpListItem.CreateWithArguments("InteropUnlockPage", TextResources.Shell_InteropUnlockTitle);
                    item8.Description = TextResources.Shell_InteropUnlockDesc;
                    item8.Logo = new Uri("ms-appx:///Assets/JumpList/interopunlock.png");
                    JumpListItem item9 = JumpListItem.CreateWithArguments("YourDevicePage", TextResources.Shell_DeviceInfoTitle);
                    item9.Description = TextResources.Shell_DeviceInfoDesc;
                    item9.Logo = new Uri("ms-appx:///Assets/JumpList/yourdevice.png");
                    jumpList.SystemGroupKind = JumpListSystemGroupKind.None;
                    jumpList.Items.Add(item1);
                    jumpList.Items.Add(item2);
                    jumpList.Items.Add(item3);
                    jumpList.Items.Add(item4);
                    jumpList.Items.Add(item6);
                    jumpList.Items.Add(item7);
                    jumpList.Items.Add(item8);
                    jumpList.Items.Add(item9);
                    await jumpList.SaveAsync();
                }
            }
        }

        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

            using (ExtendedExecutionSession session = new())
            {
                session.Reason = ExtendedExecutionReason.Unspecified;
                session.Description = TextResources.App_SuspendingDescription;
                ExtendedExecutionResult result = await session.RequestExtensionAsync();
            }

            deferral.Complete();
        }

        public class Session
        {
            public UIElement WindowContent { get; set; }
            public DateTime CreationDate { get; set; }
            public IRegistryProvider Helper { get; set; }
            public RenderTargetBitmap Preview { get; set; }
        }

        public class Remote
        {
            public string SessionID { get; set; }
            public string Hostname { get; set; }
        }
    }
}