using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA15 : EyeMacro
    {

        public ESTIA15(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth;
            string extrusionPlane;

            //
            // Estrusione anima
            //
            extrusionPlane = "C";
            extrusionDepth = SB;

            double width = SA, topY = SB;
            double tanAlfa = Math.Tan(ParALFA.ToRad());
            double radBeta = (90 - ParALFA).ToRad();
            double offsetAlfaFFX = (width - TB) * tanAlfa, offsetAlfaFMX = TB * tanAlfa;

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetAlfaFFX, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetAlfaFFX, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetAlfaFFX + ParR, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(offsetAlfaFFX - ParR * Math.Cos(radBeta), TB + ParR * Math.Sin(radBeta), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(offsetAlfaFMX + ParR * Math.Cos(radBeta), width - TB - ParR * Math.Sin(radBeta), 0, 0));
            macroPoint.Add(new ProgramPoint(offsetAlfaFMX + ParR, width - TB, 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(offsetAlfaFMX, width - TB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrini anima
            //

            double chamferDepth = ParE;

            double chamferFFX = offsetAlfaFFX - ParR * Math.Cos(radBeta) / 2, chamferFMX = offsetAlfaFMX + ParR * Math.Cos(radBeta) / 2;

            ProgramPoint startChamfer = new ProgramPoint(MirrorSideASideB ? chamferFMX : chamferFFX, TB + ParR * Math.Sin(radBeta) / 2);
            ProgramPoint endChamfer = new ProgramPoint(!MirrorSideASideB ? chamferFMX : chamferFFX, width - TB - ParR * Math.Sin(radBeta) / 2);

            if (!ParC.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParC.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            chamferDepth = TB - ParF - ParE;

            if (!ParD.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParD.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino esterno ala Side
            //
            extrusionPlane = Side;
            chamferDepth = TB;
            
            startChamfer = new ProgramPoint(offsetAlfaFFX, 0, 0, 0);
            endChamfer = new ProgramPoint(offsetAlfaFFX, topY, 0, 0);
            
            if (!ParA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino esterno ala Opposite Side
            //
            extrusionPlane = Side == "A" ? "B" : "A" ;

            startChamfer = new ProgramPoint(offsetAlfaFMX, 0, 0, 0);
            endChamfer = new ProgramPoint(offsetAlfaFMX, topY, 0, 0);
            
            if (!ParB.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParB.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}