using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;

namespace CSVToBinConverter
{
    public static class LoadUDP
    {
        public static LoadCSV.PlayerLine Convert(
            List<MotoGPTelemetry.RecordedData> recordedData, 
            int targetLapIndex = 0)
        {
            List<Vector2> playerline = new();
            string trackName = "Unknown";

            foreach (var record in recordedData)
            {
                if (record.CurrentLap != targetLapIndex)
                    continue;

                float x = record.CoordinatesX;
                float y = record.CoordinatesY;
                trackName = record.TrackId;

                if (trackName == "Losail")
                {
                    float angleDeg = -60f;
                    float angleRad = angleDeg * (float)Math.PI / 180f;
                    float cos = MathF.Cos(angleRad);
                    float sin = MathF.Sin(angleRad);
                    float rotatedX = cos * x - sin * y;
                    float rotatedY = sin * x + cos * y;

                    float scaleFactor = 6.834f;
                    rotatedX *= scaleFactor;
                    rotatedY *= scaleFactor;

                    playerline.Add(new Vector2(rotatedX, rotatedY));
                }
                else
                {
                    float angleDeg = 145.0f;
                    float angleRad = angleDeg * (float)Math.PI / 180f;
                    float cos = MathF.Cos(angleRad);
                    float sin = MathF.Sin(angleRad);
                    float rotatedX = cos * x - sin * y;
                    float rotatedY = sin * x + cos * y;

                    float scaleFactor = 0.514f;
                    rotatedX *= scaleFactor;
                    rotatedY *= scaleFactor;

                    playerline.Add(new Vector2(-rotatedX, rotatedY));
                }
            }

            if (playerline.Count == 0)
            {
                Debug.Log("No valid playerline data found.");
                return null;
            }

            string trackPath = Path.Combine(Application.streamingAssetsPath, "MotoGPTracks", $"{trackName}.bin");
            if (!File.Exists(trackPath))
            {
                Debug.Log($"Error: Edge file not found at {trackPath}");
                return null;
            }

            EdgeData edgeData = EdgeData.LoadFromBinary(trackPath, true);

            if (edgeData.OuterBoundary.Count == 0 || edgeData.InnerBoundary.Count == 0)
            {
                Debug.Log($"Error: Edge data for {trackName} is empty or malformed.");
                return null;
            }

            var allEdges = edgeData.OuterBoundary.Concat(edgeData.InnerBoundary).ToList();
            float edgeCenterX = (allEdges.Min(p => p.X) + allEdges.Max(p => p.X)) / 2f;
            float edgeCenterY = (allEdges.Min(p => p.Y) + allEdges.Max(p => p.Y)) / 2f;

            for (int i = 0; i < edgeData.OuterBoundary.Count; i++)
                edgeData.OuterBoundary[i] -= new Vector2(edgeCenterX, edgeCenterY);
            for (int i = 0; i < edgeData.InnerBoundary.Count; i++)
                edgeData.InnerBoundary[i] -= new Vector2(edgeCenterX, edgeCenterY);
            for (int i = 0; i < edgeData.Raceline.Count; i++)
                edgeData.Raceline[i] -= new Vector2(edgeCenterX, edgeCenterY);

            float playerCenterX = (playerline.Min(p => p.X) + playerline.Max(p => p.X)) / 2f;
            float playerCenterY = (playerline.Min(p => p.Y) + playerline.Max(p => p.Y)) / 2f;

            Vector2 offset = new Vector2(-1f, 0f);
            for (int i = 0; i < playerline.Count; i++)
            {
                playerline[i] -= new Vector2(playerCenterX, playerCenterY);
                playerline[i] += offset;
            }

            return new LoadCSV.PlayerLine
            {
                InnerBoundary = edgeData.InnerBoundary,
                OuterBoundary = edgeData.OuterBoundary,
                Raceline = edgeData.Raceline,
                PlayerPath = playerline
            };
        }
    }
}
