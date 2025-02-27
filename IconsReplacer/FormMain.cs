using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace IconsReplacer
{
    public partial class FormMain : Form
    {
        string path = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);

        public FormMain()
        {
            InitializeComponent();
        }

        void button1_Click(object sender, EventArgs e)
        {
            foreach (string line in Directory.GetFileSystemEntries(path, "*.mun", SearchOption.TopDirectoryOnly))
            {
                string dll = line.Replace(".mun", "");
                File.Move(line, dll);
                string filename = Path.GetFileNameWithoutExtension(dll);
                string folder = Path.Combine(path, filename);
                Process process = new Process();
                process.StartInfo.FileName = Path.Combine(path, "ResourcesExtract.exe");
                process.StartInfo.Arguments = "/Source \"" + dll + "\" /DestFolder \"" + folder + "\" /ExtractIcons 1 /OpenDestFolder 0";
                process.Start();
                process.WaitForExit();
                List<string> outList = new List<string>() {
                    "[FILENAMES]",
                    "Exe=\"" + dll + "\"",
                    "Log=\"" + Path.Combine(path, filename + ".log") + "\"",
                    "SaveAs=\"" + folder + ".dll.mun\"",
                    "[COMMANDS]"
                };
                string folder7 = folder + "_7";
                foreach (string ico in Directory.GetFileSystemEntries(folder7, "*.ico", SearchOption.TopDirectoryOnly))
                {
                    string file = Path.Combine(folder, filename + "_" + Path.GetFileName(ico));
                    if (File.Exists(file) && getMD5(ico) != getMD5(file))
                    {
                        outList.Add("-addoverwrite \"" + ico + "\", ICONGROUP, " + Path.GetFileNameWithoutExtension(ico));
                    }
                }
                string script = Path.Combine(path, filename + "_script.txt");
                File.WriteAllLines(script, outList);
                process.StartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Resource Hacker", "ResourceHacker.exe");
                process.StartInfo.Arguments = "-script \"" + script + "\"";
                process.Start();
                process.WaitForExit();
            }
        }

        string getMD5(string file)
        {
            MD5 md5 = MD5.Create();
            Stream st = File.OpenRead(file);
            byte[] hash = md5.ComputeHash(st);
            md5.Clear();
            st.Close();
            return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
        }
    }
}
