
using Ficep.RobServer.Data;

namespace Ficep.RobServer.MacroParser
{

    public class CopeParam : ICopeParams
    {
        // Properties
        public double A { get; set; }
        public double B { get; set; }
        public double C { get; set; }
        public double D { get; set; }
        public double E { get; set; }
        public double F { get; set; }
        public double G { get; set; }
        public double H { get; set; }
        public double I { get; set; }
        public double L { get; set; }
        public double M { get; set; }
        public double N { get; set; }
        public double O { get; set; }
        public double P { get; set; }
        public double Q { get; set; }
        public double R { get; set; }
        public double S { get; set; }
        public double J { get; set; }
        public double K { get; set; }
        public double ALFA { get; set; }
        public double BETA { get; set; }
        public double DA { get; set; }
        public double DB { get; set; }
        public double DC { get; set; }
        // VX:I => VX = 0
        // VX:F => VX = 1
        public string VX { get; set; }
        // VY:A => VY = 0
        // VY:B => VY = 1
        public string VY { get; set; }
        // SIDE:A => SIDE = 0
        // SIDE:B => SIDE = 1
        // SIDE:C => SIDE = 2
        public string SIDE { get; set; }
        public CuttingTool CuttingTool { get ; set ; }

        public CopeParam()
        {
            A = 0;
            B = 0;
            C = 0;
            D = 0;
            E = 0;
            F = 0;
            G = 0;
            H = 0;
            I = 0;
            L = 0;
            M = 0;
            N = 0;
            O = 0;
            P = 0;
            Q = 0;
            R = 0;
            S = 0;
            J = 0;
            K = 0;
            ALFA = 0;
            BETA = 0;
            DA = 0;
            DB = 0;
            DC = 0;
            VX = "I";
            VY = "A";
            SIDE = "C";
        }
    }

    public class ParamTaglio : IAngTaglio
    {
        public double RAI { get; set; }
        public double RAF { get; set; }
        public double RBF { get; set; }
        public double RBI { get; set; }

        public ParamTaglio()
        {
            RAI = 0;
            RAF = 0;
            RBF = 0;
            RBI = 0;
        }

        public ParamTaglio(double rai, double raf, double rbf, double rbi)
        {
            RAF = raf;
            RBF = rbf;
            RBI = rbi;
            RAI = rai;
        }
    }
}
