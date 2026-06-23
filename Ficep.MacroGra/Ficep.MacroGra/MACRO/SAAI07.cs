using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SAAI07 : EyeMacro
    {

        public SAAI07(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "IF";
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
            List<Brep> breps = new List<Brep>();

            if (CodePrf == "I")
            {
                //
                // Cianfrino esterno ALA
                //
                string extrusionPlane = Side;
                double chamferDepth = TB - ParD - ParC;

                double topY = SB;

                ProgramPoint startChamfer = new ProgramPoint(ParB - ParA / 2, topY, 0, 0);
                ProgramPoint endChamfer = new ProgramPoint(ParB + ParA / 2, topY, 0, 0);

                if (!ParALFA.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }

                //
                // Cianfrino interno ALA
                //
                chamferDepth = ParD;

                if (!ParBETA.IsEqualTo(0, TolAngle))
                {
                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);
                }
            }
            else if(CodePrf == "F")
            {
                string extrusionPlane = "C";
                double chamferDepth = ParC;
                double width = SA;

                //
                //  Cianfrino piano
                //
                ProgramPoint startChamfer = new ProgramPoint(ParB - ParA / 2, 0);
                ProgramPoint endChamfer = new ProgramPoint(ParB + ParA / 2, 0);

                if (!ParALFA.IsEqualTo(0, TolAngle) && !ParA.IsEqualTo(0, TolLinear))
                {
                    if (EyeGeometryUtils.AddExternalChamfer(endChamfer, startChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);

                    chamferDepth = ParG;

                    if (EyeGeometryUtils.AddInternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParALFA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA, true))
                        breps.Add(chamferA);
                }

                startChamfer = new ProgramPoint(ParE - ParD / 2, width);
                endChamfer = new ProgramPoint(ParE + ParD / 2, width);

                chamferDepth = ParF;

                if (!ParBETA.IsEqualTo(0, TolAngle) && !ParD.IsEqualTo(0, TolLinear))
                {
                    if (EyeGeometryUtils.AddExternalChamfer(startChamfer, endChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out Brep chamferA))
                        breps.Add(chamferA);

                    chamferDepth = ParH;
                    if (EyeGeometryUtils.AddInternalChamfer(endChamfer, startChamfer, Wp, extrusionPlane, ParBETA.ToRad(), chamferDepth, MirrorInizialeFinale, MirrorAltoBasso, Surplus, TolBrep, TolLinear, TolWebFlange, out chamferA, true))
                        breps.Add(chamferA);
                }
            }

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}