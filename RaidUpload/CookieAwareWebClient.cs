using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace RaidUtil
{
    class CookieAwareWebClient : WebClient
    {
        public readonly CookieContainer m_container = new CookieContainer();
        
        // some forms require the session id (mod_security), so it's exposed here for that reason
        public string SID = "";

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            HttpWebRequest webRequest = request as HttpWebRequest;
            if (webRequest != null)
            {
                webRequest.CookieContainer = m_container;
            }
            CookieCollection cc = webRequest.CookieContainer.GetCookies(request.RequestUri);
            foreach (Cookie c in cc)
            {
                if (c.Name.ToLower() == "phpbb2mysql_sid")
                {
                    SID = c.Value;
                }
            }
            return request;
        }
    }
}
