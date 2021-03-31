using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Net;
using IWshRuntimeLibrary;
using System.Diagnostics;

namespace RaidUtil
{
    class Program
    {
        private static string appFolder = "";
        private static string command;
        private static string toon;
        private static string server;

        public static string LogFolder = "FuRaidTool Logs";


        [STAThread]
        static void Main(string[] args)
        {
            bool debug = false;
            if (IsInstalled() || debug) {
                if (!HasConfig() && !debug)
                {
                    // make an empty config file
                    BuildEmptyConfigFile();
                    command = "config";
                }
            }
            else
            {
                // install and create config file
                if (MessageBox.Show("About to install", "FuRaidTool", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    Install();
                }
                return; // probably wont get hit because install kills this process
            }

            appFolder = Path.GetDirectoryName(Application.ExecutablePath);

            float minutes = -1;

            if (command == "config" || args.Length == 0) 
            {
                command = "config";
                //command = "loot";
            }
            else if (args.Length == 1)
            {
                command = args[0];
            }
            else
            { 
                command = args[0].ToLower();
                toon = args[1];

                if (args.Length > 2)
                {
                    server = args[2];
                }
                else
                {
                    server = "bristle";
                }
            }

            switch (command)
            {
                case "config":
                    FuHttp f = new FuHttp();
                    ConfigForm cf = new ConfigForm();
                    cf.AppPath = Application.ExecutablePath; 
                    cf.ShowDialog();
                break;
                case "loot":
                    if (minutes == -1)
                    {
                        if (!float.TryParse(Cfg.get("LootMinutes"), out minutes))
                        {
                            minutes = 10;
                        }
                    }
                    if (UpdateLoot(toon, server, minutes))
                    {
                        SendLogsToServer();
                        Process.Start("http://fuworldorder.net/admin/loot/loot_assignments.php");
                    }
                break;
                case "lootlookup":
                    if (minutes == -1)
                    {
                        if (!float.TryParse(Cfg.get("LootLookupMinutes"), out minutes))
                        {
                            minutes = 3;
                        }
                    }
                    DoLootLookup(toon,server,minutes);
                break;
                case "attendance":
                    if (UpdateRaidAttendance(toon,server))
                    {
                        SendLogsToServer();
                        Process.Start("http://fuworldorder.net/admin/raid/attendance.php");
                    }
                break;
                case "guildroster":
                    if (UpdateGuildRoster(toon,server))
                    {
                        SendLogsToServer();
                        Process.Start("http://fuworldorder.net/admin/raid/attendance.php");
                    }
                    break;
                case "sendlogs":
                    SendLogsToServer();
                break;
            }
        }

        public static string GetAppFolder()
        {
            return Path.GetDirectoryName(Application.ExecutablePath);
        }

        private static bool IsInstalled()
        {
            string fupath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FuEQ");
            string exepath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            if (fupath == exepath) return true;
            return false;
        }

        private static bool HasConfig()
        {
            return System.IO.File.Exists(Application.ExecutablePath + ".config");
        }
        
        private static void Install()
        {
            string fupath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FuEQ");

            string exeName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

            if (!Directory.Exists(fupath))
            {
                Directory.CreateDirectory(fupath);
                if (!Directory.Exists(fupath))
                {
                    MessageBox.Show("Unable to create FuEQ folder, install failed");
                    return;
                }
            }
            else
            {
                if (fupath != Environment.CurrentDirectory)
                {
                    System.IO.File.Delete(Path.Combine(fupath, exeName));
                }
            }

            // copy self to fupath
            System.IO.File.Copy(Path.Combine(Environment.CurrentDirectory, exeName), Path.Combine(fupath, exeName));

            // setup logs folder
            Directory.CreateDirectory(Path.Combine(fupath, LogFolder));

            // create desktop shortcut
            string shortcutpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FuEQ.lnk");
            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutpath);
            shortcut.Description = "FU Raid Tool Config";   // The description of the shortcut
            //shortcut.IconLocation = Path.Combine(fupath, exeName);           // The icon of the shortcut (doesnt seem needed)
            shortcut.TargetPath = Path.Combine(fupath, exeName);                 // The path of the file that will launch when the shortcut is run
            shortcut.Save();

            // if no existing config file make one
            if (!System.IO.File.Exists(Path.Combine(fupath, exeName + ".config")))
            {
                BuildEmptyConfigFile(Path.Combine(fupath, exeName + ".config"));
            }
            
            // start a new instance of this app out of the new folder (no params)
            Process.Start(Path.Combine(fupath, exeName));

            // close the current instance
            Process.GetCurrentProcess().Kill();
        }

        public static string GetLatestVersionNumber()
        {
            string version = "Unknown";



            return version;
        }



        public static bool UpdateLoot(string toon, string server, float minutes)
        {
            // parse and build upload logs
            MemoryStream loot = FuParse.ParseLoot(toon, server, minutes);
            if (loot.Length < 10)
            {
                MessageBox.Show("The loot log is empty :(");
                return false;
            }
            else
            {
                string lootFilename = String.Format("loot_{0}_{1}_{2}.txt", server, toon, DateTime.Now.ToString("yyyyMMdd-hhmmss"));
                string lootPath = Path.Combine(GetAppFolder(), LogFolder, lootFilename);
                FileStream fs = new FileStream(lootPath, FileMode.Append);
                loot.Position = 0;
                loot.CopyTo(fs);
                fs.Close();
                return true;
            }
        }

