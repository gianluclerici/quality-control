using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI19 : EyeMacro
    {
        public SCAI19(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double circleCounter = 0;

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB - ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParR, ParB, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA - ParC, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParC - ParE, ParD, 0, 0));
            while (circleCounter < ParH) ///// AND while (ParA - ParC - ParE - ParF - ParG * circleCounter - ParS) > 0 ????
            {
                macroPoint.Add(new ProgramPoint(ParA - ParC - ParE - ParF + ParS - ParG * circleCounter, ParD, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - ParC - ParE - ParF - ParG * circleCounter - ParS, ParD, 0, 0, ParS));
                circleCounter += 1;
            }
            macroPoint.Add(new ProgramPoint(0, ParD, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}