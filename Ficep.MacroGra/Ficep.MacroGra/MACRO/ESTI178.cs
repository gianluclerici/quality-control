using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Ficep.RobServer.MacroParser;

namespace Ficep.MacroGra
{
    public class ESTI178 : EyeMacro
    {
        public ESTI178(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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
            //  Estrusione anima
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            double width = SA;

            macroPoint.Add(new ProgramPoint(0, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParE, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParL, width - ParG, 0, 0));
            macroPoint.Add(new ProgramPoint(0, width - ParG, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            //  NEW APPROACH USING SCXX80
            //

            EyeParam eyeParam = new EyeParam(TolLinear, TolAngle, TolBrep, TolWebFlange, Surplus, InnerChamferDisFromWeb);

            List<EyeFeature> features = new List<EyeFeature>();
            
            CopeParam copeParams = (CopeParam)Params;
            copeParams.SIDE = "A";
            copeParams.E = ParM;
            copeParams.F = ParN;
            
            //  SCAX80  Estrusione ala FF
            bool returnValue = SCAI80.SCXX80(Wp, copeParams, eyeParam, MirrorInizialeFinale, false, MirrorAltoBasso, ref features);           
            
            copeParams.A = ParF;
            copeParams.B = ParG;
            copeParams.C = ParH;
            copeParams.D = ParI;
            copeParams.E = ParO;
            copeParams.F = ParP;
            copeParams.R = ParS;
            copeParams.ALFA = ParJ;
            copeParams.BETA = ParK;
            copeParams.SIDE = "B";
            
            //  SCBX80  Estrusione ala FM
            returnValue = SCAI80.SCXX80(Wp, copeParams, eyeParam, MirrorInizialeFinale, true, MirrorAltoBasso, ref features);
            
            Features = features;

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////
            
            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return returnValue;
        }

    }
}