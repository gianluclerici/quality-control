using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI23 : EyeMacro
    {
        public ESTI23(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "QF";
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

            double width = SA;

            ProgramPoint startChamfer = new ProgramPoint(0, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(0, width, 0, 0);

            double radAlfa = CodePrf == "Q" ? ParALFA.ToRad() : Math.Abs(ParALFA.ToRad());
            //
            // Cianfrino esterno Piano C
            //
            string extrusionPlane = "C";
            double chamferDepth = TA - (CodePrf == "Q" ? ParA : 0);
            
            if ((CodePrf == "Q" && !ParALFA.IsEqualTo(0, TolAngle)) || CodePrf == "F" && ((ParALFA < 0 && !MirrorInizialeFinale) || (ParALFA > 0 && MirrorInizialeFinale)))
            {
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radAlfa, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            //
            // Cianfrino esterno Piano D
            //
            chamferDepth = TA - (CodePrf == "Q" ? ParB : 0);

            if (CodePrf == "Q" && !ParBETA.IsEqualTo(0, TolAngle))
            {
                extrusionPlane = "D";

                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);
            }
            else if(CodePrf == "F" && ((ParALFA > 0 && !MirrorInizialeFinale) || (ParALFA < 0 && MirrorInizialeFinale)))
            {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, radAlfa, chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
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