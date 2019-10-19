using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Collections.Generic;
using System.IO;

namespace ChassisConsole
{
    class Program
    {
        private static ChromiumWebBrowser browser;
        private const string testUrl = "http://www.mobilit.fgov.be/WebdivPub/wmvpstv1.jsp";
        private const string chassisNr = "W0L0XCF6846053363";
        private static bool requested = false;


        static void Main(string[] args)
        {
            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;

            var settings = new CefSettings()
            {
                CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"CefSharp\Cache")
            };
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            browser = new ChromiumWebBrowser(testUrl);
            browser.LoadingStateChanged += BrowserLoadingStateChanged;

            Console.ReadKey();
            Cef.Shutdown();

        }

        private static void BrowserLoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            if (!e.IsLoading)
            {
                if (requested == false)
                {
                    requested = true;
                    // fill the username and password fields with their respective values, then click the submit button
                    var script = $@"document.querySelector('#CMV_WD_PBL01_CONSULT_STATUS_FRAME').firstChild.contentWindow.document.querySelector('#Writable3').value = '{chassisNr}';
                                document.querySelector('#CMV_WD_PBL01_CONSULT_STATUS_FRAME').firstChild.contentWindow.document.querySelector('#btnOphalen').click();";

                    browser.EvaluateScriptAsync(script).ContinueWith(u =>
                    {
                        Console.WriteLine("Gegevens mbt opgegeven chassisnr opgevraagd");
                    });
                }

                var pageQueryScript =
                @"(function(){
                    var lis = document.querySelector('#CMV_WD_PBL01_CONSULT_STATUS_FRAME').firstChild.contentWindow.document.querySelectorAll('input');
                    var result = [];
                    for(var i=0; i < lis.length; i++) { result.push(lis[i].value) } 
                    return result; 
                })()";
                var scriptTask = browser.EvaluateScriptAsync(pageQueryScript);
                scriptTask.ContinueWith(u =>
                {
                    if (u.Result.Success && u.Result.Result != null)
                    {
                        var response = (List<dynamic>)u.Result.Result;
                        foreach (string v in response)
                        {
                            if (!string.IsNullOrWhiteSpace(v))
                                Console.WriteLine(v);
                        }
                    }
                });
            }
        }
    }
}
