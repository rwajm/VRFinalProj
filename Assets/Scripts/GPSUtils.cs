using UnityEngine;
using System;

public static class GPSUtils
{
    public static void GetTileCoordinate(double lat, double lon, int zoom, out double tileX, out double tileY)
    {
        double n = Math.Pow(2.0, zoom);
        tileX = (lon + 180.0) / 360.0 * n;

        double latRad = lat * Mathf.Deg2Rad;
        tileY = (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n;
    }

    public static float GetTileSizeMeters(double lat, int zoom)
    {
        double circumference = 40075016.686;
        return (float)((circumference * Math.Cos(lat * Mathf.Deg2Rad)) / Math.Pow(2, zoom));
    }
}