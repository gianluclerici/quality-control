using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI171 : EyeMacro
    {
        public ESTI171(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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


            double topY = SB, offsetY = SB / 2, width = SA;

            double radAlfa = !MirrorInizialeFinale ? ParALFA.ToRad() : -ParALFA.ToRad(), tanAlfa = Math.Tan(radAlfa), absTanAlfa = Math.Abs(tanAlfa);

            double safeX = -2 * TA * absTanAlfa;
            //
            //  ParA and ParC corrispondono al bordo superiore anima come da file .MAC
            //
            //  bottomFF -> X bottom ala FF
            double bottomFF = ParA + (offsetY + TA / 2) * tanAlfa;
            //  topFF  X top ala FF
            double topFF = ParA - (offsetY - TA / 2) * tanAlfa;
            //  bottomFM  X bottom ala FM
            double bottomFM = ParC + (offsetY + TA / 2) * tanAlfa;
            //  topFM  X top ala FM
            double topFM = ParC - (offsetY - TA / 2) * tanAlfa;

            //
            // Estrusione anima FF
            //
            macroPoint.Add(new ProgramPoint(safeX, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(safeX, ParB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, -radAlfa);
            macroPoint.Clear();

            //
            // Estrusione anima FM
            //
            macroPoint.Add(new ProgramPoint(safeX, width - ParD, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, width - ParD, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParC, width, 0, 0));
            macroPoint.Add(new ProgramPoint(safeX, width, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, - radAlfa);

            //
            // Cianfrino anima  -> Fatto con cianfrino e on con unico solido inclinato perchè altrimenti il surplus entraba in gioco scombinando.
            //
            if (!radAlfa.IsEqualTo(0, TolAngle))
            {
                double chamferDepth = TA;
            
                ProgramPoint startChamfer = new ProgramPoint(0, ParB, 0, 0);
                ProgramPoint endChamfer = new ProgramPoint(0, width - ParD, 0, 0);
            
                if (radAlfa < 0) 
                {
                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, Math.Abs(radAlfa), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
                else
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, Math.Abs(radAlfa), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }                                
            }
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}