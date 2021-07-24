﻿using InteropTools.Providers;

namespace InteropTools.CorePages
{
    public enum PageGroup
    {
        Core,
        Registry,
        General,
        Tweaks,
        SSH,
        Unlock,
        Bottom
    }

    public static class PageHelper
    {
        public static string GetIconForPageGroup(PageGroup group)
        {
            return group switch
            {
                PageGroup.Bottom => "",
                PageGroup.Core => "",
                PageGroup.General => "",
                PageGroup.Registry => "",
                PageGroup.SSH => "",
                PageGroup.Tweaks => "",
                PageGroup.Unlock => "",
                _ => "",
            };
        }

        public static string GetNameForPageGroup(PageGroup group)
        {
            return group switch
            {
                PageGroup.Bottom => "",
                PageGroup.Core => "Core",
                PageGroup.General => InteropTools.Resources.TextResources.Shell_GeneralGroupName,
                PageGroup.Registry => InteropTools.Resources.TextResources.Shell_RegistryGroupName,
                PageGroup.SSH => InteropTools.Resources.TextResources.Shell_SSHGroupName,
                PageGroup.Tweaks => InteropTools.Resources.TextResources.Shell_TweakGroupName,
                PageGroup.Unlock => InteropTools.Resources.TextResources.Shell_UnlockGroupName,
                _ => "",
            };
        }
    }

    public abstract class ShellPage
    {
        public int viewid => App.CurrentSession.Value;

        public abstract PageGroup PageGroup { get; }
        public abstract string PageName { get; }

        public IRegistryProvider RegistryProvider
        {
            get => App.Sessions[viewid].Helper;

            set => App.Sessions[viewid].Helper = value;
        }
    }
}
