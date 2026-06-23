using Ficep.RobServer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FicepDstvParser
{
    public class WorkPiece : IWorkPiece
    {
        public double Lp { get; set; }
        public double WebStart { get; set; }
        public double WebEnd { get; set; }
        public double FlangeStart { get; set; }
        public double FlangeEnd { get; set; }
        public IProfile Prf { get; set; }

        /// <summary>
        /// Initializes the parameters of the profile
        /// </summary>
        /// <param name="CodePrf">
        /// Code profile
        /// </param>
        /// <param name="sA">
        /// Web lenght 
        /// </param>
        /// <param name="tA">
        /// Thickness of the web
        /// </param>
        /// <param name="sB">
        /// Flange lenght 
        /// </param>
        /// <param name="tB">
        /// Thickness of the web
        /// </param>
        /// <param name="r">
        /// Radius
        /// </param>
        public WorkPiece(string CodePrf, double sA, double tA, double sB, double tB, double r, double lenght, double webStart, double webEnd, double flangeStart, double flangeEnd)
        {
            Lp = lenght;
            WebStart = webStart;
            WebEnd = webEnd;
            FlangeStart = flangeStart;
            FlangeEnd = flangeEnd;

            Prf = new Profile(CodePrf, sA, tA, sB, tB, r);
        }

    }

    public class Profile : IProfile
    {
        public string CodePrf { get; set; }
        public double Radius { get; set; }
        public double SA { get; set; }
        public double SB { get; set; }
        public double TA { get; set; }
        public double TB { get; set; }

        public bool IsCodeProfileSet()
        {
            return CodePrf != null;
        }

        public Profile(string CodePrf, double sA, double tA, double sB, double tB, double r)
        {
            this.CodePrf = CodePrf;
            SA = sA;
            TA = tA;
            SB = sB;
            TB = tB;
            Radius = r;
        }
    }
}
