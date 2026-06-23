using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI90 : EyeMacro
    {

        public ESTI90(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth;
            string extrusionPlane = "C";

            double topY = SB, offsetY = SB / 2;

            //dubbio: serve innerChamferDisFromWeb? sia per estrusione ali che per cianfrino anima?

            double actualB = ParB > (InnerChamferDisFromWeb + TA / 2) ? offsetY + ParB : offsetY + (InnerChamferDisFromWeb + TA / 2);
            double actualD = ParD > (InnerChamferDisFromWeb + TA / 2) ? offsetY - ParD : offsetY - (InnerChamferDisFromWeb + TA / 2);
            double actualF = ParF > (InnerChamferDisFromWeb + TA / 2) ? offsetY + ParF : offsetY + (InnerChamferDisFromWeb + TA / 2);
            double actualH = ParH > (InnerChamferDisFromWeb + TA / 2) ? offsetY - ParH : offsetY - (InnerChamferDisFromWeb + TA / 2);

            //
            // Cianfrino superore anima
            //

            double chamferDepth = TA - ParI;

            ProgramPoint startChamfer = new ProgramPoint(0, TB + InnerChamferDisFromWeb, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(0, SA - (TB + InnerChamferDisFromWeb), 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Estrusione ala FF
            //

            extrusionPlane = "A";
            extrusionDepth = TB;

            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, actualB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, actualB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, actualD, 0, 0));
            macroPoint.Add(new ProgramPoint(0, actualD, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ala FM
            //

            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, actualF, 0, 0));
            macroPoint.Add(new ProgramPoint(0, actualF, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParG, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParG, actualH, 0, 0));
            macroPoint.Add(new ProgramPoint(0, actualH, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}