﻿using System;
using System.Text;

namespace InteropTools.Providers
{
    public enum RegHives
    {
        HKEY_CLASSES_ROOT = int.MinValue,
        HKEY_CURRENT_USER = -2147483647,
        HKEY_LOCAL_MACHINE = -2147483646,
        HKEY_USERS = -2147483645,
        HKEY_PERFORMANCE_DATA = -2147483644,
        HKEY_CURRENT_CONFIG = -2147483643,
        HKEY_DYN_DATA = -2147483642,
        HKEY_CURRENT_USER_LOCAL_SETTINGS = -2147483641
    }

    public enum KeyStatus
    {
        Found,
        NotFound,
        AccessDenied,
        Unknown
    }

    public enum HelperErrorCodes
    {
        Success,
        Failed,
        AccessDenied,
        NotImplemented
    }

    public enum RegistryItemType
    {
        Hive,
        Key,
        Value
    }

    public enum RegTypes
    {
        REG_ERROR = -1,
        REG_NONE = 0,
        REG_SZ = 1,
        REG_EXPAND_SZ = 2,
        REG_BINARY = 3,
        REG_DWORD = 4,
        REG_DWORD_BIG_ENDIAN = 5,
        REG_LINK = 6,
        REG_MULTI_SZ = 7,
        REG_RESOURCE_LIST = 8,
        REG_FULL_RESOURCE_DESCRIPTOR = 9,
        REG_RESOURCE_REQUIREMENTS_LIST = 10,
        REG_QWORD = 11
    }

    public sealed class RegistryItemCustom
    {
        public RegHives Hive { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public RegistryItemType Type { get; set; }
        public uint ValueType { get; set; }

        public string Value { get; set; }

        private static readonly uint[] _lookup32 = CreateLookup32();

        private static string ByteArrayToHexViaLookup32(byte[] bytes)
        {
            try
            {
                uint[] lookup32 = _lookup32;
                char[] result = new char[bytes.Length * 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    uint val = lookup32[bytes[i]];
                    result[2 * i] = (char)val;
                    result[(2 * i) + 1] = (char)(val >> 16);
                }
                return new string(result);
            }
            catch
            {
                return "";
            }
        }

        private static uint[] CreateLookup32()
        {
            uint[] result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                result[i] = s[0] + ((uint)s[1] << 16);
            }
            return result;
        }

        public string RegBufferToString(uint valtype, byte[] data)
        {
            if (data.Length == 0)
                return null;

            switch (valtype)
            {
                case (uint)RegTypes.REG_DWORD:
                    {
                        return data.Length == 0 ? "" : BitConverter.ToUInt32(data, 0).ToString();
                    }
                case (uint)RegTypes.REG_QWORD:
                    {
                        return data.Length == 0 ? "" : BitConverter.ToUInt64(data, 0).ToString();
                    }
                case (uint)RegTypes.REG_MULTI_SZ:
                    {
                        string strNullTerminated = Encoding.Unicode.GetString(data);
                        if (strNullTerminated.Substring(strNullTerminated.Length - 2) == "\0\0")
                        {
                            // The REG_MULTI_SZ is properly terminated.
                            // Remove the array terminator, and the final string terminator.
                            strNullTerminated = strNullTerminated.Substring(0, strNullTerminated.Length - 2);
                        }
                        else if (strNullTerminated.Substring(strNullTerminated.Length - 1) == "\0")
                        {
                            // The REG_MULTI_SZ is improperly terminated (only one terminator).
                            // Remove it.
                            strNullTerminated = strNullTerminated.Substring(0, strNullTerminated.Length - 1);
                        }
                        // Split by null terminator.
                        return string.Join("\n", strNullTerminated.Split('\0'));
                    }
                case (uint)RegTypes.REG_SZ:
                    {
                        return Encoding.Unicode.GetString(data).TrimEnd('\0');
                    }
                case (uint)RegTypes.REG_EXPAND_SZ:
                    {
                        return Encoding.Unicode.GetString(data).TrimEnd('\0');
                    }
                default:
                    {
                        return ByteArrayToHexViaLookup32(data);
                    }
            }
        }
    }
}