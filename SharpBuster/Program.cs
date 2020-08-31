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
using System.Threading.Tasks;

namespace SharpBuster
{
    class Program
    {
        static async Task Main(string[] args)
        {
            CommandLineApplication commandLineApplication = new CommandLineApplication(throwOnUnexpectedArg: false);
            CommandArgument names = null;
            CommandOption url = commandLineApplication.Option(
                "-u", "The URL to brute force", CommandOptionType.SingleValue);
            CommandOption wordlist = commandLineApplication.Option(
                "-w", "The full path to the wordlist to use", CommandOptionType.SingleValue);
            CommandOption wordlistURL = commandLineApplication.Option(
                "--wordlisturl", "URL of wordlist to use to avoid writing to disk", CommandOptionType.SingleValue);
            CommandOption extensions = commandLineApplication.Option(
                "-e | --ext", "A comma separated list of extensions to append, ex: php,asp,aspx", CommandOptionType.SingleValue);
            CommandOption recursive = commandLineApplication.Option(
                "-r | --recursive", "Perform a recurisve search", CommandOptionType.SingleValue);
            commandLineApplication.HelpOption("-h | --help");
            commandLineApplication.OnExecute(async () =>
            {

                string wordlistRead = "";
                if(wordlist.HasValue() && wordlistURL.HasValue())
                {
                    Console.WriteLine("Can't use both wordlist and wordlisturl.");
                    return 0;
                }
                if(wordlist.HasValue())
                {
                    wordlistRead = File.ReadAllText(wordlist.Value());
                }
                else if(wordlistURL.HasValue())
                {
                    wordlistRead = GetWordlist(wordlistURL.Value());
                }
                string[] wordlistSeparated = wordlistRead.Split("\n");

                //if (extensions.HasValue() && !recursive.HasValue())
                //{
                 //   await RunExt(url.Value(), wordlistSeparated, extensions.Value().Split(","));
                //}
                if(extensions.HasValue() && !recursive.HasValue())
                {
                    
                    await RunExt(url.Value(), wordlistSeparated, extensions.Value().Split(","));
                }
                else
                {
                    await RunNormal(url.Value(), wordlistSeparated);
                }
                return 0;
            });
            commandLineApplication.Execute(args);
            
        }

        public static async Task GetDirectory(string url, string directory)
        {
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            HttpClient client = new HttpClient(clientHandler);
            try
            {
                var request = WebRequest.Create(url + "/" + directory);
                var response = (HttpWebResponse)await Task.Factory
                    .FromAsync<WebResponse>(request.BeginGetResponse,
                                            request.EndGetResponse,
                                            null);
                Console.Write(directory + ":");
                Console.WriteLine(response.StatusCode);

            }
            catch (System.Net.WebException ex)
            {
                //Console.WriteLine("404");
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

        public static async Task RunNormal(string url, string[] wordlist)
        {
            for (int i = 0; i < wordlist.Length; i++)
            {
                await GetDirectory(url, wordlist[i]);
            }
        }

        public static async Task RunExt(string url, string[] wordlist, string[] ext)
        {
            for (int i = 0; i < wordlist.Length; i++)
            {
                for(int j = 0; j < ext.Length; j++)
                {
                    await GetDirectory(url, wordlist[i] + "." + ext[j]);
                }
            }
        }

        public static async Task RunRecursive(string url, string[] wordlist, string[] extensions)
        {
            string[] recursiveList = new string[] { };
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            HttpClient client = new HttpClient(clientHandler);
            for (int i = 0; i < wordlist.Length; i++)
            {
                for(int j = 0; j < extensions.Length; j++)
                {
                    try
                    {
                        var request = WebRequest.Create(url + "/" + wordlist[i] + "." + extensions[j]);
                        var response = (HttpWebResponse)await Task.Factory
                            .FromAsync<WebResponse>(request.BeginGetResponse,
                                                    request.EndGetResponse,
                                                    null);
                        Console.Write(wordlist[i] + ":");
                        Console.WriteLine(response.StatusCode);
                        recursiveList.Append(wordlist[i] + "." + extensions[j]);
                    }
                    catch (System.Net.WebException ex)
                    {
                        //Console.WriteLine("404");
                    }
                }
                
            }

            while(recursiveList.Any())
            {
                for(int i = 0; i < recursiveList.Length; i++)
                {
                    for(int j = 0; j < wordlist.Length; j++)
                    {
                        for(int k = 0; k < extensions.Length; k++)
                        {
                            try
                            {
                                var request = WebRequest.Create(url + "/" + recursiveList[i] + "/" + wordlist[j] + "." + extensions[k]);
                                var response = (HttpWebResponse)await Task.Factory
                                    .FromAsync<WebResponse>(request.BeginGetResponse,
                                                            request.EndGetResponse,
                                                            null);
                                Console.Write(recursiveList[i] + "/" + wordlist[j] + ":");
                                Console.WriteLine(response.StatusCode);
                                recursiveList.Append(wordlist[j]);
                                Console.WriteLine("Added {0} to queue...", recursiveList[i] + "/" + wordlist[j] + "." + extensions[k]);

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
