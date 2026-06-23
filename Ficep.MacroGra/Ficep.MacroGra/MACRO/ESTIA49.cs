using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA49 : EyeMacro
    {
        public ESTIA49(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "U";
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

            double width = SA;

            double tanAlfa = ParA / ParB, radAlfa = Math.Atan(tanAlfa);

            //
            // Estrusione anima secondo ESTIA49U.MAC
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            double x = 0;
            double y = ParB;
            macroPoint.Add(new ProgramPoint(x, y, 0, 0));
            x = ParC;
            y += ParC * tanAlfa;
            macroPoint.Add(new ProgramPoint(x, y, 0, 0));
            x += ParD;
            y -= ParD / tanAlfa;
            macroPoint.Add(new ProgramPoint(x, y, 0, 0));
            x += ParE;
            y += ParE * tanAlfa;
            macroPoint.Add(new ProgramPoint(x, y, 0, 0));
            // REG_06 is H      //   Commento del file.MAC che non ho capito
            double REG_06 = ((width - y - ParH * tanAlfa) * tanAlfa + ParG) / (Math.Cos(radAlfa) + Math.Sin(radAlfa) * tanAlfa);
            x += (ParF - ParR) * Math.Sin(radAlfa);
            y -= (ParF - ParR) * Math.Cos(radAlfa);
            double tempX = x;
            double tempY = y;
            macroPoint.Add(new ProgramPoint(x, y, 0, 0));
            x += ParR * Math.Cos(radAlfa) + ParR * Math.Sin(radAlfa);
            y += ParR * Math.Sin(radAlfa) - ParR * Math.Cos(radAlfa);
            macroPoint.Add(new ProgramPoint(x, y, 0, 0, ParR));
            x += (REG_06 - 2 * ParR) * Math.Cos(radAlfa);
            y += (REG_06 - 2 * ParR) * Math.Sin(radAlfa);
            macroPoint.Add(new ProgramPoint(x, y, 0, 0));
            x = tempX + REG_06 * Math.Cos(radAlfa);
            y = tempY + REG_06 * Math.Sin(radAlfa);
            macroPoint.Add(new ProgramPoint(x, y, 0, 0, ParR));
            x = ParC + ParD + ParE + ParG;
            y = width - ParH * tanAlfa;
            macroPoint.Add(new ProgramPoint(x, y, 0, 0));
            x = ParC + ParD + ParE + ParG + ParH;
            macroPoint.Add(new ProgramPoint(x, width, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}