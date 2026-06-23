using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.MacroGra;
using Ficep.RobServer.Data;
using Ficep.RobServer.Utility3D;
using Ficep.Utils;
using FicepInterfaces;
using FicepXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xbim.Common.Collections;
using static Ficep.RobServer.Utility3D.EyeCuttingEdge;

//
//  Classi per l'interfacciamento col formato XML vettoriale Ficep
//
namespace Ficep.RobServer.XmlNet
{
    //
    //  Classe contorno ad uso XmlInterface
    //
    public class CContour
    {
        public bool noSorting = false;  //  ad uso DEBUG
        private double tol = 0.01;

        public CContour()
        {
            Name = "";
            Comp = ToolSideComp.None;
            Type = PathType.Default;
            LeadIn = new CLeadInOut();
            LeadOut = new CLeadInOut();
            List = new List<EyeContourLine>();
            End = false;
            FeatureExtractionParam = null;
        }

        public CContour(CProfile profile, string name, ToolSideComp comp, PathType type, bool inner, List<EyeLineEdge> list, 
            CFeatureExtractionParam featureExtractionParam) : this()
        {
            Name = name;
            Comp = comp;
            Type = type;
            FeatureExtractionParam = featureExtractionParam;

            FilterAddContourLine(profile, name, comp, type, inner, list);

            MoveVectors(profile);
        }
        
