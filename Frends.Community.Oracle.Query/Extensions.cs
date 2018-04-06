using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frends.Community.Oracle.Query
{
    static class Extensions
    {
        public static TEnum ConvertEnum<TEnum>(this Enum source)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), source.ToString(), true);
        }

        public static string ToJson(this OracleDataReader reader, string cultureInfo)
        {
            var culture = String.IsNullOrWhiteSpace(cultureInfo) ? CultureInfo.InvariantCulture : new CultureInfo(cultureInfo);
            // create json result
            using (var writer = new JTokenWriter())
            {
                writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                writer.Culture = culture;

                // start array
                writer.WriteStartArray();

                while (reader.Read())
                {
                    // start row object
                    writer.WriteStartObject();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        // add row element name
                        writer.WritePropertyName(reader.GetName(i));
                        // add row element value
                        writer.WriteValue(reader.GetValue(i) ?? string.Empty);
                    }
                    writer.WriteEndObject(); // end row object
                }
                // end array
                writer.WriteEndArray();

                return writer.Token.ToString();
            }
        }
    }
}
