using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;

public static class GatewayDiscoveryService
{
    public static void Start(int tcpPort)
    {
        Task.Run(() =>
        {
            using var udp = new UdpClient(8888);
            var ep = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine("🔎 UDP Discovery Service running on port 8888");

            while (true)
            {
            Console.WriteLine("sending discovery request...");

                var data = udp.Receive(ref ep);
                var msg = Encoding.UTF8.GetString(data);

            Console.WriteLine("data received: " + msg);
            Console.WriteLine("from: " + ep.Address);
                if (msg == "WHO_IS_GATEWAY?")
                {
                    string ip = GetWifiIPAddress();
                    if (ip == null) continue;

                    string reply = $"I_AM_GATEWAY:{ip}:{tcpPort}";

                    byte[] resp = Encoding.UTF8.GetBytes(reply);
                    udp.Send(resp, resp.Length, ep);

                    Console.WriteLine($"📣 Discovery reply sent: {reply} → {ep.Address}");
                }
            }
        });
    }

    private static string GetWifiIPAddress()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 &&
                ni.OperationalStatus == OperationalStatus.Up)
            {
                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        return addr.Address.ToString();
                }
            }
        }

        return null;
    }
}
