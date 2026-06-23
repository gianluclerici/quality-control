
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.Data;
using devDept.Eyeshot.Entities;
using System.Collections.Generic;
using System.Linq;
using Ficep.Utils;

namespace Ficep.MacroGra
{
	public class SCAI01 : EyeMacro
	{

		public SCAI01(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IULQF";
        }

        //
        //  La macro tipo SCxx viene creata nel caso INIZIALE, ALA A
        //  I 4 casi possibili vengono ottenuti dalla gestione delle variabili:
        //
        //  MirrorInizialeFinale -> specula da INIZIALE a FINALE
        //  MirrorSideASideB -> specula da lato A a lato B
        //  MirrorAltoBasso -> specula da posizione ala ALTA a ala BASSA (solo per macro SAAxx)
        //
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

            ///////////////////////////////
            //      CORPO: variabile
            ///////////////////////////////
            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();
            double extrusionDepth = 0;
            string extrusionPlane = "C";

            //
            //  ESTRUSIONE SEMIALI (per ora gestito solo il profilo I)
            //
            bool flangeExtrusion = false;
            if (CodePrf == "I")
                flangeExtrusion = (ParB <= TB + TolWebFlange + ParR || Params.D <= TB + ParR);
                //flangeExtrusion = (ParB <= TB + TolWebFlange + Radius || Params.D <= TB + Radius);

            if (flangeExtrusion)
            {
                double distanceFromWeb = 2;

                if (CodePrf == "I")
                {
                    extrusionPlane = Side;
                    extrusionDepth = TB + TolWebFlange + distanceFromWeb + 2 * Radius;
                    EyeGeometryUtils.AddRectExtrusion(0, TA / 2 + distanceFromWeb, ParA, SB / 2 - TA / 2, 0, 0, 0, 0, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolWebFlange, ref breps, Surplus);
                    EyeGeometryUtils.AddRectExtrusion(0, -TA / 2 - SB / 2 + distanceFromWeb, ParA, SB / 2 - TA / 2, 0, 0, 0, 0, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolWebFlange, ref breps, Surplus);
                }
            }

            //
            //  ESTRUSIONE ANIMA
            //
            double yMin = 0;
            extrusionPlane = "C";
            if (flangeExtrusion)
            {
                yMin = 5;
                extrusionDepth = TA + 2 * Radius;
            }
            else
            {
                yMin = 0;

                if (CodePrf == "L")
                    extrusionDepth = SA;
                else if (CodePrf == "F")
                    extrusionDepth = TA;
                else
                    extrusionDepth = SB;
            }
            macroPoint.Add(new ProgramPoint(0, yMin, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, yMin, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE, ParD - (ParD - ParB) * ParE / ParA, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParD + ParC, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            //  CIANFRINI
            //
            if (ParALFA > 0 || ParBETA > 0)
            {
                //
                //  Inserire tante chiamate quanti sono i cianfrini
                //  Es: prima chiamata per cianfrino esterno, seconda chiamata per cianfrini interno inferiore,
                //      terza chiamata per cianfrino interno superiore
                //
                //  EyeGeometryUtils.AddLineChamferExt(parA, parALFA, parF, parG, parJ, sVX, sSide, Wp, Surplus, TolThickness, BrepTol, ref breps);
                //
                EyeGeometryUtils.AddLineChamfer(ParA, ParALFA, ParBETA, ParF, ParG, ParJ, VX, Side, Wp, Surplus, TolWebFlange, TolBrep, ref breps);
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////
            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

        public override bool ValidateGeometry()
        {
            if (ParA.IsLessThan(0, TolLinear) || ParB.IsLessThan(0, TolLinear) || ParC.IsLessThan(0, TolLinear) || ParD.IsLessThan(0, TolLinear) ||
                ParE.IsLessThan(0, TolLinear) || ParF.IsLessThan(0, TolLinear) || ParG.IsLessThan(0, TolLinear) || ParR.IsLessThan(0, TolLinear) ||
                ParALFA.IsLessThan(0, TolAngle) || ParBETA.IsLessThan(0, TolAngle) || ParA.IsLessThan(0, TolLinear) || ParB.IsLessThan(0, TolLinear))
                return false;

            return true;
        }
    }
}

