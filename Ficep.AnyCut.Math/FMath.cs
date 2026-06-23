using static Ficep.AnyCut.Mathematics.Errors;

namespace Ficep.AnyCut.Mathematics
{
    public static class FMath
    {  
        readonly public static double  FAT_RAD = Math.PI / 180;
        readonly public static double FAT_DEG = 180 / Math.PI;


        /// <summary>
        /// Arrotondamento di un double con un numero di decimali pari a prec.
        /// </summary>
        /// <param name="x">
        /// Numero da arrotondare
        /// </param>
        /// <param name="prec">
        /// Numero di decimali a cui arrotondare
        /// </param>
        /// <returns></returns>
        public static double Round(double x, int prec)
        {
            /*
            double power = 1.0;
            if (prec > 0)
                power = Math.Pow(10, prec);
            else if (prec < 0)
                power = 1 / Math.Pow(10, prec);

            if (x > 0)
                x = Math.Floor(x * power + 0.5) / power;
            else if (x < 0)
                x = Math.Ceiling(x * power - 0.5) / power;

            if (x == -0)
                x = 0;

            return x;
            */

            return Math.Round(x, prec);
        }

        /// <summary>
        ///  Determina l'uguaglianza di 2 double a meno della tolleranza
        ///  di precisione.
        /// </summary>
        /// <param name="value1">
        ///  Valore del primo double
        /// </param>
        /// <param name="value2">
        ///  Valore del secondo double
        /// </param>
        /// <param name="toll">
        ///  Tolleranza di precisione
        /// </param>
        /// <returns>
        ///  true o false se l'uguaglianza è soddisfatta o meno 
        /// </returns>
        public static bool Equal(double value1, double value2, double toll = 0.001)
        {
            return Math.Abs(value1 - value2) < toll;
        }

        /// <summary>
        ///  Determina l'uguaglianza di 2 double a meno del quadrato
        ///  della tolleranza di precisione.
        /// </summary>
        /// <param name="value1">
        ///  Valore del primo double
        /// </param>
        /// <param name="value2">
        ///  Valore del secondo double
        /// </param>
        /// <param name="toll">
        ///  Tolleranza di precisione
        /// </param>
        /// <returns></returns>
        public static bool SquareEqual(double value1, double value2, double toll = 0.001f)
        {
            return Math.Abs(value1 - value2) < toll * toll;
        }

        /// <summary>
        /// Calcola la distanza tra 2 punti.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns>
        /// true o false se l'uguaglianza è soddisfatta o meno 
        /// </returns>
        public static double Distance(in Point first, in Point second)
        {
            double dax, day, daz;

            dax = first.X - second.X;
            day = first.Y - second.Y;
            daz = first.Z - second.Z;

            return Math.Sqrt(dax * dax + day * day + daz * daz);
        }

        /// <summary>
        ///  Calcola la distanza tra 2 punti.
        /// </summary>
        /// <param name="x1">
        ///  Coordinata x del punto 1
        /// </param>
        /// <param name="y1">
        ///  Coordinata y del punto 1
        /// </param>
        /// <param name="z1">
        ///  Coordinata z del punto 1
        /// </param>
        /// <param name="x2">
        ///  Coordinata x del punto 2
        /// </param>
        /// <param name="y2">
        ///  Coordinata y del punto 2
        /// </param>
        /// <param name="z2">
        ///  Coordinata z del punto 2
        /// </param>
        /// <returns>
        ///  La distanza tra i due punti 
        /// </returns>
        public static double Distance(double x1, double y1, double z1,
                                  double x2, double y2, double z2)
        {
            double dax, day, daz;

            dax = x1 - x2;
            day = y1 - y2;
            daz = z1 - z2;

            return Math.Sqrt(dax * dax + day * day + daz * daz);
        }

        /// <summary>
        ///  Calcola il quadrato della distanza tra 2 punti.
        /// </summary>
        /// <param name="first">
        ///  Primo punto
        /// </param>
        /// <param name="second">
        ///  Secondo punto
        /// </param>
        /// <returns>
        ///  Il quadrato della distanza dei due punti
        /// </returns>
        public static double SquareDistance(in Point first, in Point second)
        {
            double dax, day, daz;

            dax = first.X - second.X;
            day = first.Y - second.Y;
            daz = first.Z - second.Z;

            return (dax * dax + day * day + daz * daz);
        }

        /// <summary>
        ///  Calcola il quadrato della distanza tra 2 punti.
        /// </summary>
        /// <param name="x1">
        ///  Coordinata x del punto 1
        /// </param>
        /// <param name="y1">
        ///  Coordinata y del punto 1
        /// </param>
        /// <param name="z1">
        ///  Coordinata z del punto 1
        /// </param>
        /// <param name="x2">
        ///  Coordinata x del punto 2
        /// </param>
        /// <param name="y2">
        ///  Coordinata y del punto 2
        /// </param>
        /// <param name="z2">
        ///  Coordinata z del punto 2
        /// </param>
        /// <returns>
        ///  Il quadrato della distanza tra i due punti
        /// </returns>
        public static double SquareDistance(double x1, double y1, double z1, double x2, double y2, double z2)
        {
            double dax, day, daz;

            dax = x1 - x2;
            day = y1 - y2;
            daz = z1 - z2;

            return (dax * dax + day * day + daz * daz);
        }

        /// <summary>
        ///  Calcola l'area del triangolo formato da 3 punti (si suppone
        ///  che i 3 punti appartengano al piano XY, diversamente l'angolo
        ///  e' quello formato dalle proiezioni dei 3 punti sul piano XY).
        ///  Il segno e' positivo se l'angolo descritto dai 3 punti e' antiorario.
        /// </summary>
        /// <param name="first">
        ///  Primo punto
        /// </param>
        /// <param name="second">
        ///  Secondo punto
        /// </param>
        /// <param name="third">
        ///  Terzo punto
        /// </param>
        /// <returns>
        ///  L'area del triangolo
        /// </returns>
        public static double AreaTriangolo(in Point first, in Point second, in Point third)
        {
            double Area;

            Area = first.X * second.Y - second.X * first.Y +
                  second.X * third.Y - third.X * second.Y +
                  third.X * first.Y - first.X * third.Y;

            return Area;
        }

        /// <summary>
        ///  Calcola il segno dell'angolo formato da 3 punti (si suppone
        ///  che i 3 punti appartengano al piano XY, diversamente l'angolo
        ///  e' quello formato dalle proiezioni dei 3 punti sul piano XY).
        ///  Il segno e' positivo se l'angolo e' antiorario.
        /// </summary>
        /// <param name="first">
        ///  Primo punto
        /// </param>
        /// <param name="second">
        ///  Secondo punto
        /// </param>
        /// <param name="third">
        ///  Terzo punto
        /// </param>
        /// <returns>
        ///  true se il segno è positivo, false altrimenti
        /// </returns>
        public static bool SignAngolo(in Point first, in Point second, in Point third)
        {
            double det;

            det = first.X * second.Y - second.X * first.Y +
                  second.X * third.Y - third.X * second.Y +
                  third.X * first.Y - first.X * third.Y;

            return det >= 0;
        }

        /// <summary>
        ///  Calcola il segno dell'angolo formato da 3 punti (si suppone
        ///  che i 3 punti appartengano al piano XY, diversamente l'angolo
        ///  e' quello formato dalle proiezioni dei 3 punti sul piano XY).
        ///  Il segno e' positivo se l'angolo e' antiorario.
        /// </summary>
        /// <param name="x1">
        ///  Coordinata x del punto 1
        /// </param>
        /// <param name="y1">
        ///  Coordinata y del punto 1
        /// </param>
        /// <param name="x2">
        ///  Coordinata z del punto 1
        /// </param>
        /// <param name="y2">
        ///  Coordinata x del punto 2
        /// </param>
        /// <param name="x3">
        ///  Coordinata y del punto 2
        /// </param>
        /// <param name="y3">
        ///  Coordinata z del punto 2
        /// </param>
        /// <returns>
        ///  true se il segno è positivo, false altrimenti
        /// </returns>
        public static bool SignAngolo(double x1, double y1, double x2, double y2, double x3, double y3)
        {
            double det;

            det = x1 * y2 - x2 * y1 + x2 * y3 - x3 * y2 + x3 * y1 - x1 * y3;

            return det >= 0;
        }

