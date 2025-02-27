using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace InfoTipReplacer
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
            List<string> outList = new List<string>(File.ReadAllLines(Path.Combine(path, "InfoTipReplacer.reg")));
            int count = outList.Count;
            for (int i = 0; i < count; i++)
            {
                if (outList[i].IndexOf("MAPI/") < 0 && outList[i].StartsWith("[") && outList[i + 1].StartsWith("\"InfoTip\"=\"prop:"))
                {
                    outList[i + 1] = outList[i + 1].Replace("System.ItemTypeText", "").Replace("System.ItemType", "").Replace("System.DateCreated", "");
                    if (outList[i + 1].IndexOf("System.Size", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        outList[i + 1] = outList[i + 1].Remove(outList[i + 1].Length - 1) + ";System.Size\"";
                    }
                    if (outList[i + 1].IndexOf("System.DateModified", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        outList[i + 1] = outList[i + 1].Remove(outList[i + 1].Length - 1) + ";System.DateModified\"";
                    }
                    outList[i + 1] = outList[i + 1].Replace(";*;", ";").Replace(":*;", ":").Replace(";;", ";").Replace(":;", ":");
                    i++;
                }
                else
                {
                    outList.RemoveAt(i);
                    count--;
                    i--;
                }

            }
            File.WriteAllLines(Path.Combine(path, "InfoTipReplacer.txt"), outList);
        }
    }
}
