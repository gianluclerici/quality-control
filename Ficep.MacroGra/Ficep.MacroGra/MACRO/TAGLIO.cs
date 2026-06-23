using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using System;
using Ficep.Utils;
using devDept.Geometry;

namespace Ficep.MacroGra
{
	public class TAGLIO : EyeMacroTaglio
	{

		public TAGLIO(IWorkPiece wp, IAngTaglio param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
		: base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
		{
            ProfilesEnabled = "R";
        }

        public override bool CreateMacro()
		{
            //  Verifico che il profilo sia tra quelli abilitati
            if (!ProfilesEnabled.Contains(Wp.Prf.CodePrf))
                return false;

            //  Validazione parametri geometrici
            if (Validate() != ErrMacro.No_err)
                return false;

            if (Param.RAF == 0 && Param.RAI == 0 && Param.RBI == 0 && Param.RBF == 0)
                return true;

            double brepTol = EyeParam.Tol.Brep;
            double tolLinear = EyeParam.Tol.Linear;
            double tolThickness = EyeParam.Tol.WebFlange;
            double surplus = EyeParam.Surplus;

            double amountI = tolThickness,
                   amountF = tolThickness;

            Plane planeI = Plane.YZ,
                  planeF = Plane.YZ;

            EyeWorkPiece eyeWp = (Wp as EyeWorkPiece);

            if (Param.RAI != 0 && Param.RBI != 0)
            {
                planeI.Rotate(Param.RAI.ToRad(), Vector3D.AxisZ);
                planeI.Translate(0, 0, Wp.Prf.SA / 2);
                planeI.Rotate(Param.RBI.ToRad(), Vector3D.AxisMinusY, new Point3D(0, 0, Wp.Prf.SA / 2));

                // Calcolato analiticamente tramite studio di funzione.
                // Se equation.y > 0 && equation.z > 0 allora amount i è un minimo altrimenti prendo il vettore normale 
                // nell'altra direzione per calcolare il minimo
                double yNormal = planeI.Equation.Y,
                       zNormal = planeI.Equation.Z;
               
                amountI = Math.Abs((-yNormal * (Wp.Prf.SA / 2 * (1 + Math.Cos(Math.Atan(zNormal / yNormal)))) - zNormal * (Wp.Prf.SA / 2 * (1 + Math.Sin(Math.Atan(zNormal / yNormal)))) - planeI.Equation.D) / planeI.Equation.X);
                amountI += surplus;
            }
            else if (Param.RAI > 0)
            {
                amountI = Wp.Prf.SA * Math.Tan(Param.RAI.ToRad()) + surplus;
            }
            else if (Param.RBI > 0)
            {
                amountI = Wp.Prf.SA / 2 * Math.Tan(Param.RBI.ToRad()) + surplus;
            }
            else if (Param.RBI < 0)
            {
                amountI = -Wp.Prf.SA / 2 * Math.Tan(Param.RBI.ToRad()) + surplus;
            }

            if (Param.RAF != 0 && Param.RBF != 0)
            {
                planeF.Rotate(Param.RAF.ToRad(), Vector3D.AxisZ);
                planeF.Translate(0, 0, Wp.Prf.SA / 2);
                planeF.Rotate(Param.RBF.ToRad(), Vector3D.AxisMinusY, new Point3D(0, 0, Wp.Prf.SA / 2));

                double yNormal = planeF.Equation.Y,
                       zNormal = planeF.Equation.Z;

                // Se equation.y < 0 && equation.z < 0
                amountF = Math.Abs((-yNormal * (Wp.Prf.SA / 2 * (1 + Math.Cos(Math.Atan(zNormal / yNormal)))) - zNormal * (Wp.Prf.SA / 2 * (1 + Math.Sin(Math.Atan(zNormal / yNormal)))) - planeF.Equation.D) / planeF.Equation.X);
                amountF += surplus;
                planeF.Translate(Wp.Lp, 0);
            }
            else if (Param.RAF < 0)
            {
                amountF = -Wp.Prf.SA * Math.Tan(Param.RAF.ToRad()) + surplus;
            }
            else if (Param.RBF < 0)
            {
                amountF = -Wp.Prf.SA / 2 * Math.Tan(Param.RBF.ToRad()) + surplus;
            }
            else if (Param.RBF > 0)
            {
                amountF = Wp.Prf.SA / 2 * Math.Tan(Param.RBF.ToRad()) + surplus;
            }

            // If the amount is equal to 1 means that the angle is 0 and therefore has to be equal to 0 teh amount
            // not equal to 1
            amountI = amountI == 1 ? 0 : amountI;
            amountF = amountF == 1 ? 0 : amountF;
            
            double totalLp = Wp.Lp + amountI + amountF;
			
            eyeWp.CreateSolidRawPart(totalLp, brepTol);

            if (amountI.IsGreaterThan(0, tolLinear))
                eyeWp.Solid.Translate(-amountI, 0, 0);

            if (!Param.RAI.IsEqualTo(0, tolLinear) && Param.RBI.IsEqualTo(0, tolLinear))
            {
                planeI.Rotate(Param.RAI.ToRad(), Vector3D.AxisZ);
                eyeWp.Solid.CutBy(planeI, true);
            }
            else if (!Param.RBI.IsEqualTo(0, tolLinear) && Param.RAI.IsEqualTo(0, tolLinear))
            {
                planeI.Translate(0, 0, Wp.Prf.SA / 2);
                planeI.Rotate(Param.RBI.ToRad(), Vector3D.AxisMinusY, new Point3D(0, 0, Wp.Prf.SA / 2));
                eyeWp.Solid.CutBy(planeI, true);
            }
            else if (!Param.RBI.IsEqualTo(0, tolLinear) && !Param.RAI.IsEqualTo(0, tolLinear))
            {
                eyeWp.Solid.CutBy(planeI, true);
            }

            if (!Param.RAF.IsEqualTo(0, tolLinear) && Param.RBF.IsEqualTo(0, tolLinear))
            {
                planeF.Translate(Wp.Lp, 0);
                planeF.Rotate(Param.RAF.ToRad(), Vector3D.AxisZ, new Point3D(Wp.Lp, 0, 0));
                eyeWp.Solid.CutBy(planeF, false);
            }
            else if (!Param.RBF.IsEqualTo(0, tolLinear) && Param.RAF.IsEqualTo(0, tolLinear))
            {
                planeF.Translate(Wp.Lp, 0, Wp.Prf.SA / 2);
                planeF.Rotate(Param.RBF.ToRad(), Vector3D.AxisMinusY, new Point3D(Wp.Lp, 0, Wp.Prf.SA / 2));
                eyeWp.Solid.CutBy(planeF, false);
            }
            else if (!Param.RBF.IsEqualTo(0, tolLinear) && !Param.RAF.IsEqualTo(0, tolLinear))
            {
                eyeWp.Solid.CutBy(planeF, false);
            }

            return true;
		}

	}
}
