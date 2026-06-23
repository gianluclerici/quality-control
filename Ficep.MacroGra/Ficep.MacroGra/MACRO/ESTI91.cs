using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI91 : EyeMacro
    {
        public ESTI91(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "I";
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

            double extrusionDepth = TB;
            string extrusionPlane = "A";
            
            double topY = SB, offsetY = SB / 2;
            double tanAlfa = Math.Tan(ParALFA.ToRad());

            //
            // Estrusione ala FF bassa
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC + (offsetY - ParD) * tanAlfa, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, offsetY - ParD, 0, ParR));
            if (ParG == 1 || ParG == 3)
            {
                macroPoint.Add(new ProgramPoint(ParBETA + ParJ, offsetY - ParD, 0, ParS));//Ci vuole ParS?
                macroPoint.Add(new ProgramPoint(ParBETA, offsetY - (ParD + ParH), 0, 0));
            }
            macroPoint.Add(new ProgramPoint(0, offsetY - ParD - ((ParG == 1 || ParG == 3) ? ParH : 0), 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione ala FF alta
            //
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + (offsetY - ParB) * tanAlfa, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, offsetY + ParB, 0, ParR));
            if (ParG == 1 || ParG == 3)
            {
                macroPoint.Add(new ProgramPoint(ParM + ParK + 2 * ParN * tanAlfa, offsetY + ParB, 0, ParL));
                macroPoint.Add(new ProgramPoint(ParM + ParK + ParN * tanAlfa, offsetY + ParB - ParN, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParM + ParN * tanAlfa, offsetY + ParB - ParN, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParM, offsetY + ParB, 0, 0));
            }
            macroPoint.Add(new ProgramPoint(0, offsetY + ParB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione ala FM bassa
            //
            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParP + (offsetY - ParQ) * tanAlfa, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParP, offsetY - ParQ, 0, ParR));
            if (ParG == 2 || ParG == 3)
            {
                macroPoint.Add(new ProgramPoint(ParBETA + ParJ, offsetY - ParQ, 0, ParS));//Ci vuole parS?
                macroPoint.Add(new ProgramPoint(ParBETA, offsetY - (ParQ + ParH), 0, 0));
            }
            macroPoint.Add(new ProgramPoint(0, offsetY - ParQ - ((ParG == 2 || ParG == 3) ? ParH : 0), 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione ala FM alta
            //
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + (offsetY - ParF) * tanAlfa, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, offsetY + ParF, 0, ParR));
            if (ParG == 2 || ParG == 3)
            {
                macroPoint.Add(new ProgramPoint(ParM + ParK + 2 * ParN * tanAlfa, offsetY + ParF, 0, ParL));
                macroPoint.Add(new ProgramPoint(ParM + ParK + ParN * tanAlfa, offsetY + ParF - ParN, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParM + ParN * tanAlfa, offsetY + ParF - ParN, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParM, offsetY + ParF, 0, 0));
            }
            macroPoint.Add(new ProgramPoint(0, offsetY + ParF, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}