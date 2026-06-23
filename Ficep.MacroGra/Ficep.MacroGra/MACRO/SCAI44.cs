using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI44 : EyeMacro
    {

        public SCAI44(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            // Estrusione anima
            //

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            double radAlfa = VX == "I" ? ParALFA.ToRad() : -ParALFA.ToRad();
            radAlfa = Side == "A" ? radAlfa : -radAlfa;
            double tanAlfa = Math.Tan(radAlfa);

            double tanBeta = VX == "I" ? - Math.Tan(ParBETA.ToRad()) : Math.Tan(ParBETA.ToRad()); // da rivedere in confronto su pegaso per actual beta


            macroPoint.Add(new ProgramPoint(0, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + (ParB - TB) * tanAlfa, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParD, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParB + ParC, 0, 0));
            
             EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Estrusione ala
            //
            //
            
            macroPoint.Clear();

            extrusionDepth = TB;
            extrusionPlane = Side;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB * tanAlfa - (SB / 2) * tanBeta, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB * tanAlfa + (SB / 2) * tanBeta, SB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, SB, 0, 0));

             EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, radAlfa);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}