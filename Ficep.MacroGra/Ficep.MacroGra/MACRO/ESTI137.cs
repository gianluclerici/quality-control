
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
	public class ESTI137 : EyeMacro
	{

		public ESTI137(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

			double totalLenght = Params.A + Params.R,
				   width = Params.B,
				   innerRadius = Wp.Prf.SA / 2 - Wp.Prf.TA,
				   outerRadius = Wp.Prf.SA / 2;

            if (Params.VX == "I")
				EyeUtils.CreateRadialAperture(width, totalLenght, Params.ALFA, innerRadius, outerRadius, TolBrep, false, out breps, Params.R, 0, Surplus, false, true);
			else
			{
                EyeUtils.CreateRadialAperture(width, totalLenght, Params.ALFA, innerRadius, outerRadius, TolBrep, false, out breps, Params.R, 0, Surplus, true, false);
                for (int i = 0; i < breps.Count; i++)
                {
                    Brep brep = breps[i];
                    brep.Translate(Wp.Lp - totalLenght + Surplus, 0);
                }
            }
			Features.AddRange(breps.Select(brep => new EyeFeature(brep)));
            return true;
        }
    }
}
