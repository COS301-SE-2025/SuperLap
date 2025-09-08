using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Globalization;


namespace MotoGPTelemetry
{
    public struct RecordedData
    {
        public float LastLapTime;
        public byte CurrentLap;
        public string TrackId;
        public float Speed;
        public float CoordinatesX;
        public float CoordinatesY;
    }
    public class TelemetryRecorder
    {
        private readonly int port;
        private UdpClient udp;
        private CancellationTokenSource cts;
        private Task listenerTask;
        public List<RecordedData> PlayerPath;

        public TelemetryRecorder(int port = 7100)
        {
            this.port = port;
            PlayerPath = new List<RecordedData>();
        }

        public void Start()
        {
            if (listenerTask != null && !listenerTask.IsCompleted)
                throw new InvalidOperationException("Recorder already running.");

            udp = new UdpClient(port);
            cts = new CancellationTokenSource();
            listenerTask = Task.Run(() => Listen(cts.Token), cts.Token);

            Console.WriteLine("Telemetry recording started...");
        }

        public void Stop()
        {
            if (cts != null)
            {
                cts.Cancel();
                udp?.Close();
                Console.WriteLine("Telemetry recording stopped.");
                SaveToCSV();
            }
        }

        private void Listen(CancellationToken token)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (udp.Available > 0) // check buffer
                    {
                        byte[] data = udp.Receive(ref ep);
                        MotoGP18Packet packet = ByteArrayToStructure<MotoGP18Packet>(data);
                        float frontKmh = packet.WheelSpeedF;
                        float rearKmh  = packet.WheelSpeedR;
                        float speedKmh = (frontKmh + rearKmh) / 2f;
                        if (packet.CurrentLap == 256 || speedKmh < 2f) // invalid lap or stationary
                        {
                           continue;
                        }
                        else
                        {
                            Console.WriteLine($"Last Lap Time: {packet.LastLapTime}, Lap: {packet.CurrentLap}, Speed: {speedKmh}, X: {packet.CoordinatesX}, Y: {packet.CoordinatesY}");
                            RecordedData record = new RecordedData
                            {
                              LastLapTime = packet.LastLapTime,
                              CurrentLap = packet.CurrentLap,
                              TrackId = packet.Track,
                              Speed = packet.Speed,
                              CoordinatesX = packet.CoordinatesX,
                              CoordinatesY = packet.CoordinatesY
                            };
                            PlayerPath.Add(record);
                        }   
                    }
                    else
                    {
                        Thread.Sleep(5); // avoid tight loop when no packets
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // udp closed while listening
            }
        }

        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        public void SaveToCSV()
        {
            string csvPath = "lastSession.csv";
            using (var writer = new StreamWriter(csvPath, false, new UTF8Encoding(false)))
            {
                writer.WriteLine("trackId\tlap_number\tworld_position_X\tworld_position_Y");
                foreach (var record in PlayerPath)
                {
                    writer.WriteLine($"{record.TrackId}\t{record.CurrentLap+1}\t{record.CoordinatesX.ToString(CultureInfo.InvariantCulture)}\t{record.CoordinatesY.ToString(CultureInfo.InvariantCulture)}");
                }
            }
            Console.WriteLine($"Telemetry data saved to {csvPath}");
        }
    }
}