using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI21 : EyeMacro
    {
        public SCAI21(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "L";
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

            if (!ParA.IsEqualTo(0, TolLinear) && !ParB.IsEqualTo(0, TolLinear) && !ParD.IsEqualTo(0, TolLinear))
            {
                //
                // Estrusione PIANO A
                //
                double extrusionDepth = TA;
                string extrusionPlane = "A";
                macroPoint.Add(new ProgramPoint(ParA, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA - ParG, ParF, 0));
                macroPoint.Add(new ProgramPoint(ParA - ParG + ParH, SA, 0));
                macroPoint.Add(new ProgramPoint(0, SA, 0));
                macroPoint.Add(new ProgramPoint(0, 0, 0));
                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                macroPoint.Clear();

                extrusionDepth = SA;
                extrusionPlane = "B";
                double radAngle = Math.Atan((ParD - ParB) / ParA);

                //
                // Estrusione PIANO B
                //
                macroPoint.Add(new ProgramPoint(0, ParD + ParC, 0));
                macroPoint.Add(new ProgramPoint(ParE, ParB + (ParA - ParE) * Math.Tan(radAngle), 0));
                macroPoint.Add(new ProgramPoint(ParA, ParB, 0, ParR));
                macroPoint.Add(new ProgramPoint(ParA, TA, 0));
                macroPoint.Add(new ProgramPoint(0, TA, 0));
                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
                }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}