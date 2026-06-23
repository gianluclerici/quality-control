using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA105 : EyeMacro
    {
        public ESTIA105(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IUL";
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

            double topY = CodePrf == "L" ? SA : SB,
                   offsetY = CodePrf != "I" ? 0 : SB / 2,
                   width = CodePrf == "L" ? SB : SA;

            double radAlfa = Side == "A" ? ParALFA.ToRad() : -ParALFA.ToRad(), absTanAlfa = Math.Abs(Math.Tan(radAlfa));

            double tanM = CodePrf != "I" ? 0 : Math.Tan(ParM.ToRad()),
                   tanN = CodePrf != "I" ? 0 : Math.Tan(ParN.ToRad()),
                   tanO = CodePrf != "I" ? 0 : Math.Tan(ParO.ToRad()),
                   tanP = CodePrf != "I" ? 0 : Math.Tan(ParP.ToRad());

            double offsetA = Side == "A" ? width * absTanAlfa : 0, offsetB = Side == "B" ? width * absTanAlfa : 0;

            //
            // Estrusione anima
            //
            double extrusionDepth = topY;
            string extrusionPlane = "C"; // Sarebbe extrusionPlane = CodePrf != "L" ? "C" : "B"; ma lo lascio = C per sfruttare il mirrorAB anche su prof L

            macroPoint.Add(new ProgramPoint(0, width, 0));
            macroPoint.Add(new ProgramPoint(width * absTanAlfa, 0, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            //
            // Estrusione ala Side
            //
            extrusionDepth = CodePrf != "L" ? TB : TA;
            extrusionPlane = "A";
            macroPoint.Add(new ProgramPoint(- extrusionDepth * absTanAlfa, CodePrf == "I" ? 0 : ParB, 0));
            macroPoint.Add(new ProgramPoint(- extrusionDepth * absTanAlfa, topY, 0));
            if (ParA > 0 || ParM > 0)
            {
                if (ParI.IsEqualTo(1, TolLinear) && CodePrf == "I")
                    macroPoint.Add(new ProgramPoint(offsetA + ParA, topY, 0));
                else
                {
                    macroPoint.Add(new ProgramPoint(offsetA + ParA + (offsetY - ParB) * tanM, topY, 0));
                    macroPoint.Add(new ProgramPoint(offsetA + ParA, offsetY + ParB, 0, ParR));
                }
                macroPoint.Add(new ProgramPoint(offsetA, offsetY + ParB, 0));
            }
            else
                macroPoint.Add(new ProgramPoint(offsetA, topY, 0));
            if ((ParE > 0 || ParN > 0) && CodePrf == "I")
            {
                macroPoint.Add(new ProgramPoint(offsetA, offsetY - ParF, 0));
                if (ParJ.IsEqualTo(1, TolLinear))
                    macroPoint.Add(new ProgramPoint(offsetA + ParE, 0, 0));
                else
                {
                    macroPoint.Add(new ProgramPoint(offsetA + ParE, offsetY - ParF, 0, ParR));
                    macroPoint.Add(new ProgramPoint(offsetA + ParE + (offsetY - ParF) * tanN, 0, 0));
                }
            }
            else
                macroPoint.Add(new ProgramPoint(offsetA, offsetY + ParB, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, false, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, radAlfa);
            macroPoint.Clear();

            //
            // Estrusione ala Opposite Side
            //
            if ( CodePrf != "L")
            {
                extrusionDepth = TB;
                extrusionPlane = "B";
                macroPoint.Add(new ProgramPoint(0, 0, 0));
                macroPoint.Add(new ProgramPoint(0, topY, 0));
                if (ParC > 0 || ParO > 0)
                {
                    if (ParK.IsEqualTo(1, TolLinear) && CodePrf == "I")
                        macroPoint.Add(new ProgramPoint(offsetB + ParC, topY, 0));
                    else
                    {
                        macroPoint.Add(new ProgramPoint(offsetB + ParC + (offsetY - ParD) * tanO, topY, 0));
                        macroPoint.Add(new ProgramPoint(offsetB + ParC, offsetY + ParD, 0, ParR));
                    }
                    macroPoint.Add(new ProgramPoint(offsetB, offsetY + ParD, 0));
                }
                else
                    macroPoint.Add(new ProgramPoint(offsetB, topY, 0));
                if ((ParG > 0 || ParP > 0) && CodePrf == "I")
                {
                    macroPoint.Add(new ProgramPoint(offsetB, offsetY - ParH, 0));
                    if (ParL.IsEqualTo(1, TolLinear))
                        macroPoint.Add(new ProgramPoint(offsetB + ParG, 0));
                    else
                    {
                        macroPoint.Add(new ProgramPoint(offsetB + ParG, offsetY - ParH, 0, ParR));
                        macroPoint.Add(new ProgramPoint(offsetB + ParG + (offsetY - ParH) * tanP, 0, 0));
                    }
                }
                else
                    macroPoint.Add(new ProgramPoint(offsetB, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, false, false, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, -radAlfa);
            }
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}