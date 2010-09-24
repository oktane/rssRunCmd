using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using RSS;
using Ini;


namespace rssRunCmd
{
    class Program
    {
        static string exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
        static IniFile ini = new IniFile(exePath + @"\\rssRunCmd_config.ini");
        // Feed URL
        static string rssURL = GetRSSUrl();
        static Uri rssURI = new Uri(rssURL);

        // Strip Item URL to Filename via regex
        static string urlStrip = GetURLStrip();
        static string fileNameRegEx = urlStrip;

        // the uid to extract from the url, like a version number.. used for caching and optionally an action param
        static string urlExtract = GetURLExtract();
        static Regex urlRegEx = new Regex(urlExtract);
        // action to take on items
        static string action_exe = GetAction();
        static string action_cwd = GetActionDir();
        static string action_params = GetActionParams();

        static void Main(string[] args)
        {
            int numItems = 0;
            int numFound = 0;
            int numCached = 0;
            int numDone = 0;
            int numFail = 0;
            Console.WriteLine("rssRunCmd - oktane 2010");
            Console.WriteLine("Tool for running action when new RSS item is detected in feed.");
            RssChannel channel = RssChannel.Read(rssURI);
            if ((channel != null))
            {
                numItems = channel.Rss.Items.Count;
                if (numItems > 0)
                {

                    Console.WriteLine();
                    foreach (Item item in channel.Rss.Items)
                    {
                        string filename = Regex.Replace(item.Link, fileNameRegEx, "");
                        Match versionMatch = urlRegEx.Match(filename);
                        if (versionMatch.Success)
                        {
                            numFound++;
                            string urlUniqueID = versionMatch.Result("$1");
                            if (!CheckCache(urlUniqueID))
                            {

                                Console.Write("Processing {0} ({1}) ", urlUniqueID, item.PubDate);
                                if (RunAction(urlUniqueID, filename, item.Link, item.PubDate, item.Title, item.Description))
                                {
                                    WriteCache(urlUniqueID);
                                    Console.WriteLine("OK");
                                    numDone++;
                                }
                                else
                                {
                                    Console.WriteLine("FAIL");
                                    numFail++;
                                }

                            }
                            else
                            {
                                numCached++;
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No RSS Items.");
                }
            }
            else
            {
                Console.WriteLine("No Data from RSS feed.");
            }
            Console.WriteLine("RSS Items: {0} matched, [{1} done, {2} failed, {3} ignored], {4} total.", numFound, numDone, numFail, numCached, numItems);
            Console.WriteLine("Exit in 3 seconds..");
            Thread.Sleep(3000);
        }
        static bool RunAction(string version, string filename, string url, string pubdate, string title, string description)
        {
            Process action = new Process();
            string exeName = GetAction();
            action.StartInfo.FileName = exeName;
            string exeParams = GetActionParams();
            if (exeParams.Length > 0) action.StartInfo.Arguments = string.Format(exeParams, version, filename, url, pubdate, title, description);
            string exeDir = GetActionDir();
            if (exeDir.Length > 0 || exeDir != ".") action.StartInfo.WorkingDirectory = exeDir;
            if (action.StartInfo.FileName != "changeme")
            {
                try
                {
                    action.Start();
                    while (!action.HasExited)
                    {
                        SpinnySticks(100);
                    }
                    if (action.ExitCode == 0)
                    {
                        return true;
                    }
                    else
                    {
                        Console.Write("[Exitcode: {0}] ", action.ExitCode);
                    }
                }
                catch (Exception e)
                {
                    Console.Write("Error running action! {0} ", e.Message);
                }
            }
            else
            {
                Console.Write("Edit INI file first! ");
            }
            return false;
        }

        static string GetRSSUrl()
        {
            string rssURL = @"http://localhost/feed.xml";
            rssURL = ini.IniReadValue("options", "feedURL", @"http://localhost/feed.xml");
            if (rssURL == "http://localhost/feed.xml" || rssURL == "")
            {
                ini.IniWriteValue("options", "feedURL", @"http://localhost/feed.xml");
                rssURL = @"http://localhost/feed.xml";
            }
            return rssURL;
        }

        static string GetURLStrip()
        {
            string urlStrip = @"^.*/";
            urlStrip = ini.IniReadValue("options", "urlFilename_RegEx", @"^.*/");
            if (urlStrip == @"^.*/" || urlStrip == "")
            {
                ini.IniWriteValue("options", "urlFilename_RegEx", @"^.*/");
                urlStrip = @"^.*/";
            }
            return urlStrip;
        }
        static string GetURLExtract()
        {
            string urlExtract = @"^Application_Build_(\d+)\.zip";
            urlExtract = ini.IniReadValue("options", "urlUID_RegEx", @"^Application_Build_(\d+)\.zip");
            if (urlExtract == @"^Application_Build_(\d+)\.zip" || urlExtract == "")
            {
                ini.IniWriteValue("options", "urlUID_RegEx", @"^Application_Build_(\d+)\.zip");
                urlExtract = @"^Application_Build_(\d+)\.zip";
            }
            return urlExtract;
        }

        static string GetAction()
        {
            string exeToRun = "changeme";
            exeToRun = ini.IniReadValue("options", "exeToRun", "changeme");
            if (exeToRun == "changeme" || exeToRun == "")
            {
                ini.IniWriteValue("options", "exeToRun", "changeme");
                exeToRun = "changeme";
            }
            return exeToRun;
        }
        static string GetActionDir()
        {
            string workingDir = ".";
            workingDir = ini.IniReadValue("options", "exeWorkingDir", ".");
            if (workingDir == "." || workingDir == "")
            {
                ini.IniWriteValue("options", "exeWorkingDir", ".");
            }
            return workingDir;
        }
        static string GetActionParams()
        {
            string actionParams = "{0} \"{1}\"";
            actionParams = ini.IniReadValue("options", "exeParams", "{0} \"{1}\"");
            if (actionParams == "{0} \"{1}\"")
            {
                ini.IniWriteValue("options", "exeParams", "{0} \"{1}\"");
            }
            return actionParams;
        }

        static bool CheckCache(string version)
        {
            if (ini.IniReadValue("ignore", version, "notfound") == "notfound")
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        static void WriteCache(string version)
        {
            ini.IniWriteValue("ignore", version, "processed");
        }

        static void SpinnySticks(int duration)
        {

            char[] sticks = { '/', '-', '\\', '|' };
            int i = 0;
            int cursorpos = Console.CursorLeft;
            while (i <= 3)
            {
                Console.Write("{0}", sticks[i]);
                Console.CursorLeft = cursorpos;
                i++;
                Thread.Sleep(duration);
            }
        }
    }

}
