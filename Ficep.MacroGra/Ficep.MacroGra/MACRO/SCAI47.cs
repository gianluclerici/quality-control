using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI47 : EyeMacro
    {

        public SCAI47(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            // Estrusione anima
            //

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParC, 0, CodePrf == "I" ? ParR : 0));
            macroPoint.Add(new ProgramPoint(ParA - ParB, ParC + ParD, 0, CodePrf == "I" ? ParS : 0));
            macroPoint.Add(new ProgramPoint(ParE, ParC + ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParC + ParD + ParF, 0, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            
            if (CodePrf == "I")
            {
                macroPoint.Clear();

                extrusionDepth = TB + Radius;
                extrusionPlane = Side;

                double topY = SB, offsetY = SB / 2;

                //
                // Estrusione ala alta
                //
                macroPoint.Add(new ProgramPoint(ParA, offsetY + ParH, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParG, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, topY, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();

                //
                // Estrusione ala bassa
                //
                macroPoint.Add(new ProgramPoint(ParA + ParI, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY - ParL, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));

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