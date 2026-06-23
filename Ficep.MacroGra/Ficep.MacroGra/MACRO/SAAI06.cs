using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SAAI06 : EyeMacro
    {

        public SAAI06(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IUL";
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

            double topY = 0, offsetY = 0;

            if (CodePrf == "I")
            {
                topY = SB;
                offsetY = SB / 2;
            }
            else if (CodePrf == "L")
                topY = MirrorSideASideB ? SB : SA;
            else
                topY = SB;

            // Gestione caso arco 180 gradi
            double parR = ParR;

            if (ParR.IsEqualTo(ParC / 2, TolLinear))
                parR *= 0.999;
            //ORIGINAL**************************************************************************
            //macroPoint.Add(new ProgramPoint(0, offsetY +  ParB - ParC / 2, 0, 0));
            //macroPoint.Add(new ProgramPoint(ParA, offsetY + ParB - ParC / 2, 0, parR));
            //macroPoint.Add(new ProgramPoint(ParA, offsetY + ParB + ParC / 2, 0, parR));
            //macroPoint.Add(new ProgramPoint(0, offsetY + ParB + ParC / 2, 0, 0));
            //**********************************************************************************
            macroPoint.Add(new ProgramPoint(0, offsetY + ParB - ParC / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, offsetY + ParB - ParC / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + parR, offsetY + ParB - ParC / 2 + parR, 0, 0, parR));
            macroPoint.Add(new ProgramPoint(ParA + parR, offsetY + ParB + ParC / 2 - parR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, offsetY + ParB + ParC / 2, 0, 0, parR));
            macroPoint.Add(new ProgramPoint(0, offsetY + ParB + ParC / 2, 0, 0));

            //
            //  ESTRUSIONE
            //
            double extrusionDepth = TB + Radius;
            string extrusionPlane = Side;
            List<Brep> breps = new List<Brep>();

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}
