﻿using InteropTools.CorePages;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace InteropTools.ShellPages.Registry
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RegistryHistory
    {
        public string PageName => "Registry History";
        public PageGroup PageGroup => PageGroup.Registry;

        public RegistryHistory()
        {
            InitializeComponent();
        }
    }
}
