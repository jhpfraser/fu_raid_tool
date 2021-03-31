using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Windows;

namespace RaidUtil
{
    // this class contains stuff to do with eq, like the default eq social button setup and the functions that get or set the social buttons

    class FuEQ
    {
        public static Dictionary<string, string> GetDefaultSettings()
        {
            Dictionary<string, string> d = new Dictionary<string, string>();

            d.Add("HttpLoginURL", "http://fuworldorder.net/forum/login.php");
            d.Add("HttpUploadURL", "http://fuworldorder.net/hava/testraidupload.php");
            d.Add("EqGuildName", "Fu World Order");
            d.Add("LootMinutes", "30");
            d.Add("LootLookupMinutes", "3");
            d.Add("LogFileScanSize", "20");

            return d;
        }

        public static List<string> GetToons()
        {
            return Cfg.get("_toons").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static void SetToons(List<string> toons)
        {
            Cfg.set("_toons", String.Join(",", toons));
        }

        public static string GetEQFolderForToon(string toon, string server)
        {
            Dictionary<string, string> section = Cfg.getList(String.Format("{0}-{1}", toon, server));
            if (section.ContainsKey("EqFolder"))
            {
                return section["EqFolder"];
            }
            return "";
        }

        public static void SetEQFolderForToon(string toon, string server, string folder)
        {
            Cfg.setList(String.Format("{0}-{1}", toon, server), new Dictionary<string, string> { ["EqFolder"] = folder });
        }

        public static List<List<SocialButton>> GetSocials(string toon, string server, string eqFolder) {

            string fileName = Path.Combine(eqFolder, String.Format("{0}_{1}.ini", toon, server));
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException();
            }

            Ini ini = new Ini(fileName);
            List<List<SocialButton>> pages = new List<List<SocialButton>>();

            for (int i = 1; i <= 10; i++) // 10 pages
            {
                List<SocialButton> socs = new List<SocialButton>();
                for (int j = 1; j <= 12; j++) // 12 buttons/page
                {
                    string id = String.Format("Page{0}Button{1}", i, j);
                    socs.Add(
                        new SocialButton {
                            Title = ini.GetValue(id + "Name", "Socials"),
                            Color = ini.GetValue(id + "Color", "Socials"),
                            Page = i,
                            Button = j,
                            Lines = new List<string>(
                                new String[] {
                                    ini.GetValue(id + "Line1", "Socials"),
                                    ini.GetValue(id + "Line2", "Socials"),
                                    ini.GetValue(id + "Line3", "Socials"),
                                    ini.GetValue(id + "Line4", "Socials"),
                                    ini.GetValue(id + "Line5", "Socials")
                                }
                            )
                        }
                    );
                }
                pages.Add(socs);
            }

            return pages;
        }

        public static void SaveSocial(string eqFolder, string toonName, string serverShortName, SocialButton soc)
        {
            string fileName = Path.Combine(eqFolder, String.Format("{0}_{1}.ini", toonName, serverShortName));
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException();
            }
            
            Ini ini = new Ini(fileName);

            string id = String.Format("Page{0}Button{1}", soc.Page, soc.Button);
            
            ini.WriteValue(id + "Name", "Socials", soc.Title);
            ini.WriteValue(id + "Color", "Socials", soc.Color);
            ini.WriteValue(id + "Line1", "Socials", soc.Lines[0]);
            ini.WriteValue(id + "Line2", "Socials", soc.Lines[1]);
            ini.WriteValue(id + "Line3", "Socials", soc.Lines[2]);
            ini.WriteValue(id + "Line4", "Socials", soc.Lines[3]);
            ini.WriteValue(id + "Line5", "Socials", soc.Lines[4]);

            ini.Save();

        }


    }

    class SocialButton
    {
        public string Title;
        public string Color;
        public int Page;
        public int Button;
        public List<string> Lines;

        public SocialButton()
        {
            this.Title = "";
            this.Color = "";
            this.Page = 0;
            this.Button = 0;
            this.Lines = new List<string>(new string[] { "", "", "", "", "" });
        }
    }

    class LootButton : SocialButton
    {
        public LootButton(string appPath, string toonName, string serverName)
        {
            this.Title = "FU-LootDump";
            this.Color = "";
            this.Lines = new List<string>(new string[]{
                String.Format("/system \"{0}\" Loot {1} {2}", appPath, toonName, serverName), 
                "",
                "",
                "",
                ""
            });
        }
    }
    class LootLookupButton : SocialButton
    {
        public LootLookupButton(string appPath, string toonName, string serverName)
        {
            this.Title = "FU-LootLookup";
            this.Color = "";
            this.Lines = new List<string>(new string[] { 
                String.Format("/system \"{0}\" LootLookup {1} {2}", appPath, toonName, serverName), 
                "", 
                "", 
                "", 
                "" 
            });
        }
    }
    class RaidAttendanceButton : SocialButton
    {
        public RaidAttendanceButton(string appPath, string toonName, string serverName)
        {
            this.Title = "FU-Attendance";
            this.Color = "";
            this.Lines = new List<string>(new string[] { 
                "/out raid", 
                "/out guild", 
                String.Format("/gu Taking Attendance -- tells to me for bench (IF NOT IN RAID)"),
                String.Format("/gu Taking Attendance -- tells to me for bench (IF NOT IN RAID)"),
                String.Format("/system \"{0}\" Attendance {1} {2}", appPath, toonName, serverName)
            });
        }
    }

    class GuildRosterButton : SocialButton
    {
        public GuildRosterButton(string appPath, string toonName, string serverName)
        {
            this.Title = "FU-GuildDump";
            this.Color = "";
            this.Lines = new List<string>(new string[] { 
                "/out guild", 
                String.Format("/system \"{0}\" GuildRoster {1} {2}", appPath, toonName, serverName),
                "",
                "",
                ""
            });
        }
    }
}
