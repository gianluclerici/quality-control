using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI151 : EyeMacro
    {
        public ESTI151(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double topY = SB, offsetY = SB / 2, width = SA;

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, width / 2 - ParB / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, width / 2 + ParB / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            //
            // Estrusione ALA FF
            //
            extrusionDepth = TB;
            extrusionPlane = "A";
            if (ParE > 0 && ParF > 0)
            {
                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParE, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParE, offsetY - ParF, 0, ParR));
                macroPoint.Add(new ProgramPoint(0, offsetY - ParF, 0, 0));
                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();
            }

            if (ParC > 0 && ParD > 0)
            {
                macroPoint.Add(new ProgramPoint(0, offsetY + ParD, 0, 0));
                macroPoint.Add(new ProgramPoint(ParC, offsetY + ParD, 0, ParR));
                macroPoint.Add(new ProgramPoint(ParC, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();
            }
            //
            // Estrusione ALA FM
            //
            extrusionPlane = "B";
            if (ParI > 0 && ParJ > 0)
            {
                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParI, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParI, offsetY - ParJ, 0, ParR));
                macroPoint.Add(new ProgramPoint(0, offsetY - ParJ, 0, 0)); 
                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();
            }

            if (ParG > 0 && ParH > 0)
            {
                macroPoint.Add(new ProgramPoint(0, offsetY + ParH, 0, 0));
                macroPoint.Add(new ProgramPoint(ParG, offsetY + ParH, 0, ParR));
                macroPoint.Add(new ProgramPoint(ParG, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(0, topY, 0, 0)); 
                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}