using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SAAI21 : EyeMacro
    {
        public SAAI21(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            double radAlfa = Math.Atan(ParA / (topY - ParB));

            macroPoint.Add(new ProgramPoint(0, ParB, 0));
            if (ParR > 0)
            {
                double radBeta = (Math.PI / 2 - radAlfa) / 2;
                double xR = ParR * Math.Tan(radBeta);
                macroPoint.Add(new ProgramPoint(ParA - xR * Math.Sin(radAlfa), topY - xR * Math.Cos(radAlfa), 0));
                macroPoint.Add(new ProgramPoint(ParA + xR, topY, 0, 0, - ParR));
            }
            else
            {   // se ParD > topY - ParB ci sono problemi. e non è segnato come errore nella ER_SAA21
                macroPoint.Add(new ProgramPoint(ParA - ParD * Math.Tan(radAlfa), topY - ParD, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParC, topY, 0));
            }
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