using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA38 : EyeMacro
    {
        public ESTIA38(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double width = SA, topY = SB;

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, ParF, 0, ParS));
            macroPoint.Add(new ProgramPoint(ParA, ParF, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, width - (TB + 2 * ParR), 0, 0));
            macroPoint.Add(new ProgramPoint(ParC - ParR, width - (TB + 2 * ParR), 0, 0));
            macroPoint.Add(new ProgramPoint(ParC - ParR, width - TB, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParB, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParB, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino ala Side
            //
            extrusionPlane = Side;
            double chamferDepth = TB;

            double actualAlfa = VX == "I" ? ParALFA.ToRad() : -ParALFA.ToRad();
            actualAlfa = Side == "A" ? actualAlfa : -actualAlfa;            
            ProgramPoint startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA, topY, 0, 0);
            if (actualAlfa > 0)
            {
                if (!actualAlfa.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, actualAlfa, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }
            else
            {
                if (!actualAlfa.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, -actualAlfa, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }

            //
            // Cianfrino ala Opposite Side
            //
            extrusionPlane = Side == "A" ? "B" : "A";            
            
            startChamfer = new ProgramPoint(ParB, 0, 0, 0);
            endChamfer = new ProgramPoint(ParB, SB, 0, 0);

            if (actualAlfa > 0)
            {
                if (!actualAlfa.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, actualAlfa, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }
            else
            {
                if (!actualAlfa.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, -actualAlfa, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}