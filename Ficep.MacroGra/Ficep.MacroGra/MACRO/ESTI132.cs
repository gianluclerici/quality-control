using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI132 : EyeMacro
    {
        public ESTI132(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "I";
        }

        public override bool CreateMacro()
        {
            ///////////////////////////////
            //      TESTA: fissa
            ///////////////////////////////
            //  Verifico che il profilo sia tra quelliParAbilitati
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

            double radAlfa = ParALFA.ToRad(), tanAlfa = Math.Tan(radAlfa),AbsTanAlfa = Math.Abs(tanAlfa);

            double width = SA;

            double xFF, xFM;

            if (ParALFA >= 0)
            {
                xFM = 0;
                xFF = xFM + width * AbsTanAlfa;
            }
            else
            {
                xFF = 0;
                xFM = xFF + width * AbsTanAlfa;
            }


            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            if (ParB > 0)
                macroPoint.Add(new ProgramPoint(xFF + (TB - ParC) * (Math.Tan(ParB.ToRad()) - tanAlfa), 0, 0, 0));
            else
                macroPoint.Add(new ProgramPoint(xFF, 0, 0, 0));

            macroPoint.Add(new ProgramPoint(xFF - (TB - ParC) * tanAlfa, TB - ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(xFF - TB * tanAlfa, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(xFF - (TB + 2 * ParR) * tanAlfa +ParA - ParR, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(xFF - (TB + 2 * ParR) * tanAlfa +ParA - ParR, TB + 2 * ParR, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(xFF - (TB + 2 * ParR) * tanAlfa, TB + 2 * ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(xFM + (TB + 2 * ParR) * tanAlfa, width - TB - 2 * ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(xFM + (TB + 2 * ParR) * tanAlfa +ParA - ParR, width - TB - 2 * ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(xFM + (TB + 2 * ParR) * tanAlfa +ParA - ParR, width - TB, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(xFM + TB * tanAlfa, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(xFM + (TB - ParE) * tanAlfa, width - (TB - ParE), 0, 0));

            if (ParD > 0)
                macroPoint.Add(new ProgramPoint(xFM + (TB - ParE) * (Math.Tan(ParD.ToRad()) + tanAlfa), width, 0, 0));
            else
                macroPoint.Add(new ProgramPoint(xFM, width, 0, 0));

            macroPoint.Add(new ProgramPoint(0, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            //  Usato MirrorInizialeFinale due volte, anche al posto di MirrorSideASideB per poter specchiare l'angolazione alfa
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorInizialeFinale, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}