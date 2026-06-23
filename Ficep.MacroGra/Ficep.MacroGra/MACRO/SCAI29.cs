using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI29 : EyeMacro
    {

        public SCAI29(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            double dy = 0.1;

            // POLIGONO 1
            if (ParR > 0)
            {
                macroPoint.Add(new ProgramPoint(0, ParC - dy, 0, 0));
                macroPoint.Add(new ProgramPoint(0, ParC + ParR, 0, 0));
                macroPoint.Add(new ProgramPoint(0, ParC, 0, ParR));
                macroPoint.Add(new ProgramPoint(ParR, ParC - dy, 0, 0));
                           
            }
            else
            {
                macroPoint.Add(new ProgramPoint(0, ParC, 0, 0));
                macroPoint.Add(new ProgramPoint(0, ParC + ParD, 0, 0));
                macroPoint.Add(new ProgramPoint(ParE, ParC, 0, 0));
            }

            //  ESTRUSIONE 1
            //
            double extrusionDepth = SB;
            string extrusionPlane = "C";
            List<Brep> breps = new List<Brep>();

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            double topY = SB, offsetY = SB / 2;

            // POLIGONO 2
            //
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParF, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, topY - ParH, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, ParI, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB + ParG, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            //  ESTRUSIONE 2
            //
            extrusionDepth = ParC;
            extrusionPlane = Side;

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}
