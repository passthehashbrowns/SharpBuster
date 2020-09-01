using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SharpBuster
{
    class Program
    {

        public static class GlobalHttpHandler
        {
            public static HttpClientHandler clientHandler = new HttpClientHandler();

        }

        public static class ExecutionOptions
        {
            public static string[] wordlist { get; set; }
            public static string url { get; set; }
            public static int threadCount { get; set; }
            public static string[] extensions { get; set; }
        }
        
        static async Task Main(string[] args)
        {
            CommandLineApplication commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);
            commandLineApplication.Name = "SharpBuster";
            commandLineApplication.Description = "A C# directory brute forcing tool";
            CommandArgument names = null;
            
            CommandOption url = commandLineApplication.Option(
                "-u | --url", "The URL to brute force", CommandOptionType.SingleValue);
            CommandOption wordlist = commandLineApplication.Option(
                "-w | --wordlist", "The full path to the wordlist to use", CommandOptionType.SingleValue);
            CommandOption wordlistURL = commandLineApplication.Option(
                "-wu | --wordlisturl", "URL of wordlist to use to avoid writing to disk", CommandOptionType.SingleValue);
            CommandOption useHardcodedWordlist = commandLineApplication.Option(
                "-bi | --builtin", "Use this to hardcode a wordlist. You can do this by setting the hardcodedWordlist variable in the source code with a comma separated string. Can be used to avoid writing to disk or requesting a remote file.", CommandOptionType.SingleValue);
            CommandOption extensions = commandLineApplication.Option(
                "-e | --ext", "A comma separated list of extensions to append, ex: php,asp,aspx", CommandOptionType.SingleValue);
            CommandOption recursive = commandLineApplication.Option(
                "-r | --recursive", "Perform a recurisve search", CommandOptionType.SingleValue);
            CommandOption username = commandLineApplication.Option(
                "--username", "Username for basic authentication", CommandOptionType.SingleValue);
            CommandOption password = commandLineApplication.Option(
                "--password", "Password for authentication", CommandOptionType.SingleValue);
            CommandOption proxy = commandLineApplication.Option(
                "--proxy", "Address of proxy to use, ex: http://127.0.0.1:8080", CommandOptionType.SingleValue);
            CommandOption proxyCredentials = commandLineApplication.Option(
                "--proxy-creds", "Credentials to use to authenticate to proxy, ex: username:password", CommandOptionType.SingleValue);
            CommandOption cookie = commandLineApplication.Option(
                "--cookie", "Cookie to use, ex: myCookie=value | If multiple cookies are being used, separate them with a comma", CommandOptionType.SingleValue);
            CommandOption threadCount = commandLineApplication.Option(
                "--threads", "Number of threads to use", CommandOptionType.SingleValue);
            commandLineApplication.HelpOption("-h | --help");
            commandLineApplication.OnExecute(async () =>
            {
            Console.WriteLine(@" #####  #     #    #    ######  ######  ######  #     #  #####  ####### ####### ######  
#     # #     #   # #   #     # #     # #     # #     # #     #    #    #       #     # 
#       #     #  #   #  #     # #     # #     # #     # #          #    #       #     # 
 #####  ####### #     # ######  ######  ######  #     #  #####     #    #####   ######  
      # #     # ####### #   #   #       #     # #     #       #    #    #       #   #   
#     # #     # #     # #    #  #       #     # #     # #     #    #    #       #    #  
 #####  #     # #     # #     # #       ######   #####   #####     #    ####### #     # 
Author: @passthehashbrwn
");
            
            string wordlistRead = "";
            if(!url.HasValue())
                {
                    Console.WriteLine("Must have a target URL");
                    return 0;
                }
            if (wordlist.HasValue() && wordlistURL.HasValue())
            {
                Console.WriteLine("Can't use both wordlist and wordlisturl.");
                return 0;
            }
            if (wordlist.HasValue())
            {
                wordlistRead = File.ReadAllText(wordlist.Value());
                Console.Write("Target URL: {0} | Wordlist: {1} ", url.Value(), wordlist.Value());
            }
            else if (wordlistURL.HasValue())
            {
                wordlistRead = GetWordlist(wordlistURL.Value());
                Console.Write("Target URL: {0} | Wordlist: {1} ", url.Value(), wordlistURL.Value());
            }
            else if (useHardcodedWordlist.HasValue())
                {
                    string hardcodedWordlist = "";
                    wordlistRead = hardcodedWordlist;
                    Console.Write("Target URL: {0} | Wordlist: Using builtin wordlist ", url.Value());
                }
            else
                {
                    Console.WriteLine("Must provide a wordlist");
                    return 0;
                }
            if ((username.HasValue() && !password.HasValue()) || (!username.HasValue() && password.HasValue()))
            {
                Console.WriteLine("Must provide both username and password");
            }
            if (username.HasValue() && password.HasValue())
            {
                GlobalHttpHandler.clientHandler.Credentials = new NetworkCredential(username.Value(), password.Value());
                Console.Write("| Username: {0} | Password {1} ", username.Value(), password.Value());
            }
            if (proxy.HasValue())
            {
                WebProxy myProxy = new WebProxy();
                Uri myUri = new Uri(proxy.Value());
                myProxy.Address = myUri;
                if (proxyCredentials.HasValue())
                {
                    myProxy.Credentials = new NetworkCredential(proxyCredentials.Value().Split(":")[0], proxyCredentials.Value().Split(":")[1]);
                }
                GlobalHttpHandler.clientHandler.Proxy = myProxy;
                Console.Write("| Proxy: {0} ", proxy);
            }
            if(cookie.HasValue())
            {
                CookieContainer myCookie = new CookieContainer();
                string[] cookieSplit = cookie.Value().Split(",");
                for(int i = 0; i < cookieSplit.Length; i++)
                {
                    string cookieName = cookieSplit[i].Split("=")[0];
                    string cookieValue = cookieSplit[i].Split("=")[1];
                    myCookie.Add(new Cookie(cookieName, cookieValue));
                }
                GlobalHttpHandler.clientHandler.CookieContainer = myCookie;
            }
            GlobalHttpHandler.clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            ExecutionOptions.url = url.Value();
            ExecutionOptions.wordlist = wordlistRead.Split("\n");
            if(threadCount.HasValue())
                {
                    ExecutionOptions.threadCount = Int32.Parse(threadCount.Value());
                }
                else
                {
                    ExecutionOptions.threadCount = 2;
                }
            if(extensions.HasValue())
                {
                    ExecutionOptions.extensions = extensions.Value().Split(",");
                }
            //if (extensions.HasValue() && !recursive.HasValue())
            //{
                //   await RunExt(url.Value(), ExecutionOptions.wordlist, extensions.Value().Split(","));
            //}
            if(extensions.HasValue() && !recursive.HasValue())
            {
                Console.Write("| Total iterations: {0}", (ExecutionOptions.wordlist.Length * ExecutionOptions.extensions.Length).ToString());
                await RunExt(ExecutionOptions.wordlist);
                
            }
            else if(extensions.HasValue() && recursive.HasValue())
            {
                await RunRecursive(url.Value(), ExecutionOptions.wordlist, ExecutionOptions.extensions);
            }
            else
            {
                Console.Write("| Total iterations {0}", ExecutionOptions.wordlist.Length.ToString());
                await RunNormal(ExecutionOptions.wordlist);
                
            }
                Console.WriteLine(" ");
            return 0;
            });
            commandLineApplication.Execute(args);
            
        }

        public static async Task GetDirectory(string directory)
        {
            
            //HttpClientHandler clientHandler = new HttpClientHandler();
            //clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            HttpClient client = new HttpClient(GlobalHttpHandler.clientHandler);
            try
            {
                var request = WebRequest.Create(ExecutionOptions.url + "/" + directory);
                var response = (HttpWebResponse)await Task.Factory
                    .FromAsync<WebResponse>(request.BeginGetResponse,
                                            request.EndGetResponse,
                                            null);
                Console.Write(directory + ":");
                HandleStatusCode(response);

            }
            catch (WebException ex)
            {
                
            }
        }

        public static string GetWordlist(string url)
        {
            WebRequest request = WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string wordlist = reader.ReadToEnd();
            reader.Close();
            dataStream.Close();
            response.Close();
            return wordlist;
        }

        public static void HandleStatusCode(HttpWebResponse response)
        {
            var status = response.StatusCode;
            switch(status)
            {
                case HttpStatusCode.OK:
                    Console.WriteLine("200");
                    break;
                case HttpStatusCode.Forbidden:
                    Console.WriteLine("403");
                    break;
                case HttpStatusCode.InternalServerError:
                    Console.WriteLine("500");
                    break;
                case HttpStatusCode.MovedPermanently:
                    Console.WriteLine("301");
                    break;
                case HttpStatusCode.Redirect:
                    Console.Write("302 => ");
                    Console.WriteLine(response.Headers.Get("Location"));
                    break;
                case HttpStatusCode.Unauthorized:
                    Console.WriteLine("401");
                    break;
                case HttpStatusCode.NoContent:
                    Console.WriteLine("204");
                    break;
                case HttpStatusCode.RedirectKeepVerb:
                    Console.Write("307 => ");
                    Console.WriteLine(response.Headers.Get("Location"));
                    break;
                default:
                    Console.WriteLine(status.ToString());
                    break;
            }
        }

        public static async Task RunNormal(string[] wordlist)
        {
            //for (int i = 0; i < wordlist.Length; i++)
            //{
            //    await GetDirectory(url, wordlist[i]);
            //}
            await ParallelAsync.ForeachAsync(wordlist, ExecutionOptions.threadCount, async directory =>
            {
                await GetDirectory(directory);
            });
        }
        //https://stackoverflow.com/questions/19284202/how-to-correctly-write-parallel-for-with-async-methods
        public static class ParallelAsync
        {
            public static async Task ForeachAsync<T>(IEnumerable<T> source, int maxParallelCount, Func<T, Task> action)
            {
                using (SemaphoreSlim completeSemphoreSlim = new SemaphoreSlim(1))
                using (SemaphoreSlim taskCountLimitsemaphoreSlim = new SemaphoreSlim(maxParallelCount))
                {
                    await completeSemphoreSlim.WaitAsync();
                    int runningtaskCount = source.Count();

                    foreach (var item in source)
                    {
                        await taskCountLimitsemaphoreSlim.WaitAsync();

                        Task.Run(async () =>
                        {
                            try
                            {
                                await action(item).ContinueWith(task =>
                                {
                                    Interlocked.Decrement(ref runningtaskCount);
                                    if (runningtaskCount == 0)
                                    {
                                        completeSemphoreSlim.Release();
                                    }
                                });
                            }
                            finally
                            {
                                taskCountLimitsemaphoreSlim.Release();
                            }
                        }).GetHashCode();
                    }

                    await completeSemphoreSlim.WaitAsync();
                }
            }
        }

        public static async Task RunExt(string[] wordlist)
        {
            //for (int i = 0; i < wordlist.Length; i++)
            //{
            //await GetDirectory(wordlist[i]);
            //for(int j = 0; j < ExecutionOptions.extensions.Length; j++)
            //{
            //    await GetDirectory(wordlist[i] + "." + ExecutionOptions.extensions[j]);
            //}
            await ParallelAsync.ForeachAsync(wordlist, ExecutionOptions.threadCount, async directory =>
            {
                for (int j = 0; j < ExecutionOptions.extensions.Length; j++)
                {
                    await GetDirectory(directory + "." + ExecutionOptions.extensions[j]);
                }
            });
        }
            //}
        //}

        public static async Task RunRecursive(string url, string[] wordlist, string[] extensions)
        {
            List<string> recursiveList = new List<string>();
            //HttpClientHandler clientHandler = new HttpClientHandler();
            //clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            HttpClient client = new HttpClient(GlobalHttpHandler.clientHandler);
            for (int i = 0; i < wordlist.Length; i++)
            {
                try 
                {
                    var request = WebRequest.Create(url + "/" + wordlist[i]);
                    var response = (HttpWebResponse)await Task.Factory
                        .FromAsync<WebResponse>(request.BeginGetResponse,
                                                request.EndGetResponse,
                                                null);
                    Console.Write(wordlist[i] + ":");
                    HandleStatusCode(response);
                    recursiveList.Add(wordlist[i]);
                    Console.WriteLine("[+] Adding directory to queue...");
                }
                catch
                {

                }
                for (int j = 0; j < extensions.Length; j++)
                {
                    try
                    {
                        var request = WebRequest.Create(url + "/" + wordlist[i] + "." + extensions[j]);
                        var response = (HttpWebResponse)await Task.Factory
                            .FromAsync<WebResponse>(request.BeginGetResponse,
                                                    request.EndGetResponse,
                                                    null);
                        Console.Write(wordlist[i] + "." + extensions[j] + ":");
                        HandleStatusCode(response);

                    }
                    catch (System.Net.WebException ex)
                    {
                        //Console.WriteLine("404");
                    }
                }
            }
            Console.WriteLine("Recursive queue:");
            
            for(int i = 0; i < recursiveList.Count; i++)
            {
                Console.WriteLine(recursiveList[i]);
            }
            recursiveList.RemoveAt(0);
            while(recursiveList.Any())
            {
                for(int i = 0; i < recursiveList.Count; i++)
                {
                    for(int j = 0; j < wordlist.Length; j++)
                    {
                        try
                        {
                            string recurseURL = "";
                            if (recursiveList[i].StartsWith("/"))
                            {
                                recurseURL = url + recursiveList[i] + wordlist[j];
                            }
                            else
                            {
                                recurseURL = url + "/" + recursiveList[i] + wordlist[j];
                            }
                            var request = WebRequest.Create(recurseURL);
                            var response = (HttpWebResponse)await Task.Factory
                                .FromAsync<WebResponse>(request.BeginGetResponse,
                                                        request.EndGetResponse,
                                                        null);
                            Console.Write(recursiveList[i] + "/" + wordlist[j] + ":");
                            HandleStatusCode(response);
                            recursiveList.Add(recursiveList[i] + "/" + wordlist[j]);
                            Console.WriteLine("Added {0} to queue...", recursiveList[i] + wordlist[j]);

                        }
                        catch (System.Net.WebException ex)
                        {
                            //Console.WriteLine("404");
                        }
                        for (int k = 0; k < extensions.Length; k++)
                        {
                            try
                            {
                                var request = WebRequest.Create(url + "/" + recursiveList[i] + "/" + wordlist[j] + "." + extensions[k]);
                                var response = (HttpWebResponse)await Task.Factory
                                    .FromAsync<WebResponse>(request.BeginGetResponse,
                                                            request.EndGetResponse,
                                                            null);
                                Console.Write(recursiveList[i] + "/" + wordlist[j] + ":");
                                HandleStatusCode(response);

                            }
                            catch (System.Net.WebException ex)
                            {
                                //Console.WriteLine("404");
                            }
                        }                        
                    }
                }
            }
        }

    }
}