        //
        //  Crea le linee del contorno filtrando i LineEdge in ingresso
        //
        private bool FilterAddContourLine(CProfile profile, string name, ToolSideComp comp, PathType type, bool inner, List<EyeLineEdge> list)
        {
            double HA = profile.HA, TA = profile.TA, HB = profile.HB, TB = profile.TB, HC = profile.HC, TC = profile.TC,
                webRadius = profile.Radius;
            double minEdgeLenInsideWeb = TC, minEdgeLenInsidePermittedWebRadius = webRadius / 2;
            double X1 = 0, Y1 = 0, Z1 = 0, X2 = 0, Y2 = 0, Z2 = 0;
            bool xyEdge = false, xzEdge = false, xyEdge0 = false, xyEdgeHA = false,
                parallelXEdge = false, pseudoParallelXEdge = false;
            bool skipEdge = false, prevskipEdge = false;
            double edgeLen = 0;
            string side = "C";
            EyeContourLine lastvalidl1 = null, lastvalidl2 = null;

            if (true/*profile.Code == "L"*/)
            {
                minEdgeLenInsideWeb = 1;
                minEdgeLenInsidePermittedWebRadius = 1;
            }

            //
            // Booleano usato per testare la strategia di tenere gli edge vicini alle ali in direzione X e dopo manipolarli
            //
            bool TestEdgeViciniAllAlaInDirezioneX = true;


            foreach (EyeLineEdge lineEdge in list) 
            {
                X1 = lineEdge.StartPoint.X;
                Y1 = lineEdge.StartPoint.Y;
                Z1 = lineEdge.StartPoint.Z;

                X2 = lineEdge.EndPoint.X;
                Y2 = lineEdge.EndPoint.Y;
                Z2 = lineEdge.EndPoint.Z;

                edgeLen = lineEdge.StartPoint.DistanceTo(lineEdge.EndPoint);


                xzEdge = Func.Equal(Y1, Y2, 0.01);
                xyEdge = Func.Equal(Z1, Z2, 0.01);

                parallelXEdge = xyEdge && xzEdge;
                if (parallelXEdge)
                    pseudoParallelXEdge = false;
                else
                {
                    double dx = Math.Abs(X1 - X2), dy = Math.Abs(Y1 - Y2), dz = Math.Abs(Z1 - Z2);

                    if (dx > 0)
                    {
                        if (dz < 0.1 && dy < 0.1)
                            pseudoParallelXEdge = true;
                        else
                        {
                            double angXZ = dz > 0 ? Math.Atan(dx / dz) / FicepXml.Constants.FAT_RAD : 90,
                                angXY = dy > 0 ? Math.Atan(dx / dy) / FicepXml.Constants.FAT_RAD : 90;
                            pseudoParallelXEdge = angXZ > 89.9 && angXY > 89.9;
                        }
                    }
                }

                side = "C";

                if (profile.Code == "R" || profile.Code == "Q" && !inner)
                {
                    if (profile.Code == "R")
                    {
                        bool skipEdgeOnLandingOrInternalChamfersOnPipes =
                            FeatureExtractionParam != null ? FeatureExtractionParam.NoLandingOnPipes : false;
                        double tollradius = 0.1;
                        double radius = profile.HC / 2;
                        double squared1 = (Y1 - radius) * (Y1 - radius) + (Z1 - radius) * (Z1 - radius);
                        double squared2 = (Y2 - radius) * (Y2 - radius) + (Z2 - radius) * (Z2 - radius);
                        double squaredradius = (radius - tollradius) * (radius - tollradius);

                        //  skippo gli edges che hanno almeno uno dei 2 punti con distanza dall'asse del tubo minore del raggio
                        skipEdge = skipEdgeOnLandingOrInternalChamfersOnPipes && (squared1 < squaredradius || squared2 < squaredradius);
                    }
                    else
                    {
                        bool skipEdgeOnLandingOrInternalChamfersOnPipes =
                            FeatureExtractionParam != null ? FeatureExtractionParam.NoLandingOnPipes : false;
                        double toll = 0.1;
                        bool isinsideprofile1 = Y1 > profile.TA - toll && Y1 < profile.HC - profile.TA + toll && 
                            Z1 > profile.TC - toll && Z1 < profile.HA - profile.TC + toll;
                        bool isinsideprofile2 = Y2 > profile.TA - toll && Y2 < profile.HC - profile.TA + toll &&
                            Z2 > profile.TC - toll && Z2 < profile.HA - profile.TC + toll;

                        //  skippo gli edges che hanno almeno uno dei 2 punti all'interno del profilo
                        skipEdge = skipEdgeOnLandingOrInternalChamfersOnPipes && (isinsideprofile1 || isinsideprofile2);
                    }

                    if (!skipEdge)
                    {
                        if (noSorting)
                        {
                            //
                            //  V1Start e V1End sono i vettori che più probabilmente rappresentano i vettori di taglio
                            //  V2Start e V2End sono gli atri 2 (da utilizzare in futuro solo nei casi in cui V1Start e V1End non fossero quelli utili)
                            //
                            EyeContourLine l1 = new EyeContourLine(lineEdge.StartPoint, lineEdge.StartPoint + lineEdge.V1Start, side);
                            EyeContourLine l2 = new EyeContourLine(lineEdge.EndPoint, lineEdge.EndPoint + lineEdge.V1End, side);
                            List.Add(l1);
                            List.Add(l2);
                        }
                        else
                        {
                            //
                            //  V1Start e V1End sono i vettori che più probabilmente rappresentano i vettori di taglio
                            //  V2Start e V2End sono gli atri 2 (da utilizzare in futuro solo nei casi in cui V1Start e V1End non fossero quelli utili)
                            //
                            EyeContourLine l1 = new EyeContourLine(lineEdge.StartPoint, lineEdge.StartPoint + lineEdge.V1Start, side);
                            EyeContourLine l2 = new EyeContourLine(lineEdge.EndPoint, lineEdge.EndPoint + lineEdge.V1End, side);

                            EyeContourLine first = List.Count > 0 ? List.First() : null;

                            //
                            //  Spezzo il contorno quando l'edge corrente è staccato dal precedente
                            //  Questa regola funzione a patto che tutti gli edges siano orientati
                            //  clockwise o tutti anticlockwise (metà e metà non funziona)
                            //
                            if (first != null && lastvalidl2 != null &&
                                lastvalidl2.StartPoint.DistanceTo(l1.StartPoint) > 1)
                                l1.disconnected = true;

                            List.Add(l1);
                            List.Add(l2);

                            lastvalidl1 = l1;
                            lastvalidl2 = l2;
                        }
                    }

                    prevskipEdge = skipEdge;
                }
                else
                {
                    // 
                    //  Per tenere gli edge vicini alle ali in direzione X è necessario che questo sia 0 altrimenti vengono skippati
                    //
                    double minDistanceFromInnerFlange = TestEdgeViciniAllAlaInDirezioneX ? 0 : /*FeatureExtractionParam.MinDistanceFromInnerFlange*/5;

                    bool p1OnExternalFlangeA = false, p1OnInternalFlangeA = false,
                        p2OnExternalFlangeA = false, p2OnInternalFlangeA = false,
                        p1OnExternalFlangeB = false, p1OnInternalFlangeB = false,
                        p2OnExternalFlangeB = false, p2OnInternalFlangeB = false,
                        p1InsideFlangeA = false, p2InsideFlangeA = false, p1InsideFlangeB = false, p2InsideFlangeB = false;
                    bool p1OnTopWeb = false, p1OnBottomWeb = false, p1InsideTopWebRadiusYMin = false, p1InsideTopWebRadiusYMax = false,
                        p1InsidePermittedWebRadius = false, p1OverWeb = false, p1ForbiddenArea = false, p1InsideWeb = false, p1InsideExternalRadius = false,
                        p1OnExternalSideD = false, p1OnInternalSideD = false;
                    bool p2OnTopWeb = false, p2OnBottomWeb = false, p2InsideTopWebRadiusYMin = false, p2InsideTopWebRadiusYMax = false,
                        p2InsidePermittedWebRadius = false, p2OverWeb = false, p2ForbiddenArea = false, p2InsideWeb = false, p2InsideExternalRadius = false,
                        p2OnExternalSideD = false, p2OnInternalSideD = false;
                    bool edgeOnExternalFlangeA = false, edgeOnInternalFlangeA = false, edgeInsideFlangeA = false,
                        edgeOnExternalFlangeB = false, edgeOnInternalFlangeB = false, edgeInsideFlangeB = false,
                        edgeOnTopWeb = false, edgeOnBottomWeb = false, edgeInsideWeb = false,
                        edgeOnExternalSurface = false, edgeOnExternalRadius = false, edgeInsideWebRadiusParallelX = false,
                        edgeInsideProfileThickness = false, edgeInsideForbiddenArea = false, edgeOnTopBottomFlange = false,
                        edgeInsideWebRadiusToBeSkipped = false, edgeConnectingOppositeSurfaces = false, edgeConnectingSurfaceAndInside = false,
                        edgeOnExternalSideD = false, edgeOnInternalSideD = false;
                    bool edgeForbiddenPointOnTopBottomWeb = false;
                    bool edgeVectorsPerpendicularToPlane = false;

                    if (profile.Code == "I" || profile.Code == "U" || profile.Code == "Q")
                    {
                        //	P1
                        p1OnExternalFlangeA = Y1.IsEqualTo(0, 1);
                        p1OnInternalFlangeA = Y1.IsEqualTo(TA, 1);
                        p1OnExternalFlangeB = Y1.IsEqualTo(HC, 1);
                        p1OnInternalFlangeB = Y1.IsEqualTo(HC - TA, 1);
                        p1InsideFlangeA = Y1.IsGreaterThan(0, 1) && Y1.IsLessThan(TA, 1);
                        p1InsideFlangeB = Y1.IsGreaterThan(HC - TA, 1) && Y1.IsLessThan(HC, 1);

                        //	P2
                        p2OnExternalFlangeA = Y2.IsEqualTo(0, 1);
                        p2OnInternalFlangeA = Y2.IsEqualTo(TA, 1);
                        p2OnExternalFlangeB = Y2.IsEqualTo(HC, 1);
                        p2OnInternalFlangeB = Y2.IsEqualTo(HC - TA, 1);
                        p2InsideFlangeA = Y2.IsGreaterThan(0, 1) && Y2.IsLessThan(TA, 1);
                        p2InsideFlangeB = Y2.IsGreaterThan(HC - TA, 1) && Y2.IsLessThan(HC, 1);

                        //	EDGE
                        edgeOnExternalFlangeA = p1OnExternalFlangeA && p2OnExternalFlangeA;
                        edgeOnInternalFlangeA = p1OnInternalFlangeA && p2OnInternalFlangeA;
                        edgeInsideFlangeA = p1InsideFlangeA && p2InsideFlangeA;
                        edgeOnExternalFlangeB = p1OnExternalFlangeB && p2OnExternalFlangeB;
                        edgeOnInternalFlangeB = p1OnInternalFlangeB && p2OnInternalFlangeB;
                        edgeInsideFlangeB = p1InsideFlangeB && p2InsideFlangeB;
                    }
                    else if (profile.Code == "L")
                    {
                        //	P1
                        p1OnExternalFlangeA = Func.Equal(Y1, 0, 1);
                        p1OnInternalFlangeA = Func.Equal(Y1, TA, 1);
                        p1InsideFlangeA = (Y1 > 1 && Y1 < TA - 1);

                        //	P2
                        p2OnExternalFlangeA = Func.Equal(Y2, 0, 1);
                        p2OnInternalFlangeA = Func.Equal(Y2, TA, 1);
                        p2InsideFlangeA = (Y2 > 1 && Y2 < TA - 1);

                        //	EDGE
                        edgeOnExternalFlangeA = p1OnExternalFlangeA && p2OnExternalFlangeA;
                        edgeOnInternalFlangeA = p1OnInternalFlangeA && p2OnInternalFlangeA;
                        edgeInsideFlangeA = p1InsideFlangeA && p2InsideFlangeA;
                    }

                    if (profile.Code == "I")
                    {
                        /*//	P1
                        p1OnTopWeb = Z1.IsEqualTo(HA / 2 + TC / 2, 0.1);
                        p1OnBottomWeb = Z1.IsEqualTo(HA / 2 - TC / 2, 0.1);
                        p1InsideTopWebRadiusYMin = (Z1.IsEqualTo(HA / 2 + TC / 2, 0.1) || Z1.IsGreaterThan(HA / 2 + TC / 2, 0.1)) && (Y1.IsEqualTo(TA, 0.1) || Y1.IsGreaterThan(TA, 0.1)) && (Y1.IsEqualTo(TA, 0.1) || Y1.IsLessThan(TA + webRadius, 0.1));
                        p1InsideTopWebRadiusYMax = (Z1.IsEqualTo(HA / 2 + TC / 2, 0.1) || Z1.IsGreaterThan(HA / 2 + TC / 2, 0.1)) && (Y1.IsEqualTo(HC - TA - webRadius, 0.1) || Y1.IsGreaterThan(HC - TA - webRadius, 0.1)) && (Y1.IsEqualTo(HC - TA, 0.1) || Y1.IsLessThan(HC - TA, 0.1));
                        p1InsidePermittedWebRadius = (Z1.IsEqualTo(HA / 2 + TC / 2, 0.1) || Z1.IsGreaterThan(HA / 2 + TC / 2, 0.1)) &&
                            ((Y1 >= TA + minDistanceFromInnerFlange) && (Y1 <= TA + webRadius + 2) || (Y1 >= HC - TA - webRadius - 2) && (Y1 <= HC - TA - minDistanceFromInnerFlange));
                        p1OverWeb = Z1 > HA / 2 + TC / 2 + 1;
                        //p1ForbiddenArea = Z1 <= HA / 2 - TC / 2 && Y1 >= TA && Y1 <= HC - TA;
                        //  Modifica per evitare che il cianfrino inferiore venga saltato
                        p1ForbiddenArea = Z1 < HA / 2 - TC / 2 && Y1 >= TA + webRadius && Y1 <= HC - TA - webRadius || Z1 <= HA / 2 - TC / 2 && (Y1 > TA && Y1 <= TA + webRadius || Y1 >= HC - TA - webRadius && Y1 < HC - TA); 
                        p1InsideWeb = Z1 > HA / 2 - TC / 2 + 1 && Z1 < HA / 2 + TC / 2 - 1;
                        p1InsideExternalRadius = false;

                        //	P2
                        p2OnTopWeb = Func.Equal(Z2, HA / 2 + TC / 2, 0.1);
                        p2OnBottomWeb = Func.Equal(Z2, HA / 2 - TC / 2, 0.1);
                        p2InsideTopWebRadiusYMin = (Z2 >= HA / 2 + TC / 2) && (Y2 >= TA) && (Y2 <= TA + webRadius);
                        p2InsideTopWebRadiusYMax = (Z2 >= HA / 2 + TC / 2) && (Y2 >= HC - TA - webRadius) && (Y2 <= HC - TA);
                        p2InsidePermittedWebRadius = (Z2 >= HA / 2 + TC / 2) &&
                            ((Y2 >= TA + minDistanceFromInnerFlange) && (Y2 <= TA + webRadius + 2) || (Y2 >= HC - TA - webRadius - 2) && (Y2 <= HC - TA - minDistanceFromInnerFlange));
                        p2OverWeb = Z2 > HA / 2 + TC / 2 + 1;
                        //p2ForbiddenArea = Z2 <= HA / 2 - TC / 2 && Y2 >= TA && Y2 <= HC - TA;
                        //  Modifica per evitare che il cianfrino inferiore venga saltato
                        p2ForbiddenArea = Z2 < HA / 2 - TC / 2 && Y2 >= TA + webRadius && Y2 <= HC - TA - webRadius || Z2 <= HA / 2 - TC / 2 && (Y2 > TA && Y2 <= TA + webRadius || Y2 >= HC - TA - webRadius && Y2 < HC - TA);  
                        p2InsideWeb = Z2 > HA / 2 - TC / 2 + 1 && Z2 < HA / 2 + TC / 2 - 1;
                        p2InsideExternalRadius = false;

                        //	EDGE
                        edgeOnTopWeb = p1OnTopWeb && p2OnTopWeb;
                        edgeOnBottomWeb = p1OnBottomWeb && p2OnBottomWeb;
                        edgeInsideWeb = p1InsideWeb && p2InsideWeb;
                        xyEdge0 = xyEdge && Func.Equal(Z1, 0, 0.1);
                        xyEdgeHA = xyEdge && Func.Equal(Z1, HA, 0.1);

                        edgeForbiddenPointOnTopBottomWeb = (p1OnTopWeb || p1OnBottomWeb) && (Y1 <= TA + minDistanceFromInnerFlange || Y1 >= HC - TA - minDistanceFromInnerFlange) ||
                             (p2OnTopWeb || p2OnBottomWeb) && (Y2 <= TA + minDistanceFromInnerFlange || Y2 >= HC - TA - minDistanceFromInnerFlange);
                        */
                        // P1
                        p1OnTopWeb = Z1.IsEqualTo(HA / 2 + TC / 2, 0.1);
                        p1OnBottomWeb = Z1.IsEqualTo(HA / 2 - TC / 2, 0.1);

                        p1InsideTopWebRadiusYMin =
                            (Z1.IsEqualTo(HA / 2 + TC / 2, 0.1) || Z1.IsGreaterThan(HA / 2 + TC / 2, 0.1)) &&
                            (Y1.IsEqualTo(TA, 0.1) || Y1.IsGreaterThan(TA, 0.1)) &&
                            (Y1.IsEqualTo(TA + webRadius, 0.1) || Y1.IsLessThan(TA + webRadius, 0.1));

                        p1InsideTopWebRadiusYMax =
                            (Z1.IsEqualTo(HA / 2 + TC / 2, 0.1) || Z1.IsGreaterThan(HA / 2 + TC / 2, 0.1)) &&
                            (Y1.IsEqualTo(HC - TA - webRadius, 0.1) || Y1.IsGreaterThan(HC - TA - webRadius, 0.1)) &&
                            (Y1.IsEqualTo(HC - TA, 0.1) || Y1.IsLessThan(HC - TA, 0.1));

                        p1InsidePermittedWebRadius =
                            (Z1.IsEqualTo(HA / 2 + TC / 2, 0.1) || Z1.IsGreaterThan(HA / 2 + TC / 2, 0.1)) &&
                            ((Y1.IsGreaterThan(TA + minDistanceFromInnerFlange, 0.1) || Y1.IsEqualTo(TA + minDistanceFromInnerFlange, 0.1)) &&
                             (Y1.IsLessThan(TA + webRadius + 2, 0.1) || Y1.IsEqualTo(TA + webRadius + 2, 0.1)) ||
                             (Y1.IsGreaterThan(HC - TA - webRadius - 2, 0.1) || Y1.IsEqualTo(HC - TA - webRadius - 2, 0.1)) &&
                             (Y1.IsLessThan(HC - TA - minDistanceFromInnerFlange, 0.1) || Y1.IsEqualTo(HC - TA - minDistanceFromInnerFlange, 0.1)));

                        p1OverWeb = Z1.IsGreaterThan(HA / 2 + TC / 2 + 1, 0.1);

                        // Modifica per evitare che il cianfrino inferiore venga saltato
                        p1ForbiddenArea =
                            (Z1.IsLessThan(HA / 2 - TC / 2, 0.1)) &&
                            (Y1.IsGreaterThan(TA + webRadius, 0.1) || Y1.IsEqualTo(TA + webRadius, 0.1)) &&
                            (Y1.IsLessThan(HC - TA - webRadius, 0.1) || Y1.IsEqualTo(HC - TA - webRadius, 0.1)) ||

                            (Z1.IsEqualTo(HA / 2 - TC / 2, 0.1) || Z1.IsLessThan(HA / 2 - TC / 2, 0.1)) &&
                            ((Y1.IsGreaterThan(TA, 0.1)) && (Y1.IsLessThan(TA + webRadius, 0.1) || Y1.IsEqualTo(TA + webRadius, 0.1)) ||
                             (Y1.IsGreaterThan(HC - TA - webRadius, 0.1) || Y1.IsEqualTo(HC - TA - webRadius, 0.1)) &&
                             (Y1.IsLessThan(HC - TA, 0.1)));

                        p1InsideWeb =
                            Z1.IsGreaterThan(HA / 2 - TC / 2 + 1, 0.1) &&
                            Z1.IsLessThan(HA / 2 + TC / 2 - 1, 0.1);

                        p1InsideExternalRadius = false;


                        // P2
                        p2OnTopWeb = Z2.IsEqualTo(HA / 2 + TC / 2, 0.1);
                        p2OnBottomWeb = Z2.IsEqualTo(HA / 2 - TC / 2, 0.1);

                        p2InsideTopWebRadiusYMin =
                            (Z2.IsEqualTo(HA / 2 + TC / 2, 0.1) || Z2.IsGreaterThan(HA / 2 + TC / 2, 0.1)) &&
                            (Y2.IsEqualTo(TA, 0.1) || Y2.IsGreaterThan(TA, 0.1)) &&
                            (Y2.IsEqualTo(TA, 0.1) || Y2.IsLessThan(TA + webRadius, 0.1));

                        p2InsideTopWebRadiusYMax =
                            (Z2.IsEqualTo(HA / 2 + TC / 2, 0.1) || Z2.IsGreaterThan(HA / 2 + TC / 2, 0.1)) &&
                            (Y2.IsEqualTo(HC - TA - webRadius, 0.1) || Y2.IsGreaterThan(HC - TA - webRadius, 0.1)) &&
                            (Y2.IsEqualTo(HC - TA, 0.1) || Y2.IsLessThan(HC - TA, 0.1));

                        p2InsidePermittedWebRadius =
                            (Z2.IsEqualTo(HA / 2 + TC / 2, 0.1) || Z2.IsGreaterThan(HA / 2 + TC / 2, 0.1)) &&
                            ((Y2.IsGreaterThan(TA + minDistanceFromInnerFlange, 0.1) || Y2.IsEqualTo(TA + minDistanceFromInnerFlange, 0.1)) &&
                             (Y2.IsLessThan(TA + webRadius + 2, 0.1) || Y2.IsEqualTo(TA + webRadius + 2, 0.1)) ||
                             (Y2.IsGreaterThan(HC - TA - webRadius - 2, 0.1) || Y2.IsEqualTo(HC - TA - webRadius - 2, 0.1)) &&
                             (Y2.IsLessThan(HC - TA - minDistanceFromInnerFlange, 0.1) || Y2.IsEqualTo(HC - TA - minDistanceFromInnerFlange, 0.1)));

                        p2OverWeb = Z2.IsGreaterThan(HA / 2 + TC / 2 + 1, 0.1);

                        // Modifica per evitare che il cianfrino inferiore venga saltato
                        p2ForbiddenArea =
                            (Z2.IsLessThan(HA / 2 - TC / 2, 0.1)) &&
                            (Y2.IsGreaterThan(TA + webRadius, 0.1) || Y2.IsEqualTo(TA + webRadius, 0.1)) &&
                            (Y2.IsLessThan(HC - TA - webRadius, 0.1) || Y2.IsEqualTo(HC - TA - webRadius, 0.1)) ||

                            (Z2.IsEqualTo(HA / 2 - TC / 2, 0.1) || Z2.IsLessThan(HA / 2 - TC / 2, 0.1)) &&
                            ((Y2.IsGreaterThan(TA, 0.1)) && (Y2.IsLessThan(TA + webRadius, 0.1) || Y2.IsEqualTo(TA + webRadius, 0.1)) ||
                             (Y2.IsGreaterThan(HC - TA - webRadius, 0.1) || Y2.IsEqualTo(HC - TA - webRadius, 0.1)) &&
                             (Y2.IsLessThan(HC - TA, 0.1)));

                        p2InsideWeb =
                            Z2.IsGreaterThan(HA / 2 - TC / 2 + 1, 0.1) &&
                            Z2.IsLessThan(HA / 2 + TC / 2 - 1, 0.1);

                        p2InsideExternalRadius = false;


                        // EDGE
                        edgeOnTopWeb = p1OnTopWeb && p2OnTopWeb;
                        edgeOnBottomWeb = p1OnBottomWeb && p2OnBottomWeb;
                        edgeInsideWeb = p1InsideWeb && p2InsideWeb;

                        xyEdge0 = xyEdge && Z1.IsEqualTo(0, 0.1);
                        xyEdgeHA = xyEdge && Z1.IsEqualTo(HA, 0.1);

                        edgeForbiddenPointOnTopBottomWeb =
                            ((p1OnTopWeb || p1OnBottomWeb) &&
                            (Y1.IsLessThan(TA + minDistanceFromInnerFlange, 0.1) || Y1.IsEqualTo(TA + minDistanceFromInnerFlange, 0.1) ||
                             Y1.IsGreaterThan(HC - TA - minDistanceFromInnerFlange, 0.1) || Y1.IsEqualTo(HC - TA - minDistanceFromInnerFlange, 0.1)) 
                             ||
                            (p2OnTopWeb || p2OnBottomWeb) &&
                            (Y2.IsLessThan(TA + minDistanceFromInnerFlange, 0.1) || Y2.IsEqualTo(TA + minDistanceFromInnerFlange, 0.1) ||
                             Y2.IsGreaterThan(HC - TA - minDistanceFromInnerFlange, 0.1) || Y2.IsEqualTo(HC - TA - minDistanceFromInnerFlange, 0.1)))
                             && // Messo questo and per evitare che vengano tolti gli edges appartenenti a cianfrini
                             (!Y1.IsEqualTo(TA, 0.1) && !Y2.IsEqualTo(TA, 0.1) && !Y1.IsEqualTo(HC - TA, 0.1) && !Y2.IsEqualTo(HC - TA, 0.1) &&
                             !Y1.IsEqualTo(0, 0.1) && !Y2.IsEqualTo(0, 0.1)  && !Y1.IsEqualTo(HC, 0.1) && !Y2.IsEqualTo(HC, 0.1));
                    }
                    else if (profile.Code == "U")
                    {
                        //	P1
                        p1OnTopWeb = Func.Equal(Z1, TC, 0.1);
                        p1OnBottomWeb = Func.Equal(Z1, 0, 0.1);
                        p1InsideTopWebRadiusYMin = (Z1 >= TC) && (Y1 >= TA) && (Y1 <= TA + webRadius);
                        p1InsideTopWebRadiusYMax = (Z1 >= TC) && (Y1 >= HC - TA - webRadius) && (Y1 <= HC - TA);
                        p1InsidePermittedWebRadius = (Z1 >= TC) &&
                            ((Y1 >= TA + minDistanceFromInnerFlange) && (Y1 <= TA + webRadius + 2) || (Y1 >= HC - TA - webRadius - 2) && (Y1 <= HC - TA - minDistanceFromInnerFlange));
                        p1OverWeb = Z1 > TC + 1;
                        p1ForbiddenArea = Z1 > TC && Y1 >= TA && Y1 <= HC - TA;
                        p1InsideWeb = Z1 > 1 && Z1 < TC - 1;
                        p1InsideExternalRadius = false;

                        //	P2
                        p2OnTopWeb = Func.Equal(Z2, TC, 0.1);
                        p2OnBottomWeb = Func.Equal(Z2, 0, 0.1);
                        p2InsideTopWebRadiusYMin = (Z2 >= TC) && (Y2 >= TA) && (Y2 <= TA + webRadius);
                        p2InsideTopWebRadiusYMax = (Z2 >= TC) && (Y2 >= HC - TA - webRadius) && (Y2 <= HC - TA);
                        p2InsidePermittedWebRadius = (Z2 >= TC) &&
                            ((Y2 >= TA + minDistanceFromInnerFlange) && (Y2 <= TA + webRadius + 2) || (Y2 >= HC - TA - webRadius - 2) && (Y2 <= HC - TA - minDistanceFromInnerFlange));
                        p2OverWeb = Z2 > TC + 1;
                        p2ForbiddenArea = Z2 > TC && Y2 >= TA && Y2 <= HC - TA;
                        p2InsideWeb = Z2 > 1 && Z2 < TC - 1;
                        p2InsideExternalRadius = false;

                        //	EDGE
                        edgeOnTopWeb = p1OnTopWeb && p2OnTopWeb;
                        edgeOnBottomWeb = p1OnBottomWeb && p2OnBottomWeb;
                        edgeInsideWeb = p1InsideWeb && p2InsideWeb;
                        //xyEdge0 = xyEdge && Func.Equal(Z1, 0, 0.1);
                        xyEdgeHA = xyEdge && Func.Equal(Z1, HA, 0.1);

                        edgeForbiddenPointOnTopBottomWeb = (p1OnTopWeb) && (Y1 <= TA + minDistanceFromInnerFlange || Y1 >= HC - TA - minDistanceFromInnerFlange) ||
                             (p2OnTopWeb) && (Y2 <= TA + minDistanceFromInnerFlange || Y2 >= HC - TA - minDistanceFromInnerFlange);
                    }
                    else if (profile.Code == "L")
                    {
                        //	P1
                        p1OnTopWeb = Func.Equal(Z1, TB, 0.1);
                        p1OnBottomWeb = Func.Equal(Z1, 0, 0.1);
                        p1InsideTopWebRadiusYMin = (Z1 >= TB) && (Y1 >= TA) && (Y1 <= TA + webRadius);
                        p1InsideTopWebRadiusYMax = /*(Z1 >= TB) && (Y1 >= HB - TA - webRadius) && (Y1 <= HB - TA)*/false;
                        p1InsidePermittedWebRadius = (Z1 >= TB) &&
                            ((Y1 >= TA + minDistanceFromInnerFlange) && (Y1 <= TA + webRadius + 2) || (Y1 >= HB - TA - webRadius - 2) && (Y1 <= HB - TA - minDistanceFromInnerFlange));
                        p1OverWeb = Z1 > TB + 1;
                        //p1ForbiddenArea = Z1 <= HA / 2 - TC / 2 && Y1 >= TA && Y1 <= HC - TA;
                        p1InsideWeb = Z1 > 1 && Z1 < TB - 1;
                        p1InsideExternalRadius = false;

                        //	P2
                        p2OnTopWeb = Func.Equal(Z2, TB, 0.1);
                        p2OnBottomWeb = Func.Equal(Z2, 0, 0.1);
                        p2InsideTopWebRadiusYMin = (Z2 >= TB) && (Y2 >= TA) && (Y2 <= TA + webRadius);
                        p2InsideTopWebRadiusYMax = (Z2 >= TB) && (Y2 >= HB - TA - webRadius) && (Y2 <= HB - TA);
                        p2InsidePermittedWebRadius = (Z2 >= TB) &&
                            ((Y2 >= TA + minDistanceFromInnerFlange) && (Y2 <= TA + webRadius + 2) || (Y2 >= HB - TA - webRadius - 2) && (Y2 <= HB - TA - minDistanceFromInnerFlange));
                        p2OverWeb = Z2 > TB + 1;
                        //p2ForbiddenArea = Z2 <= HA / 2 - TC / 2 && Y2 >= TA && Y2 <= HC - TA;
                        p2InsideWeb = Z2 > 1 && Z2 < TB - 1;
                        p2InsideExternalRadius = false;

                        //	EDGE
                        edgeOnTopWeb = p1OnTopWeb && p2OnTopWeb;
                        edgeOnBottomWeb = p1OnBottomWeb && p2OnBottomWeb;
                        edgeInsideWeb = p1InsideWeb && p2InsideWeb;
                        xyEdge0 = xyEdge && Func.Equal(Z1, 0, 0.1);
                        xyEdgeHA = xyEdge && Func.Equal(Z1, HA, 0.1);

                        edgeForbiddenPointOnTopBottomWeb = (p1OnTopWeb) && (Y1 <= TA + minDistanceFromInnerFlange) ||
                             (p2OnTopWeb) && (Y2 <= TA + minDistanceFromInnerFlange);
                    }
                    else if (profile.Code == "F")
                    {
                        //	P1
                        p1OnTopWeb = Func.Equal(Z1, TC, 0.1);
                        p1OnBottomWeb = Func.Equal(Z1, 0, 0.1);
                        p1OverWeb = Z1 > TC + 1;
                        p1InsideWeb = Z1 > 1 && Z1 < TC - 1;
                        p1InsideExternalRadius = false;

                        //	P2
                        p2OnTopWeb = Func.Equal(Z2, TC, 0.1);
                        p2OnBottomWeb = Func.Equal(Z2, 0, 0.1);
                        p2OverWeb = Z2 > TC + 1;
                        p2InsideWeb = Z2 > 1 && Z2 < TC - 1;
                        p2InsideExternalRadius = false;

                        //	EDGE
                        edgeOnTopWeb = p1OnTopWeb && p2OnTopWeb;
                        edgeOnBottomWeb = p1OnBottomWeb && p2OnBottomWeb;
                        edgeInsideWeb = p1InsideWeb && p2InsideWeb;
                    }
                    else if (profile.Code == "Q")
                    {
                        //	P1
                        p1OnTopWeb = Func.Equal(Z1, HA, 0.1);
                        p1OnBottomWeb = Func.Equal(Z1, HA - TC, 0.1);
                        p1OnExternalSideD = Func.Equal(Z1, 0, 0.1);
                        p1OnInternalSideD = Func.Equal(Z1, TC, 0.1);
                        //p1InsideTopWebRadiusYMin = (Z1 >= HA / 2 + TC / 2) && (Y1 >= TA) && (Y1 <= TA + webRadius);
                        //p1InsideTopWebRadiusYMax = (Z1 >= HA / 2 + TC / 2) && (Y1 >= HC - TA - webRadius) && (Y1 <= HC - TA);
                        //p1InsidePermittedWebRadius = (Z1 >= HA / 2 + TC / 2) &&
                        //    ((Y1 >= TA + minDistanceFromInnerFlange) && (Y1 <= TA + webRadius + 2) || (Y1 >= HC - TA - webRadius - 2) && (Y1 <= HC - TA - minDistanceFromInnerFlange));
                        p1OverWeb = Z1 > HA + 1;
                        p1ForbiddenArea = Z1 <= HA - TC && Z1 >= TC && Y1 >= TA && Y1 <= HC - TA;
                        p1InsideWeb = Z1 > HA - TC + 1 && Z1 < HA - 1;
                        p1InsideExternalRadius = false;

                        //	P2
                        p2OnTopWeb = Func.Equal(Z2, HA, 0.1);
                        p2OnBottomWeb = Func.Equal(Z2, HA - TC, 0.1);
                        p2OnExternalSideD = Func.Equal(Z2, 0, 0.1);
                        p2OnInternalSideD = Func.Equal(Z2, TC, 0.1);
                        //p2InsideTopWebRadiusYMin = (Z2 >= HA / 2 + TC / 2) && (Y2 >= TA) && (Y2 <= TA + webRadius);
                        //p2InsideTopWebRadiusYMax = (Z2 >= HA / 2 + TC / 2) && (Y2 >= HC - TA - webRadius) && (Y2 <= HC - TA);
                        //p2InsidePermittedWebRadius = (Z2 >= HA / 2 + TC / 2) &&
                        //    ((Y2 >= TA + minDistanceFromInnerFlange) && (Y2 <= TA + webRadius + 2) || (Y2 >= HC - TA - webRadius - 2) && (Y2 <= HC - TA - minDistanceFromInnerFlange));
                        p2OverWeb = Z2 > HA + 1;
                        p2ForbiddenArea = Z2 <= HA - TC && Z2 >= TC && Y2 >= TA && Y2 <= HC - TA;
                        p2InsideWeb = Z2 > HA - TC + 1 && Z2 < HA - 1;
                        p2InsideExternalRadius = false;

                        //	EDGE
                        edgeOnTopWeb = p1OnTopWeb && p2OnTopWeb;
                        edgeOnBottomWeb = p1OnBottomWeb && p2OnBottomWeb;
                        edgeInsideWeb = p1InsideWeb && p2InsideWeb;
                        //xyEdge0 = xyEdge && Func.Equal(Z1, 0, 0.1);
                        //xyEdgeHA = xyEdge && Func.Equal(Z1, HA, 0.1);

                        edgeOnExternalSideD = p1OnExternalSideD && p2OnExternalSideD;
                        edgeOnInternalSideD =  p1OnInternalSideD && p2OnInternalSideD;

                        //edgeForbiddenPointOnTopBottomWeb = (p1OnTopWeb || p1OnBottomWeb) && (Y1 <= TA + minDistanceFromInnerFlange || Y1 >= HC - TA - minDistanceFromInnerFlange) ||
                        //     (p2OnTopWeb || p2OnBottomWeb) && (Y2 <= TA + minDistanceFromInnerFlange || Y2 >= HC - TA - minDistanceFromInnerFlange);
                    }

                    if (edgeOnTopWeb || edgeInsideWeb || p1InsidePermittedWebRadius && p2InsidePermittedWebRadius)
                        side = "C";
                    else if (edgeOnExternalFlangeA || edgeOnInternalFlangeA || edgeInsideFlangeA)
                        side = "A";
                    else if (edgeOnExternalFlangeB || edgeOnInternalFlangeB || edgeInsideFlangeB)
                        side = "B";
                    else if (edgeOnBottomWeb && profile.Code != "Q" || (edgeOnExternalSideD || edgeOnInternalSideD) && profile.Code == "Q")
                        side = "D";
                    else
                        side = "C";

                    if (side == "C" || side == "D")
                        edgeVectorsPerpendicularToPlane = lineEdge.V1Start.Z.IsEqualTo(1, 0.01) && lineEdge.V1End.Z.IsEqualTo(1, 0.01) || lineEdge.V1Start.Z.IsEqualTo(-1, 0.01) && lineEdge.V1End.Z.IsEqualTo(-1, 0.01);
                    else
                        edgeVectorsPerpendicularToPlane = lineEdge.V1Start.Y.IsEqualTo(1, 0.01) && lineEdge.V1End.Y.IsEqualTo(1, 0.01) || lineEdge.V1Start.Y.IsEqualTo(-1, 0.01) && lineEdge.V1End.Y.IsEqualTo(-1, 0.01);

                    // E' un cianfrino interno se l'edge appartiene all'ala a o b interna come posizione (edgeOnInternalFlangeB || edgeOnInternalFlangeA)
                    // e se il lineEdge appartiene a una faccia interna (lineEdge.ChamferType == ChamferType.Internal)
                    bool isInternalChamfer = (edgeOnInternalFlangeB || edgeOnInternalFlangeA) && lineEdge.ChamferType == ChamferType.Internal && !edgeVectorsPerpendicularToPlane && lineEdge.V1Start.Z == 0 && lineEdge.V1End.Z == 0 ||
                                             edgeOnBottomWeb && !edgeVectorsPerpendicularToPlane;

                    //
                    //	Categorie di Edge da utilizzare per tutti i profili
                    //
                    //	Edge appartenente alla superficie esterna del profilo
                    //
                    if (profile.Code == "U")
                        edgeOnExternalSurface = edgeOnExternalFlangeA || edgeOnExternalFlangeB || edgeOnBottomWeb || edgeOnExternalRadius;
                    else if (profile.Code == "L")
                        edgeOnExternalSurface = edgeOnExternalFlangeA || edgeOnExternalFlangeB || edgeOnTopWeb || edgeOnExternalRadius;
                    else if (profile.Code == "Q")
                        edgeOnExternalSurface = edgeOnExternalFlangeA || edgeOnExternalFlangeB || edgeOnTopWeb || edgeOnExternalSideD || edgeOnExternalRadius;
                    else
                        edgeOnExternalSurface = edgeOnExternalFlangeA || edgeOnExternalFlangeB || edgeOnTopWeb || edgeOnExternalRadius;
                    //
                    //	Edge disposto nel piano XY all'interno del raggio profilo
                    //

                    // AGGIUNTO FLAG PER TESTARE LA STRATEGIA DEGLI EDGES PARALLELI AD X DENTRO I RAGGI PROFILO
                    edgeInsideWebRadiusParallelX = (p1InsideTopWebRadiusYMin && p2InsideTopWebRadiusYMin || p1InsideTopWebRadiusYMax && p2InsideTopWebRadiusYMax) && (xyEdge) 
                                                   && !TestEdgeViciniAllAlaInDirezioneX;// Da togliere questa variabile di controllo dopo i test
                    //
                    //  !!!!!   La riga commentata qui sotto è il vecchio codice precedente 01/08/2025
                    //  !!!!!   Da guardare solo nel caso in cui la nuova linea sopra abbia problemi con alcuni casi
                    //
                    //edgeInsideWebRadiusParallelX = p1InsideExternalRadius && p2InsideExternalRadius && (parallelXEdge || pseudoParallelXEdge);
                    //
                    //	Edge all'interno dello spessore profilo
                    //
                    edgeInsideProfileThickness = edgeInsideWeb || edgeInsideFlangeA || edgeInsideFlangeB;
                    //
                    //	Edge all'interno dell'area non accessibile dall'utensile
                    //
                    edgeInsideForbiddenArea = p1ForbiddenArea || p2ForbiddenArea || edgeForbiddenPointOnTopBottomWeb;
                    //
                    //	Edge sullo spigolo superiore / inferiore dell'ala
                    //
                    edgeOnTopBottomFlange = xyEdge0 || xyEdgeHA;
                    //
                    //	Edge all'ìnterno del raggio anima / ala
                    //
                    edgeInsideWebRadiusToBeSkipped = p1InsideTopWebRadiusYMin && p2InsideTopWebRadiusYMin || p1InsideTopWebRadiusYMax && p2InsideTopWebRadiusYMax;
                    //
                    //	Edge che connettono 2 punti su superfici contrapposte (ala interna-esterna, anima superiore-inferiore)
                    //
                    edgeConnectingOppositeSurfaces = p1OnTopWeb && p2OnBottomWeb || p1OnBottomWeb && p2OnTopWeb;
                    //
                    //	Edge che connettono la superficie interna / esterna di un piano e un punto interno allo spessore
                    //	del piano stesso (al di fuori dei raggi profilo)
                    //
                    edgeConnectingSurfaceAndInside = !edgeOnExternalRadius && ((p1OnTopWeb || p1OnBottomWeb) && p2InsideWeb || (p2OnTopWeb || p2OnBottomWeb) && p1InsideWeb ||
                        (p1OnExternalFlangeA && p2InsideFlangeA || p2OnExternalFlangeA && p1InsideFlangeA ||
                        p1OnExternalFlangeB && p2InsideFlangeB || p2OnExternalFlangeB && p1InsideFlangeB));

                    //
                    //  Skippo gli edges che appartengono ad edges non lavorabili
                    //
                    skipEdge = false;

                    //
                    //	Rispettare l'ordine
                    //
                    if (edgeInsideForbiddenArea || edgeInsideWebRadiusParallelX ||
                        edgeInsideWeb && edgeLen < minEdgeLenInsideWeb)
                    {
                        //	Edge all'interno delle aree non raggiungibili dall'utensile vanno scartati
                        //	Edge lungo l'asse X all'interno dei raggi profilo vanno scartati
                        skipEdge = true;
                    }
                    else if (edgeOnTopBottomFlange)
                    {
                        //	Edge sui piani XY con Z = 0 / HA vanno scartati (per ora scarto la possibilità 
                        //	di cianfrini sui bordi ali in direzione X)
                        skipEdge = true;
                    }
                    else if (edgeOnExternalSurface || edgeInsideProfileThickness && edgeVectorsPerpendicularToPlane || isInternalChamfer ||
                        p1InsidePermittedWebRadius && p2InsidePermittedWebRadius && edgeLen <= minEdgeLenInsidePermittedWebRadius)
                    {
                        //	Edge sulla superficie esterna vanno tenuti
                        //	Edge all'interno dello spessore profilo vanno tenuti
                        //	(corrispondono a landing e cianfrini interni) 
                        skipEdge = false;
                    }
                    else if (edgeOnBottomWeb || edgeOnInternalFlangeA || edgeOnInternalFlangeB)
                    {
                        //	Edge sull'anima inferiore, ala interna vanno scartati
                        skipEdge = true;
                    }
                    else if (edgeInsideWebRadiusToBeSkipped)
                    {
                        //	Edge all'interno dello stesso raggio anima vanno scartati.
                        skipEdge = true;
                    }
                    else if (edgeConnectingOppositeSurfaces || edgeConnectingSurfaceAndInside)
                    {
                        //	Edge con estremi sui lati contrapposti si un piano vanno scartati
                        //	Edge con un estremo sull'esterno piano e uno all'interno vanno scartati
                        skipEdge = true;
                    }
                    else
                    {
                        skipEdge = true;
                    }

                    if (!skipEdge)
                    {
                        //
                        //  V1Start e V1End sono i vettori che più probabilmente rappresentano i vettori di taglio
                        //  V2Start e V2End sono gli atri 2 (da utilizzare in futuro solo nei casi in cui V1Start e V1End non fossero quelli utili)
                        //
                        Vector3D VStart = lineEdge.V1Start, VEnd = lineEdge.V1End;
                        if (side == "C" && (Func.Equal (lineEdge.V1Start.Z, 0, 0.01) || Func.Equal(lineEdge.V1End.Z, 0, 0.01)) && 
                            Math.Abs (lineEdge.V2Start.Z) > 0 && Math.Abs(lineEdge.V2End.Z) > 0)
                        {
                            VStart = lineEdge.V2Start;
                            VEnd = lineEdge.V2End;
                        }

                        EyeContourLine l1 = new EyeContourLine(lineEdge.StartPoint, lineEdge.StartPoint + VStart, side, false, false, lineEdge.ChamferType);
                        EyeContourLine l2 = new EyeContourLine(lineEdge.EndPoint, lineEdge.EndPoint + VEnd, side, false, false, lineEdge.ChamferType);

                        EyeContourLine first = List.Count > 0 ? List.First() : null;

                        if (first != null && first.StartPoint.DistanceTo (l2.StartPoint) < 1)
                        {
                            List.Insert(0, l2);
                            List.Insert(0, l1);
                        }
                        else
                        {
                            if (first != null && lastvalidl2 != null &&
                                lastvalidl2.StartPoint.DistanceTo(l1.StartPoint) > 1)
                                l1.disconnected = true;

                            List.Add(l1);
                            List.Add(l2);
                        }

                        lastvalidl1 = l1;
                        lastvalidl2 = l2;
                    }

                    prevskipEdge = skipEdge;
                }
            }

            // Filtro i vettori dei cianfrini con profondità uguale a spessore piano perchè generano coppie di vettori sia sull'esterno del piano
            // che sull'interno. Se esiste questo caso , viene mantenuto solo il vettore esterno. Affinchè si verifichi questo caso bisogna che almeno 4 vettori
            // abbiano la stessa direzione e siano sullo stesso piano.
            FilterChamfers(profile);

            return true;
        }

