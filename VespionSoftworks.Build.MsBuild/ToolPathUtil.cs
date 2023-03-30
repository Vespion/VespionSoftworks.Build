using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace VespionSoftworks.Build.MsBuild
{
	public static class ToolPathUtil
    {
        public static bool SafeFileExists(string path, string toolName)
        {
            return SafeFileExists(Path.Combine(path, toolName));
        }
        
        public static bool SafeFileExists(string? file)
        {
            try { return File.Exists(file); }
            catch(Exception ex)
            {
                Debug.WriteLine("ERR (ToolPathUtil): {0}", ex);
            } // eat exception

            return false;
        }

        public static string MakeToolName(string name)
        {
            return (Environment.OSVersion.Platform == PlatformID.Unix) ?
                name : name + ".exe";
        }

        public static string? FindInRegistry(string toolName)
        {
            if(!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }
            
            try
            {
                var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + toolName, false);
                if (key != null)
                {
                    var possiblePath = key.GetValue(null) as string;
                    if (SafeFileExists(possiblePath))
                        return Path.GetDirectoryName(possiblePath);
                }
            }
            catch (System.Security.SecurityException) { }

            return null;
        }

        public static string? FindInPath(string toolName)
        {
            var pathEnvironmentVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            string[] paths = pathEnvironmentVariable.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var path in paths)
            {
                if (SafeFileExists(path, toolName))
                {
                    return path;
                }
            }

            return null;
        }

        public static string? FindInProgramFiles(string toolName, params string[] commonLocations)
        {
            foreach (var location in commonLocations)
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), location);
                if (SafeFileExists(path, toolName))
                {
                    return path;
                }
            }

            return null;
        }

        public static string? FindInLocalPath(string toolName, string? localPath)
        {
            if (localPath == null)
                return null;

            var path = new DirectoryInfo(localPath).FullName;
            if (SafeFileExists(localPath, toolName))
            {
                return path;
            }

            return null;
        }
    }
}