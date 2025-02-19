using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.AccessControl;

namespace HelpTool
{
    class Program
    {
        static void Main(string[] args)
        {
            bool russian = CultureInfo.CurrentUICulture.EnglishName.IndexOf("russian", StringComparison.OrdinalIgnoreCase) >= 0;
            if (args.Length == 3 && args[0].StartsWith("HK", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    RegistryKey key = null;
                    if (String.Equals(args[0], "HKC", StringComparison.OrdinalIgnoreCase))
                    {
                        key = Registry.ClassesRoot.OpenSubKey(args[1], true);
                    }
                    else if (String.Equals(args[0], "HKU", StringComparison.OrdinalIgnoreCase))
                    {
                        key = Registry.CurrentUser.OpenSubKey(args[1], true);
                    }
                    else if (String.Equals(args[0], "HKL", StringComparison.OrdinalIgnoreCase))
                    {
                        key = Registry.LocalMachine.OpenSubKey(args[1], true);
                    }
                    RegistrySecurity rs = new RegistrySecurity();
                    rs = key.GetAccessControl();
                    rs.SetAccessRuleProtection(true, true);
                    string[] sids = args[2].Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                    int count = sids.Length;
                    for (int i = 0; i < count; i++)
                    {
                        rs.AddAccessRule(new RegistryAccessRule(new System.Security.Principal.SecurityIdentifier(sids[i]).Translate(typeof(System.Security.Principal.NTAccount)).ToString(), RegistryRights.FullControl, InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
                    }
                    key.SetAccessControl(rs);
                    key.Close();
                }
                catch
                {
                    Console.WriteLine(russian ? "Не удалось изменить права для " + args[2] + " в " + args[0] + "\\" + args[1] : "Failed to change permissions for " + args[2] + " in " + args[0] + "\\" + args[1]);
                }
            }
            else if (args.Length >= 2 && args[1].EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    IWshRuntimeLibrary.IWshShortcut shortcut = new IWshRuntimeLibrary.WshShell().CreateShortcut(args[1]) as IWshRuntimeLibrary.IWshShortcut;
                    shortcut.TargetPath = args[0];
                    shortcut.WorkingDirectory = args.Length > 2 ? args[2] : Path.GetDirectoryName(args[0]);
                    if (args.Length > 3)
                    {
                        shortcut.Arguments = args[3];
                    }
                    shortcut.Save();
                }
                catch
                {
                    Console.WriteLine(russian ? "Не удалось создать ярлык для " + args[0] + " в " + args[1] : "Failed to create shortcut for " + args[0] + " in " + args[1]);
                }
            }
            else if (args.Length >= 3 && File.Exists(args[0]))
            {
                List<byte> searchBytes = new List<byte>();
                List<byte> replaceBytes = new List<byte>();
                hexToByte(searchBytes, args[1].Replace(" ", ""));
                hexToByte(replaceBytes, args[2].Replace(" ", ""));
                if (searchBytes.Count == replaceBytes.Count)
                {
                    try
                    {
                        bool find = true;
                        bool write = false;
                        byte[] bytesFile = File.ReadAllBytes(args[0]);
                        int fileSize = bytesFile.Length;
                        int searchSize = searchBytes.Count;
                        for (int i = 0; i + searchSize < fileSize; i++)
                        {
                            find = true;
                            for (int j = 0; j < searchSize; j++)
                            {
                                if (bytesFile[i + j] != searchBytes[j])
                                {
                                    find = false;
                                    break;
                                }
                            }
                            if (find)
                            {
                                Buffer.BlockCopy(replaceBytes.ToArray(), 0, bytesFile, i, searchSize);
                                write = true;
                                if (args.Length < 4)
                                {
                                    break;
                                }
                            }
                        }
                        if (write)
                        {
                            if (!File.Exists(args[0] + ".bak"))
                            {
                                File.Move(args[0], args[0] + ".bak");
                            }
                            File.WriteAllBytes(args[0], bytesFile);
                            Console.WriteLine(russian ? "Операция замены выполнена." : "Replacement operation completed.");
                        }
                        else
                        {
                            Console.WriteLine(russian ? "Операция замены не выполнена." : "Replacement operation not completed.");
                        }
                        bytesFile = null;
                    }
                    catch
                    {
                        Console.WriteLine(russian ? "Ошибка операции замены." : "Replace operation failed.");
                    }
                }
                else
                {
                    Console.WriteLine(russian ? "Поиск и замена не совпадают по размеру." : "Find and replace are not the same size.");
                }
            }
            else
            {
                Console.WriteLine(russian ? "Примеры: HK[C\\U\\L] [путь] \"SID|SID\" \\ [файл] \"00 00 00\" \"10 10 10\" [* множ. замена] \\ [файл] [ярлык] [папка] [аргументы]" : "Examples: HK[C\\U\\L] [path] \"SID|SID\" \\ [file] \"00 00 00\" \"10 10 10\" [* mult. replace] \\ [file] [shortcut] [folder] [arguments]");
            }
        }
        static void hexToByte(List<byte> list, string line)
        {
            int count = line.Length;
            for (int i = 0; i + 1 < count; i += 2)
            {
                list.Add(Convert.ToByte(line.Substring(i, 2), 16));
            }
        }
    }
}
