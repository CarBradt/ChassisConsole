using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace ChassisConsole
{
    class Program
    {
        private static ChromiumWebBrowser browser;
        private const string chassisNr = "W0L0XCF6846053363";
        private const string testUrl = "http://www.mobilit.fgov.be/WebdivPub/wmvpstv1.jsp";
        private const string targetDocument = @"document.querySelector('#CMV_WD_PBL01_CONSULT_STATUS_FRAME').firstChild.contentWindow.document";
        private const string inputElementId = "#Writable3";
        private const string requestButtonId = "#btnOphalen";
        private static bool requested = false;
        private static Stopwatch timer;


        static void Main(string[] args)
        {
            timer = new Stopwatch();
            timer.Start();
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
                    var script = $@"{targetDocument}.querySelector('{inputElementId}').value = '{chassisNr}';
                                    {targetDocument}.querySelector('{requestButtonId}').click();";

                    browser.EvaluateScriptAsync(script).ContinueWith(u =>
                    {
                        Console.WriteLine("Gegevens mbt opgegeven chassisnr opgevraagd");
                    });
                }

                else
                {
                    var pageQuery = new StringBuilder();
                    pageQuery.Append(@"(function(){");
                    pageQuery.Append($@"var lis = {targetDocument}.querySelectorAll('input');");
                    pageQuery.Append(@"var result = [];");
                    pageQuery.Append(@"for(var i=0; i < lis.length; i++) { result.push(lis[i].value) } ");
                    pageQuery.Append(@"return result;");
                    pageQuery.Append(@"})()");
                    var scriptTask = browser.EvaluateScriptAsync(pageQuery.ToString());
                    pageQuery = null;

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
                            timer.Stop();
                            Console.WriteLine($"Elapsed time: {timer.Elapsed.TotalSeconds} seconds");
                        }
                    });
                }
            }
        }
    }
}
