using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Ficep.Utils
{
    public static class Utils
    {
        [DebuggerStepThrough]
        /// <summary>
        /// Compares two floating point numbers using the given error tolerance.
        /// </summary>
        /// <param name="value1">
        /// First number
        /// </param>
        /// <param name="value2">
        /// Second number
        /// </param>
        /// <param name="tol">
        /// Comparance tolerance
        /// </param>
        /// <returns>
        /// -1: first < second 0: first == second +1: first > second
        /// </returns>
        public static int Compare(double value1, double value2, double tol)
        {
            double difference = value1 - value2;

            if (Math.Abs(difference) <= tol)
                return 0; // Numbers are considered equal within the tolerance.
            else if (difference < 0)
                return -1; // first is less than second.
            else
                return 1; // first is greater than second.
        }
        [DebuggerStepThrough]
        public static bool IsEqualTo(this double value1, double value2, double tol)
            => Compare(value1, value2, tol) == 0;
        [DebuggerStepThrough]
        public static bool IsGreaterThan(this double value1, double value2, double tol)
            => Compare(value1, value2, tol) == 1;
        [DebuggerStepThrough]
        public static bool IsLessThan(this double value1, double value2, double tol)
            => Compare(value1, value2, tol) == -1;

        [DebuggerStepThrough]
        /// <summary>
        /// Convert the value in radians
        /// </summary>
        /// <param name="value1"></param>
        /// <returns></returns>
        public static double ToRad(this double value1) => value1 * Math.PI / 180;
        [DebuggerStepThrough]
        /// <summary>
        /// Convert the value in degrees
        /// </summary>
        /// <param name="value1"></param>
        /// <returns></returns>
        public static double ToDeg(this double value1) => value1 * 180 / Math.PI;

        /// </summary>
        ///  _______________________________
        /// |       2       |        1      |
        ///  
        ///  ===========(xc, yc)===============
        ///         3              4
        /// |_______________|_______________|
        /// Get the quadrant where the point lays
        /// </summary>
        /// <param name="x1">
        /// x point coordinate
        /// </param>
        /// <param name="y1">
        /// y point coordinate
        /// </param>
        /// <param name="xc">
        /// X center plane coordinate
        /// </param>
        /// <param name="yc">
        /// Y center plane coordinate
        /// </param>
        /// <param name="q">
        /// Quadrant number
        /// </param>
        /// <returns>
        /// Return false if the arc is between two quadrants, otherwise true
        /// </returns>
        public static bool GetQuadrant(double x1, double y1, double tol, out int q, double xc = 0, double yc = 0)
        {
            q = 0;

            if (x1.IsLessThan(xc, tol) && y1.IsLessThan(yc / 2, tol))
                q = 3;
            else if (x1.IsGreaterThan(xc, tol) && y1.IsLessThan(yc, tol))
                q = 4;
            else if (x1.IsGreaterThan(xc, tol) && y1.IsGreaterThan(yc, tol))
                q = 1;
            else if (x1.IsLessThan(xc, tol) && y1.IsGreaterThan(yc, tol))
                q = 2;
            else
                return false;

            return true;
        }
    }
}
