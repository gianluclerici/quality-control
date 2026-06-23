using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI56 : EyeMacro
    {
        public ESTI56(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = TB;
            string extrusionPlane = "C";

            double width = SA;

            ProgramPoint startChamferFM = new ProgramPoint(0, width - (ParB - ParC / 2));
            ProgramPoint endChamferFM = new ProgramPoint(ParA, width - (ParB - ParC / 2));
            ProgramPoint startChamferFF = new ProgramPoint(0, width - (ParB + ParC / 2));
            ProgramPoint endChamferFF = new ProgramPoint(ParA, width - (ParB + ParC / 2));

            //
            // Estrusione anima
            //
            macroPoint.Add(startChamferFM);
            macroPoint.Add(endChamferFM);
            macroPoint.Add(endChamferFF);
            macroPoint.Add(startChamferFF);
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino di contorno estrusione anima
            //
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamferFM, endChamferFM, Wp, extrusionPlane, ParALFA.ToRad(), extrusionDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA, true))
                    breps.Add(chamferA);
                if (EyeGeometryUtils.AddExternalChamfer(startChamferFF, endChamferFF, Wp, extrusionPlane, ParALFA.ToRad(), extrusionDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino in testa anima
            //
            startChamferFM = new ProgramPoint(0, width);
            endChamferFF = new ProgramPoint(0, 0);

            if (!ParBETA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamferFM, endChamferFF, Wp, extrusionPlane, ParBETA.ToRad(), extrusionDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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