        private bool FilterChamfers(CProfile profile)
        {
            // At the moment, this method is only applicable to I profiles.
            if (profile.Code != "I")
                return false;

            // --- Flange A ---

            // Filter the List by side "A" and obtain a list of tuples (object, index)
            var sideA = List
                .Select((item, index) => (item, index))
                .Where(tuple => tuple.item.side == "A")
                .ToList();

            if (sideA.Count >= 4)
            {
                // Filter out chamfers on side A
                var inclinedSideA = sideA.Where(l => !l.item.Direction.AngleInXY.IsEqualTo(0, 0.1.ToRad()) && !l.item.Direction.AngleInXY.IsEqualTo(Math.PI, 0.1.ToRad())).ToList();

                if (inclinedSideA.Count >= 4)
                {

                    // Find vectors in the same plane
                    // The list returned contains lists of indices zero based of vectors that are in the same plane, each list represent a group of vectors in the same plane.
                    List<List<int>> vectorsAInSamePlaneList = FindVectorsInSamePlane(inclinedSideA);

                    if (vectorsAInSamePlaneList.Count >= 1)
                    {
                        // Remove lines that have a matching opposite direction in the same plane keeping only the ones in the external side
                        if (vectorsAInSamePlaneList.Count >= 1)
                        {
                            var itemsToRemove = new List<EyeContourLine>();
                            foreach (var group in vectorsAInSamePlaneList.Where(g => g.Count >= 4))
                            {
                                // Retrieve the elements of the list 
                                var items = group.Select(idx => List[idx]).ToList();

                                // The Chamfer type describes if the vector belongs to an internal or external chamfer face
                                // i.e. we are on side A and if it belongs to an internal one having y = 0 it has to be removed 
                                // 
                                foreach (var item in items)
                                {
                                    if (item.ChamferType == ChamferType.Internal)
                                    {
                                        if (item.StartPoint.Y.IsEqualTo(0, 0.1))
                                            itemsToRemove.Add(item);
                                    }
                                    else if (item.ChamferType == ChamferType.External)
                                    {
                                        if (item.StartPoint.Y.IsGreaterThan(0, 0.1))
                                            itemsToRemove.Add(item);
                                    }
                                }
                            }
                            List.RemoveAll(x => itemsToRemove.Contains(x));
                        }
                    }
                }
            }

            // --- Flange B ---

            // Filter the List by side "B" and obtain a list of tuples (object, index)
            var sideB = List
                .Select((item, index) => (item, index))
                .Where(tuple => tuple.item.side == "B")
                .ToList();

            if (sideB.Count >= 4)
            {
                // Filter out chamfers on side B
                var inclinedSideB = sideB.Where(l => !l.item.Direction.AngleInXY.IsEqualTo(0, 0.1.ToRad()) && !l.item.Direction.AngleInXY.IsEqualTo(Math.PI, 0.1.ToRad())).ToList();

                if (inclinedSideB.Count >= 4)
                {
                    List<List<int>> vectorsBInSamePlaneList = FindVectorsInSamePlane(inclinedSideB);

                    if (vectorsBInSamePlaneList.Count >= 1)
                    {
                        // Remove lines that have a matching opposite direction in the same plane keeping only the ones in the external side
                        if (vectorsBInSamePlaneList.Count >= 1)
                        {
                            // If the group has less than 4 elements, it is not considered for removal since cannot have other vectors in the same plane
                            foreach (var group in vectorsBInSamePlaneList.Where(g => g.Count >= 4))
                            {
                                var items = group
                                        .Select(idx => List[idx])
                                        .ToList();
                                foreach (var item in items)
                                {
                                    if (item.ChamferType == ChamferType.Internal)
                                    {
                                        if (item.StartPoint.Y.IsEqualTo(profile.HC, 0.1))
                                            List.Remove(item);
                                    }
                                    else if (item.ChamferType == ChamferType.External)
                                    {
                                        if (item.StartPoint.Y.IsLessThan(profile.HC, 0.1))
                                            List.Remove(item);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // --- Web ---

            // Filter the List by side "C and D" and obtain a list of tuples (object, index)
            var web = List
                .Select((item, index) => (item, index))
                .Where(tuple => tuple.item.side == "C" || tuple.item.side == "D")
                .ToList();

            if (web.Count >= 4)
            {
                // Filter out chamfers on side D
                var inclinedWeb = web.Where(l => !l.item.Direction.AngleFromXY.IsEqualTo(Math.PI / 2, 0.1.ToRad()) && !l.item.Direction.AngleFromXY.IsEqualTo(Math.PI * 3 / 2, 0.1.ToRad())).ToList();

                if (inclinedWeb.Count >= 4)
                {
                    List<List<int>> vectorsWebInSamePlaneList = FindVectorsInSamePlane(inclinedWeb);

                    if (vectorsWebInSamePlaneList.Count >= 1)
                    {
                        // Remove lines that have a matching opposite direction in the same plane keeping only the ones in the external side
                        if (vectorsWebInSamePlaneList.Count >= 1)
                        {
                            // If the group has less than 4 elements, it is not considered for removal since cannot have other vectors in the same plane
                            foreach (var group in vectorsWebInSamePlaneList.Where(g => g.Count >= 4))
                            {
                                var items = group.Select(idx => List[idx]).ToList();
                                foreach (var item in items)
                                {
                                    if (item.StartPoint.Z.IsLessThan(profile.HA / 2 - profile.TC / 2, 0.1))
                                        List.Remove(item);
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// For each element in inclinedSideA, checks if any other element in the same list is in the same plane of the current element.
        /// Returns a list of list of int containing the list of index of the elements in the same plane if > 2, more liststs if there are more goups belonging to different planes.
        /// </summary>
        public List<List<int>> FindVectorsInSamePlane(List<(EyeContourLine, int)> inclinedSideA)
        {
            var result = new List<List<int>>();
            double tol = 0.01;

            for (int i = 0; i < inclinedSideA.Count; i++)
            {
                var current = inclinedSideA[i];
                Vector3D planeNormal = new Vector3D();
                planeNormal.PerpendicularTo(current.Item1.Direction);

                if (result.SelectMany(x => x).Contains(current.Item2))
                    continue; // Skip if current index is already processed

                var matches = new List<int>();
                for (int j = 0; j < inclinedSideA.Count; j++)
                {
                    if (i == j) continue;
                    var other = inclinedSideA[j];
                    // Check if other.Direction is equal to -current.Direction within tolerance
                    bool isOpposite =
                        /*other.Direction.X.IsEqualTo(-current.Direction.X, tol) &&
                        other.Direction.Y.IsEqualTo(-current.Direction.Y, tol) &&
                        other.Direction.Z.IsEqualTo(-current.Direction.Z, tol)*/ true;

                    // Check if start points are on the same plane orthogonal to current.Direction
                    var vec = new Vector3D(
                        other.Item1.StartPoint.X - current.Item1.StartPoint.X,
                        other.Item1.StartPoint.Y - current.Item1.StartPoint.Y,
                        other.Item1.StartPoint.Z - current.Item1.StartPoint.Z
                    );

                    double dot = Vector3D.Dot(planeNormal, vec);
                    
                    bool samePlane = Math.Abs(dot) < tol;
                    if (isOpposite && samePlane)
                        matches.Add(other.Item2);
                }

                matches.Add(current.Item2); // Add the index of the current element
                result.Add(matches);
            }
            return result;
        }

        // Sposta i vettori in modo che partano dalla superficie esterna del profilo
        public bool MoveVectors(CProfile profile)
        {
            //  Per il momento sembra gestito il solo profilo I
            if (profile.Code != "I")
                return false;

            foreach (EyeContourLine l in List)
            {
                bool isSideA = l.StartPoint.Y.IsEqualTo(profile.TA, 0.1),
                     isSideB = l.StartPoint.Y.IsEqualTo(profile.HC - profile.TA, 0.1),
                     isSideC = l.StartPoint.Z.IsEqualTo(profile.HA / 2 - profile.TC / 2, 0.1),
                     isInternalSideA = l.StartPoint.Y.IsLessThan(profile.TA, 0.1) && l.StartPoint.Y.IsGreaterThan(0, 0.1),
                     isInternalSideB = l.StartPoint.Y.IsGreaterThan(profile.HC - profile.TA, 0.1) && l.StartPoint.Y.IsLessThan(profile.HC, 0.1), 
                     isInternalSideC = l.StartPoint.Z.IsGreaterThan(profile.HA / 2 - profile.TC / 2, 0.1) && l.StartPoint.Z.IsLessThan(profile.HA / 2 + profile.TC / 2, 0.1);


                if (l.ChamferType == ChamferType.Internal)
                {
                    double dx = 0, dy = 0, dz = 0;

                    if (isSideA)
                    {
                        dx = Math.Tan(-Math.PI / 2 + l.Direction.AngleInXY) * profile.TA;
                        dy = -profile.TA;
                        l.Translate(dx, dy);
                        l.Reverse();
                        l.Translate(l.Direction * l.Length());
                    }
                    else if (isSideB)
                    {
                        dx = Math.Tan(l.Direction.AngleInXY) * profile.TA;
                        dy = profile.TA;
                        l.Translate(dx, dy);
                        l.Reverse();
                        l.Translate(l.Direction * l.Length());
                    }
                    else if (isSideC)
                    {
                        dx = Math.Tan(l.Direction.AngleFromXY) * profile.TC;
                        dy = 0;
                        dz = profile.TC;
                        l.Translate(dx, dy, dz);
                        l.Reverse();
                        l.Translate(l.Direction * l.Length());
                        l.side = "C";
                    }
                }
                else if (isInternalSideA)
                {
                    l.Translate(0, -l.StartPoint.Y);

                    if (l.Direction.Y > 0)
                    {
                        l.Reverse();
                        l.Translate(l.Direction * l.Length());
                    }
                }
                if (isInternalSideB)
                {
                    l.Translate(0, profile.HC - l.StartPoint.Y);
                    if (l.Direction.Y < 0)
                    {
                        l.Reverse();
                        l.Translate(l.Direction * l.Length());
                    }
                }
                if (isInternalSideC)
                {
                    double dz = profile.Code == "I" ? profile.HA / 2 + profile.TC / 2 - l.StartPoint.Z : profile.Code == "Q"? profile.HA - l.StartPoint.Z : profile.TC - l.StartPoint.Z;
                    l.Translate(0, 0, dz);
                    l.side = "C";

                    if (l.Direction.Z < 0)
                    {
                        l.Reverse();
                        l.Translate(l.Direction * l.Length());
                    }
                }
            }

            return true;
        }

        public bool AddLine(EyeContourLine l)
        {
            List.Add(l);

            return true;
        }

        public bool Contains (EyeContourLine l)
        {
            foreach (EyeContourLine line in List)
            {
                if (line.StartPoint.DistanceTo(l.StartPoint) < 1)
                    return true;
            }
            return false;
        }

        //
        //  Inverto la sequenza dei punti del contorno
        //  
        public bool Reverse ()
        {
            //  E' sufficiente invertire la lista di vettori
            List.Reverse();

            return true;
        }

        //
        //  Inserisce le linee del contorno c:
        //
        //  - head = true prima di quelle del contorno corrente
        //  - head = false dopo quelle del contorno corrente
        //
        public bool AddContour (CContour c, bool head = false) 
        {
            if (head)
                List.InsertRange(0, c.List);
            else
                List.AddRange(c.List);

            //foreach (EyeContourLine l in c.List)
            //    AddLine(l);

            //  Cancella tutti le linee della lista del contorno aggiunto
            c.List.RemoveAll(x => x != null);

            return true;
        }

        //
        //  Determina se il contorno descrive un percorso antiorario
        //
        public bool IsCounterClockwise(double partlegth, ref bool counterClockwise, int idx)
        {
            counterClockwise = true;

            int idxfirstPoint = idx >= 0 ? idx : 0;

            if (List.Count < idxfirstPoint + 3)
            {
                //throw new ArgumentException("La sequenza di punti deve contenere almeno 3 punti.");
                return false;
            }

            //
            //  Il vettore del punto di vista coincide con la direzione del primo vettore
            //
            Vector3 vista = new Vector3((float)List[idxfirstPoint].Direction.X, 
                (float)List[idxfirstPoint].Direction.Y, 
                (float)List[idxfirstPoint].Direction.Z);

            //
            // 1)   Ricerco i primi 3 punti del contorno differenti e
            //      calcola il vettore normale al piano formato dai tre punti
            //
            int idxFirst = idxfirstPoint, idxSecond = idxfirstPoint + 1, idxThird = idxfirstPoint + 2;
            Point3D   firstPoint = List[idxFirst].StartPoint, secondPoint = null, thirdPoint = null;

            for (int i = idxFirst + 1; i < List.Count; i++)
            {
                if (!List[i].StartPoint.Equals(firstPoint))
                {
                    idxSecond = i;
                    secondPoint = List[idxSecond].StartPoint;
                    break;
                }
            }
            for (int i = idxSecond + 1; i < List.Count; i++)
            {
                // Trova due vettori che rappresentano i lati del triangolo
                thirdPoint = List[i].StartPoint;
                Vector3 side1 = new Vector3 ((float)(secondPoint.X - firstPoint.X), (float)(secondPoint.Y - firstPoint.Y), (float)(secondPoint.Z - firstPoint.Z));
                Vector3 side2 = new Vector3((float)(thirdPoint.X - firstPoint.X), (float)(thirdPoint.Y - firstPoint.Y), (float)(thirdPoint.Z - firstPoint.Z)); ;

                // Calcola il prodotto vettoriale dei due lati per ottenere il vettore normale
                Vector3 normal = Vector3.Cross(side1, side2);

                // Calcola la lunghezza del vettore normale, che è proporzionale all'area del triangolo
                float area = normal.Length() / 2;
                if (area > 3)
                {
                    idxThird = i;
                    thirdPoint = List[idxThird].StartPoint;
                    break;
                }
            }

            if (firstPoint == null || secondPoint == null || thirdPoint == null)
                return false;

            Vector3 p1 = new Vector3 ((float)firstPoint.X, (float)firstPoint.Y, (float)firstPoint.Z);
            Vector3 p2 = new Vector3((float)secondPoint.X, (float)secondPoint.Y, (float)secondPoint.Z);
            Vector3 p3 = new Vector3((float)thirdPoint.X, (float)thirdPoint.Y, (float)thirdPoint.Z);
            Vector3 normale = Vector3.Cross(p2 - p1, p3 - p2);

            bool normalexdirection = Math.Abs(normale.Y) < 0.1 && Math.Abs(normale.Z) < 0.1;

            if (normalexdirection)
            {
                //  
                //  2.1)    Caso in cui la normale ha la direzione dell'asse X (taglio a X costante)
                //  
                counterClockwise = (normale.X > 0 != firstPoint.X < partlegth / 2);
                return true;
            }
            else
            {
                Vector3 crossVector = Vector3.Cross(normale, vista);

                //
                //  2.2)    Caso COMUNE in cui la normale non ha la direzione dell'asse X
                //          Calcola il prodotto scalare tra il vettore di vista e il vettore normale
                //
                float dot = Vector3.Dot(vista, normale);

                //
                //  3)  Determina il verso del contorno in base al prodotto scalare
                //
                if (dot > 0)
                {
                    counterClockwise = true;
                    return true;
                }
                else if (dot < 0)
                {
                    counterClockwise = false;
                    return true;
                }
                else
                {
                    //throw new InvalidOperationException("I punti sono colineari rispetto al punto di vista.");
                }
            }

            return false;
        }

        //
        //  Il contorno è considerato chiuso se:
        //  -   l'ultimo punto coincide col primo o
        //  -   l'ultimo punto coincide col secondo punto o
        //  -   il primo punto coincide col penultimo punto
        //  (considero anche il secondo punto e il penultimo punto
        //  nel caso di overlap a fine percorso) e 
        //  -   tutti i punti hanno lo stesso side
        //
        public bool IsContourClosed(double toll)
        {
            if (List.Count < 3)
                return false;

            if (!IsSingleSide())
                return false;

            Point3D p1 = List[0].StartPoint, p2 = List[1].StartPoint, 
                p3 = List[List.Count - 1].StartPoint, p4 = List[List.Count - 2].StartPoint;

            double x1 = p1.X, y1 = p1.Y, z1 = p1.Z;
            double x2 = p2.X, y2 = p2.Y, z2 = p2.Z;
            double x3 = p3.X, y3 = p3.Y, z3 = p3.Z;
            double x4 = p4.X, y4 = p4.Y, z4 = p4.Z;

            return (x1 - x3) * (x1 - x3) + (y1 - y3) * (y1 - y3) + (z1 - z3) * (z1 - z3) < toll * toll ||
                (x2 - x3) * (x2 - x3) + (y2 - y3) * (y2 - y3) + (z2 - z3) * (z2 - z3) < toll * toll ||
                (x1 - x4) * (x1 - x4) + (y1 - y4) * (y1 - y4) + (z1 - z4) * (z1 - z4) < toll * toll;
        }

        //
        //  Verifica che tutti i punti del contorno abbiano lo stesso side
        //
        public bool IsSingleSide()
        {
            string side = "";

            foreach (var l in List)
            {
                if (l == List.First())
                    side = l.side;
                else if (l.side != side)
                    return false;
            }

            return true;
        }

        public bool HasDisconnectedLine ()
        {
            foreach (EyeContourLine l in List)
            {
                if (l.disconnected)
                    return true;
            }

            return false;
        }

        //
        //  Ripulisce il Contour:
        //
        //  -   eliminando punti consecutivi coincidenti
        //  -   eliminando punti di edge non lavorabili
        //
        public bool Clean()
        {
            double tol = 0.1;
            if (List.Count < 2)
                return true;

            var cleanedList = new List<EyeContourLine>();
            EyeContourLine lastValidLine = List[0];
            cleanedList.Add(lastValidLine);

            for (int i = 1; i < List.Count; i++)
            {
                EyeContourLine l = List[i];
                bool sameStart =
                    l.StartPoint.X.IsEqualTo(lastValidLine.StartPoint.X, tol) &&
                    l.StartPoint.Y.IsEqualTo(lastValidLine.StartPoint.Y, tol) &&
                    l.StartPoint.Z.IsEqualTo(lastValidLine.StartPoint.Z, tol);
                bool sameDir =
                    l.Direction.X.IsEqualTo(lastValidLine.Direction.X, 0.01) &&
                    l.Direction.Y.IsEqualTo(lastValidLine.Direction.Y, 0.01) &&
                    l.Direction.Z.IsEqualTo(lastValidLine.Direction.Z, 0.01);
                bool directionX = l.Direction.X.IsEqualTo(1, 0.01) || l.Direction.X.IsEqualTo(-1, 0.01);
                if (!(sameStart && sameDir) && !directionX)
                {
                    cleanedList.Add(l);
                    lastValidLine = l;
                }
            }

            List = cleanedList;

            return true;
        }

        public string Name { get; set; }
        public ToolSideComp Comp { get; set; }
        public PathType Type { get; set; }
        public CLeadInOut LeadIn { get; set; }
        public CLeadInOut LeadOut { get; set; }
        public List<EyeContourLine> List;
        public bool End { get; set; }
        public CFeatureExtractionParam FeatureExtractionParam { get; set; }
    }

    //
    //	Classe di parametri per la procedura di estrazione delle Feature
    //  ad uso XmlInterface
    //
    public class CFeatureExtractionParam
    {
        public CFeatureExtractionParam() 
        {
            EnableWorkpieceVectors = false;
            StartFromZminClosedEndContourPrfRQ = true;
            SplitinTwoClosedEndContourPrfRQ = true;
            StartFromZminClosedInnerContour = false;
            NoLandingOnPipes = true;
            CounterClockwiseContour = true;
            OverlapCloseContour = 0;
            MinDistanceFromInnerFlange = 0;
            MinPointDistance = 2;
            ArcSegmentLength = 2;
            TollContourClosed = 1;
            LeadIn = new CLeadInOut();
            LeadOut = new CLeadInOut();
        }
        public bool EnableWorkpieceVectors { get; set; }
        public bool StartFromZminClosedEndContourPrfRQ { get; set; }
        public bool SplitinTwoClosedEndContourPrfRQ { get; set; }
        public bool StartFromZminClosedInnerContour { get; set; }
        public bool NoLandingOnPipes { get; set; }
        public bool CounterClockwiseContour { get; set; }
        public double OverlapCloseContour { get; set; }
        public double MinDistanceFromInnerFlange { get; set; }
        public double MinPointDistance { get; set; }
        public double ArcSegmentLength { get; set; }
        public double TollContourClosed { get; set; }
        public CLeadInOut LeadIn { get; set; }
        public CLeadInOut LeadOut { get; set; }
    }

    //
    //  Classe per la gestione del formato XML vettoriale Ficep
    //
    public class XmlInterface
    {
        public XmlInterface() 
        {
            FeatureExtractionParam = new CFeatureExtractionParam();
        }

        //
        //  Splitto in 2 un contorno se chiuso
        //
        public bool SplitClosedContourInTwo (ref CContour inContour, ref List<CContour> outSortedContourList, CWorkPiece workPiece, 
            double overlapCloseContour, bool processAsPieceCutToMeasure)
        {
            double  tollContourClosed = FeatureExtractionParam.TollContourClosed;
            if (!inContour.IsContourClosed(tollContourClosed))
                return false;

            bool addOverlap = overlapCloseContour > 0/* && (!processAsPieceCutToMeasure || !inContour.End)*/;
            double overlap = overlapCloseContour;
            bool startFromZminClosedEndContourPrfRQ = FeatureExtractionParam.StartFromZminClosedEndContourPrfRQ;
            bool splitinTwoClosedEndContourPrfRQ = FeatureExtractionParam.SplitinTwoClosedEndContourPrfRQ;
            bool startFromZminClosedInnerContour = FeatureExtractionParam.StartFromZminClosedInnerContour;

            //////
            EyeContourLine topZSplittingLine = null;
            if ((workPiece.Profile.Code == "R" || workPiece.Profile.Code == "Q") && splitinTwoClosedEndContourPrfRQ && inContour.End)
            {
                EyeContourLine maxXLine = inContour.List.OrderByDescending(p => p.StartPoint.X).First();
                EyeContourLine minXLine = inContour.List.OrderByDescending(p => p.StartPoint.X).Last();
                double minX = inContour.List.Min(p => p.StartPoint.X),
                    maxX = inContour.List.Max(p => p.StartPoint.X);
                bool iniContour = (minX + maxX) / 2 < workPiece.Dimension.Length / 2;

                double targetX = iniContour ? minX : maxX;

                //  Ricerca il punto di X minima o massima con valore di Z massimo
                topZSplittingLine = inContour.List
                .Where(p => Math.Abs(p.StartPoint.X - targetX) < 1)
                .OrderByDescending(p => p.StartPoint.Z)
                .FirstOrDefault();
            }
            //////

            //
            //  Nel caso di contorno chiuso, lo modifico generando in uscita 1 o più contorni
            //
            EyeContourLine maxZLine = inContour.List.OrderByDescending(p => p.StartPoint.Z).First();
            EyeContourLine minZLine = inContour.List.OrderByDescending(p => p.StartPoint.Z).Last();
            EyeContourLine splitLine = null;

            int rotationIndex = 0;

            // Step 1: Find the point with minimum/maximum z value
            if (inContour.End && startFromZminClosedEndContourPrfRQ && (workPiece.Profile.Code == "R" || workPiece.Profile.Code == "Q") ||
                startFromZminClosedInnerContour)
            {
                rotationIndex = inContour.List.IndexOf(minZLine);

                if (topZSplittingLine != null)
                    splitLine = topZSplittingLine;
                else
                    splitLine = maxZLine;
            }
            else
            {
                rotationIndex = inContour.List.IndexOf(maxZLine);
                splitLine = minZLine;
            }

            // Step 2: Rotate the list to make the found point the first element
            inContour.List = inContour.List.Skip(rotationIndex).Concat(inContour.List.Take(rotationIndex)).ToList();

            // Step3: Remove the original first point that was identical to the original last one
            if (rotationIndex > 0)
                inContour.List.RemoveAt(inContour.List.Count - rotationIndex);

            // Step4: Add a new point at the end of the list identical to the new first point 
            inContour.List.Add(new EyeContourLine(inContour.List[0].StartPoint, inContour.List[0].EndPoint, inContour.List[0].side, inContour.List[0].disconnected));

            if ((workPiece.Profile.Code == "R" || workPiece.Profile.Code == "Q") && splitinTwoClosedEndContourPrfRQ && inContour.End)
            {
                CContour firstContour = new CContour();
                CContour secondContour = new CContour();

                firstContour.End = inContour.End;
                secondContour.End = inContour.End;

                bool reachedSplitLine = false;

                foreach (EyeContourLine l in inContour.List)
                {
                    if (!reachedSplitLine)
                    {
                        if (l == splitLine)
                        {
                            reachedSplitLine = true;
                            firstContour.AddLine(l);
                            secondContour.AddLine(l);
                        }
                        else
                            firstContour.AddLine(l);
                    }
                    else
                        secondContour.AddLine(l);
                }

                //
                //  Inserisco alla fine delle 2 liste nuovi punti in modo da determinare una sovrapposizione
                //  pari esattamente a overlap col secondo dei 2 contorni
                //
                if (addOverlap)
                {
                    if (firstContour.List.Count > 0 && secondContour.List.Count > 0)
                    {
                        Line lastsecond = secondContour.List.Last();

                        double d1 = lastsecond.StartPoint.DistanceTo(firstContour.List[1].StartPoint),
                            d2 = d1 + lastsecond.StartPoint.DistanceTo(firstContour.List[2].StartPoint),
                            d3 = d2 + lastsecond.StartPoint.DistanceTo(firstContour.List[3].StartPoint);

                        //
                        //  Inserisco il punto 1
                        //
                        secondContour.List.Add(new EyeContourLine(firstContour.List[1].StartPoint, firstContour.List[1].EndPoint, firstContour.List[1].side, 
                            firstContour.List[1].disconnected, true));
                        if (d1 < overlap)
                        {
                            //  Inserisco punto 2
                            secondContour.List.Add(new EyeContourLine(firstContour.List[2].StartPoint, firstContour.List[2].EndPoint, firstContour.List[2].side, 
                                firstContour.List[2].disconnected, true));
                            if (d2 < overlap)
                            {
                                //  Inserisco punto 3
                                secondContour.List.Add(new EyeContourLine(firstContour.List[3].StartPoint, firstContour.List[3].EndPoint, firstContour.List[3].side, 
                                    firstContour.List[3].disconnected, true));

                                if (d3 < overlap)
                                {
                                    //
                                    //  Modifico il punto 3
                                    //
                                    double p = 1;
                                    p = 1 - overlap / (d3 - d2);
                                    secondContour.List.Last().StartPoint = firstContour.List[2].StartPoint * p + (1 - p) * firstContour.List[3].StartPoint;
                                    secondContour.List.Last().EndPoint = firstContour.List[2].EndPoint * p + (1 - p) * firstContour.List[3].EndPoint;
                                }
                            }
                            else
                            {
                                //
                                //  Modifico il punto 2 aggiunto senza aggiungerne altri
                                //
                                double p = 1;
                                p = 1 - overlap / (d2 - d1);
                                secondContour.List.Last().StartPoint = firstContour.List[1].StartPoint * p + (1 - p) * firstContour.List[2].StartPoint;
                                secondContour.List.Last().EndPoint = firstContour.List[1].EndPoint * p + (1 - p) * firstContour.List[2].EndPoint;
                            }
                        }
                        else
                        {
                            //
                            //  Modifico il punto 1 aggiunto senza aggiungerne altri
                            //
                            double p = 1;
                            p = 1 - overlap / d1;
                            secondContour.List.Last().StartPoint = firstContour.List[0].StartPoint * p + (1 - p) * firstContour.List[1].StartPoint;
                            secondContour.List.Last().EndPoint = firstContour.List[0].EndPoint * p + (1 - p) * firstContour.List[1].EndPoint;
                        }

                        Line lastfirst = firstContour.List.Last();

                        d1 = lastfirst.StartPoint.DistanceTo(secondContour.List[1].StartPoint);
                        d2 = d1 + lastfirst.StartPoint.DistanceTo(secondContour.List[2].StartPoint);
                        d3 = d2 + lastfirst.StartPoint.DistanceTo(secondContour.List[3].StartPoint);


                        //
                        //  Inserisco il punto 1
                        //
                        firstContour.List.Add(new EyeContourLine(secondContour.List[1].StartPoint, secondContour.List[1].EndPoint, secondContour.List[1].side, 
                            secondContour.List[1].disconnected, true));
                        if (d1 < overlap)
                        {
                            //  Inserisco punto 2
                            firstContour.List.Add(new EyeContourLine(secondContour.List[2].StartPoint, secondContour.List[2].EndPoint, secondContour.List[2].side, 
                                secondContour.List[2].disconnected, true));
                            if (d2 < overlap)
                            {
                                //  Inserisco punto 3
                                firstContour.List.Add(new EyeContourLine(secondContour.List[3].StartPoint, secondContour.List[3].EndPoint, secondContour.List[3].side, 
                                    secondContour.List[3].disconnected, true));

                                if (d3 < overlap)
                                {
                                    //
                                    //  Modifico il punto 3
                                    //
                                    double p = 1;
                                    p = 1 - overlap / (d3 - d2);
                                    firstContour.List.Last().StartPoint = secondContour.List[2].StartPoint * p + (1 - p) * secondContour.List[3].StartPoint;
                                    firstContour.List.Last().EndPoint = secondContour.List[2].EndPoint * p + (1 - p) * secondContour.List[3].EndPoint;
                                }
                            }
                            else
                            {
                                //
                                //  Modifico il punto 2 aggiunto senza aggiungerne altri
                                //
                                double p = 1;
                                p = 1 - overlap / (d2 - d1);
                                firstContour.List.Last().StartPoint = secondContour.List[1].StartPoint * p + (1 - p) * secondContour.List[2].StartPoint;
                                firstContour.List.Last().EndPoint = secondContour.List[1].EndPoint * p + (1 - p) * secondContour.List[2].EndPoint;
                            }
                        }
                        else
                        {
                            //
                            //  Modifico il punto 1 aggiunto senza aggiungerne altri
                            //
                            double p = 1;
                            p = 1 - overlap / d1;
                            firstContour.List.Last().StartPoint = secondContour.List[0].StartPoint * p + (1 - p) * secondContour.List[1].StartPoint;
                            firstContour.List.Last().EndPoint = secondContour.List[0].EndPoint * p + (1 - p) * secondContour.List[1].EndPoint;
                        }
                    }
                }

                if (secondContour.List.Count > 0)
                    outSortedContourList.Add(secondContour);
                if (firstContour.List.Count > 0)
                    outSortedContourList.Add(firstContour);
            }
            else
            {
                CContour newContour = new CContour();

                newContour.End = inContour.End;


                foreach (EyeContourLine l in inContour.List)
                {
                    newContour.AddLine(l);
                }

                //
                //  Inserisco alla fine della lista nuovi punti al contorno chiuso
                //  in modo da determinare una sovrapposizione pari esattamente a overlap
                //
                if (addOverlap && newContour.List.Count > 2)
                {
                    Line last = newContour.List.Last();

                    double d1 = last.StartPoint.DistanceTo(newContour.List[1].StartPoint),
                        d2 = d1 + last.StartPoint.DistanceTo(newContour.List[2].StartPoint),
                        d3 = d2 + last.StartPoint.DistanceTo(newContour.List[3].StartPoint);

                    //  Inserisco punto 1
                    newContour.List.Add(new EyeContourLine(newContour.List[1].StartPoint, newContour.List[1].EndPoint, newContour.List[1].side, newContour.List[1].disconnected));
                    if (d1 < overlap)
                    {
                        //  Inserisco punto 2
                        newContour.List.Add(new EyeContourLine(newContour.List[2].StartPoint, newContour.List[2].EndPoint, newContour.List[2].side, newContour.List[2].disconnected));
                        if (d2 < overlap)
                        {
                            //  Inserisco punto 3
                            newContour.List.Add(new EyeContourLine(newContour.List[3].StartPoint, newContour.List[3].EndPoint, newContour.List[3].side, newContour.List[3].disconnected));

                            if (d3 < overlap)
                            {
                                //
                                //  Modifico il punto 3 e lo avvicino utilizzando un peso tra 2 punti
                                //
                                double p = 1;
                                p = 1 - overlap / (d3 - d2);
                                newContour.List.Last().StartPoint = newContour.List[2].StartPoint * p + (1 - p) * newContour.List[3].StartPoint;
                                newContour.List.Last().EndPoint = newContour.List[2].EndPoint * p + (1 - p) * newContour.List[3].EndPoint;
                            }
                        }
                        else
                        {
                            //
                            //  Modifico il punto 2 e lo avvicino utilizzando un peso tra 2 punti
                            //
                            double p = 1;
                            p = 1 - overlap / (d2 - d1);
                            newContour.List.Last().StartPoint = newContour.List[1].StartPoint * p + (1 - p) * newContour.List[2].StartPoint;
                            newContour.List.Last().EndPoint = newContour.List[1].EndPoint * p + (1 - p) * newContour.List[2].EndPoint;
                        }
                    }
                    else
                    {
                        //
                        //  Modifico il punto 1 e lo avvicino utilizzando un peso tra 2 punti
                        //
                        double p = 1;
                        p = 1 - overlap / d1;
                        newContour.List.Last().StartPoint = newContour.List[0].StartPoint * p + (1 - p) * newContour.List[1].StartPoint;
                        newContour.List.Last().EndPoint = newContour.List[0].EndPoint * p + (1 - p) * newContour.List[1].EndPoint;
                    }
                }


                outSortedContourList.Add(newContour);
            }

            return true; 
        }

        //
        //  Cerco di riconnettere contorni sullo stesso piano che abbiano
        //  in comune il punto iniziale o finale
        //  Questo permette di minimizzare il numero di contorni
        //
        public bool AttachContour(List<CContour> outSortedContourList)
        {
            double minPointDistance = FeatureExtractionParam.MinPointDistance;

            for (int i = 0; i < outSortedContourList.Count; i++)
            {
                CContour c1 = outSortedContourList[i];
                if (c1.List.Count > 1)
                {
                    bool attached = true;

                    EyeContourLine l1first = c1.List.First(), l1last = c1.List.Last();

                    while (attached == true)
                    {
                        attached = false;

                        for (int j = 0; j < outSortedContourList.Count; j++)
                        {
                            //  Se c2 coincide con c1 passo al prossimo
                            if (j == i)
                                continue;

                            //  Se c2 è vuoto lo ignoro
                            CContour c2 = outSortedContourList[j];
                            if (c2.List.Count <= 1)
                                continue;

                            EyeContourLine l2first = c2.List.First(), l2last = c2.List.Last();

                            double l1firstTol2last = l1first.StartPoint.DistanceTo(l2last.StartPoint),
                                l1firstTol2first = l1first.StartPoint.DistanceTo(l2first.StartPoint),
                                l1lastTol2first = l1last.StartPoint.DistanceTo(l2first.StartPoint),
                                l1lastTol2last = l1last.StartPoint.DistanceTo(l2last.StartPoint);

                            bool firstTolast = l1firstTol2last < minPointDistance,
                                firstTofirst = l1firstTol2first < minPointDistance,
                                lastTofirst = l1lastTol2first < minPointDistance,
                                lastTolast = l1lastTol2last < minPointDistance;

                            if (l1firstTol2last <= Math.Min(l1firstTol2first, Math.Min(l1lastTol2first, l1lastTol2last)))
                                firstTofirst = lastTofirst = lastTolast = false;
                            else if (l1firstTol2first <= Math.Min(l1firstTol2last, Math.Min(l1lastTol2first, l1lastTol2last)))
                                firstTolast = lastTofirst = lastTolast = false;
                            else if (l1lastTol2first <= Math.Min(l1firstTol2last, Math.Min(l1firstTol2first, l1lastTol2last)))
                                firstTolast = firstTofirst = lastTolast = false;
                            else
                                firstTolast = firstTofirst = lastTofirst = false;

                            if (firstTolast)
                            {
                                if (l1first.side == l2last.side)
                                {
                                    //  Attacca c2 in testa a c1 e cancella c2
                                    c1.AddContour(c2, true);
                                    attached = true;
                                    //  Esco da questo ciclo for perchè c1 è vuoto
                                    break;
                                }
                            }
                            else if (lastTofirst)
                            {
                                if (l1last.side == l2first.side)
                                {
                                    //  Attacca c2 in testa a c1 e cancella c2
                                    c1.AddContour(c2, false);
                                    attached = true;
                                }
                            }
                            else if (firstTofirst)
                            {
                                if (l1first.side == l2first.side)
                                {
                                    //  Attacca c2.Reverse a c1 e cancella c2
                                    c2.Reverse();
                                    c1.AddContour(c2, true);
                                    attached = true;
                                }
                            }
                            else if (lastTolast)
                            {
                                if (l1last.side == l2last.side)
                                {
                                    //  Attacca c2.Reverse a c1 e cancella c2
                                    c2.Reverse();
                                    c1.AddContour(c2, false);
                                    attached = true;
                                }
                            }
                        }
                    }
                }
            }

            //
            //  Cancello tutti i contorni vuoti che potrebbero essere
            //  stati creati dalla riconnessione
            //
            outSortedContourList.RemoveAll(c => c.List.Count == 0);

            return true;
        }

        //
        //  Riceve in ingresso il contorno inContour
        //  Genera in uscita una lista di contorni outSortedContourList
        //  Ciascun contorno in uscita viene ordinato secondo opportuni criteri
        //
        public bool SortContour(CContour inContour, ref List<CContour> outSortedContourList, CWorkPiece workPiece, double overlapCloseContour,
            bool processAsPieceCutToMeasure)
        {
            if (inContour.List.Count <= 0)
                return true;

            bool splitinTwoClosedEndContourPrfRQ = FeatureExtractionParam.SplitinTwoClosedEndContourPrfRQ;
            double tollContourClosed = FeatureExtractionParam.TollContourClosed;
            double tollEnd = 2 * workPiece.Profile.TC;

            //  Verifico che il contorno sia chiuso
            bool isContourClosed = inContour.IsContourClosed(tollContourClosed) && !inContour.HasDisconnectedLine();

            //  Clono il contorno in ingresso prima di ordinarlo
            CContour sortedContour = new CContour();
            foreach (var l in inContour.List)
                sortedContour.AddLine(new EyeContourLine (l.StartPoint, l.EndPoint, l.side, l.disconnected));

            if (inContour.noSorting)
            {
                outSortedContourList.Add(sortedContour);
                return true;
            }

            double minX = sortedContour.List.Min(p => p.StartPoint.X),
                maxX = sortedContour.List.Max(p => p.StartPoint.X);

            sortedContour.End = Func.Equal(minX, 0, tollEnd) || Func.Equal(maxX, workPiece.Dimension.Length, tollEnd);

            if (isContourClosed)
            {
                //
                //  Splitto in 2 un contorno se chiuso
                //
                if (SplitClosedContourInTwo(ref sortedContour, ref outSortedContourList, workPiece, overlapCloseContour, processAsPieceCutToMeasure))
                { 
                }
            }
            else
            {
                //
                //  Spezzo il contorno in N contorni in base al cambiamento di side
                //
                CContour currentContour = null;
                EyeContourLine prevl = null;
                bool nextValidLineToBeInsertedInANewContour = false;

                foreach (EyeContourLine l in sortedContour.List)
                {
                    bool currentEdgeAlreadyInAContourDifferentFromThis = false;

                    //
                    //  Verifico se la line corrente e la precedente sono già
                    //  inserite in un altro contorno
                    //
                    if (prevl != null)
                    {
                        foreach (CContour c in outSortedContourList)
                        {
                            if (c == currentContour) continue;

                            if (c.Contains(l) && c.Contains(prevl))
                            {
                                currentEdgeAlreadyInAContourDifferentFromThis = true;
                                nextValidLineToBeInsertedInANewContour = true;
                                prevl = l;
                                break;
                            }
                        }
                    }

                    if (currentEdgeAlreadyInAContourDifferentFromThis)
                        continue;

                    if (nextValidLineToBeInsertedInANewContour || prevl != null && l.side != prevl.side || sortedContour.List.First() == l ||
                        prevl != null && prevl.disconnected)
                    {
                        currentContour = new CContour();
                        currentContour.End = sortedContour.End;
                        outSortedContourList.Add(currentContour);

                        if (nextValidLineToBeInsertedInANewContour)
                            currentContour.AddLine(prevl);

                        nextValidLineToBeInsertedInANewContour = false;
                    }

                    currentContour.AddLine(l);

                    prevl = l;
                }

                //
                //  Questa funzione va chiamata 2 volte per garantire
                //  con la seconda chiamata di collegare catene di contorni create dalla prima chiamata
                //
                AttachContour(outSortedContourList);
                AttachContour(outSortedContourList);

                //
                //  Splitto in 2 tutti i contorni chiusi oppure no in modo
                //  da evitare di attraversare Z = 0 con lo stesso contorno
                //
                bool enslitContourPrfRQonBottomSide = splitinTwoClosedEndContourPrfRQ;
                if ((workPiece.Profile.Code == "R" || workPiece.Profile.Code == "Q") && enslitContourPrfRQonBottomSide)
                {
                    int numberContour = outSortedContourList.Count;

                    for (int i = 0; i < numberContour; i++)
                    {
                        CContour c = outSortedContourList[i];

                        if (c.List.Count <= 0)
                            continue;

                        double minXc = c.List.Min(p => p.StartPoint.X),
                            maxXc = c.List.Max(p => p.StartPoint.X);

                        c.End = Func.Equal(minXc, 0, tollEnd) || Func.Equal(maxXc, workPiece.Dimension.Length, tollEnd);

                        if (c.IsContourClosed(tollContourClosed))
                        {
                            //
                            //  Splitto in 2 un contorno se chiuso
                            //
                            if (SplitClosedContourInTwo(ref c, ref outSortedContourList, workPiece, overlapCloseContour, processAsPieceCutToMeasure))
                            {
                                //  Svuoto il contorno dopo averne create 2 nuovi che lo sostituiscono
                                c.List.Clear();
                            }
                        }
                        else
                        {
                            //
                            //  Splitto in 2 tutti i contorni che attraversano punti a Z = 0
                            //  in modo che ciascuno dei 2 abbia tutte le quote Y dallo stesso lato rispetto alla mezzeria tubo
                            //
                            if (c.List.Count > 0)
                            {
                                //
                                //  Identifico contorni che attraversano il punto Z = 0 e li spezzo in 2
                                //
                                double minY = c.List.Min(p => p.StartPoint.Y),
                                    maxY = c.List.Max(p => p.StartPoint.Y),
                                    minZ = c.List.Min(p => p.StartPoint.Z);
                                bool isContourToBeSplitted = minZ < 10 && minY < workPiece.Profile.HC / 2 && maxY > workPiece.Profile.HC / 2;
                                if (isContourToBeSplitted)
                                {
                                    EyeContourLine minZLine = c.List.OrderByDescending(p => p.StartPoint.Z).Last();

                                    CContour newContour1 = new CContour(), newContour2 = new CContour();
                                    newContour1.End = newContour2.End = c.End;

                                    EyeContourLine splitLine = null;

                                    int rotationIndex = c.List.IndexOf(minZLine);
                                    splitLine = minZLine;

                                    for (int j = 0; j < c.List.Count; j++)
                                    {
                                        EyeContourLine l = c.List[j];
                                        if (j < rotationIndex)
                                            newContour1.AddLine(l);
                                        else
                                            newContour2.AddLine(l);
                                    }

                                    //
                                    //  Aggiungo 2 nuovi contorni in fondo alla lista
                                    //
                                    if (newContour1.List.Count > 0)
                                        outSortedContourList.Add(newContour1);
                                    if (newContour2.List.Count > 0)
                                        outSortedContourList.Add(newContour2);

                                    //
                                    //  Svuoto il contorno appena spezzato (verrà poi cancellato)
                                    //
                                    c.List.Clear();
                                }
                            }
                        }
                    }

                    //
                    //  Cancello tutti i contorni vuoti (quelli che sono stati spezzati e sostituiti da 2 nuovi)
                    //
                    outSortedContourList.RemoveAll(c => c.List.Count == 0);
                    //for (int i = 0; i < outSortedContourList.Count; i++)
                    //{
                    //    CContour c = outSortedContourList[i];
                    //    if (c.List.Count <= 0)
                    //        outSortedContourList.Remove(c);
                    //}
                }
            }

            return true;
        }

        public bool CreateXMLTask (bool createTaskForEachFeature, CProfile profile, string piano, IWorkPiece Wp, CSinglePart singlePart, List<EyeFeature> features,
            ref int taskCounter, ref int pathCounter, ref CTask argtask, bool processAsPieceCutToMeasure)
        {
            double posX = 0, posY = 0, posZ = 0, vecX = 0, vecY = 0, vecZ = 0;
            double tollEnd = 0.1;

            CTask task = createTaskForEachFeature ? null : argtask;

            foreach (var feature in features)
            {
                if (createTaskForEachFeature)
                {
                    //	Creo un TASK per ogni macro
                    task = new CTask("Task");
                    singlePart.AddTask(task);
                    taskCounter++;
                    task.Name = "TASK" + taskCounter.ToString() + "_" + feature.Type.ToString();
                }

                if (feature.EdgeList != null && feature.EdgeList.Count > 0)
                {
                    CContour contour = new CContour(profile, piano, ToolSideComp.None, PathType.Cutting, feature.Type == EyeFeature.FeatureType.INT,  feature.EdgeList, FeatureExtractionParam);
                    if (false) contour.Clean();
                    double tollContourClosed = FeatureExtractionParam.TollContourClosed;
                    bool isContourCounterClockwise = FeatureExtractionParam.CounterClockwiseContour;
                    bool isContourClosed = contour.IsContourClosed(tollContourClosed);
                    bool antiorario = true;

                    if (profile.Code == "R" || profile.Code == "Q")
                    {
                        int countClockwise = 0, countCounterclockwise = 0;
                        if (contour.IsCounterClockwise(Wp.Lp, ref antiorario, 0))
                        {
                            if (antiorario)
                                countCounterclockwise++;
                            else
                                countClockwise++;
                        }
                        if (contour.IsCounterClockwise(Wp.Lp, ref antiorario, 3))
                        {
                            if (antiorario)
                                countCounterclockwise++;
                            else
                                countClockwise++;
                        }
                        if (contour.IsCounterClockwise(Wp.Lp, ref antiorario, 6))
                        {
                            if (antiorario)
                                countCounterclockwise++;
                            else
                                countClockwise++;
                        }

                        antiorario = countCounterclockwise >= countClockwise;

                        if (antiorario != isContourCounterClockwise)
                            contour.Reverse();
                    }
                    else
                        contour.Reverse();

                    //  Ordino la lista di linee secondo opportuni criteri
                    List<CContour> SortedContourList = new List<CContour>();
                    double overlapCloseContour = FeatureExtractionParam.OverlapCloseContour;
                    SortContour(contour, ref SortedContourList, singlePart.WorkPiece, overlapCloseContour, processAsPieceCutToMeasure);

                    foreach (var SortedContour in SortedContourList)
                    {
                        SortedContour.Clean();

                        if (SortedContour.List.Count < 2)
                            continue;

                        bool isSortedContourClosed = SortedContour.IsContourClosed(tollContourClosed);

                        piano = SortedContour.List[0].side;

                        //	Creo un PATH per ogni Lista ordinata
                        CPath path = new CPath();
                        pathCounter++;
                        path.Name = String.Format("Path{0} - {1}", pathCounter, piano);
                        path.Comp = ToolSideComp.Left;
                        path.Type = PathType.Cutting;
                        //  Eridito il flag End dal contorno
                        path.End = SortedContour.End;

                        //double minX = SortedContour.List.Min(p => p.StartPoint.X),
                        //    maxX = SortedContour.List.Max(p => p.StartPoint.X);
                        //bool addLeadIn = isContourClosed && 
                        //    (isSortedContourClosed || minX > FeatureExtractionParam.LeadIn.Length / 2 && maxX < Wp.Lp - FeatureExtractionParam.LeadIn.Length / 2);

                        bool addLeadIn = isContourClosed && (isSortedContourClosed || !processAsPieceCutToMeasure);
                        //
                        //  Per il momento aggiungo LeadIn/LeadOut solo sui contorni chiusi
                        //
                        if (addLeadIn)
                        {
                            if (FeatureExtractionParam.LeadIn.Type != LeadInOutType.None)
                            {
                                path.LeadIn.Type = FeatureExtractionParam.LeadIn.Type;
                                path.LeadIn.Length = FeatureExtractionParam.LeadIn.Length;
                                path.LeadIn.Radius = FeatureExtractionParam.LeadIn.Radius;
                                path.LeadIn.Angle = path.Comp != ToolSideComp.Right ? FeatureExtractionParam.LeadIn.Angle : -FeatureExtractionParam.LeadIn.Angle;
                            }
                            if (FeatureExtractionParam.LeadOut.Type != LeadInOutType.None)
                            {
                                path.LeadOut.Type = FeatureExtractionParam.LeadOut.Type;
                                path.LeadOut.Length = FeatureExtractionParam.LeadOut.Length;
                                path.LeadOut.Radius = FeatureExtractionParam.LeadOut.Radius;
                                path.LeadOut.Angle = path.Comp != ToolSideComp.Right ? -FeatureExtractionParam.LeadOut.Angle : FeatureExtractionParam.LeadOut.Angle;
                            }
                        }

                        task.AddPath(path);

                        Point3D prevPoint = null;
                        foreach (var l in SortedContour.List)
                        {
                            if (prevPoint != null)
                            {
                                if (prevPoint.Equals(l.StartPoint))
                                    continue;
                            }

                            double normavect = l.Direction.Length;

                            if (normavect > 0)
                            {
                                CPoint point = new CPoint();
                                path.AddPoint(point);

                                posX = l.StartPoint.X;
                                posY = l.StartPoint.Y;
                                posZ = l.StartPoint.Z;

                                vecX = l.Direction.X / normavect;
                                vecY = l.Direction.Y / normavect;
                                vecZ = l.Direction.Z / normavect;

                                point.Position.X = posX;
                                point.Position.Y = posY;
                                point.Position.Z = posZ;

                                point.Vector.Vx = vecX;
                                point.Vector.Vy = vecY;
                                point.Vector.Vz = vecZ;

                                bool angledX = (vecX > 0.05 && vecX < 0.95 || vecX < -0.05 && vecX > -0.95);

                                //
                                //  Regola per l'identificazione dei punti skippabili se elaborazione pezzo a misura
                                //  Il punto deve avere X sull'estremità e non deve essere angolato
                                //
                                if (path.End)
                                    point.End = !angledX && (Func.Equal(posX, 0, tollEnd) || Func.Equal(posX, Wp.Lp, tollEnd));

                                point.Overlapped = l.overlapped;

                                prevPoint = l.StartPoint;
                            }
                        }
                    }

                    //
                    // Gestisco i tagli sull'anima vicino alle ali 
                    //
                    if (true)
                    {
                        ITaskPostProcessor processor = new SafeCutStrategy(task, Wp, feature.Type);
                        processor.Execute();
                    }
                }
            }

            return true;
        }

        //
        //  Esporta in formato XML Ficep la descrizione del workpiece e della lista macro associate
        //
        //  pathFileNameXML  è il path completo del file XML da generare
        //
        public bool CreateFicepXML(string pathFileNameXML, string fileNameSTL, IWorkPiece Wp, List<EyeMacro> macros, Brep finalPart,
            bool processAsPieceCutToMeasure)
        {
            //
            //  2)  Creazione oggetto PRODUCTION
            //
            CProduction production = new CProduction();

            //
            //  3)  Creazione oggetto PROFILE
            //
            CProfile profile = new CProfile();
            profile.Code = Wp.Prf.CodePrf;
            profile.Name = "ToBeAssigned";
            if (profile.Code == "L" || profile.Code == "B")
                profile.HB = profile.HC = Wp.Prf.SA;
            else
                profile.HC = Wp.Prf.SA;

            if (profile.Code == "L" || profile.Code == "B")
                profile.TB = profile.TC = Wp.Prf.TA;
            else
                profile.TC = Wp.Prf.TA;

            if (profile.Code == "L" || profile.Code == "B")
                profile.HA = Wp.Prf.SB;
            else
            {
                profile.HA = Wp.Prf.SB;
                profile.HB = Wp.Prf.SB;
            }

            if (profile.Code == "L" || profile.Code == "B")
                profile.TA = Wp.Prf.TB;
            else
            {
                profile.TA = Wp.Prf.TB;
                profile.TB = Wp.Prf.TB;
            }

            profile.Radius = Wp.Prf.Radius;

            if (profile.Code != "R" && Func.Equal(profile.Radius, 0, 0.1))
                profile.Radius = Math.Min(profile.TA, profile.TB);

            //
            //  4)  Creazione oggetto MATERIAL
            //
            CMaterial material = new CMaterial();
            material.Name = "ToBeAssigned";
            material.Type = MaterialType.Default;

            //
            //  5)  Creazione oggetto SINGLEPART
            //
            CSinglePart singlePart = new CSinglePart();
            singlePart.WorkPiece.Mesh.Name = fileNameSTL;
            singlePart.WorkPiece.Contract =
            singlePart.WorkPiece.Drawing =
            singlePart.WorkPiece.Mark =
            singlePart.WorkPiece.Position = "ToBeAssigned";
            singlePart.WorkPiece.Profile = profile;
            singlePart.WorkPiece.Material = material;
            singlePart.WorkPiece.Dimension.Length = Wp.Lp;
            singlePart.WorkPiece.Angles.WebIni =
            singlePart.WorkPiece.Angles.FlangeIni =
            singlePart.WorkPiece.Angles.WebFin =
            singlePart.WorkPiece.Angles.FlangeFin = 0;
            singlePart.Quantity = 1;
            production.AddPart(singlePart);

            //
            //  6)  Creazione oggetti TASK e PATH
            //
            int taskCounter = 0, pathCounter = 0;
            string piano = "C";
            //double posX = 0, posY = 0, posZ = 0, vecX = 0, vecY = 0, vecZ = 0;
            //
            //  Ogni parte ha una Lista di MACRO (macros)
            //  Ogni MACRO ha una lista di FEATURE (macro.Features); una FEATURE rappresenta uno dei volumi da sottrarre (minimo uno nel caso di foro semplice)
            //  Ogni FEATURE ha una lista di EDGE (feature.FicepFaceList)
            //  Dagli EDGE creo una lista ordinata di CONTOUR (SortedContourList)
            //
            if (FeatureExtractionParam.EnableWorkpieceVectors) 
            {
                CTask task = null;
                if (!CreateXMLTask(true, profile, piano, Wp, singlePart, ((EyeWorkPiece)Wp).Features,
                    ref taskCounter, ref pathCounter, ref task, processAsPieceCutToMeasure))
                    return false;
            }
            else
            {
                foreach (var macro in macros)
                {
                    if (macro == null) 
                        continue;

                    //	Creo un TASK per ogni macro
                    CTask task = new CTask("Task");
                    singlePart.AddTask(task);
                    taskCounter++;
                    task.Name = "TASK" + taskCounter.ToString() + "_" + macro.MacroName;

                    if (!CreateXMLTask(false, profile, piano, Wp, singlePart, macro.Features, 
                        ref taskCounter, ref pathCounter, ref task, processAsPieceCutToMeasure))
                        return false;
                }
            }

            //
            //  7)  Salvataggio file XML
            //
            CXMLProduction.WriteProduction(production, pathFileNameXML);

            //
            //  8)  Salvataggio file FNC
            //
            string pathFileNameFNC = pathFileNameXML.ToUpper().Replace(".XML", ".FNC");
            FncFileManager.WriteProduction(production, pathFileNameFNC, processAsPieceCutToMeasure);

            return true;
        }

        public CFeatureExtractionParam FeatureExtractionParam { get; set; }
    }
}
