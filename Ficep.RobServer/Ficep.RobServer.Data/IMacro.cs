using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ficep.RobServer.Data
{
    //  Tipologia tecnologia di taglio
    public enum CuttingTool : uint
    {
        Default,
        Plasma = 51,
        Oxycutting= 52
    }

    public interface IMacro
    {
        string MacroClassName { get; set; }
        string MacroName { get; set; }
        string MacroBitmapName { get; set; }
        uint LineNumber { get; set; }
        IWorkPiece Wp { get; }

        bool ValidateGeometry();
    }
    public interface ICopeParams
    {
        double A { get; set; }
        double ALFA { get; set; }
        double B { get; set; }
        double BETA { get; set; }
        double C { get; set; }
        double D { get; set; }
        double DA { get; set; }
        double DB { get; set; }
        double DC { get; set; }
        double E { get; set; }
        double F { get; set; }
        double G { get; set; }
        double H { get; set; }
        double I { get; set; }
        double J { get; set; }
        double K { get; set; }
        double L { get; set; }
        double M { get; set; }
        double N { get; set; }
        double O { get; set; }
        double P { get; set; }
        double Q { get; set; }
        double R { get; set; }
        double S { get; set; }
        string SIDE { get; set; }
        string VX { get; set; }
        string VY { get; set; }
        CuttingTool CuttingTool { get; set; }
    }

    public interface IAngTaglio
    {
        double RAI { get; set; }
        double RAF { get; set; }
        double RBF{ get; set; }
        double RBI { get; set; }
    }
    public interface IMacroCope : IMacro
    {
        ICopeParams Params { get; }

        bool CreateMacro();
    }

    public interface IMacroTaglio : IMacro
    {
        IAngTaglio Param { get; }
    }
}
