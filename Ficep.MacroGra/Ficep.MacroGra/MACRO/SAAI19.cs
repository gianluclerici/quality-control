using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SAAI19 : EyeMacro
    {
        public SAAI19(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "U";
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

            double topY = SB;

            //MirrorInizialeFinale gestito manualmente perchè così è fatto su Pegaso. non mirrora effettivamente ma offsetta.
            double actualF = MirrorInizialeFinale ? Lp - ParF : ParF;

            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(actualF - ParA / 2 + ParG, topY, 0));
            macroPoint.Add(new ProgramPoint(actualF - ParA / 2 + ParG, topY - ParH, 0));
            macroPoint.Add(new ProgramPoint(actualF - ParA / 2, topY - ParH - ParI, 0));
            macroPoint.Add(new ProgramPoint(actualF - ParA / 2, topY - ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(actualF + ParA / 2, topY - ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(actualF + ParA / 2, topY - ParD - ParE, 0));
            macroPoint.Add(new ProgramPoint(actualF + ParA / 2 - ParC, topY - ParD, 0));
            macroPoint.Add(new ProgramPoint(actualF + ParA / 2 - ParC, topY, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, false, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}