using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI13 : EyeMacro
    {

        public SCAI13(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IU";
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

            //  ESTRUSIONE ANIMA
            double extrusionDepth = SB;
            string extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, ParR));
            if (ParS > 0)
            {
                macroPoint.Add(new ProgramPoint(0, ParD, 0, ParS));
                macroPoint.Add(new ProgramPoint(0, ParD + ParS, 0, 0));
            }
            else
            {
                macroPoint.Add(new ProgramPoint(ParE, ParD - (ParD - ParB) * ParE / ParA, 0, 0));
                macroPoint.Add(new ProgramPoint(0, ParD + ParC, 0, 0));
            }

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            double topY = 0, offsetY = 0;

            if (CodePrf == "I")
            {
                topY = SB;
                offsetY = SB / 2;
            }
            else
                topY = SB;

            //  ESTRUSIONE SEMI-ALA SUPERIORE
            if (ParF > 0)
            {
                extrusionDepth = TB + Radius;
                extrusionPlane = Side;

                macroPoint.Clear();

                macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParF, topY, 0, 0));
                if (CodePrf == "I")
                {
                    macroPoint.Add(new ProgramPoint(ParA, offsetY + TA / 2 + ParH, 0, 0));
                    macroPoint.Add(new ProgramPoint(ParA, offsetY + TA / 2, 0, 0));
                    macroPoint.Add(new ProgramPoint(0, offsetY + TA / 2, 0, 0));
                }
                else
                {
                    macroPoint.Add(new ProgramPoint(ParA, offsetY + ParH, 0, 0));
                    macroPoint.Add(new ProgramPoint(ParA, offsetY + TA, 0, 0));
                    macroPoint.Add(new ProgramPoint(0, offsetY + TA, 0, 0));
                }

                EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);
            }

            //  ESTRUSIONE SEMI-ALA INFERIORE
            if (ParG > 0)
            {
                extrusionDepth = TB + Radius;
                extrusionPlane = Side;

                macroPoint.Clear();

                macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA + ParG, 0, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY - TA / 2 - ParH, 0, 0));
                macroPoint.Add(new ProgramPoint(ParA, offsetY - TA / 2, 0, 0));
                macroPoint.Add(new ProgramPoint(0, offsetY - TA / 2, 0, 0));

                //  ESTRUSIONE 3
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
