﻿using Windows.ApplicationModel.Background;

namespace InteropTools.Providers.Registry.RegistryRTProvider
{
    public sealed class RegistryProvider : IBackgroundTask
    {
        private readonly IBackgroundTask internalTask = new RegistryProviderIntern();

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            internalTask.Run(taskInstance);
        }
    }
}