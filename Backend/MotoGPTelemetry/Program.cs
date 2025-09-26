using System;
using System.Net;

namespace MotoGPTelemetry
{
    class Program
    {
        static void Main()
        {
            var recorder = new TelemetryRecorder();

            recorder.Start();

            Console.WriteLine("Press ENTER to stop recording...");
            Console.ReadLine();

            recorder.Stop();

            Console.WriteLine("Program finished.");
        }
    }
}