// Reads the PlanBuild .blueprint text format (Unity-free, so it can be tested in a console build):
//   #Name:, #Creator:, #Description:"", #Category:, #SnapPoints (x;y;z lines, counted), #Pieces (one piece per line).
// A piece line is prefab;category;x;y;z;qx;qy;qz;qw;"extra";sx;sy;sz; the quoted extra (sign text, item stand data) may hold ';'.
// Other # sections (#Terrain) are skipped. Lines that do not parse are counted in BadLines, never fatal.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HomeCheat.Features.Construction
{
    internal static class BlueprintParse
    {
        public static Blueprint Parse(string text, string file)
        {
            Blueprint b = new Blueprint();
            b.File = file;
            string section = "None";

            string[] lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0) continue;

                if (line.StartsWith("#"))
                {
                    string headerLine = line.Substring(1);
                    int colonIdx = headerLine.IndexOf(':');
                    string headerName = colonIdx >= 0 ? headerLine.Substring(0, colonIdx) : headerLine;
                    string headerValue = colonIdx >= 0 ? headerLine.Substring(colonIdx + 1).Trim() : "";

                    if (headerName == "Name")
                    {
                        b.Name = headerValue;
                        section = "None";
                    }
                    else if (headerName == "Creator")
                    {
                        b.Creator = headerValue;
                        section = "None";
                    }
                    else if (headerName == "Description")
                    {
                        b.Description = headerValue;
                        if (b.Description.Length >= 2 && b.Description[0] == '"' && b.Description[b.Description.Length - 1] == '"')
                            b.Description = b.Description.Substring(1, b.Description.Length - 2);
                        section = "None";
                    }
                    else if (headerName == "Category")
                    {
                        b.Category = headerValue;
                        section = "None";
                    }
                    else if (headerName == "SnapPoints")
                    {
                        section = "SnapPoints";
                    }
                    else if (headerName == "Pieces")
                    {
                        section = "Pieces";
                    }
                    else
                    {
                        section = "None";
                    }
                    continue;
                }

                if (section == "SnapPoints")
                {
                    b.SnapPoints++;
                }
                else if (section == "Pieces")
                {
                    string[] parts = line.Split(';');
                    if (parts.Length < 9)
                    {
                        b.BadLines++;
                        continue;
                    }

                    float x, y, z, qx, qy, qz, qw;
                    if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                        !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                        !float.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out z) ||
                        !float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out qx) ||
                        !float.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out qy) ||
                        !float.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out qz) ||
                        !float.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out qw))
                    {
                        b.BadLines++;
                        continue;
                    }

                    string extra = "";
                    float sx = 1f, sy = 1f, sz = 1f;
                    bool saved = false;

                    if (parts.Length == 10)
                    {
                        extra = parts[9];
                    }
                    else if (parts.Length > 10)
                    {
                        // PlanBuild ends with the scale (sx;sy;sz); InfinityHammer adds one more field, the object's saved data in
                        // base64 (2026-10-25, portalhub): when the last field is not a number, the scale is the three before it
                        float probe;
                        int n = parts.Length;
                        bool hasData = n >= 14 && !float.TryParse(parts[n - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out probe);
                        if (hasData) { n--; saved = true; }
                        int extraEnd = n - 4;
                        if (extraEnd < 9) extraEnd = 9;
                        StringBuilder sb = new StringBuilder();
                        for (int i = 9; i <= extraEnd; i++)
                        {
                            if (i > 9) sb.Append(';');
                            sb.Append(parts[i]);
                        }
                        extra = sb.ToString();

                        int scaleXIdx = extraEnd + 1;
                        int scaleYIdx = scaleXIdx + 1;
                        int scaleZIdx = scaleYIdx + 1;
                        if (scaleZIdx < n)
                        {
                            if (!float.TryParse(parts[scaleXIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out sx) ||
                                !float.TryParse(parts[scaleYIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out sy) ||
                                !float.TryParse(parts[scaleZIdx], NumberStyles.Float, CultureInfo.InvariantCulture, out sz))
                            {
                                b.BadLines++;
                                continue;
                            }
                        }
                    }

                    if (extra.Length >= 2 && extra[0] == '"' && extra[extra.Length - 1] == '"')
                    {
                        extra = extra.Substring(1, extra.Length - 2);
                    }

                    BlueprintPiece piece = new BlueprintPiece();
                    piece.Prefab = parts[0];
                    piece.Category = parts[1];
                    piece.X = x;
                    piece.Y = y;
                    piece.Z = z;
                    piece.Qx = qx;
                    piece.Qy = qy;
                    piece.Qz = qz;
                    piece.Qw = qw;
                    piece.Extra = extra;
                    piece.HasSavedData = saved;
                    piece.Sx = sx;
                    piece.Sy = sy;
                    piece.Sz = sz;
                    b.Pieces.Add(piece);
                }
            }

            if (string.IsNullOrEmpty(b.Name))
            {
                b.Name = Path.GetFileNameWithoutExtension(file);
            }

            return b;
        }

        public static void Bounds(Blueprint b, out float minX, out float maxX, out float minY, out float maxY, out float minZ, out float maxZ)
        {
            minX = 0;
            maxX = 0;
            minY = 0;
            maxY = 0;
            minZ = 0;
            maxZ = 0;

            if (b.Pieces == null || b.Pieces.Count == 0) return;

            minX = maxX = b.Pieces[0].X;
            minY = maxY = b.Pieces[0].Y;
            minZ = maxZ = b.Pieces[0].Z;

            for (int i = 1; i < b.Pieces.Count; i++)
            {
                float x = b.Pieces[i].X;
                float y = b.Pieces[i].Y;
                float z = b.Pieces[i].Z;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                if (z < minZ) minZ = z;
                if (z > maxZ) maxZ = z;
            }
        }
    }
}
