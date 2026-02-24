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

        
                if (msg == "WHO_IS_GATEWAY?")
                {
                    string ip = GetWifiIPAddress();
                    Console.WriteLine($"📩 ip is {ip}");
                    if (ip == null) continue;

                    string reply = $"I_AM_GATEWAY:{ip}:{tcpPort}";
                    Console.WriteLine($"replying to discovery request... {reply}");


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
        if (ni.OperationalStatus != OperationalStatus.Up)
            continue;

        // On Linux (Raspberry Pi), WiFi interface name is usually: wlan0
        // On Windows: Wi-Fi
        // So we detect by NAME, not by NetworkInterfaceType
        string name = ni.Name.ToLower();

        if (!name.Contains("wlan") && !name.Contains("wi-fi") && !name.Contains("wifi"))
            continue;

        foreach (var addr in ni.GetIPProperties().UnicastAddresses)
        {
            if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(addr.Address))
            {
                return addr.Address.ToString();
            }
        }
    }

    return null;
}
}