        /// <summary>
        ///  Calcola l'angolo definito nell'angolo giro dalla semiretta PO
        ///	 dove P(x,y) e' il punto nell'angolo giro e O(xo,y0) e' il
        ///	 centro.
        /// </summary>
        /// <param name="x">
        ///  Coordinata x punto P
        /// </param>
        /// <param name="y">
        ///  Coordinata y punto P
        /// </param>
        /// <param name="xo">
        ///  Coordinata x punto O
        /// </param>
        /// <param name="yo">
        ///  Coordinata y punto O
        /// </param>
        /// <param name="quadrante">
        ///  Numero del quadrante in cui si trova la semiretta PO
        /// </param>
        /// <returns>
        ///  Angolo della semiretta
        /// </returns>
        public static double GetAngQuadrante(double x, double y, double xo, double yo, int quadrante)
        {
            double Ang = 0;

            if (quadrante == 1)
            {
                if (Equal(x, xo))
                {
                    if (!Equal(y, yo))
                        Ang = 0.5 * Math.PI;
                    else
                        Ang = 0;
                }
                else
                    Ang = Math.Abs(Math.Atan((y - yo) / (x - xo)));
            }
            else if (quadrante == 2)
            {
                if (Equal(y, yo))
                {
                    if (!Equal(x, xo))
                        Ang = Math.PI;
                    else
                        Ang = 0;
                }
                else
                    Ang = 0.5f * Math.PI + Math.Abs(Math.Atan((x - xo) / (y - yo)));
            }
            else if (quadrante == 3)
            {
                if (Equal(x, xo))
                {
                    if (!Equal(y, yo))
                        Ang = 1.5 * Math.PI;
                    else
                        Ang = 0;
                }
                else
                    Ang = Math.PI + Math.Abs(Math.Atan((y - yo) / (x - xo)));
            }
            else if (quadrante == 4)
            {
                if (Equal(y, yo))
                {
                    if (!Equal(x, xo))
                        Ang = 2 * Math.PI;
                    else
                        Ang = 0;
                }
                else
                    Ang = 1.5f * Math.PI + Math.Abs(Math.Atan((x - xo) / (y - yo)));
            }

            return Ang;
        }

        /// <summary>
        ///  Calcola il valore in modulo (RAD) dell'angolo formato da 3 punti 
        ///  (si suppone che i 3 punti appartengano al piano XY, 
        ///  diversamente l'angolo e' quello formato dalle proiezioni 
        ///  dei 3 punti sul piano XY).
        ///	 L'angolo e' sempre minore o uguale a PI_GRECO.
        /// </summary>
        /// <param name="first">
        ///  Primo punto
        /// </param>
        /// <param name="second">
        ///  Secondo punto
        /// </param>
        /// <param name="third">
        ///  Terzo punto
        /// </param>
        /// <returns>
        ///  Angolo formato dai 3 punti
        /// </returns>
        public static double GetAngolo(in Point first, in Point second, in Point third)
        {
            double AbsAngolo;
            double AbsAng12;
            double AbsAng32;

            int Quadrante1 = 0;
            int Quadrante3 = 0;

            //
            //	Quadrante1
            //
            if (first.X > second.X)
            {
                if (first.Y >= second.Y)
                    Quadrante1 = 1;
                else
                    Quadrante1 = 4;
            }
            else if (first.X < second.X)
            {
                if (first.Y >= second.Y)
                    Quadrante1 = 2;
                else
                    Quadrante1 = 3;
            }
            else
            {
                if (first.Y >= second.Y)
                    Quadrante1 = 1;
                else
                    Quadrante1 = 3;
            }

            //
            //	Quadrante3
            //
            if (third.X > second.X)
            {
                if (third.Y >= second.Y)
                    Quadrante3 = 1;
                else
                    Quadrante3 = 4;
            }
            else if (third.X < second.X)
            {
                if (third.Y >= second.Y)
                    Quadrante3 = 2;
                else
                    Quadrante3 = 3;
            }
            else
            {
                if (third.Y >= second.Y)
                    Quadrante3 = 1;
                else
                    Quadrante3 = 3;
            }
            //
            //	Calcolo l'angolo 12
            //
            AbsAng12 = GetAngQuadrante(first.X, first.Y, second.X, second.Y, Quadrante1);
            //
            //	Calcolo l'angolo 32
            //
            AbsAng32 = GetAngQuadrante(third.X, third.Y, second.X, second.Y, Quadrante3);

            AbsAngolo = Math.Abs(AbsAng12 - AbsAng32);

            if (AbsAngolo > Math.PI)
                AbsAngolo = 2 * Math.PI - AbsAngolo;

            return (AbsAngolo);
        }

        /// <summary>
        ///  Calcola il valore in modulo (RAD) dell'angolo formato da 3 punti 
        ///  (si suppone che i 3 punti appartengano al piano XY, 
        ///  diversamente l'angolo e' quello formato dalle proiezioni 
        ///  dei 3 punti sul piano XY).
        ///	 L'angolo e' sempre minore o uguale a PI_GRECO.
        /// </summary>
        /// <param name="x1">
        ///  Coordinata x del punto 1
        /// </param>
        /// <param name="y1">
        ///  Coordinata y del punto 1
        /// </param>
        /// <param name="x2">
        ///  Coordinata x del punto 2
        /// </param>
        /// <param name="y2">
        ///  Coordinata y del punto 2
        /// </param>
        /// <param name="x3">
        ///  Coordinata x del punto 3
        /// </param>
        /// <param name="y3">
        ///  Coordinata y del punto 3
        /// </param>
        /// <returns>
        ///  Angolo formato dai 3 punti
        /// </returns>
        public static double GetAngolo(double x1, double y1, double x2, double y2,
                             double x3, double y3)
        {
            double AbsAngolo;
            double AbsAng12;
            double AbsAng32;

            int Quadrante1 = 0;
            int Quadrante3 = 0;

            //
            //	Quadrante1
            //
            if (x1 > x2)
            {
                if (y1 >= y2)
                    Quadrante1 = 1;
                else
                    Quadrante1 = 4;
            }
            else if (x1 < x2)
            {
                if (y1 >= y2)
                    Quadrante1 = 2;
                else
                    Quadrante1 = 3;
            }
            else
            {
                if (y1 >= y2)
                    Quadrante1 = 1;
                else
                    Quadrante1 = 3;
            }

            //
            //	Quadrante3
            //
            if (x3 > x2)
            {
                if (y3 >= y2)
                    Quadrante3 = 1;
                else
                    Quadrante3 = 4;
            }
            else if (x3 < x2)
            {
                if (y3 >= y2)
                    Quadrante3 = 2;
                else
                    Quadrante3 = 3;
            }
            else
            {
                if (y3 >= y2)
                    Quadrante3 = 1;
                else
                    Quadrante3 = 3;
            }
            //
            //	Calcolo l'angolo 12
            //
            AbsAng12 = GetAngQuadrante(x1, y1, x2, y2, Quadrante1);
            //
            //	Calcolo l'angolo 32
            //
            AbsAng32 = GetAngQuadrante(x3, y3, x2, y2, Quadrante3);

            AbsAngolo = Math.Abs(AbsAng12 - AbsAng32);

            if (AbsAngolo > Math.PI)
                AbsAngolo = 2 * Math.PI - AbsAngolo;

            return (AbsAngolo);
        }

