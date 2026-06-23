using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI08 : EyeMacro
    {

        public SCAI08(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IULF";
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

            double extrusionDepth = 0;
            string extrusionPlane = "";

            double topY = 0, offsetY = 0;

            if (CodePrf == "I")
            {
                topY = SB;
                offsetY = SB / 2;
            }
            else if (CodePrf == "L")
                topY = MirrorSideASideB ? SB : SA;
            else
                topY = SB;

            if (ParBETA > 0 || ParF > 0)
            {
                //
                //  ESTRUSIONE SEMI-ALA SUPERIORE
                // 
                extrusionDepth = TB + Radius;
                extrusionPlane = Side;

                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(0, offsetY - TA / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY - TA / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY - ParG, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParF, offsetY - ParG, 0, ParR));
                macroPoint.Add(new ProgramPoint(ParA + ParF + (offsetY - ParG) * Math.Tan(ParBETA.ToRad()), 0, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            }
            if (ParALFA > 0 || ParD > 0)
            {
                macroPoint.Clear();

                //
                //  ESTRUSIONE SEMI-ALA INFERIORE
                // 
                extrusionDepth = TB + Radius;
                extrusionPlane = Side;

                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(0, offsetY + TA / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY + TA / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY + ParE, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParD, offsetY + ParE, 0, ParR));
                macroPoint.Add(new ProgramPoint(ParA + ParD + (offsetY - ParE) * Math.Tan(ParALFA.ToRad()), topY, 0, 0));

                // ESTRUSIONE 2

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            }

            macroPoint.Clear();

            //
            //  ESTRUSIONE ANIMA
            //
            extrusionPlane = "C";
            if (CodePrf == "L")
                extrusionDepth = SA;
            else if (CodePrf == "F")
                extrusionDepth = TA;
            else
                extrusionDepth = SB;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            if (ParC.IsEqualTo(0, TolLinear))
                macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            else
            {
                macroPoint.Add(new ProgramPoint(ParA + ParC, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - TB * ParA / ParB, TB, 0, 0));
            }

            macroPoint.Add(new ProgramPoint(0, ParB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}
