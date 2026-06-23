using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI49 : EyeMacro
    {

        public SCAI49(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            // Estrusione anima
            //

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB + ParC, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParB + ParC, ParD, 0, ParQ));
            macroPoint.Add(new ProgramPoint(ParB + ParC, ParD, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParC, ParD + ParE, 0, ParS));
            macroPoint.Add(new ProgramPoint(0, ParD + ParE, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino esterno ala Side
            //
            extrusionPlane = Side;
            double chamferDepth = TB - ParH - ParG;
            
            ProgramPoint startChamfer = new ProgramPoint(ParA + ParB + ParC, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA + ParB + ParC, SB, 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino interno ala Side basso
            //

            chamferDepth = ParG;

            startChamfer = new ProgramPoint(ParA + ParB + ParC, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA + ParB + ParC, SB / 2 - TA / 2 - InnerChamferDisFromWeb, 0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino interno ala Side alto
            //

            chamferDepth = ParG;

            startChamfer = new ProgramPoint(ParA + ParB + ParC, SB / 2 + TA / 2 + InnerChamferDisFromWeb, 0, 0);
            endChamfer = new ProgramPoint(ParA + ParB + ParC, SB, 0, 0);

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