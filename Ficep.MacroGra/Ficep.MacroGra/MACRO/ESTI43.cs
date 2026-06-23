using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI43 : EyeMacro
    {
        public ESTI43(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            double actualR = 0, millY = 0;
            double width = SA;

            if( ParDC / 2 >= ParA)
            {
                actualR = (ParDC / 2) >= ParA ? ParDC / 2 : 0;
                millY = Math.Sqrt(ParDC * ParDC / 4 - ParA * ParA);
            }
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(0, TB + ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(actualR, TB + ParB + actualR, 0, 0, actualR));
            if( width - 2 * (TB + ParB + actualR + millY) > 0)
            {
                macroPoint.Add(new ProgramPoint(ParA, TB + ParB + actualR + millY, 0, 0, actualR));
                macroPoint.Add(new ProgramPoint(ParA, width - (TB + ParB + actualR + millY), 0, 0));
            }
            else // questo else potrebbe essere inutile perchè gestito dalla validate
            {
                macroPoint.Add(new ProgramPoint(ParA, width / 2, 0, 0, actualR));
            }
            macroPoint.Add(new ProgramPoint(actualR, width - (TB + ParB + actualR), 0, 0, actualR));
            macroPoint.Add(new ProgramPoint(0, width - (TB + ParB), 0, 0, actualR));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}