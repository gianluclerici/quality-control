using static Ficep.AnyCut.Mathematics.Errors;
using Ficep.AnyCut.Geo;
using Ficep.AnyCut.Common;
using static Ficep.AnyCut.Common.Constants;

namespace Ficep.AnyCut.Mathematics
{
    public static class FmathExt
    {

        /// <summary>
        /// 	Calcola il Point intersezione I della retta per P1P2 col cilindro
        /// 	di raggio Radius con asse parallelo all'asse Y.
        /// </summary>
        /// <param name="p1">
        /// Punto sulla retta.
        /// </param>
        /// <param name="p2"></param>
        /// Punto sulla retta.
        /// <param name="radius">
        /// Raggio del cilindro
        /// </param>
        /// <param name="xAsse"></param>
        /// <param name="zAsse"></param>
        /// <param name="IEnter">
        /// Punto di intersezione quando la retta entra nel cilindro 
        /// </param>
        /// <param name="IExit">
        /// Punto di intersezione quando la retta esce dal cilindro
        /// </param>
        /// <returns>
        /// I due punti di intersezione
        /// </returns>
        public static int  IntersezRettaCilindro( in Point p1, in Point p2,
                                                 double radius, double xAsse, double zAsse,
                                                 ref Point IEnter, ref Point IExit)
        {
            if (p1 is null || p2 is null || IEnter is null || IExit is null)
                return (int)MathErr.MATH_ERR_DATA;

            GeoLine CurrLine = new GeoLine(new GeoVec3f(p1.X, p1.Y, p1.Z), new GeoVec3f(p2.X, p2.Y, p2.Z));
            GeoLine CylAxis = new GeoLine(new GeoVec3f(xAsse, 0, zAsse), new GeoVec3f(xAsse, 1, zAsse));
            GeoCylinder Cylinder= new GeoCylinder(CylAxis, radius);
            GeoVec3f iEnter = new GeoVec3f(), iExit = new GeoVec3f();

            if (Cylinder.Intersect(CurrLine, ref iEnter, ref iExit))
            {
                IEnter.X = iEnter.values[0];
                IEnter.Y = iEnter.values[1];
                IEnter.Z = iEnter.values[2];

                IExit.X = iExit.values[0];
                IExit.Y = iExit.values[1];
                IExit.Z = iExit.values[2];
            }

            return (int)MathErr.MATH_PROC_OK;
        }

