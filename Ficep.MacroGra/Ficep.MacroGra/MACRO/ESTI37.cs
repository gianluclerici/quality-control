using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI37 : EyeMacro
    {
        public ESTI37(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SA;
            string extrusionPlane = "A";

            double topY = SB, width = SA;
            //
            // Estrusioni passanti dal lato A
            //
            //  BASSA
            if (ParM > 0)
            {
                macroPoint.Add(new ProgramPoint(0, ParM + ParF, 0, 0));
                macroPoint.Add(new ProgramPoint(ParM, ParF, 0, 0, ParM));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(0, ParG + ParF, 0, 0));
                macroPoint.Add(new ProgramPoint(ParH, ParF, 0, 0));
            }
            macroPoint.Add(new ProgramPoint(ParE - ParS, ParF));
            macroPoint.Add(new ProgramPoint(ParE, ParF - ParS, 0, 0, - ParS));
            macroPoint.Add(new ProgramPoint(ParE, TA - ParK, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE + (TA - ParK) * Math.Tan(ParBETA.ToRad()), 0, 0, 0));
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            
            //  ALTA
            if (ParL > 0)
            {
                macroPoint.Add(new ProgramPoint(0, topY - ParB - ParL, 0, 0));
                macroPoint.Add(new ProgramPoint(ParL, topY - ParB, 0, 0, -ParL));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(0, topY - ParB - ParC, 0, 0));
                macroPoint.Add(new ProgramPoint(ParD, topY - ParB, 0, 0));
            }
            macroPoint.Add(new ProgramPoint(ParA - ParR, topY - ParB));
            macroPoint.Add(new ProgramPoint(ParA, topY - (ParB - ParR), 0, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParA, topY - TA + ParJ, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + (TA - ParJ) * Math.Tan(ParALFA.ToRad()), topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            macroPoint.Clear();
            
            //
            // Estrusione piano C
            //
            extrusionDepth = TA;
            extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(0, ParO));
            macroPoint.Add(new ProgramPoint(ParA + ParN, ParO, 0, ParQ));
            macroPoint.Add(new ProgramPoint(ParA + ParN, width - ParP, 0, ParQ));
            macroPoint.Add(new ProgramPoint(0, width - ParP));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}