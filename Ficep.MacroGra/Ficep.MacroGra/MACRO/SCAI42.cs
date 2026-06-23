using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI42 : EyeMacro
    {

        public SCAI42(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            // Estrusione anima
            //

            double extrusionDepth = SB;
            string extrusionPlane = "C";
                       
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParA, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA * Math.Tan(ParALFA.ToRad()), 0, 0, 0));

             EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Estrusione ala bassa
            //
            
            macroPoint.Clear();

            extrusionDepth = TB + Radius;
            extrusionPlane = Side;

            double topY = SB;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA * Math.Tan(ParALFA.ToRad()) + ParD, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParE, 0, 0));

             EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Estrusione ala alta
            //

            macroPoint.Clear();

            extrusionDepth = TB + Radius;
            extrusionPlane = Side;


            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA * Math.Tan(ParALFA.ToRad()) + ParB, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY - ParC, 0, 0));

             EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);


            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}