        /// <summary>
        /// Calcola la curva intersezione di 2 superfici cilindriche:
        /// 
        /// Cilindro 1: asse per P1 P2, raggio Radius1
        /// 
        ///	Cilindro 2: asse per P3 P4, raggio Radius2
        /// </summary>
        /// <param name="p1"></param>
        /// Punto sull'asse del cilindro 1
        /// <param name="p2">
        /// Punto sull'asse del cilindro 1
        /// </param>
        /// <param name="radius1">
        /// Raggio del cilindro 1
        /// </param>
        /// <param name="p3">
        /// Punto sull'asse del cilindro 2
        /// </param>
        /// <param name="p4">
        /// Punto sull'asse del cilindro 2
        /// </param>
        /// <param name="radius2">
        /// Raggio del cilindro 2
        /// </param>
        /// <param name="nPunti">
        /// numero di punti della spezzata con cui viene
        ///	approssimata la curva di intersezione.
        /// </param>
        /// <param name="nPuntiLeadIN">
        /// Numero di punti spezzata per arco di LEAD IN e LEAD OUT.
        /// </param>
        /// <param name="radiusLeadIN"></param>
        /// <param name="IEnter">
        /// Array di NPunti che definiscono la curva intersezione (ENTRATA)
        /// </param>
        /// <param name="IExit">
        /// Array di NPunti che definiscono la curva intersezione (USCITA)
        /// </param>
        /// <param name="nTotPunti"></param>
        /// <param name="cutType"></param>
        /// <returns>
        /// Due arrays che definiscono la curva intersezione (ENTRATA/USCITA)
        /// </returns>
        public static int  IntersezCilindroCilindro(in Point p1, in Point p2, double radius1,
                                                    in Point p3, in Point p4, double radius2,
                                                    int nPunti, int nPuntiLeadIN,
                                                    double radiusLeadIN,
                                                    ref Point[] IEnter, ref Point[] IExit, ref int nTotPunti, int cutType)
        {
            if (p1 is null || p2 is null || p3 is null || p4 is null || nPunti <= 0)
                return (int)MathErr.MATH_ERR_DATA;

            nTotPunti = nPunti + 2 * nPuntiLeadIN;

            IEnter = Enumerable.Range(0, nTotPunti).Select(x => new Point()).ToArray();
            IExit = Enumerable.Range(0, nTotPunti).Select(x => new Point()).ToArray();

            //
            //	Segnalazione errore se eccedo numero max di punti allocabili.
            //
            if (nPunti + 2 * nPuntiLeadIN > (int)LeadPoints.MAX_PUNTI_INTERSEZ + 2 * (int)LeadPoints.MAX_PUNTI_LEAD_IN)
                return (int)ParserErr.PARSER_ERR_DOMINIO;

            //	Asse cilindro1
            GeoLine cylAxis1 = new GeoLine(new GeoVec3f(p1.X, p1.Y, p1.Z), new GeoVec3f(p2.X, p2.Y, p2.Z));
            //	Asse cilindro2
            GeoLine cylAxis2 = new GeoLine(new GeoVec3f(p3.X, p3.Y, p3.Z), new GeoVec3f(p4.X, p4.Y, p4.Z));
            //	Cilindro1
            GeoCylinder cylinder1 = new GeoCylinder(cylAxis1, radius1);
            //	Cilindro2
            GeoCylinder cylinder2 = new GeoCylinder(cylAxis2, radius2);
            //
            //	Sciavicco-Siciliano	pag. 29
            //
            //	Per sovrapporre l'asse z del mio riferimento all'asse di rotazione
            //	P3P4 del cilindro2 devo compiere 2 rotazioni:
            //
            //	Rotazione di un angolo alfa attorno a z
            //	
            //	Rotazione di un angolo beta attorno a y
            //
            //	Math.Cos (alfa) = rx / Math.Sqrt (rx * rx + ry * ry)
            //
            //	Math.Cos (beta) = rz / Math.Sqrt (rx * rx + ry * ry + rz * rz)
            //
            double rx = p4.X - p3.X, ry = p4.Y - p3.Y, rz = p4.Z - p3.Z;

            double dxy = Math.Sqrt(rx * rx + ry * ry);
            double dxyz = Math.Sqrt(rx * rx + ry * ry + rz * rz);
            double alfa = dxy > 0 ? FmathExt.Acos(rx / dxy) : 0.0;
            double beta = FmathExt.Acos(rz / dxyz);

            if (ry < 0)
                alfa = -alfa;

            //
            //	Matrice Rz rotazione angolo alfa attorno a asse z.
            //
            double[,] MatrixRz = new double[3,3];

            MatrixRz[0,0] = Math.Cos(alfa);
            MatrixRz[0,1] = -Math.Sin(alfa);
            MatrixRz[0,2] = 0.0;

            MatrixRz[1,0] = Math.Sin(alfa);
            MatrixRz[1,1] = Math.Cos(alfa);
            MatrixRz[1,2] = 0.0;

            MatrixRz[2,0] = 0.0;
            MatrixRz[2,1] = 0.0;
            MatrixRz[2,2] = 1.0;

            //
            //	Matrice Ry rotazione angolo beta attorno a asse y.
            //
            double[,] MatrixRy = new double[3,3];

            MatrixRy[0,0] = Math.Cos(beta);
            MatrixRy[0,1] = 0.0;
            MatrixRy[0,2] = Math.Sin(beta);

            MatrixRy[1,0] = 0.0;
            MatrixRy[1,1] = 1.0;
            MatrixRy[1,2] = 0.0;

            MatrixRy[2,0] = -Math.Sin(beta);
            MatrixRy[2,1] = 0.0;
            MatrixRy[2,2] = Math.Cos(beta);

            double[,] MatrixR = new double[3, 3];

            //
            //	Matrice R composizione delle rotazioni alfa e beta.
            //
            //  R =Rz * Ry.
            //
            FMath.MultMatrix(MatrixRz, 3, 3,
                        MatrixRy, 3, 3,
                        ref MatrixR, 3, 3);

            double[] Vector1 = new double[3] , Vector2 = new double[3], Vector1Rot = new double[3], Vector2Rot = new double[3];
            double Angle = 0.0;
            GeoVec3f iEnter = new GeoVec3f(), iExit = new GeoVec3f();
            GeoLine CurrLine = new GeoLine();

            if (cutType == 0)
            {
                //////////////////////////////////////////////////
                //
                //	Arco di LEAD IN descritto con NPuntiLeadIN.
                //
                //////////////////////////////////////////////////
                for (int i = 0; i < nPuntiLeadIN; i++)
                {
                    Angle = -i * Math.PI / nPuntiLeadIN + Math.PI / 2;

                    //
                    //	Vector1 è il vettore raggio della circonferenza di raggio Radius2
                    //	ruotato di Angle, con asse z e quota z = -dxyz / 2.
                    //
                    Vector1[0] = radiusLeadIN * Math.Cos(Angle);
                    Vector1[1] = radiusLeadIN * Math.Sin(Angle) - radius2 + radiusLeadIN;
                    Vector1[2] = -dxyz / 2.0;

                    //
                    //	Vector2 è il vettore raggio della circonferenza di raggio Radius2
                    //	ruotato di Angle, con asse z e quota z = dxyz / 2.
                    //
                    Vector2[0] = radiusLeadIN * Math.Cos(Angle);
                    Vector2[1] = radiusLeadIN * Math.Sin(Angle) - radius2 + radiusLeadIN;
                    Vector2[2] = dxyz / 2.0;

                    //
                    //  Vector1Rot = R * Vector1.
                    //
                    FMath.MultVect(MatrixR, 3, 3,           
                                Vector1, 3,
                                ref Vector1Rot, 3);

                    //
                    //  Vector2Rot = R * Vector2.
                    //
                    FMath.MultVect(MatrixR, 3, 3,           
                                Vector2, 3,
                                ref Vector2Rot, 3);


                    Vector1Rot[0] += (p3.X + p4.X) / 2.0;
                    Vector1Rot[1] += (p3.Y + p4.Y) / 2.0;
                    Vector1Rot[2] += (p3.Z + p4.Z) / 2.0;

                    Vector2Rot[0] += (p3.X + p4.X) / 2.0;
                    Vector2Rot[1] += (p3.Y + p4.Y) / 2.0;
                    Vector2Rot[2] += (p3.Z + p4.Z) / 2.0;

                    CurrLine.SetValue(new GeoVec3f(Vector1Rot[0],
                                            Vector1Rot[1],
                                            Vector1Rot[2]),
                                    new GeoVec3f(Vector2Rot[0],
                                            Vector2Rot[1],
                                            Vector2Rot[2]));

                    //
                    //	Calcolo le 2 intersezioni tra il cilindro1 e
                    //	la retta per i 2 punti Vector1Rot e Vector2Rot.
                    //
                    if (cylinder1.Intersect(CurrLine, ref iEnter, ref iExit))
                    {
                        IEnter[i].X = iEnter.values[0];
                        IEnter[i].Y = iEnter.values[1];
                        IEnter[i].Z = iEnter.values[2];

                        IExit[i].X = iExit.values[0];
                        IExit[i].Y = iExit.values[1];
                        IExit[i].Z = iExit.values[2];
                    }
                    else
                    {
                        if (i > 0)
                        {
                            IEnter[i].X = IEnter[i - 1].X;
                            IEnter[i].Y = IEnter[i - 1].Y;
                            IEnter[i].Z = IEnter[i - 1].Z;

                            IExit[i].X = IExit[i - 1].X;
                            IExit[i].Y = IExit[i - 1].Y;
                            IExit[i].Z = IExit[i - 1].Z;
                        }
                    }
                }

                double DX = 0.0, DY = 0.0;

                //////////////////////////////////////////////////
                //
                //	Cerchio descritto con NPunti.
                //
                //////////////////////////////////////////////////
                for (int i = nPuntiLeadIN; i < nPuntiLeadIN + nPunti - 2; i++)
                {
                    Angle = -(i - nPuntiLeadIN) * (2 * Math.PI) / (nPunti - 1) + 1.5 * Math.PI;
                    if (i == nPuntiLeadIN + 1)
                        Angle += 0.5 * (2 * Math.PI) / (nPunti - 1);

                    //
                    //	Calcolo lo spostamento X e Y tra primo e secondo Point.
                    //	Serve per il LEAD OUT.
                    //
                    if (i == (nPuntiLeadIN + 1))
                    {
                        DX = radius2 * Math.Cos(Angle) - Vector1[0];
                        DY = radius2 * Math.Sin(Angle) - Vector1[1];
                    }
                    //
                    //	Vector1 è il vettore raggio della circonferenza di raggio Radius2
                    //	ruotato di Angle, con asse z e quota z = -dxyz / 2.
                    //
                    Vector1[0] = radius2 * Math.Cos(Angle);
                    Vector1[1] = radius2 * Math.Sin(Angle);
                    Vector1[2] = -dxyz / 2.0;

                    //
                    //	Vector2 è il vettore raggio della circonferenza di raggio Radius2
                    //	ruotato di Angle, con asse z e quota z = dxyz / 2.
                    //
                    Vector2[0] = radius2 * Math.Cos(Angle);
                    Vector2[1] = radius2 * Math.Sin(Angle);
                    Vector2[2] = dxyz / 2.0;

                    //
                    //  Vector1Rot = R * Vector1.
                    //
                    FMath.MultVect(MatrixR, 3, 3,
                                Vector1, 3,
                                ref Vector1Rot, 3);

                    //
                    //  Vector2Rot = R * Vector2.
                    //
                    FMath.MultVect(MatrixR, 3, 3,
                                Vector2, 3,
                                ref Vector2Rot, 3);


                    Vector1Rot[0] += (p3.X + p4.X) / 2.0;
                    Vector1Rot[1] += (p3.Y + p4.Y) / 2.0;
                    Vector1Rot[2] += (p3.Z + p4.Z) / 2.0;

                    Vector2Rot[0] += (p3.X + p4.X) / 2.0;
                    Vector2Rot[1] += (p3.Y + p4.Y) / 2.0;
                    Vector2Rot[2] += (p3.Z + p4.Z) / 2.0;

                    CurrLine.SetValue(new GeoVec3f(Vector1Rot[0],
                                            Vector1Rot[1],
                                            Vector1Rot[2]),
                                    new GeoVec3f(Vector2Rot[0],
                                            Vector2Rot[1],
                                            Vector2Rot[2]));

                    //
                    //	Calcolo le 2 intersezioni tra il cilindro1 e
                    //	la retta per i 2 punti Vector1Rot e Vector2Rot.
                    //
                    if (cylinder1.Intersect(CurrLine, ref iEnter, ref iExit))
                    {
                        IEnter[i].X = iEnter.values[0];
                        IEnter[i].Y = iEnter.values[1];
                        IEnter[i].Z = iEnter.values[2];

                        IExit[i].X = iExit.values[0];
                        IExit[i].Y = iExit.values[1];
                        IExit[i].Z = iExit.values[2];
                    }
                    else
                    {
                        if (i > 0)
                        {
                            IEnter[i].X = IEnter[i - 1].X;
                            IEnter[i].Y = IEnter[i - 1].Y;
                            IEnter[i].Z = IEnter[i - 1].Z;

                            IExit[i].X = IExit[i - 1].X;
                            IExit[i].Y = IExit[i - 1].Y;
                            IExit[i].Z = IExit[i - 1].Z;
                        }
                    }
                }

                //	Il penultimo Point coincide col primo.
                IEnter[nPuntiLeadIN + nPunti - 2].X = IEnter[nPuntiLeadIN].X;
                IEnter[nPuntiLeadIN + nPunti - 2].Y = IEnter[nPuntiLeadIN].Y;
                IEnter[nPuntiLeadIN + nPunti - 2].Z = IEnter[nPuntiLeadIN].Z;

                IExit[nPuntiLeadIN + nPunti - 2].X = IExit[nPuntiLeadIN].X;
                IExit[nPuntiLeadIN + nPunti - 2].Y = IExit[nPuntiLeadIN].Y;
                IExit[nPuntiLeadIN + nPunti - 2].Z = IExit[nPuntiLeadIN].Z;

                //	L'ultimo Point coincide col secondo.
                //	(Serve per garantire che la curva sia chiusa).
                IEnter[nPuntiLeadIN + nPunti - 1].X = IEnter[nPuntiLeadIN + 1].X;
                IEnter[nPuntiLeadIN + nPunti - 1].Y = IEnter[nPuntiLeadIN + 1].Y;
                IEnter[nPuntiLeadIN + nPunti - 1].Z = IEnter[nPuntiLeadIN + 1].Z;

                IExit[nPuntiLeadIN + nPunti - 1].X = IExit[nPuntiLeadIN + 1].X;
                IExit[nPuntiLeadIN + nPunti - 1].Y = IExit[nPuntiLeadIN + 1].Y;
                IExit[nPuntiLeadIN + nPunti - 1].Z = IExit[nPuntiLeadIN + 1].Z;

                //////////////////////////////////////////////////
                //
                //	Arco di LEAD OUT descritto con NPuntiLeadIN.
                //
                //////////////////////////////////////////////////
                for (int i = nPuntiLeadIN + nPunti; i < nPuntiLeadIN + nPunti + nPuntiLeadIN; i++)
                {
                    Angle = -(i - nPuntiLeadIN - nPunti) * (Math.PI / 2.0) / nPuntiLeadIN + 1.5f * Math.PI;
                    //Angle = -(i - NPuntiLeadIN - NPunti) * Math.PI / NPuntiLeadIN + 1.5 * Math.PI;

                    //
                    //	Vector1 è il vettore raggio della circonferenza di raggio Radius2
                    //	ruotato di Angle, con asse z e quota z = -dxyz / 2.
                    //
                    Vector1[0] = radiusLeadIN * Math.Cos(Angle) + DX;
                    Vector1[1] = radiusLeadIN * Math.Sin(Angle) - radius2 + radiusLeadIN + DY;
                    Vector1[2] = -dxyz / 2.0;

                    //
                    //	Vector2 è il vettore raggio della circonferenza di raggio Radius2
                    //	ruotato di Angle, con asse z e quota z = dxyz / 2.
                    //
                    Vector2[0] = radiusLeadIN * Math.Cos(Angle) + DX;
                    Vector2[1] = radiusLeadIN * Math.Sin(Angle) - radius2 + radiusLeadIN + DY;
                    Vector2[2] = dxyz / 2.0;

                    //
                    //  Vector1Rot = R * Vector1.
                    //
                    FMath.MultVect(MatrixR, 3, 3,
                                Vector1, 3,
                                ref Vector1Rot, 3);

                    //
                    //  Vector2Rot = R * Vector2.
                    //
                    FMath.MultVect(MatrixR, 3, 3,
                               Vector2, 3,
                                ref Vector2Rot, 3);


                    Vector1Rot[0] += (p3.X + p4.X) / 2.0;
                    Vector1Rot[1] += (p3.Y + p4.Y) / 2.0;
                    Vector1Rot[2] += (p3.Z + p4.Z) / 2.0;

                    Vector2Rot[0] += (p3.X + p4.X) / 2.0;
                    Vector2Rot[1] += (p3.Y + p4.Y) / 2.0;
                    Vector2Rot[2] += (p3.Z + p4.Z) / 2.0;

                    CurrLine.SetValue(new GeoVec3f(Vector1Rot[0],
                                            Vector1Rot[1],
                                            Vector1Rot[2]),
                                    new GeoVec3f(Vector2Rot[0],
                                            Vector2Rot[1],
                                            Vector2Rot[2]));

                    //
                    //	Calcolo le 2 intersezioni tra il cilindro1 e
                    //	la retta per i 2 punti Vector1Rot e Vector2Rot.
                    //
                    if (cylinder1.Intersect(CurrLine, ref iEnter, ref iExit))
                    {
                        IEnter[i].X = iEnter.values[0];
                        IEnter[i].Y = iEnter.values[1];
                        IEnter[i].Z = iEnter.values[2];

                        IExit[i].X = iExit.values[0];
                        IExit[i].Y = iExit.values[1];
                        IExit[i].Z = iExit.values[2];
                    }
                    else
                    {
                        if (i > 0)
                        {
                            IEnter[i].X = IEnter[i - 1].X;
                            IEnter[i].Y = IEnter[i - 1].Y;
                            IEnter[i].Z = IEnter[i - 1].Z;

                            IExit[i].X = IExit[i - 1].X;
                            IExit[i].Y = IExit[i - 1].Y;
                            IExit[i].Z = IExit[i - 1].Z;
                        }
                    }
                }
            }
            else
            {
                //
                //	FirstCutFixed	= true se il primo semitaglio è lato fisso.
                //					= false se il primo semitaglio è lato mobile
                //
                bool FirstCutFixed = true;
                double AngleSign = FirstCutFixed ? 1.0 : -1.0;

                //////////////////////////////////////////////////
                //
                //	Cerchio descritto con NPunti.
                //
                //////////////////////////////////////////////////
                for (int i = 0; i < nPunti / 2; i++)
                {
                    Angle = AngleSign * i * (2 * Math.PI) / (nPunti - 2) + 0.0 * Math.PI;
                    //
                    //	Vector1 è il vettore raggio della circonferenza di raggio Radius2
                    //	ruotato di Angle, con asse z e quota z = -dxyz / 2.
                    //
                    Vector1[0] = radius2 * Math.Cos(Angle);
                    Vector1[1] = radius2 * Math.Sin(Angle);
                    Vector1[2] = -dxyz / 2.0;

                    //
                    //	Vector2 è il vettore raggio della circonferenza di raggio Radius2
                    //	ruotato di Angle, con asse z e quota z = dxyz / 2.
                    //
                    Vector2[0] = radius2 * Math.Cos(Angle);
                    Vector2[1] = radius2 * Math.Sin(Angle);
                    Vector2[2] = dxyz / 2.0;

                    //
                    //  Vector1Rot = R * Vector1.
                    //
                    FMath.MultVect(MatrixR, 3, 3,
                                Vector1, 3,
                                ref Vector1Rot, 3);

                    //
                    //  Vector2Rot = R * Vector2.
                    //
                    FMath.MultVect(MatrixR, 3, 3,
                                Vector2, 3,
                                ref Vector2Rot, 3);


                    Vector1Rot[0] += (p3.X + p4.X) / 2.0;
                    Vector1Rot[1] += (p3.Y + p4.Y) / 2.0;
                    Vector1Rot[2] += (p3.Z + p4.Z) / 2.0;

                    Vector2Rot[0] += (p3.X + p4.X) / 2.0;
                    Vector2Rot[1] += (p3.Y + p4.Y) / 2.0;
                    Vector2Rot[2] += (p3.Z + p4.Z) / 2.0;

                    CurrLine.SetValue(new GeoVec3f(Vector1Rot[0],
                                            Vector1Rot[1],
                                            Vector1Rot[2]),
                                    new GeoVec3f(Vector2Rot[0],
                                            Vector2Rot[1],
                                            Vector2Rot[2]));

                    //
                    //	Calcolo le 2 intersezioni tra il cilindro1 e
                    //	la retta per i 2 punti Vector1Rot e Vector2Rot.
                    //
                    if (cylinder1.Intersect(CurrLine, ref iEnter, ref iExit))
                    {
                        IEnter[i].X = iEnter.values[0];
                        IEnter[i].Y = iEnter.values[1];
                        IEnter[i].Z = iEnter.values[2];

                        IExit[i].X = iExit.values[0];
                        IExit[i].Y = iExit.values[1];
                        IExit[i].Z = iExit.values[2];
                    }
                    else
                    {
                        if (i > 0)
                        {
                            IEnter[i].X = IEnter[i - 1].X;
                            IEnter[i].Y = IEnter[i - 1].Y;
                            IEnter[i].Z = IEnter[i - 1].Z;

                            IExit[i].X = IExit[i - 1].X;
                            IExit[i].Y = IExit[i - 1].Y;
                            IExit[i].Z = IExit[i - 1].Z;
                        }
                    }
                }

                for (int i = nPunti / 2; i < nPunti; i++)
                {
                    Angle = AngleSign * (nPunti - 2 - (i - nPunti / 2)) * (2 * Math.PI) / (nPunti - 2) + 0.0 * Math.PI;
                    //
                    //	Vector1 è il vettore raggio della circonferenza di raggio Radius2
                    //	ruotato di Angle, con asse z e quota z = -dxyz / 2.
                    //
                    Vector1[0] = radius2 * Math.Cos(Angle);
                    Vector1[1] = radius2 * Math.Sin(Angle);
                    Vector1[2] = -dxyz / 2.0;

                    //
                    //	Vector2 è il vettore raggio della circonferenza di raggio Radius2
                    //	ruotato di Angle, con asse z e quota z = dxyz / 2.
                    //
                    Vector2[0] = radius2 * Math.Cos(Angle);
                    Vector2[1] = radius2 * Math.Sin(Angle);
                    Vector2[2] = dxyz / 2.0;

                    //
                    //  Vector1Rot = R * Vector1.
                    //
                    FMath.MultVect(MatrixR, 3, 3,
                                Vector1, 3,
                                ref Vector1Rot, 3);

                    //
                    //  Vector2Rot = R * Vector2.
                    //
                    FMath.MultVect(MatrixR, 3, 3,
                                Vector2, 3,
                                ref Vector2Rot, 3);


                    Vector1Rot[0] += (p3.X + p4.X) / 2.0;
                    Vector1Rot[1] += (p3.Y + p4.Y) / 2.0;
                    Vector1Rot[2] += (p3.Z + p4.Z) / 2.0;

                    Vector2Rot[0] += (p3.X + p4.X) / 2.0;
                    Vector2Rot[1] += (p3.Y + p4.Y) / 2.0;
                    Vector2Rot[2] += (p3.Z + p4.Z) / 2.0;

                    CurrLine.SetValue(new GeoVec3f(Vector1Rot[0],
                                            Vector1Rot[1],
                                            Vector1Rot[2]),
                                    new GeoVec3f(Vector2Rot[0],
                                            Vector2Rot[1],
                                            Vector2Rot[2]));

                    //
                    //	Calcolo le 2 intersezioni tra il cilindro1 e
                    //	la retta per i 2 punti Vector1Rot e Vector2Rot.
                    //
                    if (cylinder1.Intersect(CurrLine, ref iEnter, ref iExit))
                    {
                        IEnter[i].X = iEnter.values[0];
                        IEnter[i].Y = iEnter.values[1];
                        IEnter[i].Z = iEnter.values[2];

                        IExit[i].X = iExit.values[0];
                        IExit[i].Y = iExit.values[1];
                        IExit[i].Z = iExit.values[2];
                    }
                    else
                    {
                        if (i > 0)
                        {
                            IEnter[i].X = IEnter[i - 1].X;
                            IEnter[i].Y = IEnter[i - 1].Y;
                            IEnter[i].Z = IEnter[i - 1].Z;

                            IExit[i].X = IExit[i - 1].X;
                            IExit[i].Y = IExit[i - 1].Y;
                            IExit[i].Z = IExit[i - 1].Z;
                        }
                    }
                }

                //	Il penultimo Point coincide col primo.
                /*		IEnter[NPuntiLeadIN + NPunti - 2].X	= IEnter[NPuntiLeadIN].X;
                        IEnter[NPuntiLeadIN + NPunti - 2].Y	= IEnter[NPuntiLeadIN].Y;
                        IEnter[NPuntiLeadIN + NPunti - 2].Z	= IEnter[NPuntiLeadIN].Z;

                        IExit[NPuntiLeadIN + NPunti - 2].X		= IExit[NPuntiLeadIN].X;
                        IExit[NPuntiLeadIN + NPunti - 2].Y		= IExit[NPuntiLeadIN].Y;
                        IExit[NPuntiLeadIN + NPunti - 2].Z		= IExit[NPuntiLeadIN].Z;

                            //	L'ultimo Point coincide col secondo.
                            //	(Serve per garantire che la curva sia chiusa).
                        IEnter[NPuntiLeadIN + NPunti - 1].X	= IEnter[NPuntiLeadIN + 1].X;
                        IEnter[NPuntiLeadIN + NPunti - 1].Y	= IEnter[NPuntiLeadIN + 1].Y;
                        IEnter[NPuntiLeadIN + NPunti - 1].Z	= IEnter[NPuntiLeadIN + 1].Z;

                        IExit[NPuntiLeadIN + NPunti - 1].X		= IExit[NPuntiLeadIN + 1].X;
                        IExit[NPuntiLeadIN + NPunti - 1].Y		= IExit[NPuntiLeadIN + 1].Y;
                        IExit[NPuntiLeadIN + NPunti - 1].Z		= IExit[NPuntiLeadIN + 1].Z;*/
            }

            return (int)MathErr.MATH_PROC_OK;
        }

