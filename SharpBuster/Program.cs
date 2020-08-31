using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
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
                for (int i = 0; i < wordlistSeparated.Length; i++)
                {
                    await GetDirectory(url.Value(), wordlistSeparated[i]);
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

    }
}