        /// <summary>
        ///  Calcola il valore con segno (gradi) dell'angolo formato da 3 punti 
        ///  (si suppone che i 3 punti appartengano al piano XY, 
        ///  diversamente l'angolo e' quello formato dalle proiezioni 
        ///  dei 3 punti sul piano XY).
        /// </summary>
        /// <param name="x1">
        ///  Coordinata x del punto 1
        /// </param>
        /// <param name="y1">
        ///  Coordinata y del punto 1
        /// </param>
        /// <param name="x2">
        ///  Coordinata x del punto 2
        /// </param>
        /// <param name="y2">
        ///  Coordinata y del punto 2
        /// </param>
        /// <param name="x3">
        ///  Coordinata x del punto 3
        /// </param>
        /// <param name="y3">
        ///  Coordinata y del punto 3
        /// </param>
        /// <param name="orario">
        ///  true se l'angolo è orario, false altrimenti
        /// </param>
        /// <param name="noSx"></param>
        /// <returns>
        ///  Angolo con senso orario o antiorario
        /// </returns>
        public static double GetSignedAngolo(double x1, double y1, double x2, double y2,
                             double x3, double y3, bool orario, bool noSx)
        {
            double Angolo;
            double AbsAngolo;
            bool AngOrario;

            //
            //  Calcolo l'angolo in modulo.
            //
            AbsAngolo = GetAngolo(x1, y1, x2, y2, x3, y3) / FAT_RAD;
            
            //
            //  Calcolo il segno dell'angolo.
            //
            AngOrario = !SignAngolo(x1, y1, x2, y2, x3, y3);
            if (noSx)
                AngOrario = !AngOrario;

            if (orario == AngOrario)
                Angolo = 360 - AbsAngolo;
            else
                Angolo = AbsAngolo;

            if (orario)
                Angolo = -Angolo;

            return (Angolo);
        }

        /// <summary>
        ///  Calcola il valore con segno (gradi) dell'angolo formato da 3 punti 
        ///  (si suppone che i 3 punti appartengano al piano XY, 
        ///  diversamente l'angolo e' quello formato dalle proiezioni 
        ///  dei 3 punti sul piano XY).
        /// </summary>
        /// <param name="x1">
        ///  Coordinata x del punto 1
        /// </param>
        /// <param name="y1">
        ///  Coordinata y del punto 1
        /// </param>
        /// <param name="x2">
        ///  Coordinata x del punto 2
        /// </param>
        /// <param name="y2">
        ///  Coordinata y del punto 2
        /// </param>
        /// <param name="x3">
        ///  Coordinata x del punto 3
        /// </param>
        /// <param name="y3">
        ///  Coordinata y del punto 3
        /// </param>
        /// <returns>
        ///  Valore assoluto dell'angolo
        /// </returns>
        public static double GetAbsAngolo(double x1, double y1, double x2, double y2,
                             double x3, double y3)
        {
            double Angolo;
            double Ang12;
            double Ang23;

            //
            //  Calcolo l'angolo in RAD formato da 12Verticale.
            //
            if (Equal(y1, y2))
                Ang12 = x1 >= x2 ? 0 : Math.PI;
            else
            {
                if (y1 > y2)
                {
                    if (x1 >= x2)
                        Ang12 = Math.Abs(Math.Atan((y1 - y2) / (x1 - x2)));
                    else
                        Ang12 = 0.5f * Math.PI + Math.Abs(Math.Atan((x1 - x2) / (y1 - y2)));
                }
                else
                {
                    if (x1 >= x2)
                        Ang12 = 1.5f * Math.PI + Math.Abs(Math.Atan((x1 - x2) / (y1 - y2)));
                    else
                        Ang12 = 1 * Math.PI + Math.Abs(Math.Atan((y1 - y2) / (x1 - x2)));
                }
            }

            //
            //  Calcolo l'angolo in RAD formato da 32Verticale.
            //
            if (Equal(y3, y2))
                Ang23 = x3 >= x2 ? 0 : Math.PI;
            else
            {
                if (y3 > y2)
                {
                    if (x3 >= x2)
                        Ang23 = Math.Abs(Math.Atan((y3 - y2) / (x3 - x2)));
                    else
                        Ang23 = 0.5f * Math.PI + Math.Abs(Math.Atan((x3 - x2) / (y3 - y2)));
                }
                else
                {
                    if (x3 >= x2)
                        Ang23 = 1.5f * Math.PI + Math.Abs(Math.Atan((x3 - x2) / (y3 - y2)));
                    else
                        Ang23 = 1 * Math.PI + Math.Abs(Math.Atan((y3 - y2) / (x3 - x2)));
                }
            }

            Angolo = Math.Abs(Ang12 - Ang23);
            return (Angolo);
        }

        /// <summary>
        ///  Calcolo il centro di una circonferenza sul piano XY di cui si
        ///  conoscono 2 punti e il raggio con segno. Tra i
        ///  2 centri possibili viene scelto quello corrispondente
        ///  a un arco che sottende un angolo acuto.
        /// </summary>
        /// <param name="point1">
        ///  Primo punto
        /// </param>
        /// <param name="point2">
        ///  Secondo punto
        /// </param>
        /// <param name="ra">
        ///  Raggio
        /// </param>
        /// <param name="centro">
        ///  Punto centro
        /// </param>
        /// <param name="tollRaggi"></param>
        /// <returns>
        ///  Punto centro circonferenza e codice errore
        /// </returns>
        public static int CalcolaCentro(in Point point1,in Point point2, ref double ra,
                        ref Point centro, double tollRaggi = 1)
        {
            double delta_x,
                    delta_y,
                    distanza,
                    m;
            double raggio = ra;
            double x1, y1, x2, y2, xm, ym, z;
            
            x1 = point1.X;
            y1 = point1.Y;
            x2 = point2.X;
            y2 = point2.Y;
            z = point1.Z;
            
            delta_x = x2 - x1;
            delta_y = y2 - y1;

            /*  Punti coincidenti : non esiste centro.   */
            if (Equal(delta_x, 0.0) && Equal(delta_y, 0.0))
            {
                return (int)MathErr.MATH_ERR_C_NULL;
            }

            /*  Coordinate del punto medio  M di P1P2. */
            xm = (x1 + x2) / 2;
            ym = (y1 + y2) / 2;

            /*  Quadrato della distanza P1P2.   */
            distanza = delta_x * delta_x + delta_y * delta_y;

            if (Math.Sqrt(distanza) / 2 - Math.Abs(raggio) > tollRaggi)
            {
                /*  Non esiste una circonferenza passante per   */
                /*  P1 e P2 e con raggio dato.                  */
                return (int)MathErr.MATH_ERR_R_LEN;
            }
            else if (distanza - 4 * raggio * raggio > 0)
            {
                /*  Correggo il raggio in modo da raccordare    */
                /*  P1 e P2 con un arco di circonferenza.       */
                raggio = Math.Sqrt(distanza / 4);
                if (ra >= 0)
                    ra = raggio;
                else
                    ra = -raggio;
            }

            /*  Quadrato della distanza MC. */
            distanza = Math.Max(raggio * raggio - distanza / 4, 0.0);

            /*  Distanza MC.    */
            distanza = Math.Sqrt((distanza));

            if (!Equal(delta_y, 0))
            {
                /*  Calcolo il coefficiente angolare della retta normale    */
                /*  a quella passante per i punti PUNTO1 e PUNTO2.          */
                m = -delta_x / delta_y;

                /*  Calcolo i contributi da sommare alle coordinate del */
                /*  punto M per ottenere il punto desiderato.           */
                delta_x = distanza / Math.Sqrt((1 + m * m));
                delta_y = delta_x * m;
            }
            else
            {
                /*  Retta verticale.    */
                delta_x = 0;
                delta_y = distanza;
            }

            /*  Coordinate del punto candidato ad essere il punto cercato.  */
            centro.X = (xm + delta_x);
            centro.Y = (ym + delta_y);

            /*  Calcolo il segno dell'angolo formato dai 3 punti.   */
            if (SignAngolo(centro, point1, point2) != (raggio > 0))
            {
                centro.X = (xm - delta_x);
                centro.Y = (ym - delta_y);
            }

            centro.Z = z;

            return (int)MathErr.MATH_PROC_OK;
        }

        /// <summary>
        ///  Calcola l'angolo (in RAD) formato dalla
        ///  semiretta Centro-Point1 nel piano XY rispetto allo zero.
        /// </summary>
        /// <param name="centro">
        ///  Punto centro
        /// </param>
        /// <param name="point1">
        ///  Punto 1
        /// </param>
        /// <returns>
        ///  Angolo (in RAD) formato dalla
        ///  semiretta Centro-Point1 nel piano XY rispetto allo zero.
        /// </returns>
        public static double CalcolaAngolo(in Point centro, in Point point1)
        {
            double Angolo = 0.0;

            if (Equal(centro.X, point1.X))
            {
                if (point1.Y > centro.Y)
                    Angolo = 90.0 * FAT_RAD;
                else if (point1.Y < centro.Y)
                    Angolo = 270.0 * FAT_RAD;
                else
                    Angolo = 0.0;
            }
            else if (Equal(centro.Y, point1.Y))
            {
                if (point1.X > centro.X)
                    Angolo = 0.0;
                else if (point1.X < centro.X)
                    Angolo = 180.0 * FAT_RAD;
                else
                    Angolo = 0.0;
            }
            else
            {
                Angolo = Math.Abs(Math.Atan((point1.Y - centro.Y) / (point1.X - centro.X)));

                if (point1.X < centro.X)
                    Angolo = 180.0 * FAT_RAD - Angolo;

                if (point1.Y < centro.Y)
                    Angolo = 360.0 * FAT_RAD - Angolo;
            }

            return Angolo;
        }

