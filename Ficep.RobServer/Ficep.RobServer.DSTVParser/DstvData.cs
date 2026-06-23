using Ficep.RobServer.Data;
using System.Collections.Generic;
using System.Linq;

namespace FicepDstvParser
{
    public interface IDstvBlock
    {
        List<DstvLine> DstvLines { get; set; }
        string Plane { get; }
    }

    public class DstvLine
    {
        public List<(string leadingLetters, double? value, string trailinglLetters)> LineTokens { get; set; }

        public DstvLine(List<(string leadingLetters, double? value, string trailinglLetters)> tokens)
        {
            LineTokens = tokens;
        }
    }

    public class Ak : IDstvBlock, IContour
    {
        public string Plane { get; private set; }
        public List<DstvLine> DstvLines { get; set; }
        public List<ProgramPoint> ProgramPoints { get; private set; }

        public List<(int idx, double phi1, double y1, double phi2, double y2)> ChamferDescriptionList { get; private set; }

        public Ak(string plane, List<DstvLine> dstvLines)
        {
            Plane = plane;
            DstvLines = dstvLines;
            ProgramPoints = new List<ProgramPoint>();
            ChamferDescriptionList = new List<(int idx, double phi1, double y1, double phi2, double y2)>();
        }
    }

    public class Ik : IDstvBlock, IContour
    {
        public List<DstvLine> DstvLines { get; set; }
        public string Plane { get; private set; }
        public List<ProgramPoint> ProgramPoints { get; private set; }
        public List<(int idx, double phi1, double y1, double phi2, double y2)> ChamferDescriptionList { get; private set; }

        public Ik(string plane, List<DstvLine> dstvLines)
        {
            Plane = plane;
            DstvLines = dstvLines;
            ProgramPoints = new List<ProgramPoint>();
            ChamferDescriptionList = new List<(int idx, double phi1, double y1, double phi2, double y2)>();
        }
    }

    public class Bo : IDstvBlock
    {
        public List<DstvLine> DstvLines { get; set; }
        public List<Hole2D> Holes { get; set; }
        public string Plane { get; set; }
        public Bo(string plane, List<DstvLine> dstvLines)
        {
            Plane = plane;
            DstvLines = dstvLines;
            Holes = new List<Hole2D>();

            ComputeHoles();
        }

        private bool ComputeHoles()
        {
            foreach (var dstvLine in DstvLines) 
            {
                if (dstvLine.LineTokens.Any(x => x.value == null))
                    return false;

                double d = dstvLine.LineTokens[2].value.Value,
                       xc = dstvLine.LineTokens[0].value.Value,
                       yc = dstvLine.LineTokens[1].value.Value,
                       depth = 0;

                bool hasDepth = dstvLine.LineTokens.Count > 3;
                if (hasDepth)
                    depth = dstvLine.LineTokens[3].value.Value;
                else
                { 
                    Holes.Add(new DstvHole(d, xc, yc, depth, Plane));
                    continue;
                }

                if (!dstvLine.LineTokens[3].trailinglLetters.Equals("l", System.StringComparison.OrdinalIgnoreCase))
                    Holes.Add(new DstvHole(d, xc, yc, depth, Plane));
                else
                {
                    double b = dstvLine.LineTokens[3].value.Value,
                           h = dstvLine.LineTokens[0].value.Value,
                           w = dstvLine.LineTokens[1].value.Value;

                    Holes.Add(new DstvSlottedHole(d, xc, yc, depth, Plane, b, h, w));
                }
            }

            return true;
        }
    }

    public class DstvHole : Hole2D
    {
        public double D { get; set; }
        public double Xc { get; set; }
        public double Yc { get; set; }
        public double Depth { get; set; }
        public string Plane { get; set; }

        public DstvHole(double d, double xc, double yc, double depth, string plane)
        {
            D = d;
            Xc = xc;
            Yc = yc;
            Depth = depth;
            Plane = plane;
        }
    }

    class DstvSlottedHole : Hole2D
    {
        public double D { get; set; }
        public double Xc { get; set; }
        public double Yc { get; set; }
        public double Depth { get; set; }
        public double B { get; set; } // Horizontal distance between circles centers
        public double H { get; set; } // Vertical distance between circles centers
        public double W { get; set; } // Angle of rotation
        public string Plane { get; set; }

        public DstvSlottedHole(double d, double xc, double yc, double depth, string plane, double b, double h, double w)
        {
            D = d;
            Xc = xc;
            Yc = yc;
            Depth = depth;
            Plane = plane;
            B = b;
            H = h;
            W = w;
        }
    }
}