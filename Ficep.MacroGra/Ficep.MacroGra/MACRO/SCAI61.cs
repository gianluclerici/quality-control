using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI61 : EyeMacro
    {

        public SCAI61(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IU";
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

            double topY = SB, offsetY = SB / 2;

            double webDist = TA / 2; //  + InnerChamferDisFromWeb???
            //
            // Estrusione anima
            //
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            if (ParG > 0)
            {
                macroPoint.Add(new ProgramPoint(ParA - ParG, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - ParG, TB, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
            }
            else 
                macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, ParC, 0, ParR));
            macroPoint.Add(new ProgramPoint(0, ParD, 0, ParS));
            macroPoint.Add(new ProgramPoint(0, ParD + ParS, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            
            if(CodePrf == "I")
            {
                macroPoint.Clear();

                extrusionDepth = TB + Radius;
                extrusionPlane = Side;

                //
                // Estrusione ala bassa
                //
                macroPoint.Add(new ProgramPoint(ParA - ParG, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParF, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, ParF, 0, 0, -ParF));
                macroPoint.Add(new ProgramPoint(ParA, offsetY - webDist, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - ParG, offsetY - webDist, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();

                //
                // Estrusione ala alta
                //
                macroPoint.Add(new ProgramPoint(ParA, SB - ParF, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParF, topY, 0, 0, -ParF));
                macroPoint.Add(new ProgramPoint(ParA - ParG, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - ParG, offsetY + webDist, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY + webDist, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            }
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}