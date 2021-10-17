﻿// Copyright 2015-2021 (c) Interop Tools Development Team
// This file is licensed to you under the MIT license.

using System;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InteropToolsAppExtensibilityTestingApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            string ret = await LaunchAppForResults();
            await new MessageDialog(ret).ShowAsync();
        }

        private async Task<string> LaunchAppForResults()
        {
            Uri testAppUri = new("interoptools-appextensionregistrar:"); // The protocol handled by the launched app
            LauncherOptions options = new()
            {
                TargetApplicationPackageFamilyName = "52346ITDevTeam.InteropToolsPreview_feeqnmc1868va"
            };

            ValueSet inputData = new()
            {
                ["TestData"] = "Test data"
            };

            string theResult = "";
            LaunchUriResult result = await Launcher.LaunchUriForResultsAsync(testAppUri, options, inputData);
            if (result.Status == LaunchUriStatus.Success &&
                result.Result?.ContainsKey("ReturnedData") == true)
            {
                ValueSet theValues = result.Result;
                theResult = theValues["ReturnedData"] as string;
            }
            return theResult;
        }
    }
}