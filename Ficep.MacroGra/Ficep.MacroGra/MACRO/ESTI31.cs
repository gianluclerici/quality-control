using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI31 : EyeMacro
    {
        public ESTI31(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            //
            // Estrusione anima FF
            //
            double tanAlfa = Math.Tan(ParALFA.ToRad());

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD + TB * tanAlfa, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD - (ParC - 2 * ParR - TB) * tanAlfa, ParC - 2 * ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, ParC - 2 * ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, ParC, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE, ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParC + ParF, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            //
            // Estrusione anima FM
            //
            double width = SA;
            double tanBeta = Math.Tan(ParBETA.ToRad());

            macroPoint.Add(new ProgramPoint(0, width, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + TB * tanBeta, width, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - (ParC - 2 * ParR - TB) * tanBeta, width - (ParC - 2 * ParR), 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, width - (ParC - 2 * ParR), 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, width - ParC, 0, 0, - ParR));
            macroPoint.Add(new ProgramPoint(ParE, width - ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - (ParC + ParF), 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }
    }
}