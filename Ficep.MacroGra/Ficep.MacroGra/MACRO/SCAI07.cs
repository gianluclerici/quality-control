using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI07 : EyeMacro
    {

        public SCAI07(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            List<Brep> breps = new List<Brep>();

            //
            // Estrusione anima
            //

            double extrusionDepth = CodePrf == "L" ? SA : SB;
            string extrusionPlane = CodePrf == "L" ? "B" : "C";

            double spessoreAla = CodePrf == "L" ? TA : TB;
            double actualD = ParD;
            if (ParD.IsEqualTo(Radius, TolLinear))
                actualD += TolWebFlange;


            if (CodePrf == "I") //  Anche se in realtà tenere il cianfrino interno per tutti e tre i profili non causerebbe problemi
            {
                macroPoint.Add(new ProgramPoint(0, spessoreAla - ParE, 0, 0));
                macroPoint.Add(new ProgramPoint((ParE + actualD) * Math.Tan(ParBETA.ToRad()), spessoreAla + actualD, 0, 0));
            }
            else
                macroPoint.Add(new ProgramPoint(0, spessoreAla + actualD));

            macroPoint.Add(new ProgramPoint(ParA, spessoreAla + actualD, 0, ParB / 2));
            macroPoint.Add(new ProgramPoint(ParA, spessoreAla + actualD + ParB, 0, ParB / 2));
            macroPoint.Add(new ProgramPoint(0, spessoreAla + actualD + ParB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino esterno ala 
            //
            extrusionPlane = Side;
            double chamferDepth = spessoreAla - ParC - (CodePrf == "I" ? ParE : 0);
            //
            ProgramPoint startChamfer = new ProgramPoint(0, 0, 0, 0);
            ProgramPoint endChamfer = new ProgramPoint(0, extrusionDepth, 0, 0);
            //
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