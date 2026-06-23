using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SAAI04 : EyeMacro
    {

        public SAAI04(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double topY = 0, offsetY = 0;

            if (CodePrf == "I")
            {
                topY = SB;
                offsetY = SB / 2;
            }
            else if (CodePrf == "L")
                topY = MirrorSideASideB ? SB : SA;
            else
                topY = SB;

            ProgramPoint startChamfer = new ProgramPoint(0, offsetY + ParB, 0, 0),
                         endChamfer = new ProgramPoint(ParA, topY, 0, 0);
            macroPoint.Add(startChamfer);
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            macroPoint.Add(endChamfer);

            //
            //  ESTRUSIONE
            //
            double extrusionDepth = TB + Radius;
            string extrusionPlane = Side;

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            // Cianfrino
            //
            if (!ParALFA.IsEqualTo(0, TolAngle))
            { 
                if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), TB - ParC, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamfer))
                    breps.Add(chamfer);
            }   

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}
