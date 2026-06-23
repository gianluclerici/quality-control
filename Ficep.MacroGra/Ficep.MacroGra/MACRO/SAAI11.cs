using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SAAI11 : EyeMacro
    {

        public SAAI11(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            // Estrusione ala

            double extrusionDepth = TB + Radius;
            string extrusionPlane = Side;

            double topY = SB, offsetY = SB / 2;

            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, offsetY + ParG - ParH, 0, 0));
            macroPoint.Add(new ProgramPoint(ParI, offsetY + ParG + (ParF - ParG) * ParI / ParE, 0, 0)); //triangoli simili
            macroPoint.Add(new ProgramPoint(ParE, offsetY + ParF, 0, ParS));
            macroPoint.Add(new ProgramPoint(ParE + (ParD - ParF) * Math.Tan(ParBETA.ToRad()), offsetY + ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, offsetY + ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA + (offsetY - ParB) * Math.Tan(ParALFA.ToRad()), topY, 0, 0));


             EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);


            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}