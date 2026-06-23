using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA16 : EyeMacro
    {
        public ESTIA16(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth;
            string extrusionPlane;

            //
            // Estrusione anima
            //
            extrusionPlane = "C";
            extrusionDepth = SB;

            // gamma e' l'angolo in corrispondenza del tondo.
            double gamma = Math.Atan(ParB / (ParA - ParF));
            double sinGamma = Math.Sin(gamma), cosGamma = Math.Cos(gamma);

            macroPoint.Add(new ProgramPoint(0, ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParC + 2 * ParR, 0, 0, ParR));
            // dubbi su questi due punti di seguito
            macroPoint.Add(new ProgramPoint(ParA - ParR * cosGamma, ParC + ParR * (1 + sinGamma), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA - ParR * cosGamma * (1 + sinGamma), ParC + ParR + ParR * (sinGamma - cosGamma * cosGamma)));

            macroPoint.Add(new ProgramPoint(ParF, ParB + ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF, SA, 0, 0));
            macroPoint.Add(new ProgramPoint(0, SA, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}