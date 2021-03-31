using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;

namespace RaidUtil
{
    // this class contains the stuff that handles log file reading and parsing
    class FuParse
    {
        // fast read the last x megs of a file as a string array (of lines in that file)
        private static string[] ReadLog(string filePath, float megs)
        {
            // this function will break if you ask for more than 2GB at once
            // all of the FileStream method parameters we use are INT - with a cap of 2GB (fs.Seek, fs.Read)
            if (megs > 2048) megs = 2048;
            
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            
            int readLength = int.MaxValue;
            byte linefeed = Convert.ToByte('\n');
            int lf = (int)linefeed;
            int test;

            // if more megs were requested than exist, read the whole file
            // also if we do read the whole file, no need to delete the first entry
            if (fs.Length < readLength)
            {
                readLength = (int)fs.Length;
            }

            if (readLength > megs * 1024 * 1024)
            {
                readLength = (int) (megs * 1024 * 1024);
                fs.Seek(-readLength, SeekOrigin.End);

                // this chops off the first probably partial line of text
                do
                {
                    test = fs.ReadByte();
                    readLength--;
                }
                while (test != lf);
            } 

            if (readLength == 0) return new string[0];

            byte[] buffer = new byte[readLength];
            int count;
            int sum = 0;

            while ((count = fs.Read(buffer, sum, readLength - sum)) > 0)
            {
                sum += count;
            }

            fs.Close();

            // this might be non optimal
            string[] lineList = Encoding.ASCII.GetString(buffer).Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            
            return lineList;
        }

        public static MemoryStream ParseLoot(string toon, string server, float minutes)
        {
            float m = minutes / 2;
            if (m < 1) m = 1; // 1 meg min

            float c;
            if (float.TryParse(Cfg.get("LogFileScanSize"), out c))
            {
                if (m > c) m = c; //logfilescansize limit
            }
            else
            {
                if (m > 20) m = 20; // 20 meg limit
            }

            return ParseLoot(toon, server, minutes, DateTime.Now, m);
        }

        public static MemoryStream ParseLoot(string toon, string server, float minutes, DateTime when, float megs)
        {
            // rather than writing the output to a file we'll use a memory stream
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms,Encoding.ASCII);

            // load the log file
            string logPath = Path.Combine(FuEQ.GetEQFolderForToon(toon, server), "Logs", String.Format("eqlog_{0}_{1}.txt", toon, server));

            // datetime format that EQ uses in log file
            string dateFormat = "ddd MMM dd HH:mm:ss yyyy";

            // "before" is a datetime after which we start caring about the log entries.  RaidHours is configurable in config file
            DateTime before = when.AddMinutes(-minutes);

            string[] partFile = ReadLog(logPath, megs);

            DateTime d;

            foreach (string l in partFile)
            {
                // if the line doesnt contain at least the length of the date, ignore it
                if (l.Length < dateFormat.Length + 1) continue;

                // get a DateTime out of the log for this line, ignore the line if it didnt work
                if (DateTime.TryParseExact(l.Substring(1, dateFormat.Length), dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out d) != false)
                {
                    // if the line is after the "before" date and contains stuff we want, write it to the stream
                    if (
                        d >= before
                        && (
                            l.Contains("You say to your guild") // potential grats message from me
                            || l.Contains("tells the guild") // potential grats message from other
                            || l.Contains("] --") // potential looted message
                        )
                    )
                    {
                        sw.WriteLine(l);
                    }
                }
            }
            sw.Flush();
            ms.Position = 0;
            return ms;
        }

        private static string GetLastFile(string folder, string filespec)
        {
            IEnumerable<string> dir = Directory.EnumerateFiles(folder, filespec);
            if (dir.Count() == 0)
            {
                return ""; // file not found
            }
            string lastFile = dir.OrderByDescending(s => s).First();

            if (lastFile.Length == 0) return "";

            // check that the last file date is reasonably close to now
            FileInfo fi = new FileInfo(lastFile);
            int minutesDiff = (DateTime.Now - fi.LastWriteTime).Minutes;
            if (minutesDiff > 15)
            {
                System.Windows.Forms.DialogResult choice = System.Windows.Forms.MessageBox.Show(
                    String.Format("The file {0} is more than 15 minutes old, continue?", lastFile),
                    "Warning, that might be an old file",
                    System.Windows.Forms.MessageBoxButtons.YesNo
                );

                if (choice == System.Windows.Forms.DialogResult.Yes)
                {
                    return lastFile;
                }
                return "";
            }
            return lastFile;
        }

