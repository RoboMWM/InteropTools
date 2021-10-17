﻿// Copyright 2015-2021 (c) Interop Tools Development Team
// This file is licensed to you under the MIT license.

using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace InteropTools.ContentDialogs.Core
{
    public sealed partial class MessageDialogContentDialog : ContentDialog
    {
        public MessageDialogContentDialog()
        {
            InitializeComponent();
        }

        public async Task ShowMessageDialog(string Description = "", string Title = "")
        {
            this.Title = Title;
            this.Description.Text = Description;
            await ShowAsync();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}