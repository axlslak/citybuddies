using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using AOSharp.Clientless;
using AOSharp.Clientless.Common;
using Newtonsoft.Json;
using Serilog;
using Serilog.Core;

namespace CityBuddies
{
    internal static class Program
    {
        private const string ConfigFileName = "buddies.json";
        private static readonly List<ClientDomain> Domains = new List<ClientDomain>();

        private static int Main(string[] args)
        {
            if (ClientlessGameDataBootstrap.IsRestoreCommand(args))
                return ClientlessGameDataBootstrap.Run(args);

            Console.Title = "CityBuddies";
            Console.WriteLine("CityBuddies");
            Console.WriteLine("===========");

            try
            {
                List<BuddyAccount> accounts = ReadAccounts();
                Console.WriteLine($"Starting {accounts.Count} configured buddies...");

                Logger logger = new LoggerConfiguration()
                    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .MinimumLevel.Information()
                    .CreateLogger();

                string pluginPath = Assembly.GetExecutingAssembly().Location;

                foreach (BuddyAccount account in accounts)
                {
                    ClientDomain domain = Client.CreateInstance(
                        account.Username,
                        account.Password,
                        account.Character,
                        Dimension.RubiKa,
                        logger);

                    domain.LoadPlugin(pluginPath);
                    Domains.Add(domain);
                }

                Task<bool>[] starts = Domains
                    .Select((domain, index) => Task.Run(() => Start(domain, accounts[index])))
                    .ToArray();

                Task.WaitAll(starts);

                int started = starts.Count(task => task.Result);

                Console.WriteLine();
                Console.WriteLine($"Started {started} of {accounts.Count} configured buddy clients.");
                Console.WriteLine("Press ENTER to exit.");
                Console.ReadLine();
                return started == accounts.Count ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("CityBuddies could not start:");
                Console.WriteLine(ex.Message);
                Console.WriteLine();
                Console.WriteLine("Press ENTER to exit.");
                Console.ReadLine();
                return 1;
            }
        }

        private static bool Start(ClientDomain domain, BuddyAccount account)
        {
            try
            {
                Console.WriteLine($"Starting {account.Character} on account {account.Username}...");
                domain.Start();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not start {account.Character}: {ex.Message}");
                return false;
            }
        }

        private static List<BuddyAccount> ReadAccounts()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Put {ConfigFileName} beside Buddies.exe. See buddies.example.json for the format.",
                    path);
            }

            string json = File.ReadAllText(path);
            List<BuddyAccount> accounts = JsonConvert.DeserializeObject<List<BuddyAccount>>(json);

            if (accounts == null || accounts.Count == 0)
                throw new InvalidDataException($"{ConfigFileName} must contain at least one buddy account.");

            for (int index = 0; index < accounts.Count; index++)
            {
                BuddyAccount account = accounts[index];
                if (account == null ||
                    string.IsNullOrWhiteSpace(account.Username) ||
                    string.IsNullOrWhiteSpace(account.Password) ||
                    string.IsNullOrWhiteSpace(account.Character))
                {
                    throw new InvalidDataException(
                        $"Entry {index + 1} in {ConfigFileName} requires Username, Password, and Character.");
                }

                account.Username = account.Username.Trim();
                account.Character = account.Character.Trim();
            }

            return accounts;
        }

        private sealed class BuddyAccount
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Character { get; set; }
        }
    }
}
