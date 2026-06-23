using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class INTC33 : EyeMacro
    {
        public INTC33(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "Q";
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

            double cutDepth = ParE < ParJ ? ParE : ParJ;
            double extrusionDepth = TA + (Radius < cutDepth ? Radius : cutDepth);
            string extrusionPlane = Side;
            double width = SA, topY = SB;

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2, ParC - ParR));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2 + ParD, ParC - ParR));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2 + ParD, ParC + ParR, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2, ParC + ParR));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2, width));
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2, width));
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2, ParC + ParR));
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2 - ParD, ParC + ParR));
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2 - ParD, ParC - ParR, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2, ParC - ParR));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            //
            // Estrusione ala FM
            //
            extrusionDepth = TB;
            extrusionPlane = "B";
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2, topY));
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2, topY - (ParJ + ParL)));
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2 + ParN, topY - (ParJ + ParL)));
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2 + ParN, topY - ParJ));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2 - ParM, topY - ParJ));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2 - ParM, topY - (ParJ + ParK)));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2, topY - (ParJ + ParK)));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2, topY));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            //
            // Estrusione ala FF
            //
            extrusionPlane = "A";
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2, topY));
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2, topY - (ParE + ParG)));
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2 + ParH, topY - (ParE + ParG)));
            macroPoint.Add(new ProgramPoint(ParA - ParB / 2 + ParH, topY - ParE));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2 - ParI, topY - ParE));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2 - ParI, topY - (ParE + ParF)));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2, topY - (ParE + ParF)));
            macroPoint.Add(new ProgramPoint(ParA + ParB / 2, topY));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}