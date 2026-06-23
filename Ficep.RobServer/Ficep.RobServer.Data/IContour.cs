using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ficep.RobServer.Data
{
    public interface IContour
    {
        List<ProgramPoint> ProgramPoints { get;  }

        // Gli elementi della lista contengono: indice del ProgramPoint, angolo del cianfrino, profondità/landing del cianfrino
        List<(int idx, double phi1, double y1, double phi2, double y2)> ChamferDescriptionList {  get; }
        string Plane { get; }
    }
}