        /// <summary>
        /// Calcola la curva intersezione di 1 superficie cilindrica e di una
        /// appartenente a profilato rettangolare raggiato:
        /// 
        ///	Cilindro : asse per P1 P2, raggio Radius1
        ///
        ///	Profilato: asse per P3 P4, dimensioni Lx e Ly, raggio Radius2
        ///
        ///	I punti intersezione vengono calcolati intersecando il cilindro1 con
        ///	linee di superficie del cilindro2 (linee parallele all'asse del cilindro
        ///	con distanza pari al raggio).
        /// </summary>
        /// <param name="p1">
        /// Punto sull'asse del cilindro
        /// </param>
        /// Punto sull'asse del cilindro
        /// <param name="p2">
        /// </param>
        /// <param name="radius1">
        /// Raggio del cilindro
        /// </param>
        /// <param name="p3">
        /// Punto sull'asse del profilato
        /// </param>
        /// <param name="p4">
        /// Punto sull'asse del profilato
        /// </param>
        /// <param name="Lx">
        /// Lunghezza in x del profilato
        /// </param>
        /// <param name="Ly">
        /// Lunghezza in y del profilato
        /// </param>
        /// <param name="radius2">
        /// Raggio del profilato 
        /// </param>
        /// <param name="nPuntiLeadIN">
        /// Numero di punti spezzata per arco di LEAD IN e LEAD OUT.
        /// </param>
        /// <param name="RadiusLeadIN"></param>
        /// <param name="NPunti">
        /// numero di punti della spezzata con cui viene approssimata la curva di intersezione.
        /// </param>
        /// <param name="nPuntiRad"></param>
        /// <param name="overlap"></param>
        /// <param name="IEnter">
        /// Array di NPunti che definiscono la curva intersezione (ENTRATA)
        /// </param>
        /// <param name="IExit">
        /// Array di NPunti che definiscono la curva intersezione (USCITA)
        /// </param>
        /// <param name="nTotPunti"></param>
        /// <returns></returns>
        public static int  IntersezCilindroSquare(in Point p1, in Point p2, double radius1,
                                                    in Point p3, in Point p4,
                                                    double Lx, double Ly, double radius2,
                                                    int nPuntiLeadIN,
                                                    double RadiusLeadIN, int nPuntiRad,
                                                    double overlap,
                                                    ref Point[] IEnter, ref Point[] IExit, ref int nTotPunti)
        {
            if (p1 is null || p2 is null || p3 is null || p4 is null)
                return (int)MathErr.MATH_ERR_DATA;

            Point[] Contour = Enumerable.Range(0, (int)LeadPoints.MAX_PUNTI_INTERSEZ + 2 * (int)LeadPoints.MAX_PUNTI_LEAD_IN).Select(x => new Point()).ToArray();
            

            int npuntiarc = nPuntiRad;
            int nPunti = 3 + 2 * nPuntiLeadIN;

            if (radius2 > 0)
                nPunti += 4 * nPuntiRad;
            else
                nPunti += 4;

            if (Lx > 2 * radius2)
                nPunti += 2 * nPuntiRad;

            nTotPunti = nPunti;

            IEnter = Enumerable.Range(0, nPunti).Select(x => new Point()).ToArray();
            IExit = Enumerable.Range(0, nPunti).Select(x => new Point()).ToArray();
            //
            //	Segnalazione errore se eccedo numero max di punti allocabili.
            //
            if (nPunti > (int)LeadPoints.MAX_PUNTI_INTERSEZ + 2 * (int)LeadPoints.MAX_PUNTI_LEAD_IN)
                return (int)ParserErr.PARSER_ERR_DOMINIO;

            int NActPoint = 0;
            double Angle = 0.0;

            //////////////////////////////////////////////////
            //
            //	Arco di LEAD IN descritto con NPuntiLeadIN.
            //
            //////////////////////////////////////////////////
            for (int i = NActPoint; i < NActPoint + nPuntiLeadIN; i++)
            {
                Angle = (i - NActPoint) * Math.PI / nPuntiLeadIN;

                Contour[i].X = -Lx / 2 + RadiusLeadIN + RadiusLeadIN * Math.Cos(Angle);
                Contour[i].Y = -RadiusLeadIN * Math.Sin(Angle);
                Contour[i].Z = 0.0;
            }

            NActPoint += nPuntiLeadIN;

            //
            //	Primo Point presente sempre.
            //
            Contour[NActPoint].X = -Lx / 2;
            Contour[NActPoint].Y = 0.0;
            Contour[NActPoint].Z = 0.0;
            NActPoint++;

            /////////////////////////////////////////////////////////////
            //
            //	Primo raggio di arrotondamento descritto con NPuntiArc.
            //
            /////////////////////////////////////////////////////////////
            if (radius2 > 0)
            {
                for (int i = NActPoint; i < NActPoint + npuntiarc; i++)
                {
                    Angle = -(i - NActPoint) * Math.PI / 2 / (npuntiarc - 1) + 0.0 * Math.PI;

                    Contour[i].X = -Lx / 2 + radius2 - radius2 * Math.Cos(Angle);
                    Contour[i].Y = Ly / 2 - radius2 - radius2 * Math.Sin(Angle);
                    Contour[i].Z = 0.0;
                }

                NActPoint += npuntiarc;
            }

            //
            //	Spezzo lo spostamento lineare in direzione X in NPuntiArc.
            //
            if (Lx > 2 * radius2)
            {
                for (int i = NActPoint; i < NActPoint + npuntiarc; i++)
                {
                    Contour[i].X = -Lx / 2 + radius2 + (i - NActPoint + 1) * (Lx - 2 * radius2) / (npuntiarc + 1);
                    Contour[i].Y = Ly / 2;
                    Contour[i].Z = 0.0;
                }

                NActPoint += npuntiarc;
            }

            if (FMath.Equal(radius2, 0.0))
            {
                Contour[NActPoint].X = -Lx / 2;
                Contour[NActPoint].Y = Ly / 2;
                Contour[NActPoint].Z = 0.0;
                NActPoint++;
            }

            /////////////////////////////////////////////////////////////
            //
            //	Secondo raggio di arrotondamento descritto con NPuntiArc.
            //
            /////////////////////////////////////////////////////////////
            if (radius2 > 0)
            {
                for (int i = NActPoint; i < NActPoint + npuntiarc; i++)
                {
                    Angle = -(i - NActPoint) * Math.PI / 2 / (npuntiarc - 1) - Math.PI / 2.0f;

                    Contour[i].X = Lx / 2 - radius2 - radius2 * Math.Cos(Angle);
                    Contour[i].Y = Ly / 2 - radius2 - radius2 * Math.Sin(Angle);
                    Contour[i].Z = 0.0;
                }

                NActPoint += npuntiarc;
            }
            else
            {
                Contour[NActPoint].X = Lx / 2;
                Contour[NActPoint].Y = Ly / 2;
                Contour[NActPoint].Z = 0.0;
                NActPoint++;
            }

            /////////////////////////////////////////////////////////////
            //
            //	Terzo raggio di arrotondamento descritto con NPuntiArc.
            //
            /////////////////////////////////////////////////////////////
            if (radius2 > 0)
            {
                for (int i = NActPoint; i < NActPoint + npuntiarc; i++)
                {
                    Angle = -(i - NActPoint) * Math.PI / 2 / (npuntiarc - 1) + Math.PI;

                    Contour[i].X = Lx / 2 - radius2 - radius2 * Math.Cos(Angle);
                    Contour[i].Y = -Ly / 2 + radius2 - radius2 * Math.Sin(Angle);
                    Contour[i].Z = 0;
                }

                NActPoint += npuntiarc;
            }

            //
            //	Spezzo lo spostamento lineare in direzione X in NPuntiArc.
            //
            if (Lx > 2 * radius2)
            {
                for (int i = NActPoint; i < NActPoint + npuntiarc; i++)
                {
                    Contour[i].X = Lx / 2 - radius2 - (i - NActPoint + 1) * (Lx - 2 * radius2) / (npuntiarc + 1);
                    Contour[i].Y = -Ly / 2;
                    Contour[i].Z = 0.0;
                }

                NActPoint += npuntiarc;
            }

            if (FMath.Equal(radius2, 0.0))
            {
                Contour[NActPoint].X = Lx / 2;
                Contour[NActPoint].Y = -Ly / 2;
                Contour[NActPoint].Z = 0.0;
                NActPoint++;
            }

            /////////////////////////////////////////////////////////////
            //
            //	Quarto raggio di arrotondamento descritto con NPuntiArc.
            //
            /////////////////////////////////////////////////////////////
            if (radius2 > 0)
            {
                for (int i = NActPoint; i < NActPoint + npuntiarc; i++)
                {
                    Angle = -(i - NActPoint) * Math.PI / 2 / (npuntiarc - 1) + Math.PI / 2.0f;

                    Contour[i].X = -Lx / 2 + radius2 - radius2 * Math.Cos(Angle);
                    Contour[i].Y = -Ly / 2 + radius2 - radius2 * Math.Sin(Angle);
                    Contour[i].Z = 0.0;
                }

                NActPoint += npuntiarc;
            }
            else
            {
                Contour[NActPoint].X = -Lx / 2;
                Contour[NActPoint].Y = -Ly / 2;
                Contour[NActPoint].Z = 0.0;
                NActPoint++;
            }

            Contour[NActPoint].X = -Lx / 2;
            Contour[NActPoint].Y = 0.0;
            Contour[NActPoint].Z = 0.0;
            NActPoint++;

            Contour[NActPoint].X = -Lx / 2;
            Contour[NActPoint].Y = overlap;
            Contour[NActPoint].Z = 0.0;
            NActPoint++;

            //////////////////////////////////////////////////
            //
            //	Arco di LEAD OUT descritto con NPuntiLeadIN.
            //
            //////////////////////////////////////////////////
            for (int i = NActPoint; i < NActPoint + nPuntiLeadIN; i++)
            {
                Angle = (i - NActPoint) * (Math.PI / 2.0f) / nPuntiLeadIN + Math.PI;
                //Angle = (i - NActPoint) * Math.PI / NPuntiLeadIN + Math.PI;

                Contour[i].X = -Lx / 2 + RadiusLeadIN + RadiusLeadIN * Math.Cos(Angle);
                Contour[i].Y = Contour[NActPoint - 1].Y - RadiusLeadIN * Math.Sin(Angle);
                Contour[i].Z = 0.0;
            }

            NActPoint += nPuntiLeadIN;

            IntersezCilindroExtruded(p1, p2, radius1,
                                    p3, p4, Contour, NActPoint,
                                    ref IEnter, ref IExit);

            return (int)MathErr.MATH_PROC_OK;
        }


