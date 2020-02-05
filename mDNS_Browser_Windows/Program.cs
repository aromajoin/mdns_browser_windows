using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Mono.Zeroconf;

namespace mDNS_Browser_Windows
{
    class Program
    {
        private static bool resolve_shares = true;
        private static uint @interface = 0;
        private static AddressProtocol address_protocol = AddressProtocol.Any;
        private static string domain = "local";
        private static Dictionary<string, string> serial2IP = new Dictionary<string, string>();
        private static Dictionary<string, string> name2Serial = new Dictionary<string, string>();

        public static int Main(string[] args)
        {
            string type = "_http._tcp";
            ArrayList services = new ArrayList();

            Console.WriteLine("Hit ^C when you're bored waiting for responses.");
            Console.WriteLine();

            // Listen for events of some service type
            ServiceBrowser browser = new ServiceBrowser();
            browser.ServiceAdded += OnServiceAdded;
            browser.ServiceRemoved += OnServiceRemoved;
            browser.Browse(@interface, address_protocol, type, domain);

            while (true)
            {
                System.Threading.Thread.Sleep(1000);
            }

        }

        private static void OnServiceAdded(object o, ServiceBrowseEventArgs args)
        {
            if (resolve_shares)
            {
                Thread thread = new Thread(() => resolve(args));
                thread.Start();
            }
        }

        private static void resolve(ServiceBrowseEventArgs args)
        {
            args.Service.Resolved += OnServiceResolved;
            args.Service.Resolve();
        }

        private static void OnServiceRemoved(object o, ServiceBrowseEventArgs args)
        {
            if (name2Serial.ContainsKey(args.Service.Name))
            {
                Console.WriteLine("Disconnected Aroma Shooter: {0} at IP: {1}", name2Serial[args.Service.Name], serial2IP[name2Serial[args.Service.Name]]);
                serial2IP.Remove(name2Serial[args.Service.Name]);
                name2Serial.Remove(args.Service.Name);
            }
        }

        private static void OnServiceResolved(object o, ServiceResolvedEventArgs args)
        {
            IResolvableService service = o as IResolvableService;

            string ip = service.HostEntry.AddressList[0].ToString();
            string serial = "";

            ITxtRecord record = service.TxtRecord;
            int record_count = record != null ? record.Count : 0;
            if (record_count > 0)
            {
                for (int i = 0, n = record.Count; i < n; i++)
                {
                    TxtRecordItem item = record.GetItemAt(i);
                    if (item.Key == "device")
                    {
                        serial = item.ValueString;
                        Regex regexASNSerial = new Regex(@"AS\w{8}");
                        Match match;

                        match = regexASNSerial.Match(serial);

                        if (match.Success)
                        {
                            name2Serial.Add(args.Service.Name, serial);
                            serial2IP.Add(serial, ip);
                            Console.WriteLine("Found Aroma Shooter: serial = {0}, IP = {1}.", serial, ip);
                        }
                    }                        
                }
            }
        }

    }
}