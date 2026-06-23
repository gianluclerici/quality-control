using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI92 : EyeMacro
    {
        public ESTI92(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double REG_01 = 0, REG_02 = 0;

            double topY = SA;

            // REG_01 is the soul angle defined by A, B, E
            if (ParE > 0)
            {
                REG_01 = Math.Atan((ParA - ParB) / ParE);
            }
            // REG_02 is the soul angle defined by C, D, F
            if (ParF > 0)
            {
                REG_02 = Math.Atan((ParD - ParC) / ParF);
            }

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB, 0, 0));
            if (ParR > 0)
            {
                macroPoint.Add(new ProgramPoint(ParA - TB * Math.Tan(REG_01) + ParR, TB, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - TB * Math.Tan(REG_01) - ParR * Math.Sin(REG_01), TB + ParR * Math.Cos(REG_01), 0, 0, ParR));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParA - TB * Math.Tan(REG_01), TB, 0, 0));
            }
            macroPoint.Add(new ProgramPoint(ParB, ParE, 0, 0));

            //ParP ^ 2 > (SA - ParF - ParE) ^ 2 + (ParB - ParC) ^ 2; Altrimenti non vine generato correttamente il solido
            if (ParP != 0)
            {
                macroPoint.Add(new ProgramPoint(ParC, topY - ParF, 0, 0, ParP));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParC, topY - ParF, 0, 0));
            }

            if (ParS > 0)
            {
                macroPoint.Add(new ProgramPoint(ParD - TB * Math.Tan(REG_02) - ParS * Math.Sin(REG_02), topY - TB - ParS * Math.Cos(REG_02), 0, 0));
                macroPoint.Add(new ProgramPoint(ParD - TB * Math.Tan(REG_02) + ParS, topY - TB, 0, 0, ParS));
                macroPoint.Add(new ProgramPoint(ParD, topY - TB, 0, 0));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParD - TB * Math.Tan(REG_02), topY - TB, 0, 0));
            }
            macroPoint.Add(new ProgramPoint(ParD, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino esterno ala FF
            //            
            extrusionPlane = "A";
            double chamferDepth = TB - ParK - ParL;

            topY = SB;

            ProgramPoint startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA, topY, 0, 0);
            
            if (!ParJ.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParJ.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FF
            //            
            chamferDepth = ParL;
            
            startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            endChamfer = new ProgramPoint(ParA, topY, 0, 0);
            
            if (!ParG.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParG.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }    

            //
            // Cianfrino esterno ala FM
            //
            extrusionPlane = "B";
            chamferDepth = TB - ParO - ParN;
            
            startChamfer = new ProgramPoint(ParD, 0, 0, 0);
            endChamfer = new ProgramPoint(ParD, topY, 0, 0);
            
            if (!ParM.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParM.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }

            //
            // Cianfrino interno ala FM
            //            
            chamferDepth = ParO;
            
            startChamfer = new ProgramPoint(ParD, 0, 0, 0);
            endChamfer = new ProgramPoint(ParD, topY, 0, 0);
            
            if (!ParH.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParH.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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