        /// <summary>
        /// Calcola la curva intersezione di 1 superficie cilindrica e di una
        /// estrusa.
        ///
        ///	Cilindro 1: asse per P1 P2, raggio Radius1
        ///
        ///	Extruded 2: asse per P3 P4, Contour è l'array di punti del contorno
        ///				estruso descritti nel piano xy
        ///				
        ///	I punti intersezione vengono calcolati intersecando il cilindro1 con
        ///	linee di superficie del solido estruso 2.
        /// </summary>
        /// <param name="p1">
        /// Punto sull'asse del cilindro 
        /// </param>
        /// <param name="p2">
        /// Punto sull'asse del cilindro
        /// </param>
        /// <param name="radius1"></param>
        /// <param name="p3">
        /// Punto sull'asse del solido estruso
        /// </param>
        /// <param name="p4">
        /// Punto sull'asse del solido estruso
        /// </param>
        /// <param name="contour">
        /// Array di punti del contorno estruso descritti nel piano xy
        /// </param>
        /// <param name="nPunti">
        /// Numero di punti del contorno estruso.
        /// </param>
        /// <param name="IEnter">
        /// Array di NPunti che definiscono la curva intersezione (ENTRATA)
        /// </param>
        /// <param name="IExit">
        /// Array di NPunti che definiscono la curva intersezione (USCITA)
        /// </param>
        /// <returns>
        /// Array di NPunti che definiscono la curva intersezione (ENTRATA/USCITA)
        /// </returns>
        public static int  IntersezCilindroExtruded(in Point p1, in Point p2, double radius1,
                                                    in Point p3, in Point p4, in Point[] contour, int nPunti,
                                                    ref Point[] IEnter, ref Point[] IExit)
        {
            if (p1 is null || p2 is null || contour is null || nPunti <= 0)
                return (int)MathErr.MATH_ERR_DATA;


            IEnter = Enumerable.Range(0, nPunti).Select(x => new Point()).ToArray();
            IExit = Enumerable.Range(0, nPunti).Select(x => new Point()).ToArray();

            //	Asse cilindro1
            GeoLine CylAxis1 = new GeoLine(new GeoVec3f(p1.X, p1.Y, p1.Z), new GeoVec3f(p2.X, p2.Y, p2.Z));
            //	Asse superficie estrusa
            GeoLine CylAxis2 = new GeoLine(new GeoVec3f(p3.X, p3.Y, p3.Z), new GeoVec3f(p4.X, p4.Y, p4.Z));
            //	Cilindro1
            GeoCylinder Cylinder1 = new GeoCylinder(CylAxis1, radius1);
            //
            //	Sciavicco-Siciliano	pag. 29
            //
            //	Per sovrapporre l'asse z del mio riferimento all'asse di rotazione
            //	P3P4 del cilindro2 devo compiere 2 rotazioni:
            //
            //	Rotazione di un angolo alfa attorno a z
            //	
            //	Rotazione di un angolo beta attorno a y
            //
            //	Math.Cos (alfa) = rx / Math.Sqrt (rx * rx + ry * ry)
            //
            //	Math.Cos (beta) = rz / Math.Sqrt (rx * rx + ry * ry + rz * rz)
            //
            double rx = p4.X - p3.X, ry = p4.Y - p3.Y, rz = p4.Z - p3.Z;

            double dxy = Math.Sqrt(rx * rx + ry * ry);
            double dxyz = Math.Sqrt(rx * rx + ry * ry + rz * rz);
            double alfa = dxy > 0 ? FmathExt.Acos(rx / dxy) : 0;
            double beta = FmathExt.Acos(rz / dxyz);

            if (ry < 0)
                alfa = -alfa;

            //
            //	Matrice Rz rotazione angolo alfa attorno a asse z.
            //
            double[,] MatrixRz = new double[3,3];

            MatrixRz[0,0] = Math.Cos(alfa);
            MatrixRz[0,1] = -Math.Sin(alfa);
            MatrixRz[0,2] = 0.0;

            MatrixRz[1,0] = Math.Sin(alfa);
            MatrixRz[1,1] = Math.Cos(alfa);
            MatrixRz[1,2] = 0.0;

            MatrixRz[2,0] = 0.0;
            MatrixRz[2,1] = 0.0;
            MatrixRz[2,2] = 1.0;

            //
            //	Matrice Ry rotazione angolo beta attorno a asse y.
            //
            double[,] MatrixRy = new double[3,3];

            MatrixRy[0,0] = Math.Cos(beta);
            MatrixRy[0,1] = 0.0;
            MatrixRy[0,2] = Math.Sin(beta);

            MatrixRy[1,0] = 0.0;
            MatrixRy[1,1] = 1.0;
            MatrixRy[1,2] = 0.0;

            MatrixRy[2,0] = -Math.Sin(beta);
            MatrixRy[2,1] = 0.0;
            MatrixRy[2,2] = Math.Cos(beta);

            double[,] MatrixR = new double[3,3];

            //
            //	Matrice R composizione delle rotazioni alfa e beta.
            //
            //  R =Rz * Ry.
            //
            FMath.MultMatrix(MatrixRz, 3, 3,
                        MatrixRy, 3, 3,
                        ref MatrixR, 3, 3);

            double[] Vector1 = new double[3], Vector2 = new double[3], Vector1Rot = new double[3], Vector2Rot = new double[3];
            GeoVec3f iEnter = new GeoVec3f(), iExit = new GeoVec3f();
            GeoLine CurrLine = new GeoLine();

            for (int i = 0; i < nPunti; i++)
            {
                //
                //	Vector1.
                //
                Vector1[0] = contour[i].X;
                Vector1[1] = contour[i].Y;
                Vector1[2] = -dxyz / 2.0;

                //
                //	Vector2.
                //
                Vector2[0] = contour[i].X;
                Vector2[1] = contour[i].Y;
                Vector2[2] = dxyz / 2.0;

                //
                //  Vector1Rot = R * Vector1.
                //
                FMath.MultVect(MatrixR, 3, 3,
                            Vector1, 3,
                            ref Vector1Rot, 3);

                //
                //  Vector2Rot = R * Vector2.
                //
                FMath.MultVect(MatrixR, 3, 3,
                            Vector2, 3,
                            ref Vector2Rot, 3);


                Vector1Rot[0] += (p3.X + p4.X) / 2.0;
                Vector1Rot[1] += (p3.Y + p4.Y) / 2.0;
                Vector1Rot[2] += (p3.Z + p4.Z) / 2.0;

                Vector2Rot[0] += (p3.X + p4.X) / 2.0;
                Vector2Rot[1] += (p3.Y + p4.Y) / 2.0;
                Vector2Rot[2] += (p3.Z + p4.Z) / 2.0;

                CurrLine.SetValue(new GeoVec3f(Vector1Rot[0],
                                        Vector1Rot[1],
                                        Vector1Rot[2]),
                                new GeoVec3f(Vector2Rot[0],
                                        Vector2Rot[1],
                                        Vector2Rot[2]));

                //
                //	Calcolo le 2 intersezioni tra il cilindro1 e
                //	la retta per i 2 punti Vector1Rot e Vector2Rot.
                //
                if (Cylinder1.Intersect(CurrLine, ref iEnter, ref iExit))
                {
                    IEnter[i].X = iEnter.values[0];
                    IEnter[i].Y = iEnter.values[1];
                    IEnter[i].Z = iEnter.values[2];

                    IExit[i].X = iExit.values[0];
                    IExit[i].Y = iExit.values[1];
                    IExit[i].Z = iExit.values[2];
                }
                else
                {
                    if (i > 0)
                    {
                        IEnter[i].X = IEnter[i - 1].X;
                        IEnter[i].Y = IEnter[i - 1].Y;
                        IEnter[i].Z = IEnter[i - 1].Z;

                        IExit[i].X = IExit[i - 1].X;
                        IExit[i].Y = IExit[i - 1].Y;
                        IExit[i].Z = IExit[i - 1].Z;
                    }
                }
            }

            //	Il penultimo Point coincide col primo.
            //	(Serve per garantire che la curva sia chiusa).
            /*	IEnter[NPunti - 2].X	= IEnter[0].X;
                IEnter[NPunti - 2].Y	= IEnter[0].Y;
                IEnter[NPunti - 2].Z	= IEnter[0].Z;

                IExit[NPunti - 2].X		= IExit[0].X;
                IExit[NPunti - 2].Y		= IExit[0].Y;
                IExit[NPunti - 2].Z		= IExit[0].Z;

                    //	L'ultimo Point coincide col secondo.
                    //	(Serve per garantire che la curva sia chiusa).
                IEnter[NPunti - 1].X	= IEnter[1].X;
                IEnter[NPunti - 1].Y	= IEnter[1].Y;
                IEnter[NPunti - 1].Z	= IEnter[1].Z;

                IExit[NPunti - 1].X		= IExit[1].X;
                IExit[NPunti - 1].Y		= IExit[1].Y;
                IExit[NPunti - 1].Z		= IExit[1].Z;*/

            return (int)MathErr.MATH_PROC_OK;
        }

