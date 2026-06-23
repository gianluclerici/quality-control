using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using System.Collections.Generic;
using System.Linq;
using System;
using Ficep.Utils;

namespace Ficep.MacroGra
{
    public class SCAI06 : EyeMacro
    {
        public SCAI06(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IULQ";
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

            ///////////////////////////////
            //      CORPO: variabile
            ///////////////////////////////
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            //
            //  ESTRUSIONE ANIMA
            //
            string extrusionPlane = "C";
            double extrusionDepth = 0;
            double radALFA = ParALFA.ToRad();

            if (CodePrf == "L")
                extrusionDepth = SA;
            else
                extrusionDepth = SB;

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            macroPoint.Add(new ProgramPoint(ParE - ParA / 2 - (TB - ParI) * Math.Tan(radALFA), 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE - ParA / 2, TB - ParI, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE - ParA / 2, ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE - ParH / 2 - ParG, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE - ParH / 2, ParB + ParF, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + ParH / 2, ParB + ParF, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + ParH / 2 + ParG, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + ParA / 2, ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE + ParA / 2, TB - ParI, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + ParA / 2 + (TB - ParI) * Math.Tan(radALFA), 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////
            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}