using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ficep.RobServer.Data
{
    public interface ITol
    {
        /// <summary>
        /// Tolerance used to compare linear units
        /// </summary>
        double Linear {  get; }
        /// <summary>
        /// Tolerance used to compare angular units 
        /// </summary>
        double Angle { get; }
    }
}
