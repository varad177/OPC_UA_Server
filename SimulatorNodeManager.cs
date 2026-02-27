using Opc.Ua;
using Opc.Ua.Server;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

public sealed class SimulatorNodeManager : CustomNodeManager2
{
    private BaseDataVariableState _voltage = null!;
    private BaseDataVariableState _temperature = null!;
    private BaseDataVariableState _sound = null!;

    private IDictionary<NodeId, IList<IReference>> _externalReferences = null!;

    public SimulatorNodeManager(
        IServerInternal server,
        ApplicationConfiguration configuration)
        : base(server, configuration, "http://tmind/gateway/")
    {
        SystemContext.NodeIdFactory = this;
    }

    public override void CreateAddressSpace(
        IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        _externalReferences = externalReferences;

        var objects = ObjectIds.ObjectsFolder;

        var plant = CreateFolder(objects, "MUMBAI_PLANT", "Plant=MUMBAI_PLANT");
        var line = CreateFolder(plant.NodeId, "ASSEMBLY_01", "Plant=MUMBAI_PLANT/Line=ASSEMBLY_01");
        var machine = CreateFolder(line.NodeId, "CNC_02", "Plant=MUMBAI_PLANT/Line=ASSEMBLY_01/Machine=CNC_02");

        _voltage = CreateVariable(
            machine,
            "VOLTAGE",
            "Plant=MUMBAI_PLANT/Line=ASSEMBLY_01/Machine=CNC_02/Signal=VOLTAGE",
            DataTypeIds.Double,
            0.0
        );

        _temperature = CreateVariable(
            machine,
            "TEMP",
            "Plant=MUMBAI_PLANT/Line=ASSEMBLY_01/Machine=CNC_02/Signal=TEMP",
            DataTypeIds.Double,
            0.0
        );
        _sound = CreateVariable(
            machine,
            "SOUND",
            "Plant=MUMBAI_PLANT/Line=ASSEMBLY_01/Machine=CNC_02/Signal=SOUND",
            DataTypeIds.Double,
            0.0
        );

        StartTcpServer();
    }

    // ───────────────── TCP SERVER ─────────────────

    private void StartTcpServer()
    {
        _ = Task.Run(async () =>
        {
            var listener = new TcpListener(IPAddress.Any, 9000);
            listener.Start();

            Console.WriteLine("📡 TCP Gateway Listening on port 9000");

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                Console.WriteLine($"🔌 ESP32 Connected: {client.Client.RemoteEndPoint}");

                _ = Task.Run(() => HandleClient(client));
            }
        });
    }

    private async Task HandleClient(TcpClient client)
{
    using var stream = client.GetStream();
    byte[] buffer = new byte[1024];
    var sb = new StringBuilder();

    while (true)
    {
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        if (bytesRead == 0) break;

        sb.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));

        while (true)
        {
            int newlineIndex = sb.ToString().IndexOf('\n');
            if (newlineIndex < 0) break;

            string line = sb.ToString(0, newlineIndex).Trim();
            sb.Remove(0, newlineIndex + 1);

            if (!string.IsNullOrWhiteSpace(line))
                ProcessIncomingData(line);
        }
    }

    Console.WriteLine("❌ ESP32 Disconnected");
}

    private void ProcessIncomingData(string json)
{
    try
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        double voltage = root.GetProperty(
            "ns=2;s=Plant=MUMBAI_PLANT/Line=ASSEMBLY_01/Machine=CNC_02/Signal=VOLTAGE"
        ).GetDouble();

        double temperature = root.GetProperty(
            "ns=2;s=Plant=MUMBAI_PLANT/Line=ASSEMBLY_01/Machine=CNC_02/Signal=TEMP"
        ).GetDouble();

        double sound = root.GetProperty(
            "ns=2;s=Plant=MUMBAI_PLANT/Line=ASSEMBLY_01/Machine=CNC_02/Signal=SOUND"
        ).GetDouble();

        var now = DateTime.UtcNow;

        _voltage.Value = voltage;
        _temperature.Value = temperature;
        _sound.Value = sound;

        _voltage.Timestamp = now;
        _temperature.Timestamp = now;
        _sound.Timestamp = now;

        _voltage.ClearChangeMasks(SystemContext, false);
        _temperature.ClearChangeMasks(SystemContext, false);
        _sound.ClearChangeMasks(SystemContext, false);

        Console.WriteLine(
            $"⚡ OPC UA Updated → V={voltage:F2}V | T={temperature:F2}°C | 🔊 Sound={sound:F1}%"
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine("⚠ TCP Payload Error: " + ex.Message);
    }
}

    // ───────────────── OPC UA HELPERS ─────────────────

    private FolderState CreateFolder(NodeId parentId, string browseName, string nodeId)
    {
        var folder = new FolderState(null)
        {
            NodeId = new NodeId(nodeId, 2),
            BrowseName = new QualifiedName(browseName, 2),
            DisplayName = browseName,
            TypeDefinitionId = ObjectTypeIds.FolderType
        };

        AddPredefinedNode(SystemContext, folder);

        folder.AddReference(ReferenceTypeIds.Organizes, true, parentId);

        if (!_externalReferences.TryGetValue(parentId, out var refs))
        {
            refs = new List<IReference>();
            _externalReferences[parentId] = refs;
        }

        refs.Add(new NodeStateReference(
            ReferenceTypeIds.Organizes,
            false,
            folder.NodeId));

        return folder;
    }

    private BaseDataVariableState CreateVariable(
        NodeState parent,
        string browseName,
        string nodeId,
        NodeId dataType,
        object initialValue)
    {
        var variable = new BaseDataVariableState(parent)
        {
            NodeId = new NodeId(nodeId, 2),
            BrowseName = new QualifiedName(browseName, 2),
            DisplayName = browseName,
            DataType = dataType,
            Value = initialValue,
            StatusCode = StatusCodes.Good,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead
        };

        parent.AddChild(variable);
        AddPredefinedNode(SystemContext, variable);

        return variable;
    }
}
