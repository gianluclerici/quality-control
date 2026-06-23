using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI12 : EyeMacro
    {

        public SCAI12(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IL";
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

            double extrusionDepth = CodePrf == "L" ? SA : SB;
            string extrusionPlane = CodePrf == "L" ? "B" : "C";

            double topY = CodePrf == "L" ? SA : SB, offsetY = CodePrf == "L" ? 0 : SB / 2;

            double intChamferDist = CodePrf == "L" ? TB + InnerChamferDisFromWeb : (ParE > InnerChamferDisFromWeb ? TA / 2 + ParE : TA / 2 + InnerChamferDisFromWeb);
            //
            // Estrusione anima
            //
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParR, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParB + ParR, 0, 0, - ParR));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            extrusionPlane = Side;

            if ( CodePrf != "L" )
            {
                //
                // Estrusione ala basso
                //

                extrusionDepth = TB + Radius;

                macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParS, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, ParS, 0, 0, ParS));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();

                //
                // Estrusione ala alto
                //

                macroPoint.Add(new ProgramPoint(ParA, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, topY - ParS, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParS, topY, 0, 0, ParS));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            }

            //
            // Cianfrino esterno ala Side
            //
            double chamferDepth = (CodePrf != "L" ? TB : TA) - ParD - ParC;

            ProgramPoint startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA, topY, 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala Side basso
            //
            chamferDepth = ParC;

            startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA, offsetY - intChamferDist, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle) && CodePrf != "L")
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala Side alto
            //

            startChamfer = new ProgramPoint(ParA, offsetY + intChamferDist, 0, 0);
            endChamfer = new ProgramPoint(ParA, topY, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}