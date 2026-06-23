using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class INTC14 : EyeMacro
    {

        public INTC14(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IQ";
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

            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            if (CodePrf == "I")
            {
                //
                //  Lista dei punti che descrivono il contorno da estrudere
                //
                double offsetY = SB / 2 + ParD;

                macroPoint.Add(new ProgramPoint(ParA - ParB / 2, offsetY + ParC / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParB / 2, offsetY + ParC / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParB / 2, offsetY - ParC / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - ParB / 2, offsetY - ParC / 2, 0, 0));

                //
                //  ESTRUSIONE
                //
                double extrusionDepth = SA;
                string extrusionPlane = "A";

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            }
            else if (CodePrf == "Q")
            {
                if (ParB > 0 && ParC > 0)
                {
                    //
                    //  Lista dei punti che descrivono il contorno da estrudere
                    //
                    double offsetY = SA / 2 + ParD;
                    macroPoint.Add(new ProgramPoint(ParA - ParB / 2, offsetY + ParC / 2, 0, 0));
                    macroPoint.Add(new ProgramPoint(ParA + ParB / 2, offsetY + ParC / 2, 0, 0));
                    macroPoint.Add(new ProgramPoint(ParA + ParB / 2, offsetY - ParC / 2, 0, 0));
                    macroPoint.Add(new ProgramPoint(ParA - ParB / 2, offsetY - ParC / 2, 0, 0));

                    //
                    //  ESTRUSIONE
                    //
                    double extrusionDepth = SB;
                    string extrusionPlane = "C";

                    EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                }

                if (ParE > 0 && ParF > 0)
                {
                    macroPoint.Clear();

                    //
                    //  Lista dei punti che descrivono il contorno da estrudere
                    //
                    double offsetY = SB / 2 + ParG;

                    macroPoint.Add(new ProgramPoint(ParA - ParE / 2, offsetY + ParF / 2, 0, 0));
                    macroPoint.Add(new ProgramPoint(ParA + ParE / 2, offsetY + ParF / 2, 0, 0));
                    macroPoint.Add(new ProgramPoint(ParA + ParE / 2, offsetY - ParF / 2, 0, 0));
                    macroPoint.Add(new ProgramPoint(ParA - ParE / 2, offsetY - ParF / 2, 0, 0));

                    //
                    //  ESTRUSIONE
                    //
                    double extrusionDepth = SA;
                    string extrusionPlane = "A";

                    EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                }
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}
