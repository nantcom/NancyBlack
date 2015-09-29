using Nancy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NantCom.NancyBlack.Modules.RobotSystem
{
    public class RobotModule : BaseModule
    {
        public RobotModule ()
        {
            Get["/robots.txt"] = this.HandleRequest((arg) =>
            {                
                var str = 
@"User-agent: *
Allow: /";
                var response = new Response();
                response.ContentType = "text/text";
                response.Contents = (s) => {
                    using (var sw = new StreamWriter(s)) { 
                        sw.Write(str);
                        sw.Flush();
                        sw.Close();                    
                    }
                };
                return response;
            });
        }

    }
}