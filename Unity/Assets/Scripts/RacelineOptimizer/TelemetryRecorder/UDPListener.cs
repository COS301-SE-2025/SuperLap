using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Globalization;
using Vector2 = System.Numerics.Vector2;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

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
    private string Model;
    private string startTrack;
    private int lastLapNo = 0;
    public List<RecordedData> PlayerPath;

    public TelemetryRecorder(int port = 7100)
    {
      this.port = port;
      PlayerPath = new List<RecordedData>();
    }

    ~TelemetryRecorder()
    {
      Dispose();
    }

    public void Dispose()
    {
      cts?.Cancel();
      udp?.Close();
      udp?.Dispose();
      udp = null;
      listenerTask = null;
      cts = null;
    }


    public void Start()
    {
      if (listenerTask != null && !listenerTask.IsCompleted)
        throw new InvalidOperationException("Recorder already running.");

      udp = new UdpClient(port);
      cts = new CancellationTokenSource();
      listenerTask = Task.Run(() => Listen(cts.Token), cts.Token);

      Debug.Log("Telemetry recording started...");
    }

    public List<RecordedData> Stop()
    {
      if (cts != null)
      {
        cts.Cancel();

        try
        {
          udp?.Close();
          udp?.Dispose();
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"Error while closing UDP client: {ex}");
        }

        udp = null;
        listenerTask = null;
        cts = null;

        Debug.Log("Telemetry recording stopped.");
      }
      return PlayerPath;
    }


    public List<int> getLaps()
    {
      List<int> ret = new List<int>();
      for (int i = 0; i < PlayerPath.Count; i++)
      {
        if (i == 0 || PlayerPath[i].CurrentLap != PlayerPath[i - 1].CurrentLap)
        {
          ret.Add(PlayerPath[i].CurrentLap);
        }
      }
      return ret;
    }
    public int getFastestLapIndex()
    {
      Dictionary<int, float> lapTimes = new Dictionary<int, float>();
      for (int i = 0; i < PlayerPath.Count; i++)
      {
        if (PlayerPath[i].CurrentLap != 255)
        {
          if (!lapTimes.ContainsKey(PlayerPath[i].CurrentLap))
          {
            lapTimes[PlayerPath[i].CurrentLap] = PlayerPath[i].LastLapTime;
          }
          else
          {
            lapTimes[PlayerPath[i].CurrentLap] = Math.Min(lapTimes[PlayerPath[i].CurrentLap], PlayerPath[i].LastLapTime);
          }
        }
      }

      int fastestLap = -1;
      float fastestTime = float.MaxValue;
      foreach (var kvp in lapTimes)
      {
        if (kvp.Value > 0 && kvp.Value < fastestTime)
        {
          fastestTime = kvp.Value;
          fastestLap = kvp.Key;
        }
      }
      return fastestLap;
    }

    public float getFastestLapTime()
    {
      int fastestLap = getFastestLapIndex();
      if (fastestLap == -1)
        return 0f;

      float fastestTime = float.MaxValue;
      for (int i = 0; i < PlayerPath.Count; i++)
      {
        if (PlayerPath[i].CurrentLap == fastestLap)
        {
          fastestTime = Math.Min(fastestTime, PlayerPath[i].LastLapTime);
        }
      }
      return fastestTime == float.MaxValue ? 0f : fastestTime;
    }

    public float getAverageSpeed(int lapIndex)
    {
      float totalSpeed = 0f;
      int count = 0;
      for (int i = 0; i < PlayerPath.Count; i++)
      {
        if (PlayerPath[i].CurrentLap == lapIndex)
        {
          totalSpeed += PlayerPath[i].Speed;
          count++;
        }
      }
      return count > 0 ? totalSpeed / count : 0f;
    }

    public float getTopSpeed(int lapIndex)
    {
      float topSpeed = 0f;
      for (int i = 0; i < PlayerPath.Count; i++)
      {
        if (PlayerPath[i].CurrentLap == lapIndex)
        {
          if (PlayerPath[i].Speed > topSpeed)
          {
            topSpeed = PlayerPath[i].Speed;
          }
        }
      }
      return topSpeed;
    }


    public string getModel()
    {
      return Model;
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
            float rearKmh = packet.WheelSpeedR;
            float speedKmh = (frontKmh + rearKmh) / 2f;
            if (packet.CurrentLap == 255 || speedKmh < 2f) // invalid lap or stationary
            {
              if (packet.CurrentLap == 255 && startTrack == packet.Track)
              {
                lastLapNo = 0;
                PlayerPath.Clear();
                Model = null;
              }

              continue;
            }
            else
            {
              Debug.Log($"Last Lap Time: {packet.LastLapTime}, Lap: {packet.CurrentLap}, Speed: {speedKmh}, X: {packet.CoordinatesX}, Y: {packet.CoordinatesY}");
              if (string.IsNullOrEmpty(startTrack))
              {
                startTrack = packet.Track;
              }
              else if (startTrack != packet.Track)
              {
                Debug.LogWarning($"Track changed from {startTrack} to {packet.Track}. Stopping recording.");
                cts.Cancel();
                break;
              }
              if (packet.CurrentLap == lastLapNo + 1 || lastLapNo == 0)
              {
                lastLapNo = packet.CurrentLap;
              }
              else if (packet.CurrentLap != lastLapNo)
              {
                Debug.LogWarning($"Lap number jumped from {lastLapNo} to {packet.CurrentLap}. Ignoring this packet.");
                lastLapNo = 0;
                PlayerPath.Clear();
                startTrack = packet.Track;
                Model = null;
              }
              if (string.IsNullOrEmpty(Model))
              {
                Model = packet.Model;
              }
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

    public void SaveToCSV(string path = null)
    {
      string csvPath = path ?? Path.Combine(Application.streamingAssetsPath, "lastSession.csv");

      string directory = Path.GetDirectoryName(csvPath);
      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      try
      {
        using (var writer = new StreamWriter(csvPath, false, new UTF8Encoding(false)))
        {
          writer.WriteLine("trackId\tlap_number\tworld_position_X\tworld_position_Y");

          foreach (var record in PlayerPath)
          {
            writer.WriteLine($"{record.TrackId}\t{record.CurrentLap + 1}\t{record.CoordinatesX.ToString(CultureInfo.InvariantCulture)}\t{record.CoordinatesY.ToString(CultureInfo.InvariantCulture)}");
          }
        }

        Debug.Log($"Telemetry data saved to {csvPath}");
      }
      catch (System.Exception e)
      {
        Debug.LogError($"Failed to save CSV: {e.Message}");
      }
    }

  }
}