using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI54 : EyeMacro
    {
        public SCAI54(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "Q";
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

            double extrusionDepth = TB;
            string extrusionPlane = Side;

            double offsetY = SB / 2;

            double chamferDepth = TB - ParF;
            //
            // Estrusione anima
            //
                macroPoint.Add(new ProgramPoint(0, offsetY + ParC - ParB / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY + ParC - ParB / 2, 0, ParR));
                macroPoint.Add(new ProgramPoint(ParA, offsetY + ParC + ParB / 2, 0, ParR));
                macroPoint.Add(new ProgramPoint(0, offsetY + ParC + ParB / 2, 0, 0));

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrini esterni di contorno
            //
            if (!ParALFA.IsEqualTo(0, TolAngle))
            {
                //  Cianfrino bordo basso
                ProgramPoint startChamfer = new ProgramPoint(0, offsetY + ParC - ParB / 2);
                ProgramPoint endChamfer = new ProgramPoint(ParA - ParR, offsetY + ParC - ParB / 2);            

                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                    breps.Add(chamferA);

                //  Cianfrino bordo verticale
                startChamfer = new ProgramPoint(ParA, offsetY + ParC - ParB / 2 + ParR);
                endChamfer = new ProgramPoint(ParA, offsetY + ParC + ParB / 2 - ParR);

                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth - TolWebFlange, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                    breps.Add(chamferA);

                //  Cianfrino bordo alto
                startChamfer = new ProgramPoint(ParA - ParR, offsetY + ParC + ParB / 2);
                endChamfer = new ProgramPoint(0, offsetY + ParC + ParB / 2);

                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth - TolWebFlange, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                    breps.Add(chamferA);

                //  Cianfrino angolo basso
                ProgramPoint chamferCentre = new ProgramPoint(ParA - ParR, offsetY + ParC - ParB / 2 + ParR, 0, ParR);

                if (EyeGeometryUtils.AddExternalCircularChamfer(chamferCentre, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
                    breps.Add(chamferA);

                //  Cianfrino angolo alto
                chamferCentre = new ProgramPoint(ParA - ParR, offsetY + ParC + ParB / 2 - ParR, 0, ParR);

                if (EyeGeometryUtils.AddExternalCircularChamfer(chamferCentre, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA))
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