        public static bool UpdateRaidAttendance(string toon, string server)
        {
            // parse and build upload logs
            MemoryStream part1 = FuParse.ParseRoster(toon, server);
            // I picked 10 because "empty" ones seem to have a length of 3, so yeah
            if (part1.Length < 10)
            {
                MessageBox.Show("Problem with the Guild Roster.");
                return false;
            }
            else
            {
                MemoryStream part2 = FuParse.ParseAttendance(toon, server);
                if (part2.Length < 10)
                {
                    MessageBox.Show("Unable to locate or process your raid dump file.");
                    return false;
                }
                else
                {
                    string raidFilename = String.Format("raid_{0}_{1}_{2}.txt", server, toon, DateTime.Now.ToString("yyyyMMdd-hhmmss"));
                    string raidPath = Path.Combine(GetAppFolder(), LogFolder, raidFilename);
                    FileStream fs = new FileStream(raidPath, FileMode.Append);
                    part1.CopyTo(fs);
                    new MemoryStream(Encoding.ASCII.GetBytes("FU-DEMILITER-THINGER")).CopyTo(fs);
                    part2.CopyTo(fs);
                    fs.Close();
                    return true;
                }
            }
        }

       public static bool UpdateGuildRoster(string toon, string server)
        {
            MemoryStream rost = FuParse.ParseRoster(toon, server);
            if (rost.Length < 10)
            {
                MessageBox.Show("Guild Roster is Empty :(");
                return false;
            }
            else
            {
                // parse and build upload logs
                string guildFilename = String.Format("guild_{0}_{1}_{2}.txt", server, toon, DateTime.Now.ToString("yyyyMMdd-hhmmss"));
                string guildPath = Path.Combine(GetAppFolder(), LogFolder, guildFilename);
                FileStream fs = new FileStream(guildPath, FileMode.Append);
                rost.CopyTo(fs);
                fs.Close();
                return true;
            }
        }

        public static bool DoLootLookup(string toon, string server, float minutes, string defaultName = "")
        {
            string namelist = FuParse.ParseLootLookup(toon, server, minutes);
            if (namelist.Length == 0)
            {
                if (defaultName.Length > 0)
                {
                    namelist = defaultName;
                }
                else
                {
                    MessageBox.Show("Unable to find any tells in the last " + minutes + " minutes.");
                    return false;
                }
            }
            Process.Start("http://fuworldorder.net/admin/lootlookup?userlist=" + Uri.EscapeDataString(namelist));
            return true;
        }

        private static void BuildEmptyConfigFile(string path)
        {
            System.Text.StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
            sb.AppendLine("<configuration>");
            sb.AppendLine("<appSettings>");

            foreach (KeyValuePair<string, string> kvp in FuEQ.GetDefaultSettings())
            {
                sb.AppendLine(String.Format("<add key=\"{0}\" value=\"{1}\" />", kvp.Key, kvp.Value));
            }

            sb.AppendLine("</appSettings>");
            sb.AppendLine("</configuration>");

            System.IO.File.WriteAllText(path, sb.ToString());
        }

        private static void BuildEmptyConfigFile()
        {
            BuildEmptyConfigFile(String.Concat(Application.ExecutablePath, ".config"));
        }

        public static void SendLogsToServer()
        {
            // check for logs that didnt make it (either new or failed last time)
            string logPath = Path.Combine(GetAppFolder(), LogFolder);
            
            // should do them in the right order
            var logs = Directory.EnumerateFiles(logPath);

            List<string> failures = new List<string>();

            FuHttp h = new FuHttp();

            if (!h.Login(Cfg.getEncrypted("HttpUsername"), Cfg.getEncrypted("HttpPassword")))
            {
                //MessageBox.Show("There was a problem with your Fu Website credentials");
                return;
            }

            foreach (string log in logs)
            {
                string fname = log;
                string logFile = Path.GetFileName(log);
                if (logFile.StartsWith("COMPLETE_") || logFile.StartsWith("INCOMPLETE_"))
                {
                    // already finished, skip
                }
                else 
                {
                    try
                    {
                        string response = h.UploadLog(fname);
                        if (response.ToLower() == "success")
                        {
                            string newFilePath = Path.Combine(Path.GetDirectoryName(fname), String.Format("COMPLETE_{0}", Path.GetFileName(fname)));
                            System.IO.File.Move(fname, newFilePath);
                        } else {
                            string newFilePath = Path.Combine(Path.GetDirectoryName(fname), String.Format("INCOMPLETE_{0}", Path.GetFileName(fname)));
                            System.IO.File.Move(fname, newFilePath);
                            failures.Add(response);
                        }
                    } catch (Exception e) {
                        string newFilePath = Path.Combine(Path.GetDirectoryName(fname), String.Format("INCOMPLETE_{0}", Path.GetFileName(fname)));
                        System.IO.File.Move(fname, newFilePath);
                        failures.Add(e.Message);
                    }
                }
            }

            if (failures.Count > 0)
            {
                MessageBox.Show("Transmission error(s)!" + Environment.NewLine + String.Join(Environment.NewLine, failures.ToArray()));
            }

        }
    }
}
