using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RaidUtil
{
    // this class contains the stuff that sends data from the client to the web site
    class FuHttp
    {
        private CookieAwareWebClient m_WebClient;
        private bool m_Authorized = false;

        public FuHttp()
        {
            m_WebClient = new CookieAwareWebClient();
        }

        private void PreparePost(string url)
        {
            m_WebClient.Headers.Add("Referer:" + url);
            m_WebClient.Headers.Add("Accept:text/html");
            m_WebClient.Headers.Add("User-Agent:FuRaidTool");
            m_WebClient.Headers.Add("Content-Type:application/x-www-form-urlencoded");
        }

        public bool Login(string username, string password)
        {
            Dictionary<string, string> d = new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password,
                ["redirect"] = "../index.php",
                ["login"] = "Log in"
            };

            byte[] result;

            PreparePost(Cfg.get("HttpLoginUrl"));

            try
            {
                result = m_WebClient.UploadData(Cfg.get("HttpLoginUrl"), FormBytes(d));
            }
            catch (WebException e)
            {
                MessageBox.Show(e.Message, "Error connecting to FU Website");
                m_Authorized = false;
                return false;
            }

            string html = Encoding.UTF8.GetString(result).ToLower();

            if (html.Contains(String.Format("welcome, {0}!", username.ToLower())))
            {
                m_Authorized = true;
                return true;
            }

            MessageBox.Show("Invalid FU Website Username/Password");
            m_Authorized = false;
            return false;

        }

        /*
        // update thing
        public bool IsUpdateAvailable()
        {
            Dictionary<string, string> d = new Dictionary<string, string>
            {
                ["command"] = "GetLatestVersion"
            };
            try
            {
                String url = Cfg.get("HttpUploadUrl");
                PreparePost(Cfg.get("EndpointURL"));

                byte[] toSend = FormBytes(d);

                byte[] respB = m_WebClient.UploadData(url, "POST", toSend);
                string response = Encoding.UTF8.GetString(respB);
                
                // check the response for success
                if (response.ToLower().Contains("success"))
                {
                    return "success";
                }

                return response;
            }
            catch (Exception e)
            {
                return e.Message;
            }

        }
        */
        // written as a test for the http system
        // send a PM to a user
        // might come in handy
        public void SendPM(string toUser, string subject, string message)
        {
            if (m_Authorized)
            {
                // In case you can only post a message from the message screen (cookie or form field), load this page to store the cookies in m_WebClient's auto-cookie manager
                m_WebClient.DownloadData("http://fuworldorder.net/forum/privmsg.php?folder=inbox");

                // build the "form"
                Dictionary<string, string> d = new Dictionary<string, string>
                {
                    ["sid"] = m_WebClient.SID,
                    ["username"] = toUser,
                    ["subject"] = subject,
                    ["message"] = message,
                    ["folder"] = "inbox",
                    ["mode"] = "post",
                    ["post"] = "Submit"
                };
                // add the required "form sending" headers to m_WebClient
                PreparePost("http://fuworldorder.net/forum/privmsg.php?folder=inbox");

                m_WebClient.UploadData("http://fuworldorder.net/forum/privmsg.php", FormBytes(d));
            }
        }

        public string UploadLog(string filePath)
        {
            if (!m_Authorized)
            {
                return "Bad username/password";
            }

            string fileName = Path.GetFileName(filePath);

            string[] bits = fileName.Split('_');

            if (bits.Length != 4)
            {
                return "Something did not compute";
            }

            Dictionary<string, string> d = new Dictionary<string, string>
            {
                ["sid"] = m_WebClient.SID,
                ["toon"] = bits[2],
                ["type"] = bits[0],
                ["server"] = bits[1],
                ["file"] = File.ReadAllText(filePath, Encoding.UTF8)
            };


            try
            {
                String url = Cfg.get("HttpUploadUrl");
                //m_WebClient.DownloadData(u);
                PreparePost(url);

                byte[] toSend = FormBytes(d);
                
                byte[] respB = m_WebClient.UploadData(url, "POST", toSend);
                string response = Encoding.UTF8.GetString(respB);
                // check the response for success
                if (response.ToLower().Contains("success"))
                {
                    return "success";
                }
                   
                return response;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        // convert a dictionary of form items into upload-ready byte array
        // also gets rid of windows line endings and replaces w linux
        private byte[] FormBytes(Dictionary<string, string> formItems)
        {
            List<string> data = new List<string>();
            foreach (KeyValuePair<string, string> kvp in formItems)
            {
                data.Add(String.Format("{0}={1}", kvp.Key, EscapeFormString(kvp.Value)));
            }
            return Encoding.UTF8.GetBytes(String.Join("&", data));
        }

        private string EscapeFormString(string toEscape)
        {
            // this encodes the form data
            // the provided Uri.Escapedatastring has a limit of 32k and our log files can easily exceed it so it must be split up
            int limit = 32000;

            StringBuilder sb = new StringBuilder();
            int loops = toEscape.Length / limit;

            for (int i = 0; i <= loops; i++)
            {
                if (i < loops)
                {
                    sb.Append(Uri.EscapeDataString(toEscape.Substring(limit * i, limit)));
                }
                else
                {
                    sb.Append(Uri.EscapeDataString(toEscape.Substring(limit * i)));
                }
            }
            return sb.ToString();
        }

    }
}
