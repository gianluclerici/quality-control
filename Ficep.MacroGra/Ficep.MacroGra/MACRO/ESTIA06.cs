using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA06 : EyeMacro
    {

        public ESTIA06(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IUL";
        }

        public override bool CreateMacro()
        {
            ///////////////////////////////
            //      TESTA: fissa
            ///////////////////////////////
            //  Verifico che il profilo sia tra quelli abilitati
            if (!ProfilesEnabled.Contains(CodePrf))
                return false;

            //  Validazione parametri geometrici
            if (Validate() != ErrMacro.No_err)
                return false;

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            double radAlfa = VX == "I" ? ParALFA.ToRad() : - ParALFA.ToRad();
            double tanAlfa = Math.Tan(radAlfa);
            double absTanAlfa = Math.Abs(tanAlfa);

            double topY = CodePrf == "L" ? SA : SB, 
                   offsetY = SB / 2, 
                   width = SA;

            double wingThickness = CodePrf == "L" ? TA : TB;

            double sideWebWingX = tanAlfa * (CodePrf == "I" ? offsetY : topY);
            double oppositeSideWebWingX = ParA + ParB + (CodePrf == "I" ? ParD : 0);

            double caseFU = CodePrf != "I" && radAlfa < 0 ? 0 : 1;

            //
            // Estrusione anima
            //
            double extrusionDepth = topY;
            string extrusionPlane = "C";

            if (CodePrf != "L")
            {
                macroPoint.Add(new ProgramPoint(Math.Abs(sideWebWingX) * caseFU, wingThickness, 0, 0));
                macroPoint.Add(new ProgramPoint(0, wingThickness, 0, 0));
                macroPoint.Add(new ProgramPoint(0, width - wingThickness, 0, 0));
                macroPoint.Add(new ProgramPoint(oppositeSideWebWingX, width - wingThickness, 0, 0));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParA, width, 0, 0));
                macroPoint.Add(new ProgramPoint(0, width, 0, 0));
                macroPoint.Add(new ProgramPoint(0, wingThickness, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParB, wingThickness));
            }
            macroPoint.Add(new ProgramPoint(ParA + ParB, ParC, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, ParC, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ala Side
            //
            if (ParALFA != 0 && CodePrf != "L")
            {
                extrusionDepth = wingThickness;
                extrusionPlane = Side == "A" ? "A" : "B";

                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(absTanAlfa * topY, radAlfa > 0 ? 0 : topY, 0, 0));
                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();
            }

            //
            // Estrusione ala Opposite Side
            //
            extrusionDepth = wingThickness;
            extrusionPlane = Side == "A" ? "B" : "A";

            if (CodePrf == "I")
            {
                if (extrusionPlane == "B")
                    offsetY = -offsetY;

                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(oppositeSideWebWingX + tanAlfa * offsetY, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(oppositeSideWebWingX - tanAlfa * offsetY, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            }
            else 
            {
                if (CodePrf == "L")
                    extrusionPlane = Side;
                //else if (extrusionPlane == "A")
                //    topY = -topY; //    correggo manualmente e temporaneamente il mirroring

                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(oppositeSideWebWingX - sideWebWingX, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(oppositeSideWebWingX, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            }

            if (radAlfa > 0)
            {
                //  Change order to facilitate avoiding Eyeshot bugs
                Brep temp = breps.First();
                breps.RemoveAt(0);
                breps.Add(temp);
            }
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}