        /// <summary>
        /// Calcola la curva intersezione di 1 superficie cilindrica e di un piano.
        /// 
        ///	Cilindro 1	: asse per P1 P2, raggio Radius
        ///
        ///	Piano 2		: normale a N per P3
        ///
        /// I punti intersezione vengono calcolati intersecando il piano con le linee
        ///	di superficie del cilindro.
        /// </summary>
        /// <param name="p1">
        /// Punto sull'asse del cilindro
        /// </param>
        /// <param name="p2">
        /// Punto sull'asse del cilindro
        /// </param>
        /// <param name="radius">
        /// Raggio del cilindro
        /// </param>
        /// <param name="p3">
        /// </param>
        /// <param name="n"></param>
        /// <param name="nPunti">
        /// Numero di punti del contorno estruso.
        /// </param>
        /// <param name="intersec">
        /// Array di NPunti che definiscono la curva intersezione
        /// </param>
        /// <returns>
        /// Array di NPunti che definiscono la curva intersezione
        /// </returns>
        public static int  IntersezCilindroPiano(in Point p1, in Point p2, double radius,
                                                    in Point p3, in Point n,
                                                    int nPunti, ref Point[] intersec)
        {
            if (p1 is null || p2 is null || p3 is null || n is null || nPunti <= 0)
                return (int)MathErr.MATH_ERR_DATA;

            intersec = Enumerable.Range(0, nPunti).Select(x => new Point()).ToArray();

            //	Asse cilindro1
            GeoLine CylAxis1 = new GeoLine(new GeoVec3f(p1.X, p1.Y, p1.Z), new GeoVec3f(p2.X, p2.Y, p2.Z));
            //	Cilindro1
            GeoCylinder Cylinder1 = new GeoCylinder(CylAxis1, radius);
            //	Piano
            GeoPlane Plane2 = new GeoPlane(new GeoVec3f(n.X, n.Y, n.Z), new GeoVec3f(p3.X, p3.Y, p3.Z));

            double[] Vector1 = new double[3], Vector2 = new double[3];
            double Angle = 0.0;
            GeoVec3f iInt = new GeoVec3f();
            GeoLine CurrLine = new GeoLine();

            for (int i = 0; i < nPunti / 2; i++)
            {
                //		Angle = (i) * (2 * Math.PI) / (NPunti - 2) - Math.PI / 2.0f;
                Angle = (-i) * (2 * Math.PI) / (nPunti - 2) - Math.PI / 2.0;

                //
                //	Vector1.
                //
                Vector1[0] = p1.X + radius * Math.Cos(Angle);
                Vector1[1] = p1.Y;
                Vector1[2] = p1.Z + radius * Math.Sin(Angle);

                //
                //	Vector2.
                //
                Vector2[0] = p2.X + radius * Math.Cos(Angle);
                Vector2[1] = p2.Y;
                Vector2[2] = p2.Z + radius * Math.Sin(Angle);


                CurrLine.SetValue(new GeoVec3f(Vector1[0],
                                        Vector1[1],
                                        Vector1[2]),
                                new GeoVec3f(Vector2[0],
                                        Vector2[1],
                                        Vector2[2]));

                //
                //	Calcolo l'intersezione tra il piano Plane2 e la retta CurrLine.
                //
                if (Plane2.Intersect(CurrLine, ref iInt))
                {
                    intersec[i].X = iInt.values[0];
                    intersec[i].Y = iInt.values[1];
                    intersec[i].Z = iInt.values[2];
                }
                else
                {
                    if (i > 0)
                    {
                        intersec[i].X = intersec[i - 1].X;
                        intersec[i].Y = intersec[i - 1].Y;
                        intersec[i].Z = intersec[i - 1].Z;
                    }
                }
            }

            for (int i = nPunti / 2; i < nPunti; i++)
            {
                Angle = (nPunti - 1 + (i - 1 - nPunti / 2)) * (2 * Math.PI) / (nPunti - 2) - Math.PI / 2.0f;
                //		Angle = (NPunti - 1 - (i + 1 - NPunti / 2)) * (2 * Math.PI) / (NPunti - 2) - Math.PI / 2.0f;

                //
                //	Vector1.
                //
                Vector1[0] = p1.X + radius * Math.Cos(Angle);
                Vector1[1] = p1.Y;
                Vector1[2] = p1.Z + radius * Math.Sin(Angle);

                //
                //	Vector2.
                //
                Vector2[0] = p2.X + radius * Math.Cos(Angle);
                Vector2[1] = p2.Y;
                Vector2[2] = p2.Z + radius * Math.Sin(Angle);


                CurrLine.SetValue(new GeoVec3f(Vector1[0],
                                        Vector1[1],
                                        Vector1[2]),
                                new GeoVec3f(Vector2[0],
                                        Vector2[1],
                                        Vector2[2]));

                //
                //	Calcolo l'intersezione tra il piano Plane2 e la retta CurrLine.
                //
                if (Plane2.Intersect(CurrLine, ref iInt))
                {
                    intersec[i].X = iInt.values[0];
                    intersec[i].Y = iInt.values[1];
                    intersec[i].Z = iInt.values[2];
                }
                else
                {
                    if (i > 0)
                    {
                        intersec[i].X = intersec[i - 1].X;
                        intersec[i].Y = intersec[i - 1].Y;
                        intersec[i].Z = intersec[i - 1].Z;
                    }
                }
            }

            return (int)MathErr.MATH_PROC_OK;
        }

        
        /// <summary>
        /// Calcola la curva intersezione tra la retta per P1, P2 e il piano normale a N per P3
        /// </summary>
        /// <param name="p1">
        /// Punto sulla retta
        /// </param>
        /// <param name="p2">
        /// Punto sulla retta
        /// </param>
        /// <param name="p3"></param>
        /// <param name="n"></param>
        /// <param name="intersec">
        /// Punto di intersezione
        /// </param>
        /// <returns></returns>
        public static int IntersezRettaPiano(in Point p1, in Point p2, in Point p3, in Point n, ref Point intersec)
        {
            if (p1 is null || p2 is null || p3 is null || n is null || intersec is null)
                return (int)MathErr.MATH_ERR_DATA;

            //	Retta
            GeoLine Retta = new GeoLine(new GeoVec3f(p1.X, p1.Y, p1.Z), new GeoVec3f(p2.X, p2.Y, p2.Z));
            //	Piano
            GeoPlane Piano = new GeoPlane(new GeoVec3f(n.X, n.Y, n.Z), new GeoVec3f(p3.X, p3.Y, p3.Z));
            //	Point di intersezione
            GeoVec3f iInt = new GeoVec3f();

            //
            //	Calcolo l'intersezione tra il Piano e la Retta.
            //
            if (Piano.Intersect(Retta, ref iInt))
            {
                intersec.X = iInt.values[0];
                intersec.Y = iInt.values[1];
                intersec.Z = iInt.values[2];

                return (int)MathErr.MATH_PROC_OK;
            }

            return (int)MathErr.MATH_ERR_NO_INTERSEZ;
        }

