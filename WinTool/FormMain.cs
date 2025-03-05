using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WinTool
{
    public partial class FormMain : Form
    {
        static string folderSystem = Environment.GetFolderPath(Environment.SpecialFolder.System);
        static string folderWindows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        static string folderSystemApps = Path.Combine(folderWindows, "SystemApps");
        static string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        static string culture = CultureInfo.CurrentUICulture.EnglishName;
        static bool russian = culture.IndexOf("russian", StringComparison.OrdinalIgnoreCase) >= 0;
        static bool windows11 = !Environment.OSVersion.Version.ToString().StartsWith("10.0.1");
        List<string> exeList = new List<string>() {
            Path.Combine(folderSystem, "reg.exe"),
            Path.Combine(folderSystem, "taskkill.exe"),
            Path.Combine(folderWindows, "explorer.exe"),
            Path.Combine(folderSystem, "schtasks.exe"),
            Path.Combine(folderSystem, "WindowsPowerShell", "v1.0", "powershell.exe"),
            Path.Combine(folderSystem, "takeown.exe"),
            Path.Combine(folderSystem, "icacls.exe"),
            Path.Combine(folderSystem, "ipconfig.exe"),
            Path.Combine(folderSystem, "certutil.exe"),
            Path.Combine(folderSystem, "sc.exe"),
            Path.Combine(folderSystem, "net.exe"),
            Path.Combine(folderSystem, "mountvol.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PostClear", "WinHelp.html")
        };
        List<string> defaultStart = new List<string>() {
            Path.Combine(folderSystemApps, "Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy", "StartMenuExperienceHost.exe"),
            windows11 ? Path.Combine(folderSystemApps, "MicrosoftWindows.Client.CBS_cw5n1h2txyewy", "SearchHost.exe") : Path.Combine(folderSystemApps, "Microsoft.Windows.Search_cw5n1h2txyewy", "SearchApp.exe")
        };
        List<string> defaultStartReg = new List<string>() { windows11 ? @"HKEY_CLASSES_ROOT\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages\MicrosoftWindows.Client.CBS_1000.26100.48.0_x64__cw5n1h2txyewy\MicrosoftWindows.Client.CBS_cw5n1h2txyewy!CortanaUI" : @"HKEY_CLASSES_ROOT\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages\Microsoft.Windows.Search_1.14.17.19041_neutral_neutral_cw5n1h2txyewy"
        };
        string screenClippingHost = windows11 ? Path.Combine(folderSystemApps, "MicrosoftWindows.Client.Core_cw5n1h2txyewy", "ScreenClippingHost.exe") : Path.Combine(folderSystemApps, "MicrosoftWindows.Client.CBS_cw5n1h2txyewy", "ScreenClippingHost.exe");
        string smartScreen = Path.Combine(folderSystem, "smartscreen.exe");
        string edgeUpdate = Path.Combine(programFilesX86, "Microsoft", "EdgeUpdate");
        string certLocal = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "roots.sst");
        string tempImport = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".reg");
        string tempExport = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".reg");
        string wtRegPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WinTool\";
        string eDelete = russian ? "Не удалось удалить: " : "Failed to delete: ";
        string eFailure = russian ? "Неудачно" : "Failure";
        string eRead = russian ? "Не удалось прочитать файл: " : "Failed to read file: ";
        string eRegistry = russian ? "Ошибка доступа к реестру: " : "Error accessing registry: ";
        string eStart = russian ? "Не удалось запустить процесс (скопировано в буфер обмена): " : "Failed to start process (copied to clipboard): ";
        string eWrite = russian ? "Не удалось записать: " : "Failed to write: ";
        string sCertificates = russian ? "Обновить корневые сертификаты? Поиск файла \"Рабочий стол\\roots.sst\" или скачивание." : "Update root certificates? Search for \"Desktop\\roots.sst\" file or download.";
        string sCompatibility = russian ? "Сбросить все параметры совместимости для всех приложений?" : "Reset all compatibility settings for all apps?";
        string sConfirm = russian ? "Подтвеждение" : "Confirmation";
        string sExplorer = russian ? "Закрыть Проводник?" : "Close Explorer?";
        string sFolders = russian ? "Сбросить настройки отображения для всех папок?" : "Reset display settings for all folders?";
        string sHistory = russian ? "Сбросить историю в меню Пуск?" : "Reset history in the Start menu?";
        string sLaunch = russian ? "Запустить?" : "Start?";
        string sMountVol = russian ? "Сбросить точки когда либо подключенных устройств-томов? Не затронет текущие подключенные." : "Reset points of ever connected devices-volumes? Will not affect currently connected ones.";
        string sMixer = russian ? "Сбросить настройки аудио микшера?" : "Reset audio mixer settings?";
        string sRestart = russian ? "При изменении требуется перезагрузка." : "Changes require a reboot.";
        string sRestore = russian ? "Удалить задачу автоматического создания точек восстановления?" : "Delete the automatic restore point creation task?";
        string sTotal = russian ? "Всего: " : "Total: ";
        const int CS_DBLCLKS = 0x8;
        const int WS_MINIMIZEBOX = 0x20000;
        Point lastLocation;

        public FormMain()
        {
            InitializeComponent();
            if (culture.IndexOf("chinese", StringComparison.OrdinalIgnoreCase) < 0)
            {
                defaultStart.Add(Path.Combine(folderSystemApps, "MicrosoftWindows.Client.CBS_cw5n1h2txyewy", "TextInputHost.exe"));
                defaultStartReg.Add(windows11 ? @"HKEY_CLASSES_ROOT\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages\MicrosoftWindows.Client.CBS_1000.26100.48.0_x64__cw5n1h2txyewy\MicrosoftWindows.Client.CBS_cw5n1h2txyewy!InputApp" : @"HKEY_CLASSES_ROOT\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages\MicrosoftWindows.Client.CBS_1000.19061.1000.0_x64__cw5n1h2txyewy\MicrosoftWindows.Client.CBS_cw5n1h2txyewy!InputApp");
            }
            refrashValues();
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 0)
            {
                foreach (string line in args)
                {
                    if (line.StartsWith("-setup", StringComparison.OrdinalIgnoreCase))
                    {
                        uint mask = 0;
                        bool parse = UInt32.TryParse(line.Remove(0, line.IndexOf("=") + 1), out mask);
                        List<Button> buttons = (parse && windows11) || (!parse && line.EndsWith("!")) ? new List<Button>() { contex_PinToTaskbar, contex_PintoStartScreen, contex_PinToHome, contex_Compatibility, contex_ModernSharing, contex_SendTo, contex_Sharing, contex_LibraryLocation, contex_PinToHomeFile, service_EdgeUpdate, service_Sketch, service_DefaultStart, service_NewContexMenu } : new List<Button>() { contex_PinToTaskbar, contex_PintoStartScreen, contex_PinToHome, contex_Compatibility, contex_ModernSharing, contex_SendTo, contex_Sharing, contex_LibraryLocation, contex_StartMenuExt, thispc_3DObjects, thispc_Downloads, thispc_Images, thispc_Music, thispc_Video, service_EdgeUpdate, service_Sketch, service_DefaultStart };
                        List<string> list = new List<string>();
                        uint total = 0;
                        int count = buttons.Count - 1;
                        for (int i = count; i >= 0; i--)
                        {
                            if (parse && mask >= (1 << i))
                            {
                                buttons[i].GetType().GetMethod("OnClick", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(buttons[i], new object[] { EventArgs.Empty });
                                mask -= (uint)1 << i;
                            }
                            else if (!parse)
                            {
                                list.Add(buttons[i].Text + " " + (1 << i));
                                total += (uint)1 << i;
                            }
                        }
                        if (!parse)
                        {
                            list.Add(sTotal + total);
                            MessageBox.Show(String.Join(Environment.NewLine, list));
                        }
                        Environment.Exit(0);
                    }
                }
            }
            if (!File.Exists(exeList[9]))
            {
                buttonHelp.Visible = false;
            }
            if (!windows11)
            {
                Text = "Win 10 Tool";
                labelLogo.Image = Properties.Resources.MainLogo10;
                tabControl1.Controls.Remove(tabPage4);
            }
            else
            {
                tabControl1.Controls.Remove(tabPage3);
            }
            if (!russian)
            {
                toEnglish();
            }
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= WS_MINIMIZEBOX;
                cp.ClassStyle |= CS_DBLCLKS;
                return cp;
            }
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void refrashValues()
        {
            setColor(contex_PinToTaskbar, @"HKEY_CLASSES_ROOT\*\shellex\ContextMenuHandlers\{90AA3A4E-1CBA-4233-B8BB-535773D48449}");
            setColor(contex_PintoStartScreen, @"HKEY_CLASSES_ROOT\exefile\shellex\ContextMenuHandlers\PintoStartScreen");
            setColor(contex_PinToHome, @"HKEY_CLASSES_ROOT\Folder\shell\pintohome");
            setColor(contex_Compatibility, @"HKEY_CLASSES_ROOT\exefile\shellex\ContextMenuHandlers\Compatibility");
            setColor(contex_ModernSharing, windows11 ? @"HKEY_CLASSES_ROOT\AllFilesystemObjects\shellex\ContextMenuHandlers\ModernSharing" : @"HKEY_CLASSES_ROOT\*\shellex\ContextMenuHandlers\ModernSharing");
            setColor(contex_SendTo, @"HKEY_CLASSES_ROOT\AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo");
            setColor(contex_Sharing, @"HKEY_CLASSES_ROOT\*\shellex\ContextMenuHandlers\Sharing");
            setColor(contex_LibraryLocation, @"HKEY_CLASSES_ROOT\Folder\shellex\ContextMenuHandlers\Library Location");
            if (windows11)
            {
                contex_StartMenuExt.Visible = false;
                setColor(contex_PinToHomeFile, @"HKEY_CLASSES_ROOT\*\shell\pintohomefile");
                setColor(service_NewContexMenu, @"HKEY_CURRENT_USER\SOFTWARE\CLASSES\CLSID\{86CA1AA0-34AA-4E8B-A509-50C905BAE2A2}\InprocServer32", null, null, true);
            }
            else
            {
                contex_PinToHomeFile.Visible = false;
                service_NewContexMenu.Visible = false;
                setColor(contex_StartMenuExt, @"HKEY_CLASSES_ROOT\exefile\shellex\ContextMenuHandlers\StartMenuExt");
                setColor(thispc_Desktop, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}");
                setColor(thispc_Documents, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{D3162B92-9365-467A-956B-92703ACA08AF}");
                setColor(thispc_3DObjects, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}");
                setColor(thispc_Downloads, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{088e3905-0323-4b02-9826-5d99428e115f}");
                setColor(thispc_Images, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{24ad3ad4-a569-4530-98e1-ab02f9417aa8}");
                setColor(thispc_Music, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}");
                setColor(thispc_Video, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}");
            }
            setColor(service_UAC, @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", "1");
            setColor(service_Firewall, @"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\mpssvc", "Start", "2");
            service_Store.ForeColor = getValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\InstallService", "Start", "3") && getValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\mpssvc", "Start", "2") ? SystemColors.ControlText : Color.Red;
            service_SystemRestore.ForeColor = File.Exists(Path.Combine(folderSystem, "Tasks", "Microsoft", "Windows", "SystemRestore", "SR")) ? SystemColors.ControlText : Color.Red;
            setColor(service_EdgeUpdate, @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate", "version", "999.999.999.999", true);
            service_Sketch.ForeColor = getAccessFile(screenClippingHost) ? SystemColors.ControlText : Color.Red;
            service_DefaultStart.ForeColor = getAccessFile(defaultStart[0]) ? SystemColors.ControlText : Color.Red;
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void contex_PinToTaskbar_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_CLASSES_ROOT\*\shellex\ContextMenuHandlers\{90AA3A4E-1CBA-4233-B8BB-535773D48449}",
                @"HKEY_CLASSES_ROOT\Application.Reference\shellex\ContextMenuHandlers\{90AA3A4E-1CBA-4233-B8BB-535773D48449}",
                @"HKEY_CLASSES_ROOT\IE.AssocFile.WEBSITE\ShellEx\ContextMenuHandlers\{90AA3A4E-1CBA-4233-B8BB-535773D48449}",
                @"HKEY_CLASSES_ROOT\Launcher.AllAppsDesktopApplication\shellex\ContextMenuHandlers\{90AA3A4E-1CBA-4233-B8BB-535773D48449}",
                @"HKEY_CLASSES_ROOT\Launcher.DesktopPackagedApplication\shellex\ContextMenuHandlers\{90AA3A4E-1CBA-4233-B8BB-535773D48449}",
                @"HKEY_CLASSES_ROOT\Launcher.ImmersiveApplication\shellex\ContextMenuHandlers\{90AA3A4E-1CBA-4233-B8BB-535773D48449}",
                @"HKEY_CLASSES_ROOT\Launcher.SystemSettings\shellex\ContextMenuHandlers\{90AA3A4E-1CBA-4233-B8BB-535773D48449}",
                @"HKEY_CLASSES_ROOT\Microsoft.Website\shellex\ContextMenuHandlers\{90AA3A4E-1CBA-4233-B8BB-535773D48449}",
                @"HKEY_CLASSES_ROOT\MSILink\shellex\ContextMenuHandlers\{90AA3A4E-1CBA-4233-B8BB-535773D48449}"
            });
        }
        void contex_PintoStartScreen_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_CLASSES_ROOT\exefile\shellex\ContextMenuHandlers\PintoStartScreen",
                @"HKEY_CLASSES_ROOT\Folder\shellex\ContextMenuHandlers\PintoStartScreen",
                @"HKEY_CLASSES_ROOT\Launcher.AllAppsDesktopApplication\shellex\ContextMenuHandlers\PintoStartScreen",
                @"HKEY_CLASSES_ROOT\Launcher.Computer\shellex\ContextMenuHandlers\PintoStartScreen",
                @"HKEY_CLASSES_ROOT\Launcher.DesktopPackagedApplication\shellex\ContextMenuHandlers\PintoStartScreen",
                @"HKEY_CLASSES_ROOT\Launcher.DualModeApplication\shellex\ContextMenuHandlers\PintoStartScreen",
                @"HKEY_CLASSES_ROOT\Launcher.ImmersiveApplication\shellex\ContextMenuHandlers\PintoStartScreen",
                @"HKEY_CLASSES_ROOT\Launcher.SystemSettings\shellex\ContextMenuHandlers\PintoStartScreen",
                @"HKEY_CLASSES_ROOT\Microsoft.Website\shellex\ContextMenuHandlers\PintoStartScreen",
                @"HKEY_CLASSES_ROOT\mscfile\shellex\ContextMenuHandlers\PintoStartScreen"
            });
        }
        void contex_PinToHome_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, windows11 ? new List<string>() {
                @"HKEY_CLASSES_ROOT\Drive\shell\pintohome",
                @"HKEY_CLASSES_ROOT\Folder\shell\pintohome",
                @"HKEY_CLASSES_ROOT\Network\shell\pintohome"
            } : new List<string>() {
                @"HKEY_CLASSES_ROOT\Folder\shell\pintohome"
            });
        }
        void contex_Compatibility_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_CLASSES_ROOT\batfile\shellex\ContextMenuHandlers\Compatibility",
                @"HKEY_CLASSES_ROOT\cmdfile\shellex\ContextMenuHandlers\Compatibility",
                @"HKEY_CLASSES_ROOT\exefile\shellex\ContextMenuHandlers\Compatibility",
                @"HKEY_CLASSES_ROOT\Msi.Package\shellex\ContextMenuHandlers\Compatibility"
            });
        }
        void contex_ModernSharing_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, windows11 ? new List<string>() {
                @"HKEY_CLASSES_ROOT\AllFilesystemObjects\shellex\ContextMenuHandlers\ModernSharing"
            } : new List<string>() {
                @"HKEY_CLASSES_ROOT\*\shellex\ContextMenuHandlers\ModernSharing"
            });
        }
        void contex_SendTo_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_CLASSES_ROOT\AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo",
                @"HKEY_CLASSES_ROOT\UserLibraryFolder\shellex\ContextMenuHandlers\SendTo"
            });
        }
        void contex_Sharing_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_CLASSES_ROOT\*\shellex\ContextMenuHandlers\Sharing",
                @"HKEY_CLASSES_ROOT\Directory\background\shellex\ContextMenuHandlers\Sharing",
                @"HKEY_CLASSES_ROOT\Directory\shellex\ContextMenuHandlers\Sharing",
                @"HKEY_CLASSES_ROOT\Drive\shellex\ContextMenuHandlers\Sharing",
                @"HKEY_CLASSES_ROOT\LibraryFolder\background\shellex\ContextMenuHandlers\Sharing",
                @"HKEY_CLASSES_ROOT\UserLibraryFolder\shellex\ContextMenuHandlers\Sharing"
            });
        }
        void contex_LibraryLocation_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_CLASSES_ROOT\Folder\shellex\ContextMenuHandlers\Library Location"
            });
        }
        void contex_PinToHomeFile_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_CLASSES_ROOT\*\shell\pintohomefile"
            });
        }
        void contex_StartMenuExt_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_CLASSES_ROOT\exefile\shellex\ContextMenuHandlers\StartMenuExt",
                @"HKEY_CLASSES_ROOT\Folder\shellex\ContextMenuHandlers\StartMenuExt",
                @"HKEY_CLASSES_ROOT\lnkfile\shellex\ContextMenuHandlers\StartMenuExt",
                @"HKEY_CLASSES_ROOT\Launcher.SystemSettings\shellex\ContextMenuHandlers\StartMenuExt",
                @"HKEY_CLASSES_ROOT\Launcher.ImmersiveApplication\shellex\ContextMenuHandlers\StartMenuExt"
            });
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void service_RestartExplorer_Click(object sender, EventArgs e)
        {
            if (dialogResult(sExplorer, sConfirm))
            {
                stopProcess("explorer.exe");
                if (dialogResult(sLaunch, sConfirm))
                {
                    startProcess(2);
                }
            }
        }
        void service_ResetFolders_Click(object sender, EventArgs e)
        {
            if (dialogResult(sFolders, sConfirm))
            {
                stopProcess("explorer.exe");
                stopProcess("ShellExperienceHost.exe");
                startProcess(0, "export " + "\"" + @"HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders" + "\" " + "\"" + tempExport + "\"");
                importRegistry(new List<string>() {
                    @"[-HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell]",
                    @"[HKEY_CURRENT_USER\SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\Shell]",
                    @"[-HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage]",
                    @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage]",
                    @"[-HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2]",
                    @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2]",
                    @"[-HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs]",
                    @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs]",
                    @"[-HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32]",
                    @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32]",
                    @"[-HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Search\JumplistData]",
                    @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Search\JumplistData]",
                    @"[-HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\UFH\SHC]",
                    @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\UFH\SHC]",
                    @"[-HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders]",
                    @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\Folders]"
                });
                if (File.Exists(tempExport))
                {
                    startProcess(0, "import \"" + tempExport + "\"");
                    deleteFile(tempExport);
                }
                deleteFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IconCache.db"));
                foreach (string line in Directory.EnumerateFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Windows", "Explorer"), "*.db"))
                {
                    deleteFile(line);
                }
                startProcess(2);
            }
        }
        void service_ResetMixer_Click(object sender, EventArgs e)
        {
            if (dialogResult(sMixer, sConfirm))
            {
                stopProcess("explorer.exe");
                importRegistry(new List<string>() {
                    @"[-HKEY_CURRENT_USER\SOFTWARE\Microsoft\Internet Explorer\LowRegistry\Audio\PolicyConfig\PropertyStore]",
                    @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Internet Explorer\LowRegistry\Audio\PolicyConfig\PropertyStore]"
                });
                startProcess(2);
            }
        }
        void service_ResetCompatibility_Click(object sender, EventArgs e)
        {
            if (dialogResult(sCompatibility, sConfirm))
            {
                importRegistry(new List<string>() {
                    @"[-HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers]",
                    @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers]",
                    @"[-HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers]",
                    @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers]",
                    @"[-HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers]",
                    @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers]",
                    @"[-HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\DirectDraw\Compatibility]",
                    @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\DirectDraw\Compatibility]",
                    @"[-HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\DirectDraw\Compatibility]",
                    @"[HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\DirectDraw\Compatibility]",
                    @"[-HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\DirectPlay\Applications]",
                    @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\DirectPlay\Applications]",
                    @"[-HKEY_CURRENT_USER\SOFTWARE\Microsoft\DirectInput]",
                    @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\DirectInput]",
                    @"[HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\AppCompatCache]",
                    "\"AppCompatCache\"=-"
                });
            }
        }
        void service_ResetMountVol_Click(object sender, EventArgs e)
        {
            if (dialogResult(sMountVol, sConfirm))
            {
                startProcess(11, "/R");
            }
        }
        void service_ResetHistory_Click(object sender, EventArgs e)
        {
            if (dialogResult(sHistory, sConfirm))
            {
                stopProcess("explorer.exe");
                importRegistry(new List<string>() {
                    @"[-HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\UserAssist]",
                    @"[HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\UserAssist]"
                });
                startProcess(2);
            }
        }
        void service_FlushDNS_Click(object sender, EventArgs e)
        {
            startProcess(7, @"/flushdns");
        }
        void service_UAC_Click(object sender, EventArgs e)
        {
            importRegistry(service_UAC.ForeColor != Color.Red ? new List<string>() {
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System]",
                "\"EnableLUA\"=dword:00000000"
            } : new List<string>() {
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System]",
                "\"EnableLUA\"=dword:00000001"
            });
            refrashValues();
        }
        void service_UpdateCertificate_Click(object sender, EventArgs e)
        {
            if (dialogResult(sCertificates, sConfirm))
            {
                string file = null;
                if (File.Exists(certLocal) && dialogResult(certLocal, sConfirm))
                {
                    file = certLocal;
                }
                else
                {
                    startProcess(8, "-generateSSTFromWU \"" + certLocal + "\"");
                    if (File.Exists(certLocal))
                    {
                        file = certLocal;
                    }
                }
                if (file != null)
                {
                    startProcess(4, "-windowstyle hidden -executionpolicy remotesigned -Command \"$sstStore = (Get-ChildItem -Path '" + file + "')" + Environment.NewLine + "$sstStore | Import-Certificate -CertStoreLocation Cert:\\LocalMachine\\Root\"");
                }
                else
                {
                    MessageBox.Show(eFailure);
                }
            }
        }
        void service_Firewall_Click(object sender, EventArgs e)
        {
            importRegistry(service_Firewall.ForeColor != Color.Red ? new List<string>() {
                @"[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\mpssvc]",
                "\"Start\"=dword:00000004",
                @"[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\InstallService]",
                "\"Start\"=dword:00000004"
            } : new List<string>() {
                @"[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\mpssvc]",
                "\"Start\"=dword:00000002"
            });
            refrashValues();
            MessageBox.Show(sRestart);
        }
        void service_Store_Click(object sender, EventArgs e)
        {
            importRegistry(service_Store.ForeColor != Color.Red ? new List<string>() {
                @"[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\InstallService]",
                "\"Start\"=dword:00000004"
            } : new List<string>() {
                @"[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\mpssvc]",
                "\"Start\"=dword:00000002",
                @"[HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\InstallService]",
                "\"Start\"=dword:00000003"
            });
            blockUnblock(service_Store.ForeColor != Color.Red, smartScreen, false);
            refrashValues();
            MessageBox.Show(sRestart);
        }
        void service_SystemRestore_Click(object sender, EventArgs e)
        {
            if (service_SystemRestore.ForeColor != Color.Red && dialogResult(sRestore, sConfirm))
            {
                startProcess(3, @"/delete /tn Microsoft\Windows\SystemRestore\SR /f");
            }
            refrashValues();
        }
        void service_EdgeUpdate_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(edgeUpdate) && getAccessFolder(edgeUpdate))
            {
                stopProcess("msedge.exe");
                foreach (string line in Directory.GetFileSystemEntries(edgeUpdate, "*.exe", SearchOption.AllDirectories))
                {
                    stopProcess(Path.GetFileName(line));
                }
                deleteFolder(edgeUpdate);
            }
            blockUnblock(service_EdgeUpdate.ForeColor != Color.Red, edgeUpdate, true);
            importRegistry(service_EdgeUpdate.ForeColor != Color.Red ? new List<string>() {
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft Edge]",
                "\"DisplayVersion\"=\"999.999.999.999\"",
                "\"Version\"=\"999.999.999.999\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft EdgeWebView]",
                "\"DisplayVersion\"=\"999.999.999.999\"",
                "\"Version\"=\"999.999.999.999\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate]",
                "\"version\"=\"999.999.999.999\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}]",
                "\"pv\"=\"999.999.999.999\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}]",
                "\"pv\"=\"999.999.999.999\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3C4FE00-EFD5-403B-9569-398A20F1BA4A}]",
                "\"pv\"=\"999.999.999.999\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\ClientState\{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}]",
                "\"pv\"=\"999.999.999.999\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\ClientState\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}]",
                "\"pv\"=\"999.999.999.999\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\ClientState\{F3C4FE00-EFD5-403B-9569-398A20F1BA4A}]",
                "\"pv\"=\"999.999.999.999\""
            } : new List<string>() {
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft Edge]",
                "\"DisplayVersion\"=\"90.0.0.0\"",
                "\"Version\"=\"90.0.0.0\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft EdgeWebView]",
                "\"DisplayVersion\"=\"90.0.0.0\"",
                "\"Version\"=\"90.0.0.0\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate]",
                "\"version\"=\"1.0.0.0\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}]",
                "\"pv\"=\"90.0.0.0\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}]",
                "\"pv\"=\"90.0.0.0\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3C4FE00-EFD5-403B-9569-398A20F1BA4A}]",
                "\"pv\"=\"90.0.0.0\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\ClientState\{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}]",
                "\"pv\"=\"90.0.0.0\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\ClientState\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}]",
                "\"pv\"=\"90.0.0.0\"",
                @"[HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\ClientState\{F3C4FE00-EFD5-403B-9569-398A20F1BA4A}]",
                "\"pv\"=\"90.0.0.0\""
            });
            refrashValues();
        }
        void service_Sketch_Click(object sender, EventArgs e)
        {
            blockUnblock(service_Sketch.ForeColor != Color.Red, screenClippingHost, false);
            refrashValues();
        }
        void service_DefaultStart_Click(object sender, EventArgs e)
        {
            foreach (string line in defaultStart)
            {
                blockUnblock(service_DefaultStart.ForeColor != Color.Red, line, false);
            }
            toggleButton(((Button)sender).ForeColor == Color.Red, defaultStartReg);
        }
        void service_NewContexMenu_Click(object sender, EventArgs e)
        {
            importRegistry(service_NewContexMenu.ForeColor != Color.Red ? new List<string>() {
                @"[HKEY_CURRENT_USER\SOFTWARE\CLASSES\CLSID\{86CA1AA0-34AA-4E8B-A509-50C905BAE2A2}\InprocServer32]"
            } : new List<string>() {
                @"[-HKEY_CURRENT_USER\SOFTWARE\CLASSES\CLSID\{86CA1AA0-34AA-4E8B-A509-50C905BAE2A2}\InprocServer32]",
                "@=\"\""
            });
            refrashValues();
            MessageBox.Show(sRestart);
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void thispc_Desktop_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}"
            });
        }
        void thispc_Documents_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{D3162B92-9365-467A-956B-92703ACA08AF}",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{D3162B92-9365-467A-956B-92703ACA08AF}"
            });
        }
        void thispc_3DObjects_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}"
            });
        }
        void thispc_Downloads_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{088e3905-0323-4b02-9826-5d99428e115f}",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{088e3905-0323-4b02-9826-5d99428e115f}"
            });
        }
        void thispc_Images_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{24ad3ad4-a569-4530-98e1-ab02f9417aa8}",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{24ad3ad4-a569-4530-98e1-ab02f9417aa8}"
            });
        }
        void thispc_Music_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}"
            });
        }
        void thispc_Video_Click(object sender, EventArgs e)
        {
            toggleButton(((Button)sender).ForeColor == Color.Red, new List<string>() {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace\{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}"
            });
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void fix_NetTrayIcon_Click(object sender, EventArgs e)
        {
            startProcess(9, "config RasMan start=auto");
            startProcess(10, "start RasMan");
            startProcess(9, "config RasMan start=disabled");
            startProcess(10, "stop RasMan");
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void setColor(Button button, string path, string key = null, string expect = null, bool invert = false)
        {
            button.ForeColor = getValue(path, key, expect) ? !invert ? SystemColors.ControlText : Color.Red : !invert ? Color.Red : SystemColors.ControlText;
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void toggleButton(bool add, List<string> list)
        {
            tabControl1.Enabled = false;
            List<string> removeList = new List<string>();
            List<string> importList = new List<string>();
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                bool disabled = getValue(wtRegPath + list[i]);
                bool enabled = getValue(list[i]);
                if ((add && disabled) || (!add && enabled))
                {
                    startProcess(0, "export " + "\"" + (add ? wtRegPath + list[i] : list[i]) + "\" " + "\"" + tempExport + "\"");
                    if (File.Exists(tempExport))
                    {
                        List<string> exportFile = new List<string>(readTextFile(tempExport));
                        deleteFile(tempExport);
                        if (exportFile.Count > 0)
                        {
                            removeList.Add("[-" + (add ? list[i] : wtRegPath + list[i]) + "]");
                            removeList.Add("[-" + (add ? wtRegPath + list[i] : list[i]) + "]");
                            foreach (string line in exportFile)
                            {
                                if (!String.IsNullOrEmpty(line) && line.IndexOf("Windows Registry Editor Version", StringComparison.OrdinalIgnoreCase) < 0)
                                {
                                    importList.Add(line.StartsWith("[") ? line.Replace(add ? wtRegPath + list[i] : list[i], add ? list[i] : wtRegPath + list[i]) : line);
                                }
                            }
                        }
                        exportFile.Clear();
                    }
                }
            }
            importRegistry(removeList);
            removeList.Clear();
            importRegistry(importList);
            importList.Clear();
            tabControl1.Enabled = true;
            refrashValues();
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        bool getValue(string path, string key = null, string expect = null)
        {
            try
            {
                RegistryKey regkey = path.StartsWith("HKEY_CLASSES_ROOT") ? Registry.ClassesRoot.OpenSubKey(path.Remove(0, 18)) : path.StartsWith("HKEY_CURRENT_USER") ? Registry.CurrentUser.OpenSubKey(path.Remove(0, 18)) : Registry.LocalMachine.OpenSubKey(path.Remove(0, 19));
                if (regkey != null)
                {
                    if (key != null)
                    {
                        object value = regkey.GetValue(key);
                        regkey.Close();
                        if (value != null)
                        {
                            return value.ToString() == expect;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    regkey.Close();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                MessageBox.Show(eRegistry + path);
                return false;
            }
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void importRegistry(List<string> list)
        {
            tabControl1.Enabled = false;
            list.Insert(0, "Windows Registry Editor Version 5.00");
            if (writeToFile(tempImport, list))
            {
                startProcess(0, "import \"" + tempImport + "\"");
                deleteFile(tempImport);
            }
            tabControl1.Enabled = true;
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void blockUnblock(bool deny, string path, bool folder = false)
        {
            tabControl1.Enabled = false;
            if (deny)
            {
                if (folder)
                {
                    createDirectory(path);
                }
                else
                {
                    stopProcess(Path.GetFileName(path));
                }
                startProcess(5, "/f \"" + path + "\"");
                startProcess(6, "\"" + path + "\" /grant \"" + Environment.UserName + "\":f /c /l /q");
                startProcess(6, "\"" + path + "\" /deny \"*S-1-1-0:(W,D,X,R,RX,M,F)\" \"*S-1-5-7:(W,D,X,R,RX,M,F)\"");
            }
            else
            {
                startProcess(4, "-windowstyle hidden -executionpolicy remotesigned -Command \"& Get-Acl -Path '" + (folder ? Path.Combine(programFilesX86, "Microsoft") : Path.Combine(folderSystem, "control.exe")) + "' | Set-Acl -Path '" + path + "'\"");
            }
            tabControl1.Enabled = true;
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void startProcess(int index, string args = null)
        {
            tabControl1.Enabled = false;
            Process process = new Process();
            process.StartInfo.FileName = exeList[index];
            if (args != null)
            {
                process.StartInfo.Arguments = args;
            }
            process.StartInfo.CreateNoWindow = true;
            try
            {
                process.Start();
                if (index != 2 && index != 9)
                {
                    process.WaitForExit();
                    if (index != 1 && process.HasExited && process.ExitCode > 0)
                    {
                        Clipboard.SetText(exeList[index] + " " + args);
                        MessageBox.Show(eStart + exeList[index] + " " + args);
                    }
                }
            }
            catch
            {
                Clipboard.SetText(exeList[index] + " " + args);
                MessageBox.Show(eStart + exeList[index] + " " + args);
            }
            tabControl1.Enabled = true;
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void stopProcess(string name)
        {
            tabControl1.Enabled = false;
            startProcess(1, "/f /im \"" + name + "\"");
            int wait = 0;
            while (true)
            {
                Process[] processes = Process.GetProcessesByName(name);
                if (processes.Length > 0)
                {
                    if (wait > 1000)
                    {
                        break;
                    }
                    Thread.Sleep(10);
                    wait += 10;
                }
                else
                {
                    break;
                }
            }
            tabControl1.Enabled = true;
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        List<string> readTextFile(string path)
        {
            try
            {
                return new List<string>(File.ReadAllLines(path));
            }
            catch
            {
                MessageBox.Show(eRead + path);
            }
            return new List<string>();
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        bool writeToFile(string path, List<string> list)
        {
            try
            {
                File.WriteAllLines(path, list, new UTF8Encoding(false));
                return true;
            }
            catch
            {
                MessageBox.Show(eWrite + path);
                return false;
            }
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void deleteFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                MessageBox.Show(eDelete + path);
            }
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void deleteFolder(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
                MessageBox.Show(eDelete + path);
            }
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void createDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch
            {
                MessageBox.Show(eWrite + path);
            }
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        bool getAccessFile(string path)
        {
            try
            {
                FileStream fs = File.OpenRead(path);
                fs.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        bool getAccessFolder(string path)
        {
            try
            {
                Directory.GetDirectories(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        bool dialogResult(string message, string title)
        {
            tabControl1.Enabled = false;
            DialogResult dialog = MessageBox.Show(message, title, MessageBoxButtons.YesNo);
            tabControl1.Enabled = true;
            return dialog == DialogResult.Yes;
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void buttonHelp_Click(object sender, EventArgs e)
        {
            labelMain.Focus();
            startProcess(12);
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void buttonRefresh_Click(object sender, EventArgs e)
        {
            labelMain.Focus();
            refrashValues();
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void buttonMinimize_Click(object sender, EventArgs e)
        {
            labelMain.Focus();
            WindowState = FormWindowState.Minimized;
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void buttonClose_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void mainLabels_MouseDown(object sender, MouseEventArgs e)
        {
            lastLocation = e.Location;
            ((Label)sender).MouseMove += mainLabels_MouseMove;
            ((Label)sender).MouseLeave += mainLabels_MouseLeave;
        }
        void mainLabels_MouseUp(object sender, MouseEventArgs e)
        {
            ((Label)sender).MouseMove -= mainLabels_MouseMove;
            ((Label)sender).MouseLeave -= mainLabels_MouseLeave;
        }
        void mainLabels_MouseLeave(object sender, EventArgs e)
        {
            ((Label)sender).MouseMove -= mainLabels_MouseMove;
            ((Label)sender).MouseLeave -= mainLabels_MouseLeave;
        }
        void mainLabels_MouseMove(object sender, MouseEventArgs e)
        {
            Location = new Point((Location.X - lastLocation.X) + e.X, (Location.Y - lastLocation.Y) + e.Y);
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void buttonHelp_MouseEnter(object sender, EventArgs e)
        {
            buttonHelp.BackgroundImage = Properties.Resources.buttonHelpGlow;
        }
        void buttonHelp_MouseLeave(object sender, EventArgs e)
        {
            buttonHelp.BackgroundImage = Properties.Resources.buttonHelp;
        }
        void buttonRefresh_MouseEnter(object sender, EventArgs e)
        {
            buttonRefresh.BackgroundImage = Properties.Resources.buttonRefreshGlow;
        }
        void buttonRefresh_MouseLeave(object sender, EventArgs e)
        {
            buttonRefresh.BackgroundImage = Properties.Resources.buttonRefresh;
        }
        void buttonMinimize_MouseEnter(object sender, EventArgs e)
        {
            buttonMinimize.BackgroundImage = Properties.Resources.buttonMinimizeGlow;
        }
        void buttonMinimize_MouseLeave(object sender, EventArgs e)
        {
            buttonMinimize.BackgroundImage = Properties.Resources.buttonMinimize;
        }
        void buttonClose_MouseEnter(object sender, EventArgs e)
        {
            buttonClose.BackgroundImage = Properties.Resources.buttonCloseGlow;
        }
        void buttonClose_MouseLeave(object sender, EventArgs e)
        {
            buttonClose.BackgroundImage = Properties.Resources.buttonClose;
        }
        // ------------------------------------------------ BORDER OF FUNCTION ------------------------------------------------ //
        void toEnglish()
        {
            contex_PinToTaskbar.Text = "Pin to taskbar";
            contex_StartMenuExt.Text = "Pin for Classic Shell";
            contex_PintoStartScreen.Text = "Pin to home screen";
            contex_PinToHome.Text = "Pin to Quick Access Toolbar";
            contex_Compatibility.Text = "Fix compatibility issues";
            contex_ModernSharing.Text = "Send (Sharing)";
            contex_SendTo.Text = "Send";
            contex_Sharing.Text = "Grant access to";
            contex_LibraryLocation.Text = "Add to Library";
            contex_PinToHomeFile.Text = "Add to Favourites";
            contex_label1.Text = "Context menu items:";
            fix_NetTrayIcon.Text = "Network icon";
            fix_label1.Text = "Various fixes:";
            service_RestartExplorer.Text = "Restart explorer";
            service_Store.Text = "Store Install Service";
            service_SystemRestore.Text = "System Restore Task";
            service_Sketch.Text = "Screenshot by Win+Shift+S";
            service_DefaultStart.Text = "Default Start";
            service_NewContexMenu.Text = "New contex menu";
            service_ResetFolders.Text = "Reset folders";
            service_ResetMixer.Text = "Reset mixer";
            service_ResetCompatibility.Text = "Reset compatibility";
            service_ResetHistory.Text = "Reset history";
            service_UpdateCertificate.Text = "Update certificates";
            service_Firewall.Text = "Firefall Windows";
            service_label1.Text = "Various service commands:";
            tabPage1.Text = "Service";
            tabPage2.Text = "Context menu";
            tabPage3.Text = "This computer";
            tabPage4.Text = "Fixes";
            thispc_Desktop.Text = "Desktop";
            thispc_Documents.Text = "Documents";
            thispc_3DObjects.Text = "3D objects";
            thispc_Downloads.Text = "Downloads";
            thispc_Images.Text = "Images";
            thispc_Music.Text = "Music";
            thispc_Video.Text = "Video";
            thispc_label1.Text = "This computer elements:";
        }
    }
}