        /// <summary>
        ///  Ruota un punto P1 di un angolo AngRAD attorno al punto P0
        ///  nel piano XY.
        /// </summary>
        /// <param name="p0">
        ///  Punto attorno al quale viene ruotato P1
        /// </param>
        /// <param name="p1">
        ///  Punto 1 prima di essere ruotato
        /// </param>
        /// <param name="pRot">
        ///  Punto 1 ruotato
        /// </param>
        /// <param name="angRAD">
        ///  Angolo di rotazione
        /// </param>
        /// <returns>
        ///  Punto P1 ruotato di un angolo AngRAD
        /// </returns>
        public static int  RotatePunto(in Point p0, in Point p1, ref Point pRot, double angRAD)
        {
            double dx;
            double dy;

            dx = p1.X - p0.X;
            dy = p1.Y - p0.Y;

            pRot.X = (p0.X + dx * Math.Cos(angRAD) - dy * Math.Sin(angRAD));
            pRot.Y = (p0.Y + dx * Math.Sin(angRAD) + dy * Math.Cos(angRAD));

            return (int)MathErr.MATH_PROC_OK;
        }

        /// <summary>
        ///  Dato un profilo piano di lunghezza lp (x), larghezza width (y) e 
        ///  spessore  sp (z), dato un punto P1 sulla superficie superiore (z = 0),
        ///  calcola il punto P2 intersezione della retta r, passante per P1 e 
        ///  inclinata degli angoli alfa e beta rispetto agli assi X e Z 
        ///  rispettivamente, con il profilo (questa e' la seconda intersezione
        ///  dal momento che P1 e' la prima).
        ///
        ///  ALGORITMO:
        ///
        ///      1)  viene calcolato il punto P2 intersezione della retta r con
        ///          il piano z = -sp
        ///
        ///      2)  se il punto P2 non appartiene al profilo calcolo la
        ///          intersezione della retta P1P2 col profilo (le intersezioni
        ///          possibili sono 6 tanti quanti i lati di un profilo piano)
        /// </summary>
        /// <param name="p1">
        ///  Punto sulla retta 
        /// </param>
        /// <param name="p2">
        ///  Punto d'intersezione della retta con il profilo 
        /// </param>
        /// <param name="alfa">
        ///  Inclinazione retta attorno asse x
        /// </param>
        /// <param name="beta">
        ///  Inclinazione retta attorno asse Y
        /// </param>
        /// <param name="lp">
        ///  Lunghezza profilo
        /// </param>
        /// <param name="width">
        ///  Larghezza profilo
        /// </param>
        /// <param name="sp">
        ///  Spessore profilo
        /// </param>
        /// <returns>
        ///  Punto d'intersezione e codice di errore
        /// </returns>
        public static int  IntersezRettaPiano(in Point p1, ref Point p2,
                            double alfa, double beta,
                            double lp, double width, double sp) 
        {
            double offsx, offsy, offsz;
            Point P3 = new Point();
            int RetCode;

            //
            //  Calcolo il punto P2 intersezione della retta r, passante
            //  per P1 con inclinazioni alfa e beta, con il piano z = -sp.
            //
            offsx = sp * Math.Sin(FAT_RAD * beta) * Math.Cos(FAT_RAD * alfa) /
                        Math.Cos(FAT_RAD * beta);

            offsy = sp * Math.Sin(FAT_RAD * beta) * Math.Sin(FAT_RAD * alfa) /
                        Math.Cos(FAT_RAD * beta);
            offsz = -sp;

            p2.X = p1.X + offsx;
            p2.Y = p1.Y + offsy;
            p2.Z = offsz;

            //
            //  Se il punto P2 non appartiene al lato z = -sp del profilo,
            //  calcolo l'intersezione della retta P1P2 con gli altri possibili
            //  lati.
            //
            if (p2.X < 0 || p2.X > lp || p2.Y < 0 || p2.Y > width)
            {
                //
                //  Calcolo in P3 l'intersezione della retta P1P2 con i
                //  lati del profilo.
                //
                if ((RetCode = IntersezRettaBox(p1, p2, ref P3, lp, width, sp)) != (int)MathErr.MATH_PROC_OK)
                    return RetCode;

                //
                //  Copio in P2 l'intersezione.
                //
                p2.X = P3.X;
                p2.Y = P3.Y;
                p2.Z = P3.Z;
            }

            return (int)MathErr.MATH_PROC_OK;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="piano"></param>
        /// <param name="quota"></param>
        /// <returns></returns>
        public static int IntersezRettaPPPiano(in Point p1, in Point p2,
                            ref Point p3, int piano, double quota)
        {
            double a = 0, b = 0, c = 0, m = 0;

            //
            //  Calcolo il coefficiente angolare m della retta proiezione
            //  di P1P2 sul piano Z = 0; l'equazione di questa retta e':
            //
            //  y = y1 + m * (x - x1) = c + m * x
            //
            //  dove    m = (y2 - y1) / (x2 - x1)
            //
            //          c = y1 - m * x1;
            //
            if (!Equal(p1.X, p2.X))
            {
                //
                //  Retta non verticale.
                //
                a = (p1.Z - p2.Z) / (p1.X - p2.X);
                b = (p1.X * p2.Z - p2.X * p1.Z) / (p1.X - p2.X);

                m = (p2.Y - p1.Y) / (p2.X - p1.X);
                c = p1.Y - m * p1.X;
            }
            else
            {
                //
                //  Retta verticale x = x1 = x2
                //
                m = double.MaxValue;
            }

            switch (piano)
            {
                case 0:
                    {
                        if (m != double.MaxValue)
                        {
                            p3.X = quota;
                            p3.Y = m * quota + c;
                            p3.Z = a * quota + b;
                        }
                        else
                            return (int)MathErr.MATH_ERR_NO_INTERSEZ;
                        break;
                    }
                case 1:
                    {
                        if (m != double.MaxValue && !Equal(m, 0))
                        {
                            p3.X = (quota - c) / m;
                            p3.Y = quota;
                            p3.Z = (a * (quota - c) / m + b);
                        }
                        else
                            return (int)MathErr.MATH_ERR_NO_INTERSEZ;
                        break;
                    }
                    
                default:
                    break;
            }

            return (int)MathErr.MATH_PROC_OK;
        }

        /// <summary>
        ///  Dati 2 punti P1 e P2, dato un parallelepipedo di dimensioni
        ///  lp, width, sp (dove P1 appartiene al piano superiore
        ///  del parallelepipedo e P2 a quello inferiore), calcola la 
        ///  seconda intersezione P3 della retta P1P2 con il parallelepipedo, 
        ///  supposto che P1 sia la prima intersezione (il punto P3 
        ///  appartiene al segmento P1P2).
        /// </summary>
        /// <param name="p1">
        ///  Punto sulla retta 
        /// </param>
        /// <param name="p2">
        ///  Punto sulla retta
        /// </param>
        /// <param name="p3">
        ///  Seconda intersezione retta con parallelepipedo
        /// </param>
        /// <param name="lp">
        ///  Lunghezza parallelepipedo
        /// </param>
        /// <param name="width">
        ///  Larghezza parallelepipedo
        /// </param>
        /// <param name="sp">
        ///  Spessore paralelepipedo
        /// </param>
        /// <returns>
        ///  Punto d'intersezione e codice errore
        /// </returns>
        public static int IntersezRettaBox(in Point p1, in Point p2, ref Point p3,
                        double lp, double width, double sp)
        {
            int RetCode;

            if (p2.X > lp)
            {
                if (p2.Y <= width && p2.Y >= 0)
                {
                    //  PIANO X = LP
                    RetCode = IntersezRettaPPPiano(p1, p2, ref p3, 0, lp);
                }
                else if (p2.Y > width)
                {
                    //  PIANO Y = WIDTH
                    RetCode = IntersezRettaPPPiano(p1, p2, ref p3, 1, width);
                }
                else
                {
                    //  PIANO Y = 0
                    RetCode = IntersezRettaPPPiano(p1, p2, ref p3, 1, 0);
                }
            }
            else if (p2.X < 0)
            {
                if (p2.Y <= width && p2.Y >= 0)
                {
                    //  PIANO X = 0
                    RetCode = IntersezRettaPPPiano(p1, p2, ref p3, 0, 0.0);
                }
                else if (p2.Y > width)
                {
                    //  PIANO Y = WIDTH
                    RetCode = IntersezRettaPPPiano(p1, p2, ref p3, 1, width);
                }
                else
                {
                    //  PIANO Y = 0
                    RetCode = IntersezRettaPPPiano(p1, p2, ref p3, 1, 0);
                }
            }
            else
            {
                p3.X = p2.X;
                p3.Y = p2.Y;
                p3.Z = p2.Z;

                RetCode = (int)MathErr.MATH_PROC_OK;
            }

            return RetCode;
        }

        /// <summary>
        ///  Calcola il punto finale P2 di un arco di circonferenza P1P2
        ///  dati P1, il centro, il raggio con segno e l'angolo (in RAD)
        ///  sotteso dall'arco P1P2.
        /// </summary>
        /// <param name="centro">
        ///  Centro arco di circonferenza 
        /// </param>
        /// <param name="point1">
        ///  Punto iniziale arco di circonferenza
        /// </param>
        /// <param name="point2">
        ///  Punto finale arco di circonferenza 
        /// </param>
        /// <param name="raggio">
        ///  Raggio arco di circonferenza
        /// </param>
        /// <param name="angRAD">
        ///  Angolo dell'arco di circonferenza
        /// </param>
        /// <returns>
        ///  Punto finale arco di circonferenza e codice errore
        /// </returns>
        public static int CalcolaPuntoCirc(in Point centro,
                                    in Point point1, ref Point point2,
                                    double raggio, double angRAD)
        {
            double AlfaI, AlfaF;

            AlfaI = CalcolaAngolo(centro, point1);
            AlfaF = AlfaI + angRAD;

            point2.X = centro.X + Math.Abs(raggio) * Math.Cos(AlfaF);
            point2.Y = centro.Y + Math.Abs(raggio) * Math.Sin(AlfaF);
            point2.Z = centro.Z;

            return (int)MathErr.MATH_PROC_OK;
        }

        //
        /// <summary>
        ///  Calcola la quota x del punto P intersezione della retta y = k
        ///  con l'arco per P1 e P2 di raggio assegnato.
        /// </summary>
        /// <param name="x1">
        ///  Coordinata x del punto P1
        /// </param>
        /// <param name="y1">
        ///  Coordinata y del punto P1
        /// </param>
        /// <param name="x2">
        ///  Coordinata x del punto P2
        /// </param>
        /// <param name="y2">
        ///  Coordinata y del punto P2
        /// </param>
        /// <param name="r">
        ///  Raggio dell'arco di circonferenza
        /// </param>
        /// <param name="k">
        ///  Quota y della retta
        /// </param>
        /// <param name="x">
        ///  Quota x del punto d'intersezione
        /// </param>
        /// <returns>
        ///  Quota x del punto d'intersezione e codice errore
        /// </returns>
        public static int CalcolaYArc(double x1, double y1, double x2, double y2, double r, double k,
                                        ref double x)
        {
            int RetCode;
            Point Point1 = new Point(x1, y1, 0.0);
            Point Point2 = new Point(x2, y2, 0.0);
            Point Point3 = new Point(0.0, 0.0, 0.0);
            Point Centro = new Point(0.0, 0.0, 0.0);
            double Raggio = r;

            //
            //	Calcolo il centro dell'arco di circonferenza.
            //
            RetCode = CalcolaCentro(Point1, Point2, ref Raggio, ref Centro);
            if (RetCode != (int)MathErr.MATH_PROC_OK)
                return RetCode;

            double Alfa, Beta, AngRad;

            if ((y1 - Centro.Y) / r > 1.0)
                Alfa = Math.PI / 2.0;
            else if ((y1 - Centro.Y) / r < -1.0)
                Alfa = -Math.PI / 2.0;
            else
                Alfa = Math.Asin((y1 - Centro.Y) / r);

            if ((k - Centro.Y) / r > 1.0)
                Beta = Math.PI / 2.0;
            else if ((k - Centro.Y) / r < -1.0)
                Beta = -Math.PI / 2.0;
            else
                Beta = Math.Asin((k - Centro.Y) / r);

            if (r > 0)
                AngRad = Math.Abs(Alfa) + Math.Abs(Beta);
            else
                AngRad = -(Math.Abs(Alfa) + Math.Abs(Beta));

            RetCode = CalcolaPuntoCirc(Centro, Point1, ref Point3, r, AngRad);
            if (RetCode != (int)MathErr.MATH_PROC_OK)
                return RetCode;

            x = Point3.X;

            return (int)MathErr.MATH_PROC_OK;
        }


        /// <summary>
        ///  Funzione che calcola un punto sulla normale alla retta per
        ///  2 punti, passante per uno dei suoi estremi e a distanza
        ///  assegnata da quest'ultimo.
        /// </summary>
        /// <param name="line">
        ///  Retta
        /// </param>
        /// <returns>
        ///  Punto sulla normale alla retta, passante per i suoi estremi e a distanza assegnata da quest'ultimo
        /// </returns>
        public static bool NormalRetta(ref Line line)
        {
            double DeltaX,
                    DeltaY,
                    m;
            Point aux_point = new Point();
            Point other_point;

            DeltaX = line.Point2.X - line.Point1.X;
            DeltaY = line.Point2.Y - line.Point1.Y;

            //
            //  Punti coincidenti : scarto il punto.
            //
            if (Equal(DeltaX, 0.0) && Equal(DeltaY, 0.0))
                return false;

            if (!Equal(DeltaX, 0.0))
            {
                //
                //  Calcolo il coefficiente angolare della retta passante
                //  per i punti PUNTO1 e PUNTO2.
                //
                m = DeltaY / DeltaX;

                //
                //  Calcolo i contributi da sommare alle coordinate di
                //  NORM_PUNTO per ottenere il punto desiderato.
                //
                DeltaY = line.Distance / Math.Sqrt(1 + m * m);
                DeltaX = DeltaY * Math.Abs(m);

                //
                //  DeltaY deve avere il segno di m.
                //
                if (m < 0)
                    DeltaY *= -1;
            }
            else
            {
                //
                //  Retta verticale.
                //
                DeltaX = line.Distance;
                DeltaY = 0;

                m = double.MaxValue;
            }

            //
            //  Coordinate del punto candidato ad essere il punto cercato.
            //
            aux_point.X = line.NormPoint.X + DeltaX;
            aux_point.Y = line.NormPoint.Y - DeltaY;

            //
            //  Punto ausiliario per il calcolo del segno dell'angolo.
            //
            other_point = (line.NormPoint == line.Point1 ?
                           line.Point2 : line.Point1);

            //
            //  Calcolo il segno dell'angolo formato dai 3 punti.
            //
            if ((SignAngolo(aux_point, line.NormPoint, other_point) !=
                            line.CruSx) ==
                            (line.NormPoint == line.Point2))
            {
                //
                //  Il punto cercato e' proprio AUX_PUNTO.
                //
                line.DestPoint.X = aux_point.X;
                line.DestPoint.Y = aux_point.Y;
            }
            else
            {
                //
                //  Il punto cercato non e' AUX_PUNTO ma il suo simmetrico
                //  rispetto alla retta per PUNTO1 e PUNTO2.
                //
                line.DestPoint.X = line.NormPoint.X - DeltaX;
                line.DestPoint.Y = line.NormPoint.Y + DeltaY;
            }

            //
            //  Memorizzo il coefficiente angolare della retta per
            //  PUNTO1 e PUNTO2.
            //
            line.m = m;

            return true;
        }

        /// <summary>
        ///  Funzione che calcola un punto sulla normale ad una
        ///  circonferenza di centro noto passante per un punto della
        ///  circonferenza assegnato e a distanza nota dal punto stesso.
        /// </summary>
        /// <param name="circle">
        ///  Cerchio
        /// </param>
        /// <returns>
        ///  Punto sulla normale alla circonferenza e a distanza nota dal punto
        /// </returns>
        public static bool NormalCirc(ref Circle circle)
        {
            double DeltaX,
                    DeltaY,
                    distanza,
                    ratio,
                    mp,
                    raggio;

            //
            //  Valore assoluto del raggio.
            //
            raggio = Math.Abs(circle.Raggio);

            DeltaX = circle.NormPoint.X - circle.Centro.X;
            DeltaY = circle.NormPoint.Y - circle.Centro.Y;

            //
            //  Raggio di curvatura = 0
            //  => ERRORE di programmazione.
            //
            if (Equal(raggio, 0.0))
                return false;

            //
            //  Calcola la distanza dal centro della circonferenza
            //  del punto ricercato.
            //
            if (circle.CruSx == circle.Raggio < 0)
                distanza = raggio + circle.Distance;
            else if (circle.Distance <= raggio)
                distanza = raggio - circle.Distance;
            else
                //
                //  Raggio di curvatura < Distance
                //  => ERRORE di programmazione.
                //
                return false;

            //
            //  Memorizzo il raggio del profilo utensile.
            //
            circle.RaggioUt = circle.Raggio < 0 ? -distanza : distanza;

            ratio = distanza / raggio;

            circle.DestPoint.X = circle.Centro.X -
                        ratio * (circle.Centro.X - circle.NormPoint.X);

            circle.DestPoint.Y = circle.Centro.Y -
                        ratio * (circle.Centro.Y - circle.NormPoint.Y);

            if (!Equal(DeltaX, 0.0))
            {
                //
                //  Coefficiente angolare retta
                //  passante per CENTRO e NORM_PUNTO.
                //
                mp = DeltaY / DeltaX;

                //
                //  Memorizzo il coefficiente angolare della retta
                //  normale al raggio per CENTRO e NORM_PUNTO.
                //
                circle.M = (!Equal(mp, 0.0) ? -1 / mp : double.MaxValue);
            }
            else
                //
                //  Il raggio per CENTRO e NORM_PUNTO e' verticale.
                //
                circle.M = 0;

            return true;
        }

        //
        /// <summary>
        ///  Calcola l'intersezione di 2 rette di cui si conoscono 2 punti di
        ///  passaggio per ciascuna.
        /// </summary>
        /// <param name="a1">
        ///  Punto sulla retta 1
        /// </param>
        /// <param name="a2">
        ///  Punto sulla retta 1
        /// </param>
        /// <param name="b1">
        ///  Punto sulla retta 2
        /// </param>
        /// <param name="b2">
        ///  Punto sulla retta 2
        /// </param>
        /// <param name="c">
        ///  Punto d'intersezione 
        /// </param>
        /// <returns>
        ///  true se l'intersezione è verificata e il punto d'intersezione, false altrimenti
        /// </returns>
        public static bool IntersezLL(in Point a1, in Point a2, in Point b1, in Point b2, ref Point c)
        {
            double ma = 0.0, mb = 0.0;

            //	Calcolo ma.
            if (Equal(a1.X, a2.X))
                ma = double.MaxValue;
            else if (Equal(a1.Y, a2.Y))
                ma = 0.0;
            else
                ma = (a2.Y - a1.Y) / (a2.X - a1.X);

            //	Calcolo mb.
            if (Equal(b1.X, b2.X))
                mb = double.MaxValue;
            else if (Equal(b1.Y, b2.Y))
                mb = 0.0;
            else
                mb = (b2.Y - b1.Y) / (b2.X - b1.X);

            return IntersezLL(a1,  ma, b1,  mb, ref c);
        }

        /// <summary>
        ///  Calcola l'intersezione di 2 rette di coeff. angolare
        ///  assegnato e di cui si conosce un punto di passaggio per
        ///  ciascuna.
        /// </summary>
        /// <param name="a">
        ///  Punto sulla retta 1
        /// </param>
        /// <param name="ma">
        ///  Coefficente angolare retta 1
        /// </param>
        /// <param name="b">
        ///  Punto sulla retta 2
        /// </param>
        /// <param name="mb">
        ///  Coefficente retta angolare 2
        /// </param>
        /// <param name="c">
        ///  Punto d'intersezione
        /// </param>
        /// <returns>
        ///  true se l'intersezione è verificata e il punto d'intersezione, false altrimenti
        /// </returns>
        public static bool IntersezLL(in Point a, double ma, in Point b, double mb, ref Point c)
        {
            //
            //  Se i 2 coefficienti angolari sono uguali, le rette
            //  sono parallele e non c'e' intersezione.
            //
            if (Equal(ma, mb))
            {
                //
                //  Faccio coincidere il punto intersezione
                //  col primo punto.
                //
                c.X = a.X;
                c.Y = a.Y;

                return false;
            }

            if (ma >= double.MaxValue)
            {
                //
                //  Retta per a verticale.
                //
                c.X = a.X;
                c.Y = b.Y + mb * (a.X - b.X);
            }
            else if (mb >= double.MaxValue)
            {
                //
                //  Retta per b verticale.
                //
                c.X = b.X;
                c.Y = (a.Y + ma * (b.X - a.X));
            }
            else
            {
                c.X = (b.Y - a.Y + ma * a.X - mb * b.X) / (ma - mb);
                c.Y = (a.Y + ma * (c.X - a.X));
            }

            return true;
        }

        //
        /// <summary>
        ///  Calcola un punto interno al segmento AD a distanza
        ///  DISTANCE da A.
        /// </summary>
        /// <param name="a">
        ///  Punto iniziale segmento
        /// </param>
        /// <param name="d">
        ///  Punto finale segmento
        /// </param>
        /// <param name="e">
        ///  Punto interno al segmento
        /// </param>
        /// <param name="distance">
        ///  Distanza dal punto A del punto interno
        /// </param>
        /// <returns>
        ///  Punto a distanza distance dal segmento
        /// </returns>
        public static bool PuntoInterno(in Point a, in Point d, ref Point e, double distance)
        {
            double dx,
                    dy,
                    ratio;

            dx = d.X - a.X;
            dy = d.Y - a.Y;

            if (Equal(dx, 0.0) && Equal(dy, 0.0))
            {
                e.X = a.X;
                e.Y = a.Y;
            }
            else
            {
                ratio = distance / Math.Sqrt(dx * dx + dy * dy);

                e.X = a.X + ratio * (d.X - a.X);
                e.Y = a.Y + ratio * (d.Y - a.Y);
            }

            return true;
        }


        /// <summary>
        ///  Calcola un punto ausiliario per il raccordo di un
        ///  angolo a 360.
        /// </summary>
        /// <param name="c">
        ///  Punto raccordo
        /// </param>
        /// <param name="a">
        ///  Punto Raccordo 
        /// </param>
        /// <param name="ma"></param>
        /// <param name="p">
        ///  Punto ausiliario
        /// </param>
        /// <param name="verso">
        ///  true orario, false antiorario
        /// </param>
        /// <param name="distance"></param>
        public static void AuxPoints360(in Point c, in Point a, double ma, ref Point p, bool verso,
                            double distance)
        {
            double xa, ya, delta;

            xa = a.X;
            ya = a.Y;

            if (ma < double.MaxValue)
            {
                delta = distance * Math.Sqrt(1 + ma * ma);

                p.X = xa + delta;
                p.Y = ya + ma * (p.X - xa);

                if (SignAngolo(c, a, p) == verso)
                {
                    p.X = xa - delta;
                    p.Y = ya + ma * (p.X - xa);
                }
            }
            else
            {
                p.X = xa;
                p.Y = ya + distance;

                if (SignAngolo(c, a, p) == verso)
                    p.Y = ya - distance;
            }
        }

        /// <summary>
        ///  Calcolo il punto medio di un arco di circonferenza
        ///  di cui si conoscono i 2 estremi e il raggio con segno.
        /// </summary>
        /// <param name="point1">
        ///  Punto iniziale arco di circonferenza
        /// </param>
        /// <param name="point2">
        ///  Punto finale arco di circonferenza
        /// </param>
        /// <param name="ra">
        ///  Raggio arco di circonferenza
        /// </param>
        /// <param name="pMedio">
        ///  Punto medio arco di circonferenza
        /// </param>
        /// <param name="tollRaggi"></param>
        /// <returns>
        ///  Punto medio arco di circonferenza e codice errore
        /// </returns>
        public static int  CalcolaPMedioArc(in Point point1, in Point point2, ref double ra,
                        ref Point pMedio, double tollRaggi)
        {
            double delta_x,
                    delta_y,
                    distanza,
                    distanzaMC,
                    distanzaPM,
                    m;
            double raggio = ra;
            double x1, y1, x2, y2, xm, ym;

            x1 = point1.X;
            y1 = point1.Y;
            x2 = point2.X;
            y2 = point2.Y;

            delta_x = x2 - x1;
            delta_y = y2 - y1;

            //  Punti coincidenti : non esiste arco
            if (Equal(delta_x, 0.0) && Equal(delta_y, 0.0))
            {
                return (int)MathErr.MATH_ERR_C_NULL;
            }

            //  Coordinate del punto medio  M del segmento P1P2.
            xm = (x1 + x2) / 2;
            ym = (y1 + y2) / 2;

            //  Quadrato della distanza P1P2.
            distanza = delta_x * delta_x + delta_y * delta_y;

            //    if (distanza - 4 * raggio * raggio > TollRaggi * TollRaggi)
            if (Math.Sqrt(distanza) / 2 - Math.Abs(raggio) > tollRaggi)
            {
                //  Non esiste una circonferenza passante per
                //  P1 e P2 e con raggio dato.
                return (int)MathErr.MATH_ERR_R_LEN;
            }
            else if (distanza - 4 * raggio * raggio > 0)
            {
                //  Correggo il raggio in modo da raccordare
                //  P1 e P2 con un arco di circonferenza.
                raggio = Math.Sqrt((distanza / 4));
                ra = raggio;
            }

            //  Quadrato della distanza MC.
            distanza = Math.Max(raggio * raggio - distanza / 4, 0.0);

            //  Distanza MC.
            distanzaMC = Math.Sqrt(distanza);

            //  Distanza PM.
            distanzaPM = Math.Abs(raggio) - distanzaMC;

            if (!Equal(delta_y, 0))
            {
                //  Calcolo il coefficiente angolare della retta normale
                //  a quella passante per i punti PUNTO1 e PUNTO2.
                m = -delta_x / delta_y;

                //  Calcolo i contributi da sommare alle coordinate del
                //  punto M per ottenere il punto desiderato.
                delta_x = distanzaPM / Math.Sqrt(1 + m * m);
                delta_y = delta_x * m;
            }
            else
            {
                //  Retta verticale.
                delta_x = 0;
                delta_y = distanzaPM;
            }

            //  Coordinate del punto candidato ad essere il punto cercato.
            pMedio.X = (xm + delta_x);
            pMedio.Y = (ym + delta_y);

            //  Calcolo il segno dell'angolo formato dai 3 punti.
            if (SignAngolo(point1, pMedio, point2) != (raggio > 0))
            {
                pMedio.X = (xm - delta_x);
                pMedio.Y = (ym - delta_y);
            }

            return (int)MathErr.MATH_PROC_OK;
        }

        /// <summary>
        ///  Calcolo il punto medio di un arco di circonferenza nello spazio
        ///  di cui si conoscono i 2 estremi e il raggio con segno.
        /// </summary>
        /// <param name="x1">
        ///  Coordinata x punto iniziale arco di circonferenza
        /// </param>
        /// <param name="y1">
        ///  Coordinata y punto iniziale arco di circonferenza
        /// </param>
        /// <param name="z1">
        ///  Coordinata z punto iniziale arco di circonferenza
        /// </param>
        /// <param name="x2">
        ///  Coordinata x punto finale arco di circonferenza
        /// </param>
        /// <param name="y2">
        ///  Coordinata y punto finale arco di circonferenza
        /// </param>
        /// <param name="z2">
        ///  Coordinata z punto finale arco di circonferenza
        /// </param>
        /// <param name="raggio">
        ///  Raggio arco di circonferenza
        /// </param>
        /// <param name="vx"></param>
        /// <param name="vy"></param>
        /// <param name="vz"></param>
        /// <param name="xm">
        ///  Coordinata x punto medio arco di circonferenza
        /// </param>
        /// <param name="ym">
        ///  Coordinata y punto medio arco di circonferenza
        /// </param>
        /// <param name="zm">
        ///  Coordinata z punto medio arco di circonferenza
        /// </param>
        /// <param name="tollRaggi"></param>
        /// <returns>
        ///  Punto medio arco di circonferenza
        /// </returns>
        public static int CalcolaPMedioArc3D(double x1, double y1, double z1, double x2, double y2, double z2, double raggio,
            double vx, double vy, double vz,
            ref double xm, ref double ym, ref double zm, double tollRaggi)
        {
            double delta_x, delta_y, delta_z,
                distanza,
                distanzaMC,
                distanzaPM,
                xmSeg, ymSeg, zmSeg;

            delta_x = x2 - x1;
            delta_y = y2 - y1;
            delta_z = z2 - z1;

            //  Punti coincidenti : non esiste arco
            if (Equal(delta_x, 0.0) && Equal(delta_y, 0.0) && Equal(delta_z, 0.0))
            {
                return (int)MathErr.MATH_ERR_C_NULL;
            }

            //  Coordinate del punto medio  M del segmento P1P2.
            xmSeg = (x1 + x2) / 2;
            ymSeg = (y1 + y2) / 2;
            zmSeg = (z1 + z2) / 2;

            //  Quadrato della distanza P1P2.
            distanza = delta_x * delta_x + delta_y * delta_y + delta_z * delta_z;

            //    if (distanza - 4 * raggio * raggio > TollRaggi * TollRaggi)
            if (Math.Sqrt(distanza) / 2 - Math.Abs(raggio) > tollRaggi)
            {
                //  Non esiste una circonferenza passante per
                //  P1 e P2 e con raggio dato.
                return (int)MathErr.MATH_ERR_R_LEN;
            }
            else if (distanza - 4 * raggio * raggio > 0)
            {
                //  Correggo il raggio in modo da raccordare
                //  P1 e P2 con un arco di circonferenza.
                raggio = Math.Sqrt(distanza / 4);
            }

            //  Quadrato della distanza MC.
            distanza = Math.Max(raggio * raggio - distanza / 4, 0.0);

            //  Distanza MC.
            distanzaMC = Math.Sqrt(distanza);

            //  Distanza PM.
            distanzaPM = Math.Abs(raggio) - distanzaMC;

            //	Normalizzo il vettore.
            double Modulo = Math.Sqrt(vx * vx + vy * vy + vz * vz);
            if (Modulo > 0.0)
            {
                vx /= Modulo;
                vy /= Modulo;
                vz /= Modulo;
            }

            xm = xmSeg + vx * distanzaPM;
            ym = ymSeg + vy * distanzaPM;
            zm = zmSeg + vz * distanzaPM;

            return (int)MathErr.MATH_PROC_OK;
        }

        /// <summary>
        ///  Rende true se il percorso definito dai 3 punti Prec, Path, Next risulta tangente in Path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="prec"></param>
        /// <param name="next"></param>
        /// <param name="tangToll"></param>
        /// <returns></returns>
        public static bool IsPathTangent(in PathPrg path, in PathPrg prec, in PathPrg next, double tangToll)
        {
            //  Ammetto solo Prec = 0x0
            if (path is null || next is null)
                return false;

            bool IsTangent = false;
            bool LineArc = Equal(path.R, 0, 0.1f);
            bool ArcArcSameSignRadius = path.R * next.R > 0;
            bool ArcArcDiffSignRadius = path.R * next.R < 0;

            if (LineArc)
            {
                if (prec != null)
                {
                    double Angolo = GetAngolo(prec.X, prec.Y, path.X, path.Y, next.X, next.Y) / FAT_RAD;

                    if (Equal(next.R, 0, 1))
                        IsTangent = Math.Abs(Angolo) > 180 - tangToll;
                    else
                        IsTangent = Angolo * next.R > 0;
                }
            }
            else if (ArcArcSameSignRadius)
                IsTangent = true;
            else if (ArcArcDiffSignRadius)
                IsTangent = false;

            return IsTangent;
        }

        /**************************     FUNZIONI ROBOT      ************************/

        //
        /// <summary>
        ///  Moltiplicazione matrice - vettore:
        /// 	
        ///  c[mC] = A[mA, nA] * b[mB]
        /// </summary>
        /// <param name="A">
        ///  Matrice A
        /// </param>
        /// <param name="mA">
        ///  Numero righe matrice A 
        /// </param>
        /// <param name="nA">
        ///  Numero colonne matrice A 
        /// </param>
        /// <param name="b">
        ///  Matrice B
        /// </param>
        /// <param name="mB">
        ///  Numero righe matrice B
        /// </param>
        /// <param name="c">
        ///  Matrice C 
        /// </param>
        /// <param name="mC">
        ///  Numero righe matrice C 
        /// </param>
        /// <returns>
        ///  Matrice C = A * B
        /// </returns>
        public static int MultVect( in double[,] A, int mA, int nA,
                                 in double[] b, int mB,
                                 ref double[] c, int mC)
        {
            //
            //	Verifico che le dimensioni matriciali siano
            //	congruenti.
            //
            if (mC != mA || mB != mA)
                return (int)MathErr.MATH_ERR_DATA;

            int m, n;
            for (m = 0; m < mC; m++)
            {
                double sum = 0;

                for (n = 0; n < mC; n++)
                    sum += A[m,n] * b[n];
                c[m] = sum;
            }

            return (int)MathErr.MATH_PROC_OK;
        }

        //
        /// <summary>
        ///  Moltiplicazione matriciale:
        /// 	
        ///  C[mC, nC] = A[mA, nA] * B[mB, nB]
        ///  
        /// </summary>
        /// <param name="A">
        ///  Matrice A
        /// </param>
        /// <param name="mA">
        ///  Numero righe matrice A 
        /// </param>
        /// <param name="nA">
        ///  Numero colonne matrice A 
        /// </param>
        /// <param name="B">
        ///  Matrice B
        /// </param>
        /// <param name="mB">
        ///  Numero righe matrice B
        /// </param>
        /// <param name="nB">
        ///  Numero colonne matrice B 
        /// </param>
        /// <param name="C">
        ///  Matrice C
        /// </param>
        /// <param name="mC">
        ///  Numero righe matrice C
        /// </param>
        /// <param name="nC">
        ///  Numero colonne matrice C 
        /// </param>
        /// <returns></returns>
        public static int MultMatrix(in double[,] A, int mA, int nA,
                                     in double[,] B, int mB, int nB,
                                     ref double[,] C, int mC, int nC)
        {
            //
            //	Verifico che le dimensioni matriciali siano
            //	congruenti.
            //
            if (mC != mA || nC != nB || nA != mB)
                return (int)MathErr.MATH_ERR_DATA;
            // If the matrix dimensions are 4x4 the class Matrix4x4 is the faster in matrix multiplications (SIMD method used) 
            if (false)
            {

            }
            //if (mA == 4 && nA == 4 && mB == 4 && nB == 4)
            //{
            //    Matrix4x4 A4x4 = new Matrix4x4((float)A[0, 0], (float)A[0, 1], (float)A[0, 2], (float)A[0, 3], (float)A[1, 0], (float)A[1, 1], (float)A[1, 2], (float)A[1, 3], (float)A[2, 0], (float)A[2, 1], (float)A[2, 2], (float)A[2, 3], (float)A[3, 0], (float)A[3, 1], (float)A[3, 2], (float)A[3, 3]);
            //    Matrix4x4 B4x4 = new Matrix4x4((float)B[0, 0], (float)B[0, 1], (float)B[0, 2], (float)B[0, 3], (float)B[1, 0], (float)B[1, 1], (float)B[1, 2], (float)B[1, 3], (float)B[2, 0], (float)B[2, 1], (float)B[2, 2], (float)B[2, 3], (float)B[3, 0], (float)B[3, 1], (float)B[3, 2], (float)B[3, 3]);
            //    Matrix4x4 C4x4 = A4x4 * B4x4;
            //    C[0, 0] = C4x4.M11; C[0, 1] = C4x4.M12; C[0, 2] = C4x4.M13; C[0, 3] = C4x4.M14; C[1, 0] = C4x4.M21; C[1, 1] = C4x4.M22; C[1, 2] = C4x4.M23; C[1, 3] = C4x4.M24; C[2, 0] = C4x4.M31; C[2, 1] = C4x4.M32; C[2, 2] = C4x4.M33; C[2, 3] = C4x4.M34; C[3, 0] = C4x4.M41; C[3, 1] = C4x4.M42; C[3, 2] = C4x4.M43; C[3, 3] = C4x4.M44;
            //}
            else
            {
                for (int i = 0; i < mC; i++)
                {
                    for (int j = 0; j < nC; j++)
                    {
                        double sum = 0;
                        for (int k = 0; k < nA; k++)
                        {
                            sum += A[i, k] * B[k, j];
                        }
                        C[i, j] = sum;
                    }
                }
            }
            return (int)MathErr.MATH_PROC_OK;
        }

        /**************************************************/

        /// <summary>
        ///  Calcola la circonferenza nel piano passante per 3 punti P1(X1,Y1), P2(X2,Y2), P3(X3,Y3)
        /// </summary>
        /// <param name="x1">
        ///  Coordinata x del punto P1
        /// </param>
        /// <param name="y1">
        ///  Coordinata y del punto P1
        /// </param>
        /// <param name="x2">
        ///  Coordinata x del punto P2
        /// </param>
        /// <param name="y2">
        ///  Coordinata y del punto P2
        /// </param>
        /// <param name="x3">
        ///  Coordinata x del punto P3
        /// </param>
        /// <param name="y3">
        ///  Coordinata y del punto P3
        /// </param>
        /// <param name="xc">
        ///  Coordinata x del punto C
        /// </param>
        /// <param name="yc">
        ///  Coordinata y del punto C
        /// </param>
        /// <param name="r">
        ///  Raggio
        /// </param>
        /// <param name="skipCheckDifferentXY"></param>
        /// <returns>
        ///  Raggio e coordinate del centro C (XC, YC)
        /// </returns>
        public static bool CalcCircTrePunti(double x1, double y1, double x2, double y2, double x3, double y3,
            ref double xc, ref double yc, ref double r, bool skipCheckDifferentXY)
        {

            //	P2 deve avere le quote X/Y diverse da quelle di P1 e P3.
            if (!skipCheckDifferentXY)
            {
                if (Equal(y1, y2, 0.1) || Equal(y3, y2, 0.1) || Equal(x1, x2, 0.1) || Equal(x3, x2, 0.1))
                    return false;
            }

            //	A è il punto medio di P1P2.
            double XA = (x1 + x2) / 2, YA = (y1 + y2) / 2;
            //	m12 è il coefficiente angolare della retta per P1P2.
            double m12 = (y1 - y2) / (x1 - x2);

            //	B è il punto medio di P2P3.
            double XB = (x2 + x3) / 2, YB = (y2 + y3) / 2;
            //	m23 è il coefficiente angolare della retta per P2P3.
            double m23 = (y2 - y3) / (x2 - x3);

            //	mAC e mBC sono i coefficienti angolari delle rette r1 e r2 passanti per AC e BC rispettivamente.
            double mAC = -1 / m12, mBC = -1 / m23;

            //	Questo caso non si dovrebbe mai verificare.
            if (Equal(mAC, mBC))
                return false;

            //	Il centro C si ottiene come intersezione tra le rette r1 e r2, ovvero tra le normali a P1P2 e P2P3 
            //	passanti per A e B rispettivamente.
            double XCentro = (YB - YA + mAC * XA - mBC * XB) / (mAC - mBC);
            double YCentro = YA + mAC * (XCentro - XA);
            double Raggio = Math.Sqrt((XCentro - x2) * (XCentro - x2) + (YCentro - y2) * (YCentro - y2));

            xc = XCentro;
            yc = YCentro;
            r = Raggio;

            return true;
        }
    }
}