        /// <summary>
        /// Calcola il Point intersezione I della retta per P1P2 col piano
        /// di equazione a*x + b*y + c*z + d = 0.
        /// </summary>
        /// <param name="p1">
        /// Punto sulla retta 
        /// </param>
        /// <param name="p2">
        /// Punto sulla retta 
        /// </param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <param name="i">
        /// Punto d'intersezione
        /// </param>
        /// <returns>
        /// Punto d'intersezione
        /// </returns>
        public static int  IntersezRettaPiano(Point p1, Point p2,
                                    double a, double b, double c, double d,
                                    ref Point i)
        {
            if (p1 is null || p2 is null || i is null)
                return (int)MathErr.MATH_ERR_DATA;

            //
            //	Piano z = zMath.Cost.
            //
            //	a = b = 0
            //
            //	zMath.Cost = -d / c
            //
            if (FMath.Equal(a, 0.0) && FMath.Equal(b, 0))
            {
                if (FMath.Equal(c, 0.0))
                    return (int)MathErr.MATH_ERR_DATA;

                double zcost = -d / c;

                i.X = p1.X + (p2.X - p1.X) * (zcost - p1.Z) / (p2.Z - p1.Z);
                i.Y = p1.Y + (p2.Y - p1.Y) * (zcost - p1.Z) / (p2.Z - p1.Z);
                i.Z = zcost;

                return (int)MathErr.MATH_PROC_OK;
            }

            //
            //	Piano x = xMath.Cost.
            //
            //	b = c = 0
            //
            //	xMath.Cost = -d / a
            //
            if (FMath.Equal(b, 0.0) && FMath.Equal(c, 0.0))
            {
                if (FMath.Equal(a, 0.0))
                    return (int)MathErr.MATH_ERR_DATA;

                double xcost = -d / a;

                i.X = xcost;
                i.Y = p1.Y + (p2.Y - p1.Y) * (xcost - p1.X) / (p2.X - p1.X);
                i.Z = p1.Z + (p2.Z - p1.Z) * (xcost - p1.X) / (p2.X - p1.X);

                return (int)MathErr.MATH_PROC_OK;
            }

            //
            //	Piano y = yMath.Cost.
            //
            //	a = c = 0
            //
            //	yMath.Cost = -d / b
            //
            if (FMath.Equal(a, 0.0) && FMath.Equal(c, 0.0))
            {
                if (FMath.Equal(b, 0.0))
                    return (int)MathErr.MATH_ERR_DATA;

                double ycost = -d / b;

                i.X = p1.X + (p2.X - p1.X) * (ycost - p1.Y) / (p2.Y - p1.Y);
                i.Y = ycost;
                i.Z = p1.Z + (p2.Z - p1.Z) * (ycost - p1.Y) / (p2.Y - p1.Y);

                return (int)MathErr.MATH_PROC_OK;
            }

            //
            //	Al momento non sono gestite intersezioni con piani non paralleli
            //	ai 3 piani cartesiani.
            //
            return (int)MathErr.MATH_ERR_DATA;
        }

        
        /// <summary>
        /// Converts LEAD/CUT in Vectors.
        /// </summary>
        /// <param name="codicePrf">
        /// </param>
        /// <param name="sA"></param>
        /// <param name="tA"></param>
        /// <param name="sB"></param>
        /// <param name="tB"></param>
        /// <param name="piano"></param>
        /// <param name="reverseSignVector"></param>
        /// <param name="reverseRadiusOnSideB"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="angX"></param>
        /// <param name="angZ"></param>
        /// <param name="depth"></param>
        /// <param name="channelDown"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <param name="posZ"></param>
        /// <param name="vecX"></param>
        /// <param name="vecY"></param>
        /// <param name="vecZ"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public static bool  GetVector(char codicePrf, double sA, double tA, double sB, double tB, int piano,   //	IN
            bool reverseSignVector, bool reverseRadiusOnSideB,                      //	IN
            double x, double y, double angX, double angZ, double depth,             //	IN
            bool channelDown,                                                       //	IN
            ref double posX, ref double posY, ref double posZ,                            //	OUT
            ref double vecX, ref double vecY, ref double vecZ, ref double r)                 //	OUT
        {
            if (codicePrf == 0)
                return false;

            double SinAngZ = Math.Sin(FMath.FAT_RAD * angZ);

            if (codicePrf == 'R')
            {
                double RaggioTubo = sA / 2;
                double AlfaTubo = y * 360.0 / (2 * Math.PI * RaggioTubo);
                double YTubo = RaggioTubo - RaggioTubo * Math.Cos(FMath.FAT_RAD * (AlfaTubo - 90.0));
                double ZTubo = RaggioTubo + RaggioTubo * Math.Sin(FMath.FAT_RAD * (AlfaTubo - 90.0));

                posX = x;
                posY = YTubo;
                posZ = ZTubo;

                if (true)
                {
                    //
                    //	W(wx,wy,wz) è il vettore orientamento rispetto al sistema di riferimento
                    //	x'y'z' con z' normale al profilo e x'y' nel piano tangente
                    //
                    double wx = Math.Sin(FMath.FAT_RAD * angZ) * Math.Cos(FMath.FAT_RAD * (angX - 90.0)),
                        wy = Math.Sin(FMath.FAT_RAD * angZ) * Math.Sin(FMath.FAT_RAD * (angX - 90.0)),
                        wz = Math.Cos(FMath.FAT_RAD * angZ);

                    //
                    //	Per passare dal sistema di riferimento x'y'z' a quello xyz del workpiece,
                    //	devo applicare un arotazione attorno all'asse x pari a 180 - AlfaTubo
                    //
                    double AngRotx = 180.0 - AlfaTubo;
                    vecX = wx;
                    vecY = wy * Math.Cos(FMath.FAT_RAD * AngRotx) - wz * Math.Sin(FMath.FAT_RAD * AngRotx);
                    vecZ = wy * Math.Sin(FMath.FAT_RAD * AngRotx) + wz * Math.Cos(FMath.FAT_RAD * AngRotx);
                }
                else
                {
                    vecX = 0.0;
                    vecY = -Math.Cos(FMath.FAT_RAD * (AlfaTubo - 90.0 + angZ));
                    vecZ = Math.Sin(FMath.FAT_RAD * (AlfaTubo - 90.0 + angZ));
                }
            }
            else
            {
                if (piano == (int)Piano.A)
                {
                    posX = x;
                    posY = 0.0 + depth;
                    posZ = y;

                    vecX = -SinAngZ * Math.Cos(FMath.FAT_RAD * angX);
                    vecY = -Math.Cos(FMath.FAT_RAD * angZ);
                    vecZ = SinAngZ * Math.Sin(FMath.FAT_RAD * angX);
                }
                else if (piano == (int)Piano.B && codicePrf != 'L')
                {
                    posX = x;
                    posY = sA - depth;
                    posZ = y;

                    vecX = -SinAngZ * Math.Cos(FMath.FAT_RAD * angX);
                    vecY = Math.Cos(FMath.FAT_RAD * angZ);
                    vecZ = SinAngZ * Math.Sin(FMath.FAT_RAD * angX);

                    if (reverseRadiusOnSideB)
                        r = -r;
                }
                else if (piano == (int)Piano.C || (piano == (int)Piano.B && codicePrf == 'L'))
                {
                    posX = x;
                    posY = y;

                    if (codicePrf == 'I')
                        posZ = sB / 2 + tA / 2;
                    else if (codicePrf == 'U')
                    {
                        if (channelDown)
                            posZ = sB;
                        else
                            posZ = tA;
                    }
                    else if (codicePrf == 'Q')
                        posZ = sB;
                    else if (codicePrf == 'L')
                        posZ = tB;
                    else if (codicePrf == 'F' || codicePrf == 'P')
                        posZ = tA;
                    else
                        posZ = 0.0;

                    posZ -= depth;

                    vecX = -SinAngZ * Math.Cos(FMath.FAT_RAD * angX);
                    if (codicePrf == 'U' && channelDown)
                        vecY = SinAngZ * Math.Sin(FMath.FAT_RAD * angX);
                    else
                        vecY = -SinAngZ * Math.Sin(FMath.FAT_RAD * angX);
                    vecZ = Math.Cos(FMath.FAT_RAD * angZ);
                }
                else if (piano == (int)Piano.D)
                {
                    posX = x;
                    posY = y;
                    posZ = 0.0 + depth;

                    vecX = -SinAngZ * Math.Cos(FMath.FAT_RAD * angX);
                    vecY = -SinAngZ * Math.Sin(FMath.FAT_RAD * angX);
                    vecZ = -Math.Cos(FMath.FAT_RAD * angZ);
                }
            }

            if (reverseSignVector)
            {
                vecX = -vecX;
                vecY = -vecY;
            }

            return true;
        }

