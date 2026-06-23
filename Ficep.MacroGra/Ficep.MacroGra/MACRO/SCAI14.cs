using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI14 : EyeMacro
    {

        public SCAI14(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double topY = CodePrf == "L" ? SA : SB, offsetY = SB / 2;
            //
            // Estrusione anima
            //
            //

            //se ParA == 2 * ParR --> Solido non si genera?
            //se ParD + ParR > ParA --> Solido si genera male e si bugga l'estrusione

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB + ParF, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - 2 * ParR, ParB + ParF, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA - 2 * ParR, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParB + ParC, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            extrusionPlane = Side;
            if (CodePrf != "L")
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
            double chamferDepth = (CodePrf != "L" ? TB : TA) - ParE;

            ProgramPoint startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA, topY, 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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