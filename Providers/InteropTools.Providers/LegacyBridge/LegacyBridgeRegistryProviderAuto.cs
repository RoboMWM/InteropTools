﻿// Copyright 2015-2021 (c) Interop Tools Development Team
// This file is licensed to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace InteropTools.Providers
{
    public class LegacyBridgeRegistryProviderAuto : IRegistryProvider
    {
        public async Task<HelperErrorCodes> ExecuteAction(Func<IRegProvider, Task<HelperErrorCodes>> providerFunctionCall)
        {
            return await ExecuteAction(providerFunctionCall, (t) => t, (t) => t, (t) => t);
        }

        public async Task<T1> ExecuteAction<T1, T2>(Func<IRegProvider, Task<T2>> providerFunctionCall, Func<T2, T1> typeConverterCall, Func<T2, HelperErrorCodes> typeStatusConverter, Func<HelperErrorCodes, T1> statusTypeConverter, bool SecondCall = false)
        {
            try
            {
                bool hadaccessdenied = false;
                bool hadfailed = false;

                using (AppPlugin.PluginList.PluginList<string, string, double> reglist = await Registry.Definition.RegistryProvidersWithOptions.ListAsync(Registry.Definition.RegistryProvidersWithOptions.PLUGIN_NAME))
                {
                    foreach (AppPlugin.PluginList.PluginList<string, string, double>.PluginProvider plugin in reglist.Plugins)
                    {
                        RegistryProvider provider = new(plugin);

                        T2 result = await providerFunctionCall(provider);

                        if (typeStatusConverter(result) == HelperErrorCodes.Success)
                        {
                            reglist.Dispose();
                            return typeConverterCall(result);
                        }

                        if (typeStatusConverter(result) == HelperErrorCodes.NotImplemented)
                        {
                            continue;
                        }

                        if (typeStatusConverter(result) == HelperErrorCodes.AccessDenied)
                        {
                            hadaccessdenied = true;
                            continue;
                        }

                        if (typeStatusConverter(result) == HelperErrorCodes.Failed)
                        {
                            hadfailed = true;
                            continue;
                        }
                    }
                }

                if (hadaccessdenied)
                {
                    return statusTypeConverter(HelperErrorCodes.AccessDenied);
                }

                if (hadfailed)
                {
                    return statusTypeConverter(HelperErrorCodes.Failed);
                }

                return statusTypeConverter(HelperErrorCodes.NotImplemented);
            }
            catch { }

            return statusTypeConverter(HelperErrorCodes.Failed);
        }

        public async Task<HelperErrorCodes> AddKey(RegHives hive, string key)
        {
            return await ExecuteAction((t) => t.RegAddKey(hive, key));
        }

        public bool AllowsRegistryEditing()
        {
            return true;
        }

        public async Task<HelperErrorCodes> DeleteKey(RegHives hive, string key, bool recursive)
        {
            return await ExecuteAction((t) => t.RegDeleteKey(hive, key, recursive));
        }

        public async Task<HelperErrorCodes> DeleteValue(RegHives hive, string key, string keyvalue)
        {
            return await ExecuteAction((t) => t.RegDeleteValue(hive, key, keyvalue));
        }

        public bool DoesFileExists(string path)
        {
            bool fileexists;
            try
            {
                fileexists = File.Exists(path);
            }
            catch (InvalidOperationException)
            {
                fileexists = true;
            }

            return fileexists;
        }

        public string GetAppInstallationPath()
        {
            return Windows.ApplicationModel.Package.Current.InstalledLocation.Path;
        }

        public string GetDescription()
        {
            return "This device (through provider extensions)";
        }

        public string GetFriendlyName()
        {
            return "This device (through provider extensions)";
        }

        public string GetHostName()
        {
            return "127.0.0.1";
        }

        public async Task<GetKeyLastModifiedTime> GetKeyLastModifiedTime(RegHives hive, string key)
        {
            return await ExecuteAction((t) => t.RegQueryKeyLastModifiedTime(hive, key), (t) => new Providers.GetKeyLastModifiedTime() { LastModified = new DateTime(t.LastModified), returncode = t.returncode }, (t) => t.returncode, (t) => new Providers.GetKeyLastModifiedTime() { LastModified = new DateTime(), returncode = t });
        }

        public async Task<KeyStatus> GetKeyStatus(RegHives hive, string key)
        {
            return await ExecuteAction((Func<IRegProvider, Task<Providers.KeyStatus>>)((t) => (Task<Providers.KeyStatus>)t.RegQueryKeyStatus((RegHives)hive, (string)key)), (t) => t, (Func<Providers.KeyStatus, HelperErrorCodes>)((t) =>
            {
                switch (t)
                {
                    case Providers.KeyStatus.Found:
                        return HelperErrorCodes.Success;
                    case Providers.KeyStatus.NotFound:
                        return HelperErrorCodes.Failed;
                    case Providers.KeyStatus.AccessDenied:
                        return HelperErrorCodes.AccessDenied;
                    case Providers.KeyStatus.Unknown:
                        return HelperErrorCodes.NotImplemented;
                }

                return HelperErrorCodes.NotImplemented;
            }), (Func<HelperErrorCodes, KeyStatus>)((t) =>
            {
                switch (t)
                {
                    case HelperErrorCodes.Success:
                        return (KeyStatus)Providers.KeyStatus.Found;
                    case HelperErrorCodes.Failed:
                        return (KeyStatus)Providers.KeyStatus.NotFound;
                    case HelperErrorCodes.AccessDenied:
                        return (KeyStatus)Providers.KeyStatus.AccessDenied;
                    case HelperErrorCodes.NotImplemented:
                        return (KeyStatus)Providers.KeyStatus.Unknown;
                }

                return (KeyStatus)Providers.KeyStatus.Unknown;
            }));
        }

        public async Task<GetKeyValueReturn> GetKeyValue(RegHives hive, string key, string keyvalue, RegTypes type)
        {
            return await ExecuteAction((t) => t.RegQueryValue(hive, key, keyvalue, type), (t) => new Providers.GetKeyValueReturn() { regtype = t.regtype, regvalue = t.regvalue, returncode = t.returncode }, (t) => t.returncode, (t) => new Providers.GetKeyValueReturn() { regtype = RegTypes.REG_ERROR, regvalue = "", returncode = t });
        }

        public async Task<GetKeyValueReturn2> GetKeyValue(RegHives hive, string key, string keyvalue, uint type)
        {
            return await ExecuteAction((t) => t.RegQueryValue(hive, key, keyvalue, type), (t) => new Providers.GetKeyValueReturn2() { regtype = t.regtype, regvalue = t.regvalue, returncode = t.returncode }, (t) => t.returncode, (t) => new Providers.GetKeyValueReturn2() { regtype = 0, regvalue = "", returncode = t });
        }

        public async Task<IReadOnlyList<RegistryItemCustom>> GetRegistryHives2()
        {
            return await ExecuteAction((t) => t.RegEnumKey(null, ""), (t) => t.items, (t) => t.returncode, (t) => new List<RegistryItemCustom>());
        }

        public async Task<IReadOnlyList<RegistryItemCustom>> GetRegistryItems2(RegHives hive, string key)
        {
            return await ExecuteAction((t) => t.RegEnumKey(hive, key), (t) => t.items, (t) => t.returncode, (t) => new List<RegistryItemCustom>());
        }

        public string GetSymbol()
        {
            return "";
        }

        public string GetTitle()
        {
            return "This device (through provider extensions)";
        }

        public bool IsLocal()
        {
            return true;
        }

        public async Task<HelperErrorCodes> LoadHive(string FileName, string mountpoint, bool inUser)
        {
            return await ExecuteAction((t) => t.RegLoadHive(FileName, mountpoint, inUser));
        }

        public async Task<HelperErrorCodes> RenameKey(RegHives hive, string key, string newname)
        {
            return await ExecuteAction((t) => t.RegRenameKey(hive, key, newname));
        }

        public async Task<HelperErrorCodes> SetKeyValue(RegHives hive, string key, string keyvalue, RegTypes type, string data)
        {
            return await ExecuteAction((t) => t.RegSetValue(hive, key, keyvalue, type, data));
        }

        public async Task<HelperErrorCodes> SetKeyValue(RegHives hive, string key, string keyvalue, uint type, string data)
        {
            return await ExecuteAction((t) => t.RegSetValue(hive, key, keyvalue, type, data));
        }

        public async Task<HelperErrorCodes> UnloadHive(string mountpoint, bool inUser)
        {
            return await ExecuteAction((t) => t.RegUnloadHive(mountpoint, inUser));
        }
    }
}