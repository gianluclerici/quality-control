using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SAAI05 : EyeMacro
    {
        public SAAI05(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IULQ";
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

            double extrusionDepth = (CodePrf == "L" && Side == "A") ? TA + Radius : TB + Radius;
            string extrusionPlane = Side;

            double topY = SB, offsetY = 0;
            double angExtrusion = 0;
            double radAlfa = -ParALFA.ToRad(), radBeta = -ParBETA.ToRad();

            if (CodePrf == "L" && Side == "A") topY = SA;

            if (CodePrf == "I")
            {
                offsetY = SB / 2;
                angExtrusion = MirrorInizialeFinale != MirrorSideASideB ? -ParD.ToRad() : ParD.ToRad();
                radAlfa = -radAlfa;
                radBeta = -radBeta;
            }

            //
            // Estrusione ala
            //
            if (CodePrf == "Q")
            {
                topY = MirrorAltoBasso ? 0 : topY;
                extrusionDepth = SA;
                macroPoint.Add(new ProgramPoint(ParE - ParA / 2, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(ParE - ParA / 2, topY - ParB, 0, ParR));
                macroPoint.Add(new ProgramPoint(ParE + ParA / 2, topY - ParB, 0, ParR));
                macroPoint.Add(new ProgramPoint(ParE + ParA / 2, topY, 0, 0));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParE - ParA / 2 - (offsetY - ParB) * Math.Tan(radAlfa), topY, 0, 0));
                macroPoint.Add(new ProgramPoint(ParE - ParA / 2, offsetY + ParB, 0, ParR));
                macroPoint.Add(new ProgramPoint(ParE + ParA / 2, offsetY + ParB, 0, ParR));
                macroPoint.Add(new ProgramPoint(ParE + ParA / 2 + (offsetY - ParB) * Math.Tan(radBeta), topY, 0, 0));
            }
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, angExtrusion);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}