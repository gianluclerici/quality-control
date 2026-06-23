using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class ESTI76 : EyeMacro
    {
        public ESTI76(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = ParB;
            string extrusionPlane = "A";

            double topY = SB;

            //
            // Estrusione ALA FF
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParF, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParG, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParE, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));
            
            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            macroPoint.Clear();
            
            //
            // Estrusione ALA FM
            //
            extrusionDepth = ParD;
            extrusionPlane = "B";

            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC + ParI, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC, ParJ, 0, 0));
            macroPoint.Add(new ProgramPoint(ParC + ParH, topY, 0, 0));
            macroPoint.Add(new ProgramPoint(0, topY, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }
    }
}