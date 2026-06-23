using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA41 : EyeMacro
    {
        public ESTIA41(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IU";
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

            //  LA VALIDATE DEVE LANCIARE UN ERRORE IF(ParB > 0 && ParI > 0) ALTRIMENTI RISCHIO DI AVERE L'INCLINAZIONE DEL TAGLIO SULL'ALA DIVERSA DALL'INCLINAZIONE DEL TAGLIO SULL'ANIMA 

            string extrusionPlane = "C";
            double extrusionDepth = SB;
             
            double width = SA,
                   offsetY = CodePrf == "U" ? 0 : SB / 2,
                   topY = SB;
            double radAlfa = ParB > 0 ? Math.Atan(ParB / ParC) : -Math.Atan(ParI / ParC);
            double radBeta = Math.PI / 2 - radAlfa;

            double lowerWingDistance = (TA / 2 + InnerChamferDisFromWeb) > ParH ? (TA / 2 + InnerChamferDisFromWeb) : ParH;
            double upperWingDistance = (TA / 2 + InnerChamferDisFromWeb) > ParF ? (TA / 2 + InnerChamferDisFromWeb) : ParF;

            double x1 = CodePrf == "I" || ParI.IsEqualTo(0, TolLinear) ? ParA : ParA - ParI,
                   x2 = CodePrf == "I" || ParI.IsEqualTo(0, TolLinear) ? ParA - ParB : ParA;

            macroPoint.Add(new ProgramPoint(x1, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(x2, ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(x2 + ParD, ParC + ParD / Math.Tan(radBeta), 0, ParR));
            macroPoint.Add(new ProgramPoint(x2 + ParD - (width - ParC - ParD / Math.Tan(radBeta)) * Math.Tan(radAlfa), width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();

            extrusionPlane = Side;
            extrusionDepth = TB;

            if(CodePrf  == "I")
            {
                // Estrusione ala superiore
                macroPoint.Add(new ProgramPoint(0, offsetY + upperWingDistance, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParE, offsetY + upperWingDistance, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParA + ParE, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();

                // Estrusione ala inferiore
                macroPoint.Add(new ProgramPoint(0, offsetY - lowerWingDistance, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParG, offsetY - lowerWingDistance, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParA + ParG, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            }
            else
            {
                macroPoint.Add(new ProgramPoint(0, ParE));
                macroPoint.Add(new ProgramPoint(x1, ParE));
                macroPoint.Add(new ProgramPoint(x1 + (topY - ParE) * Math.Tan(ParALFA.ToRad()), topY));
                macroPoint.Add(new ProgramPoint(0, topY));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, radAlfa);
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}