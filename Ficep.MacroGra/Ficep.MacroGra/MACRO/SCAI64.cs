using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI64 : EyeMacro
    {
        public SCAI64(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "L";
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

            double extrusionDepth = (ParC - ParR) > TA + Radius ? TA + Radius : TA;
            string extrusionPlane = "A";

            //
            // Estrusione piano A
            //
            macroPoint.Add(new ProgramPoint(ParE + ParA / 2, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + ParA / 2, ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE - ParA / 2, ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE - ParA / 2, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione piano B
            //
            extrusionDepth = (ParB - ParR) > TB + Radius ? TB + Radius : TB;
            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(ParE + ParA / 2, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + ParA / 2, ParC, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE - ParA / 2, ParC, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE - ParA / 2, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}