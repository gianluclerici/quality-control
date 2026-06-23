using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ficep.Utils
{
    public class Log
    {
        /// <summary>
        /// Aggiorno il file di log con la stringa specificata se esiste altrimenti lo creo.
        /// </summary>
        /// <param name="logFileName">
        /// Path del file che in cui si vuole scrivere
        /// </param>
        /// <param name="logString">
        /// Stringa che si vuole scrivere nel file
        /// </param>
        /// <param name="deleteIfExist">
        /// Se true cancella il file se esiste 
        /// </param>
        /// <returns>
        /// True se la scrittura della stringa nel file è avvenuta con successo, false altrimenti
        /// </returns>
        public static bool Write(string logFileName, string logString, bool deleteIfExist = false, bool CreationTime = true)
        {
            string outputDir = Path.GetDirectoryName(logFileName);

            return Write(outputDir, logFileName, logString, deleteIfExist, CreationTime);
        }

        /// <summary>
        /// Aggiorno il file di log con la stringa specificata se esiste altrimenti lo creo.
        /// </summary>
        /// <param name="outputDir">
        /// Directory in cui si deve trovare il file
        /// </param>
        /// <param name="logFileName">
        /// Nome del file che in cui si vuole scrivere
        /// </param>
        /// <param name="logString">
        /// Stringa che si vuole scrivere nel file
        /// </param>
        /// <param name="deleteIfExist">
        /// Se true cancella il file se esiste 
        /// </param>
        /// <returns>
        /// True se la scrittura della stringa nel file è avvenuta con successo, false altrimenti
        /// </returns>
        public static bool Write(string outputDir, string logFileName, string logString, bool deleteIfExist = false, bool CreationTime = true)
        {
            logFileName = Path.Combine(outputDir, logFileName);
            if (!Directory.Exists(outputDir))
                return false;

            string textToAppend;

            try
            {
                if (deleteIfExist)
                {
                    if (File.Exists(logFileName))
                        File.Delete(logFileName);
                }

                //
                //	Se è la prima scrittura su file,inserisco una riga con l'ora corrente
                //
                if (!(File.Exists(logFileName)))
                {
                    if (CreationTime)
                    {
                        // Aggiungo il testo al file
                        string fileExtension = System.IO.Path.GetExtension(logFileName).ToUpper();
                        if (fileExtension != ".XML")
                        {
                            //
                            //  Leggo l'ora corrente.
                            //
                            DateTime currentTime = DateTime.Now;
                            string formattedTime = string.Format("{0:dd/MM/yyyy HH:mm:ss}", currentTime);

                            // Preparo il testo da scrivere
                            textToAppend = ";----------------------------------\n" +
                                            ";\t" + formattedTime + "\n" +
                                            ";----------------------------------\n";
                            File.AppendAllText(logFileName, textToAppend);
                        }
                    }
                }

                // Aggiungo il testo al file
                textToAppend = logString + "\n";
                File.AppendAllText(logFileName, textToAppend);
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }
    }
}
