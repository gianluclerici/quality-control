using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA93 : EyeMacro
    {
        public ESTIA93(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "I";
        }

        public override bool CreateMacro()
        {
            ///////////////////////////////
            //      TESTA: fiswidth
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

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            double topY = SB, offsetY = SB / 2, width = SA;

            // webX e' la quota X del taglio anima.
            double webX = ParC * (ParB - TA / 2) / (offsetY + ParB);
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB, 0));
            macroPoint.Add(new ProgramPoint(webX, TB, 0));
            macroPoint.Add(new ProgramPoint(webX, ParE - ParR, 0));
            macroPoint.Add(new ProgramPoint(webX - ParR, ParE, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParD, ParE, 0));
            macroPoint.Add(new ProgramPoint(ParD, width - ParE, 0));
            macroPoint.Add(new ProgramPoint(webX - ParR, width - ParE, 0));
            macroPoint.Add(new ProgramPoint(webX, width - (ParE - ParR), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(webX, width - TB, 0));
            macroPoint.Add(new ProgramPoint(0, width - TB, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ala Side
            //
            extrusionDepth = TB;
            extrusionPlane = Side;
            macroPoint.Add(new ProgramPoint(0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(0, offsetY + ParB, 0));
            macroPoint.Add(new ProgramPoint(ParA, topY, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, MirrorSideASideB, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            //
            // Cianfrino esterno ala Side
            //
            double chamferDepth = TB - ParF;
            
            ProgramPoint startChamfer = new ProgramPoint(ParC, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(0, offsetY + ParB, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorSideASideB, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Estrusione ala Opposite Side
            //
            extrusionPlane = Side == "A" ? "B" : "A";
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, MirrorSideASideB, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            //
            // Cianfrino esterno ala Opposite Side
            //
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorSideASideB, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            ///////////////////////////////
            //      CODA: fiswidth
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}