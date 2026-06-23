using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SAAI20 : EyeMacro
    {
        public SAAI20(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = TB;
            string extrusionPlane = Side;

            double topY = SB;
            //
            // Estrusione anima
            //
            double A = 2;
            double B = 2 * ParA;
            double C = ParB * ParB + (topY - ParB - ParC) * (topY - ParB - ParC) - (topY - ParC) * (topY - ParC);

            double root = 0, radAlfa = 0, absRoot = 0;

            if (Solve2(A, B, C, ref root))
            {                
                absRoot = Math.Abs(root);
                radAlfa = Math.Atan(absRoot / ParB);
            }

            macroPoint.Add(new ProgramPoint(0, ParC, 0));
            macroPoint.Add(new ProgramPoint(ParA + absRoot, topY - ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, topY, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}