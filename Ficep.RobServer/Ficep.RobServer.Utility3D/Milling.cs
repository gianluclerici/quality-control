using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Milling;
using devDept.Geometry;
using System.IO;

namespace Ficep.RobServer.Utility3D
{
    public class Milling
    {
        public Milling() 
        {
        }

        public void Test() 
        {
            //  Design
            CompositeCurve square = CompositeCurve.CreateRectangle(Plane.XY, 200, 200, false); // square centered in the origin
            Circle circle = new Circle(Plane.XY, new Point3D(100,100), 40); // hole in the middle
            Circle circle2 = new Circle(Plane.XY, new Point3D(90, 90), 40); // hole in the middle

            Region region = new Region(square, circle, circle2); // final region to mill
 

            Geometry2D geom = new Geometry2D(region.ContourList, .1);

            //  Setup
            Stock stock = Stock.CreateBox(0, 0, 200, 200, 10);

            Setup setup = new Setup("Top", linearUnitsType.Millimeters, Plane.XY, stock);

            //  Tool
            EndMill flat = new EndMill(10, 0);

            //  Machining
            Pocket2D pocket = new Pocket2D(setup, flat, geom, new devDept.Geometry.Interval(-5, 15), 5, 1);
            pocket.DoWork();

            Toolpath toolpath = pocket.Result;
            //  G-code
            string filePath = "C:\\MARCO\\TEMP\\output.nc"; // Use double backslashes

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("%"); // Start of G-code

                // Assume toolpath is a list of motions with points
                foreach (var motion in toolpath.MotionList)
                {
                    Point3D pt = motion.EndPoint; // Get point

                    // Write G1 linear move (change G0 for rapid move if needed)
                    writer.WriteLine($"{motion.Code} X{pt.X:F3} Y{pt.Y:F3} Z{pt.Z:F3} F{motion.Feed:F3}");
                }

                writer.WriteLine("M30"); // End of program
                writer.WriteLine("%");   // End of G-code
            }
        }
    }
}