        /// <summary>
        /// 	Converts LEAD/CUT in Vectors.
        /// </summary>
        /// <param name="codicePrf"></param>
        /// <param name="sA"></param>
        /// <param name="tA"></param>
        /// <param name="sB"></param>
        /// <param name="tB"></param>
        /// <param name="piano"></param>
        /// <param name="reverseSignVector"></param>
        /// <param name="reverseRadiusOnSideB"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="angX"></param>
        /// <param name="angZ"></param>
        /// <param name="depth"></param>
        /// <param name="channelDown"></param>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <param name="posZ"></param>
        /// <param name="vecX"></param>
        /// <param name="vecY"></param>
        /// <param name="vecZ"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        //
        public static bool  GetVector(uint codicePrf, double sA, double tA, double sB, double tB, int piano,  //	IN
            bool reverseSignVector, bool reverseRadiusOnSideB,                      //	IN
            double x, double y, double angX, double angZ, double depth,             //	IN
            bool channelDown,                                                       //	IN
            ref double posX, ref double posY, ref double posZ,                            //	OUT
            ref double vecX, ref double vecY, ref double vecZ, ref double r)                 //	OUT
        {
            char CharCodPrf = ' ';

            switch (codicePrf)
            {
                case (int)CodPRF.PRF_L:
                    CharCodPrf = 'L';
                    break;
                case (int)CodPRF.PRF_B:
                    CharCodPrf = 'B';
                    break;
                case (int)CodPRF.PRF_I:
                    CharCodPrf = 'I';
                    break;
                case (int)CodPRF.PRF_W:
                    CharCodPrf = 'W';
                    break;
                case (int)CodPRF.PRF_D:
                    CharCodPrf = 'D';
                    break;
                case (int)CodPRF.PRF_U:
                    CharCodPrf = 'U';
                    break;
                case (int)CodPRF.PRF_Q:
                    CharCodPrf = 'Q';
                    break;
                case (int)CodPRF.PRF_R:
                    CharCodPrf = 'R';
                    break;
                case (int)CodPRF.PRF_O:
                    CharCodPrf = 'O';
                    break;
                case (int)CodPRF.PRF_F:
                    CharCodPrf = 'F';
                    break;
                case (int)CodPRF.PRF_P:
                    CharCodPrf = 'P';
                    break;
            }

            return GetVector(CharCodPrf, sA, tA, sB, tB, piano, reverseSignVector, reverseRadiusOnSideB,
                x, y, angX, angZ, depth,
                channelDown,
                ref posX, ref posY, ref posZ,
                ref vecX, ref vecY, ref vecZ, ref r);
        }


