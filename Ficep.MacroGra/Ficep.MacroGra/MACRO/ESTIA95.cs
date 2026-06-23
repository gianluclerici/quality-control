using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA95 : EyeMacro
    {
        public ESTIA95(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "I";
        }

        public override bool CreateMacro()
        {
            ///////////////////////////////
            //      TESTA: fiswidth
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

            double width = SA;

            double radAlfa = ParALFA.ToRad(), radBeta = ParBETA.ToRad(), radK = ParK.ToRad();

            double ratHoleAngle = Math.Atan((ParA - ParL - ParN) / ParB);

            double actualR = ParR.IsEqualTo(15, TolAngle) ? ParR + TolWebFlange : ParR;
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + TB * Math.Tan(radK), TB, 0));
            macroPoint.Add(new ProgramPoint(ParA - TB * Math.Tan(ratHoleAngle) + actualR, TB, 0));
            macroPoint.Add(new ProgramPoint(ParA - TB * Math.Tan(ratHoleAngle) - actualR * Math.Sin(ratHoleAngle), TB + actualR * Math.Cos(ratHoleAngle), 0, 0, actualR));
            macroPoint.Add(new ProgramPoint(ParL + ParN, ParB, 0));
            macroPoint.Add(new ProgramPoint(ParL, ParB + ParM, 0));
            macroPoint.Add(new ProgramPoint(ParL + ParC, ParB + ParM + ParD, 0, ParS));
            macroPoint.Add(new ProgramPoint(ParE, width - ParF, 0));
            if (ParH == 0)
            {
                macroPoint.Add(new ProgramPoint(ParG, width, 0));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParH, width - TB, 0));
                macroPoint.Add(new ProgramPoint(ParH - ParJ * Math.Tan(radBeta), width - TB + ParJ, 0));
                macroPoint.Add(new ProgramPoint(ParH - ParJ * Math.Tan(radBeta), width - TB + ParJ + ParI, 0));
                macroPoint.Add(new ProgramPoint(ParH - ParJ * Math.Tan(radBeta) + (TB - ParI - ParJ) * Math.Tan(radAlfa), width, 0));
            }
            macroPoint.Add(new ProgramPoint(0, width, 0));
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            ///////////////////////////////
            //      CODA: fiswidth
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}