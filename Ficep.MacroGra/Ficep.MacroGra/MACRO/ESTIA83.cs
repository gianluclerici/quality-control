using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA83 : EyeMacro
    {
        public ESTIA83(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            double width = SA, topY = SB, offsetY = SB / 2;

            double radAlfa = ParALFA.ToRad(), tanAlfa = Math.Tan(radAlfa), sinAlfa = Math.Sin(radAlfa), cosAlfa = Math.Cos(radAlfa);

            //double tanBeta = (ParE - ParF) / ParC, radBeta = Math.Atan(tanBeta); // TANBETA non è questo!? che viene da file . MAC
            double tanBeta = (ParE - ParF) / (ParC + (width - ParE) * tanAlfa), radBeta = Math.Atan(tanBeta);

            double offsetX = width * tanAlfa;
            double WME = width - ParE; //   WME stands for width - ParE
            
            macroPoint.Add(new ProgramPoint(0, 0));
            macroPoint.Add(new ProgramPoint(ParA + offsetX, 0));
            macroPoint.Add(new ProgramPoint(ParC + offsetX, width - ParF, 0, ParG));
            macroPoint.Add(new ProgramPoint(ParD + offsetX + 2 * ParR * cosAlfa + ParL * cosAlfa, WME + (WME * tanAlfa + ParD) * tanBeta + 2 * ParR * sinAlfa + ParL * sinAlfa, 0, ParS));
            macroPoint.Add(new ProgramPoint(ParD + offsetX - ParH * sinAlfa + 2 * ParR * cosAlfa + ParL * cosAlfa, WME + (WME * tanAlfa + ParD) * tanBeta + ParH * cosAlfa + 2 * ParR * sinAlfa + ParL * sinAlfa));
            macroPoint.Add(new ProgramPoint(ParD + offsetX - ParH * sinAlfa + ParR * (cosAlfa - sinAlfa) + ParL * cosAlfa, WME + (WME * tanAlfa + ParD) * tanBeta + ParH * cosAlfa + ParR * (cosAlfa + sinAlfa) + ParL * sinAlfa, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParD + offsetX - ParH * sinAlfa + ParR * (cosAlfa - sinAlfa), WME + (WME * tanAlfa + ParD) * tanBeta + ParH * cosAlfa + ParR * (cosAlfa + sinAlfa)));
            macroPoint.Add(new ProgramPoint(ParD + offsetX - ParH * sinAlfa, WME + (WME * tanAlfa + ParD) * tanBeta + ParH * cosAlfa, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParD + offsetX, WME + (WME * tanAlfa + ParD) * tanBeta, 0, ParS));
            macroPoint.Add(new ProgramPoint(ParE * tanAlfa, WME));
            macroPoint.Add(new ProgramPoint(0, width));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}