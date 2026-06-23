using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI03 : EyeMacro
    {

        public SCAI03(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            // Poligono 1
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParB + ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
           
            //  ESTRUSIONE 1
            //
            double extrusionDepth = 0;
            string extrusionPlane = "C";

            if (CodePrf == "L")
                extrusionDepth = SA;
            else
                extrusionDepth = SB;

            List<Brep> breps = new List<Brep>();

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();

            // POLIGONO 2
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

            if (CodePrf == "I")
            {
                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParD, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParD, offsetY - TA / 2 - ParF, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParA, offsetY - TA / 2 - ParF, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY + TA / 2 + ParF, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParD, offsetY + TA / 2 + ParF, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParA + ParD, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(0, TA + ParF, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParD, TA + ParF, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParA + ParD, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            }

            //  ESTRUSIONE 2
            //
            extrusionDepth = TB + Radius;
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
