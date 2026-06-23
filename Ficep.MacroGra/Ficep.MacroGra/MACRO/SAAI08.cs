using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SAAI08 : EyeMacro
    {

        public SAAI08(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE, ParD -  (ParD - ParB) / ParA * ParE, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParD + ParC, 0, 0));

            //
            //  Estrusione anima
            //
            double extrusionDepth = SB;
            string extrusionPlane = "C";
            List<Brep> breps = new List<Brep>();

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, false/*MirrorAltoBasso*/, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            //  Ripulisco lista punti precedentemente creati per l'estrusione dell'anima
            //
            macroPoint.Clear();
            //
            //  Estrusione ala A
            //
            double topY = 0, offsetY = 0;

            if (CodePrf == "I")
            {
                topY = SB;
                offsetY = SB / 2;
            }
            else
                topY = SB;

            macroPoint.Add(new ProgramPoint(ParA, offsetY + ParH, 0, 0));
            macroPoint.Add(new ProgramPoint(ParF, offsetY + ParG, 0, ParS));
            macroPoint.Add(new ProgramPoint(ParF + (SB / 2 - ParG) * Math.Tan(ParALFA.ToRad()), topY, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, topY, 0, 0));
          
            //
            //  ESTRUSIONE
            //
            extrusionPlane = Side;
            extrusionDepth = TB + Math.Min(Radius, ParB - ParR);
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}
