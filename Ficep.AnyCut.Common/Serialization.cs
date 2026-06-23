using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Xml.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ficep.AnyCut.Common
{
    //
    //  SerializeObject include una raccolta di Metodi generici per
    //  la serializzazione / deserializzazione di classe su file.
    //
    public class SerializeObject
    {
        //  Serializza un oggetto <T> in un file XML
        public static bool SerializeToXml<T>(T anyobject, string xmlFilePath)
        {
            if (anyobject == null)
                return false;

            XmlSerializer xmlSerializer = new XmlSerializer(anyobject.GetType());

            using (StreamWriter writer = new StreamWriter(xmlFilePath))
            {
                xmlSerializer.Serialize(writer, anyobject);
            }

            return true;
        }

        //  Deserializza un oggetto <T> da file XML
        public static bool DeserializeXmlToObject<T>(string xmlFilePath, out T? xmlObject) where T : class
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            xmlObject = null;
            using (StreamReader sr = new StreamReader(xmlFilePath))
            {
                var deserializedObject = ser.Deserialize(sr);

                if (deserializedObject != null)
                {
                    xmlObject = (T)deserializedObject;
                    return true;
                }
                else 
                    return false;
            }
        }

        //  Serializza un oggetto <T> in un file JSON
        public static void SerializeToJSONFile<T>(T anyobject, string jsonFilePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(anyobject, options);
            File.WriteAllText(jsonFilePath, jsonString);
        }

        //  Deserializza un oggetto <T> da file JSON
        public static bool DeserializeJSONFileToObject<T>(string jsonFilePath, out T? jsonObject) where T : class
        {
            jsonObject = null;

            //  Copio il contenuto del file JSON in una stringa
            try 
            { 
                string json = File.ReadAllText(jsonFilePath);
                jsonObject = JsonSerializer.Deserialize<T>(json);

                return jsonObject != null;
            }
            catch 
            {
                return false;
            }
        }

        //  Serializza un oggetto <T> in una stringa JSON
        public static string SerializeToJSONString<T>(T anyobject)
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                ReferenceHandler = ReferenceHandler.Preserve,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };
            return JsonSerializer.Serialize(anyobject, options);
        }

        //  Deserializza un oggetto <T> da una stringa JSON
        public static T? DeserializeJSONStringToObject<T>(string jsonString) where T : class
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                ReferenceHandler = ReferenceHandler.Preserve,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
            };

            //  Copio il contenuto del file JSON in una stringa
            try
            {
                return JsonSerializer.Deserialize<T>(jsonString, options);
            }
            catch
            {
                return null;
            }
        }

    }
}
