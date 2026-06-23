using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTIA174 : EyeMacro
    {
        public ESTIA174(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SA;
            string extrusionPlane = Side;
            double topY = SB, offsetY = SB / 2, width = SA;

            double absAlfa = Math.Abs(ParALFA.ToRad()), absTanAlfa = Math.Tan(absAlfa);

            ////ATTENZIONE parametri tali che (R >= A && R >= B) && (S >= C && S >= D)
            //
            // Estrusione unica inclinata da ala Side
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0)); 
            if (!ParS.IsEqualTo(0, TolLinear) || !ParR.IsEqualTo(0, TolLinear))
            {
                if (!ParS.IsEqualTo(0, TolLinear))
                {
                    macroPoint.Add(new ProgramPoint(width * absTanAlfa + ParC, 0, 0));
                    macroPoint.Add(new ProgramPoint(width * absTanAlfa, ParD, 0, 0, -ParS));
                }
                if (!ParR.IsEqualTo(0, TolLinear))
                {
                    macroPoint.Add(new ProgramPoint(width * absTanAlfa, topY - ParB, 0));
                    macroPoint.Add(new ProgramPoint(width * absTanAlfa + ParA, topY, 0, 0, -ParR));
                }
                else
                {
                    macroPoint.Add(new ProgramPoint(width * absTanAlfa, topY, 0));
                }
            }
            else
            {
                macroPoint.Add(new ProgramPoint(width * absTanAlfa, 0, 0));
                macroPoint.Add(new ProgramPoint(width * absTanAlfa, topY, 0));
            }
            macroPoint.Add(new ProgramPoint(0, topY, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, absAlfa);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}