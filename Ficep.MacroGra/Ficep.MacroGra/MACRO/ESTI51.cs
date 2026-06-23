using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI51 : EyeMacro
    {

        public ESTI51(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SA;
            string extrusionPlane = "A";

            //
            // Estrusione ala FF con unico solido obliquo
            //
            double extrusionAngle = VX == "I" ? ParALFA.ToRad() : -ParALFA.ToRad();

            double offsetY = SB / 2;
            double offsetX = extrusionAngle > 0 ? 0 : SA * Math.Tan(extrusionAngle) - Surplus;

            double cutY = (ParB / 2 > TA / 2 + InnerChamferDisFromWeb ? ParB / 2 : TA / 2 + InnerChamferDisFromWeb);

            macroPoint.Add(new ProgramPoint(offsetX, offsetY - cutY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, offsetY - cutY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, offsetY + cutY, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetX, offsetY + cutY, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, extrusionAngle);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }
    }
}