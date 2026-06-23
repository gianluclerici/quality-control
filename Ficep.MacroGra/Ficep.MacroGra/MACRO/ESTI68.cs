using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI68 : EyeMacro
    {
        public ESTI68(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SB; //   or SA its same
            string extrusionPlane = "C";

            double offsetY = SB / 2;

            //  Dati per cianfrini in testa
            double chamferDepth = TB;

            ProgramPoint startChamfer = new ProgramPoint(0, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(0, 2 * offsetY, 0, 0);

            // Estrusione passante piano C/D
            macroPoint.Add(new ProgramPoint(0, offsetY + ParC - ParB / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, offsetY + ParC - ParB / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, offsetY + ParC + ParB / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(0, offsetY + ParC + ParB / 2, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            // Estrusione passante piano A/B
            extrusionPlane = "A";
            macroPoint.Add(new ProgramPoint(0, offsetY + ParF - ParE / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD, offsetY + ParF - ParE / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD, offsetY + ParF + ParE / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(0, offsetY + ParF + ParE / 2, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            
            //  Cianfrini in testa
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "A", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "B", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                    breps.Add(chamferA);
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "C", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                    breps.Add(chamferA);
                //if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, "D", ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                //    breps.Add(chamferA);
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}