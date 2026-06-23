using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI26 : EyeMacro
    {
        public ESTI26(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "F";
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
            List<Brep> breps = new List<Brep>();

            double width = SA, length = Lp;

            ProgramPoint initFF = new ProgramPoint(0, 0, 0, 0);
            ProgramPoint endFF = new ProgramPoint(length, 0, 0, 0);
            ProgramPoint initFM = new ProgramPoint(0, width, 0, 0);
            ProgramPoint endFM = new ProgramPoint(length, width, 0, 0);

            //
            // Cianfrino esterno ala Side
            //
            string extrusionPlane = "C";
            double chamferDepth = ParA;

            if (!ParALFA.IsEqualTo(0, TolAngle) && !ParA.IsEqualTo(0, TolLinear))
            {
                if (EyeGeometryUtils.AddExternalChamfer(endFF, initFF, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
                if (EyeGeometryUtils.AddExternalChamfer(endFM, endFF, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA, true))
                    breps.Add(chamferA);
                if (EyeGeometryUtils.AddExternalChamfer(initFM, endFM, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                    breps.Add(chamferA);
                if (EyeGeometryUtils.AddExternalChamfer(initFM, initFF, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
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