        public static MemoryStream ParseRoster(string toon, string server)
        {
            // get the guild dump
            // looking for last file of format fu world order-<datetime>.txt in eq\Fu World Order-20161226-090424.txt

            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms, Encoding.ASCII);
            string lastdumpfile = GetLastFile(FuEQ.GetEQFolderForToon(toon, server), String.Format("{0}_{1}-*.txt", Cfg.get("EqGuildName"), server));
            if (String.IsNullOrEmpty(lastdumpfile)) return ms;

            try
            {
                sw.Write(File.ReadAllText(lastdumpfile));
            }
            catch (Exception e)
            {
                return ms; // failure reading file
            }
            sw.Flush();

            ms.Position = 0;
            return ms;
        }

        public static MemoryStream ParseAttendance(string toon, string server)
        {
            MemoryStream ms = new MemoryStream();
            StreamWriter sw = new StreamWriter(ms, Encoding.ASCII);
            
            string lastdumpfile = GetLastFile(FuEQ.GetEQFolderForToon(toon, server), String.Format("RaidRoster_{0}-*.txt", server));
            if (String.IsNullOrEmpty(lastdumpfile))
            {
                System.Windows.Forms.MessageBox.Show(String.Format("There were no files matching {0} in the {1} folder", "RaidRoster-*.txt", FuEQ.GetEQFolderForToon(toon, server)));
                return ms;
            }

            try
            {
                string dump = File.ReadAllText(lastdumpfile);
                if (dump.Length > 0)
                {
                    sw.Write(dump);
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show(String.Format("The file {0} was empty", lastdumpfile));
                }

            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(String.Format("An exception occurred - {0}", e.Message));
                return ms; // failure reading file
            }
            sw.Flush();
            ms.Position = 0;
            return ms;
        }

        public static string ParseLootLookup(string toon, string server, float minutes)
        {
            float m = minutes / 2;
            if (m < 1) m = 1; // 1 meg min

            float c;
            if (float.TryParse(Cfg.get("LogFileScanSize"), out c)) {
                if (m > c) m = c; //logfilescansize limit
            } else
            {
                if (m > 20) m = 20; // 20 meg limit
            }

            return ParseLootLookup(toon, server, minutes, DateTime.Now, m);
        }

        public static string ParseLootLookup(string toon, string server, float minutes, DateTime when, float megs)
        {
            // load the log file
            string logPath = Path.Combine(FuEQ.GetEQFolderForToon(toon, server), "Logs", String.Format("eqlog_{0}_{1}.txt", toon, server));

            // datetime format that EQ uses in log file
            string dateFormat = "ddd MMM dd HH:mm:ss yyyy";

            // "before" is a datetime after which we start caring about the log entries.
            DateTime before = when.AddMinutes(-minutes);

            string[] partFile = ReadLog(logPath, megs);

            // list of names
            List<string> names = new List<string>();

            DateTime d;

            foreach (string l in partFile) { 
                // if the line doesnt contain at least the length of the date, ignore it
                if (l.Length < dateFormat.Length + 1) continue;

                // get a DateTime out of the log for this line, ignore the line if it didnt work
                if (DateTime.TryParseExact(l.Substring(1, dateFormat.Length), dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out d) != false)
                {
                    // if the line is after the "before" date and contains a tell
                    if (
                        d >= before
                        && (l.Contains("tells you") || l.Contains("told you"))
                    )
                    {
                        // extract the name and add it to the list
                        Regex re = new Regex(@"(\w+)\s+((tells)|(told))\s+you", RegexOptions.IgnoreCase);
                        Match m = re.Match(l);
                        if (m.Success && !names.Contains(m.Groups[1].Value)) {
                            names.Add(m.Groups[1].Value);
                        }
                    }
                }
            }
            // stick list together with commas
            return String.Join(",", names);
        }

    }
}
