using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI22 : EyeMacro
    {

        public SCAI22(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            //
            // Estrusione anima //Secondo file SCAI22.MAC
            //



            double reg_21 = Math.Acos(ParB / ParR); //Non lo avevo capito dalla bitmap
            double reg_01 = ParS / Math.Tan(((90 - ParBETA) / 2).ToRad());
            double reg_02 = ParS * Math.Cos(ParBETA.ToRad());
            double reg_03 = ParS * Math.Sin(ParBETA.ToRad());


            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, TB + ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD, TB + ParC, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParR * Math.Sin(reg_21), TB + ParR * Math.Cos(reg_21), 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, TB + ParR, 0, 0, - ParR));
            macroPoint.Add(new ProgramPoint(ParA + (ParE + ParR) * Math.Tan(ParBETA.ToRad()) - reg_01, TB + ParR, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + (ParE + ParR) * Math.Tan(ParBETA.ToRad()) - reg_01 + reg_02, TB + ParR - ParS - reg_03, 0, 0, -ParS));
            macroPoint.Add(new ProgramPoint(ParA, TB - ParE, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));


            //Precedentemente inventata
            
            //macroPoint.Add(new ProgramPoint(ParA, TB - ParE, 0, 0));
            //macroPoint.Add(new ProgramPoint(ParA + (ParE + ParB + ParR) * Math.Tan(ParBETA.ToRad()), TB + ParB + ParR, 0, ParS)); //interpretazione mia?
            //macroPoint.Add(new ProgramPoint(ParA, TB + ParB + ParR, 0, 0)); //interpretazione mia?
            //macroPoint.Add(new ProgramPoint(ParA - ParR, TB + ParB + ParR, 0, ParR)); //interpretazione mia?
            //macroPoint.Add(new ProgramPoint(ParA - ParR, TB + ParB, 0, 0)); //interpretazione mia?
            //macroPoint.Add(new ProgramPoint(ParD, TB + ParC, 0, 0));
            //macroPoint.Add(new ProgramPoint(0, TB + ParC, 0, 0));
            //macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            //macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));

             EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino esterno ala Side
            //

            extrusionPlane = Side;
            double chamferDepth = TB - ParE - ParF;
        
            ProgramPoint startChamfer = new ProgramPoint(ParA, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(ParA, SB, 0, 0);
            
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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