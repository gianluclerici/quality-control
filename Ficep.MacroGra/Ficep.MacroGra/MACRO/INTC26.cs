
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
	public class INTC26 : EyeMacro
	{

		public INTC26(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "R";
        }

        public override bool CreateMacro()
		{
            //  Verifico che il profilo sia tra quelli abilitati
            if (!ProfilesEnabled.Contains(CodePrf))
                return false;

            //  Validazione parametri geometrici
            if (Validate() != ErrMacro.No_err)
                return false;

            List<Brep> breps;

			double totalLenght = Params.C,
				   width = Params.B,
				   outerRadius = Wp.Prf.SA / 2,
				   innerRadius = Wp.Prf.SA / 2 - Wp.Prf.TA;

            EyeUtils.CreateRadialAperture(width, totalLenght, Params.ALFA, innerRadius, outerRadius, TolBrep, false, out breps, Params.R, Params.A, Surplus);
            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));
            
            return true;
        }

	}
}
