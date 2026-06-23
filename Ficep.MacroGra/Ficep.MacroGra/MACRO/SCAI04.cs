using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI04 : EyeMacro
    {
        public SCAI04(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IULQ";
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

            ///////////////////////////////
            //      CORPO: variabile
            ///////////////////////////////
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            //
            //  ESTRUSIONE ANIMA
            //
            string extrusionPlane = "C";
            double extrusionDepth;

            if (CodePrf == "L")
                extrusionDepth = SA;
            else
                extrusionDepth = SB;

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParD, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, 0));
            if(CodePrf != "Q")
            {
                macroPoint.Add(new ProgramPoint(ParF + ParR, ParB, 0, 0));
                macroPoint.Add(new ProgramPoint(ParF + ParR, ParB + ParG + ParR, 0, ParR * 0.999));
                macroPoint.Add(new ProgramPoint(ParF - ParR, ParB + ParG + ParR, 0, ParR * 0.999));
                macroPoint.Add(new ProgramPoint(ParF - ParR, ParB, 0, 0));
            }
            macroPoint.Add(new ProgramPoint(ParE, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParB + ParC, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }
    }
}
