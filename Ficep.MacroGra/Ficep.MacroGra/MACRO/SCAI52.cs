using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI52 : EyeMacro
    {
        public SCAI52(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = ParB; //    + Surplus
            string extrusionPlane = Side;

            double topY = SB;

            //  Alfa è l'angolo di taglio anima
            double absRadAlfa = Math.Abs(Math.Atan(ParA / ParB));

            //  Beta è l'angolo di taglio ala
            double radBeta = Math.Atan((ParC - ParA) / topY);

            //  rX è la proiezione di ParR sull'obliquo dell'ala
            double rX = ParR * Math.Tan(Math.PI / 2 - radBeta) / 2;

            //
            // Estrusione obliqua dall'ala
            //
            macroPoint.Add(new ProgramPoint(0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0));
            macroPoint.Add(new ProgramPoint(ParC - rX * Math.Sin(radBeta), topY - rX * Math.Cos(radBeta)));
            macroPoint.Add(new ProgramPoint(ParC + rX, topY, 0, 0, -ParR));
            macroPoint.Add(new ProgramPoint(0, topY));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, absRadAlfa);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}