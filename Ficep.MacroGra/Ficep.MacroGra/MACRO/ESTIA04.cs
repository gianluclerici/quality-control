using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA04 : EyeMacro
    {

        public ESTIA04(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double radALFA = ParALFA.ToRad();
            if (MirrorInizialeFinale)
                radALFA = -radALFA;

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            double topY = SB, offsetY = SB / 2, width = SA;

            double extrusionDepth = width, 
                   extrusionAngle = radALFA >= 0 ? -radALFA : radALFA;
            string extrusionPlane = radALFA >= 0 ? "B" : "A";

            double offsetX = - width * Math.Abs(Math.Tan(radALFA)) - Surplus;

            //
            //  Estrusione ala con inclinazione
            //
            if (CodePrf == "I")
            {
                macroPoint.Add(new ProgramPoint(ParC, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(0, offsetY + ParB, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, topY, 0, 0));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(topY * Math.Tan(ParBETA.ToRad()), 0, 0, 0));
                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            }
            macroPoint.Add(new ProgramPoint(0 + offsetX, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0 + offsetX, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, extrusionAngle, 0);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }
    }
}