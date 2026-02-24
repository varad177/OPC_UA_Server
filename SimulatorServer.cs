using Opc.Ua;
using Opc.Ua.Server;

public sealed class SimulatorServer : StandardServer
{
    protected override MasterNodeManager CreateMasterNodeManager(
        IServerInternal server,
        ApplicationConfiguration configuration)
    {
        return new MasterNodeManager(
            server,
            configuration,
            null,
            new[] { new SimulatorNodeManager(server, configuration) }
        );
    }
}
