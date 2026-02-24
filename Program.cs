using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

internal class Program
{
    static async Task Main()
    {

        GatewayDiscoveryService.Start(9000);

        var application = new ApplicationInstance
        {
            ApplicationName = "TMind Edge Gateway",
            ApplicationType = ApplicationType.Server
        };

        var config = new ApplicationConfiguration
        {
            ApplicationName = "TMind Edge Gateway",
            ApplicationType = ApplicationType.Server,

            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = "pki/own",
                    SubjectName = "CN=TMind Edge Gateway"
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = "pki/trusted"
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = "pki/rejected"
                },
                AutoAcceptUntrustedCertificates = true
            },

            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses =
                {
                    "opc.tcp://localhost:4840/TMindGateway"
                }
            },

            TransportQuotas = new TransportQuotas(),
            TraceConfiguration = new TraceConfiguration()
        };

        await config.Validate(ApplicationType.Server);
        application.ApplicationConfiguration = config;

        bool certOk = await application.CheckApplicationInstanceCertificate(true, 2048);
        if (!certOk)
            throw new Exception("Certificate invalid");

        await application.Start(new SimulatorServer());

        Console.WriteLine("✅ TMind OPC UA Gateway Running");
        Console.WriteLine("📡 OPC UA Endpoint: opc.tcp://localhost:4840/TMindGateway");
        Console.WriteLine("📡 TCP Server Port: 9000");
        Console.WriteLine("Press ENTER to stop...");
        Console.ReadLine();
    }
}
