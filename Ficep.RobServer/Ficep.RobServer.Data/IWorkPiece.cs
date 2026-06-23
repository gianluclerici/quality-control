using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ficep.RobServer.Data
{
    public interface IProfile
    {
        string CodePrf { get; set; }
        double Radius { get; set; }
        double SA { get; set; }
        double SB { get; set; }
        double TA { get; set; }
        double TB { get; set; }
    }
    public interface IWorkPiece
    {
        double Lp { get; set; }
        IProfile Prf { get; set; }
    }
}