        /// <summary>
        /// Rende il valore assoluto di X dato il valore di X riferito
        /// ad un preciso riferimento.
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="x"></param>
        /// <param name="piano"></param>
        /// <param name="riferimento"></param>
        /// <returns></returns>

        public static double  GetXFromRef(Piece piece, double x, int piano, int riferimento)
        {
            double XAssoluta = 0.0;

            if (riferimento == (int)References.BOTTOM_LEFT || riferimento == (int)References.MIDDLE_LEFT || riferimento == (int)References.MIDDLE_LEFT_2 ||
                riferimento == (int)References.MIDDLE_LEFT_C || riferimento == (int)References.TOP_LEFT)
                XAssoluta = x;
            else if (riferimento == (int)References.BOTTOM_RIGHT || riferimento == (int)References.MIDDLE_RIGHT || riferimento == (int)References.MIDDLE_RIGHT_2 ||
                riferimento == (int)References.MIDDLE_RIGHT_C || riferimento == (int)References.TOP_RIGHT)
                XAssoluta = piece.Lp + x;

            return XAssoluta;
        }


        /// <summary>
        ///  Rende il valore assoluto di Y dato il valore di Y riferito
        ///  ad un preciso riferimento
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="y"></param>
        /// <param name="piano"></param>
        /// <param name="riferimento"></param>
        /// <returns></returns>
        public static double  GetYFromRef(in Piece piece, double y, int piano, int riferimento)
        {
            double YAssoluta = 0.0;

            if (riferimento == (int)References.BOTTOM_LEFT || riferimento == (int)References.BOTTOM_RIGHT)
            {
                YAssoluta = y;
            }
            else if (riferimento == (int)References.MIDDLE_LEFT || riferimento == (int)References.MIDDLE_RIGHT ||
                riferimento == (int)References.MIDDLE_LEFT_2 || riferimento == (int)References.MIDDLE_RIGHT_2 ||
                riferimento == (int)References.MIDDLE_LEFT_C || riferimento == (int)References.MIDDLE_RIGHT_C)
            {
                if (piece.Prf.CodPrf == 'U' || piece.Prf.CodPrf == 'O')
                {
                    if (piano == (int)Piano.A)
                    {
                        if (riferimento == (int)References.MIDDLE_LEFT_C || riferimento == (int)References.MIDDLE_RIGHT_C)
                            YAssoluta = piece.Prf.Ha / 2 + y;
                        else
                            YAssoluta = piece.Prf.Tc + y;
                    }
                    else if (piano == (int)Piano.B)
                    {
                        if (riferimento == (int)References.MIDDLE_LEFT_C || riferimento == (int)References.MIDDLE_RIGHT_C)
                            YAssoluta = piece.Prf.Hb / 2 + y;
                        else
                            YAssoluta = piece.Prf.Tc + y;
                    }
                    else if (piano == (int)Piano.C || piano == (int)Piano.D)
                        YAssoluta = piece.Prf.Hc / 2 + y;
                }
                else
                {
                    if (piano == (int)Piano.A)
                        YAssoluta = piece.Prf.Ha / 2 - piece.Prf.Dsa + y;
                    else if (piano == (int)Piano.B)
                        YAssoluta = piece.Prf.Hb / 2 - piece.Prf.Dsb + y;
                    else if (piano == (int)Piano.C || piano == (int)Piano.D)
                        YAssoluta = piece.Prf.Hc / 2 + y;
                }
            }
            else if (riferimento == (int)References.TOP_LEFT || riferimento == (int)References.TOP_RIGHT)
            {
                if (piano == (int)Piano.A)
                    YAssoluta = piece.Prf.Ha + y;
                else if (piano == (int)Piano.B)
                    YAssoluta = piece.Prf.Hb + y;
                else if (piano == (int)Piano.C || piano == (int)Piano.D)
                {
                    if (piece.Prf.CodPrf == 'O')
                        YAssoluta = (piece.Prf.Hc + 2 * (piece.Prf.Hc_left - piece.Prf.Ta)) + y;
                    else
                        YAssoluta = piece.Prf.Hc + y;
                }
            }

            return YAssoluta;
        }


        /// <summary>
        ///  Rende il valore di X rispetto al riferimento passato 
        ///  dato il valore di X assoluto
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="x"></param>
        /// <param name="piano"></param>
        /// <param name="riferimento"></param>
        /// <returns></returns>
        public static double  GetXRefFromX(in Piece piece, double x, int piano, int riferimento)
        {
            double XRef = 0.0;

            if (riferimento == (int)References.BOTTOM_LEFT || riferimento == (int)References.MIDDLE_LEFT ||
                riferimento == (int)References.TOP_LEFT || riferimento == (int)References.MIDDLE_LEFT_C)
                XRef = x;
            else if (riferimento == (int)References.BOTTOM_RIGHT || riferimento == (int)References.MIDDLE_RIGHT ||
                riferimento == (int)References.TOP_RIGHT || riferimento == (int)References.MIDDLE_RIGHT_C)
                XRef = x - piece.Lp;

            return XRef;
        }

        /// <summary>
        ///  Rende il valore di Y rispetto al riferimento passato 
        ///  dato il valore di Y assoluto
        /// </summary>
        /// <param name="piece"></param>
        /// <param name="y"></param>
        /// <param name="piano"></param>
        /// <param name="riferimento"></param>
        /// <returns></returns>
        public static double  GetYRefFromY(in Piece piece, double y, int piano, int riferimento)
        {
            double YRef = 0.0;

            if (riferimento == (int)References.BOTTOM_LEFT || riferimento == (int)References.BOTTOM_RIGHT)
            {
                YRef = y;
            }
            else if (riferimento == (int)References.MIDDLE_LEFT || riferimento == (int)References.MIDDLE_RIGHT ||
                riferimento == (int)References.MIDDLE_LEFT_2 || riferimento == (int)References.MIDDLE_RIGHT_2 ||
                riferimento == (int)References.MIDDLE_LEFT_C || riferimento == (int)References.MIDDLE_RIGHT_C)
            {
                if (piano == (int)Piano.A)
                {
                    if (piece.Prf.CodPrf == 'U' || piece.Prf.CodPrf == 'O')
                        YRef = y - piece.Prf.Tc;
                    else
                        YRef = y - piece.Prf.Ha / 2 + piece.Prf.Dsa;
                }
                else if (piano == (int)Piano.B)
                {
                    if (piece.Prf.CodPrf == 'U' || piece.Prf.CodPrf == 'O')
                        YRef = y - piece.Prf.Tc;
                    else
                        YRef = y - piece.Prf.Hb / 2 + piece.Prf.Dsb;
                }
                else if (piano == (int)Piano.C || piano == (int)Piano.D)
                    YRef = y - piece.Prf.Hc / 2;
            }
            else if (riferimento == (int)References.TOP_LEFT || riferimento == (int)References.TOP_RIGHT)
            {
                if (piano == (int)Piano.A)
                    YRef = y - piece.Prf.Ha;
                else if (piano == (int)Piano.B)
                    YRef = y - piece.Prf.Hb;
                else if (piano == (int)Piano.C || piano == (int)Piano.D)
                {
                    if (piece.Prf.CodPrf == 'O')
                        YRef = y - (piece.Prf.Hc + 2 * (piece.Prf.Hc_left - piece.Prf.Ta));
                    else if (piece.Prf.CodPrf == 'R')
                        YRef = y;
                    else
                        YRef = y - piece.Prf.Hc;
                }
            }

            return YRef;
        }
    
        //
        //  Funzione Acos che satura a |1| il valore in modo che non possa essere reso Nan
        //
        public static double Acos(double v)
        {
            double normalized = v;

            if (normalized > 1)
                normalized = 1;
            else if (normalized < -1)
                normalized = -1;

            return Math.Acos(normalized);
        }
    }
}
