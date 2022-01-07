using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Frends.Community.Oracle
{
    static class Extensions
    {

        internal static IsolationLevel GetTransactionIsolationLevel(this Oracle_IsolationLevel Oracle_IsolationLevel)
        {
            return GetEnum<IsolationLevel>(Oracle_IsolationLevel);
        }

        private static T GetEnum<T>(Enum enumValue)
        {
            return (T)Enum.Parse(typeof(T), enumValue.ToString());
        }

        public static TEnum ConvertEnum<TEnum>(this Enum source)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), source.ToString(), true);
        }

        private static string ParseOracleDate(OracleDataReader reader, int index, string dateFormat)
        {
            if (string.IsNullOrWhiteSpace(dateFormat)) return reader.GetValue(index).ToString();

            var dateType = reader.GetDataTypeName(index);
            var dateString = ""; // Formatted output date.
            
            switch (dateType)
            {
                case "Date":
                    var oDate = reader.GetOracleDate(index);
                    if(!oDate.IsNull)
                    {
                        var dt = new DateTime(oDate.Year,oDate.Month,oDate.Day,oDate.Hour,oDate.Minute,oDate.Second);
                        dateString = dt.ToString(dateFormat);
                    }
                    break;
                case "TimeStamp":
                    var oTimeStamp = reader.GetOracleTimeStamp(index);
                    if(!oTimeStamp.IsNull)
                    {
                        // Is this the best way to get milliseconds from double to int?
                        int.TryParse(oTimeStamp.Millisecond.ToString("000").Substring(0, 3), out int msOut);
                        var dt = new DateTime(oTimeStamp.Year, oTimeStamp.Month, oTimeStamp.Day, oTimeStamp.Hour, oTimeStamp.Minute, oTimeStamp.Second, msOut);
                        dateString = dt.ToString(dateFormat);
                    }
                    break;
                case "TimeStampLTZ":
                    var oTimeStampLTZ = reader.GetOracleTimeStampLTZ(index);
                    if(!oTimeStampLTZ.IsNull)
                    {
                        // Is this the best way to get milliseconds from double to int?
                        int.TryParse(oTimeStampLTZ.Millisecond.ToString("000").Substring(0, 3), out int msOut);
                        var dt = new DateTime(oTimeStampLTZ.Year, oTimeStampLTZ.Month, oTimeStampLTZ.Day, oTimeStampLTZ.Hour, oTimeStampLTZ.Minute, oTimeStampLTZ.Second, msOut);
                        dateString = dt.ToString(dateFormat);
                    }
                    break;
                case "TimeStampTZ":
                    var oTimeStampTZ = reader.GetOracleTimeStampTZ(index);
                    if(!oTimeStampTZ.IsNull)
                    {
                        // Is this the best way to get milliseconds from double to int?
                        int.TryParse(oTimeStampTZ.Millisecond.ToString("000").Substring(0, 3), out int msOut);
                        var dt = new DateTime(oTimeStampTZ.Year, oTimeStampTZ.Month, oTimeStampTZ.Day, oTimeStampTZ.Hour, oTimeStampTZ.Minute, oTimeStampTZ.Second, msOut);
                        dateString = dt.ToString(dateFormat);
                    }
                    break;
                default:
                    throw new Exception("Trying to parse unknown date type.");
            }
            return dateString;
        }

        /// <summary>
        /// Write query results to csv string or file.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="queryOutput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>String</returns>
        public static async Task<string> ToXmlAsync(this OracleCommand command, QueryOutputProperties queryOutput, CancellationToken cancellationToken)
        {
            var encoding = string.IsNullOrWhiteSpace(queryOutput.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(queryOutput.OutputFile.Encoding);

            using (var writer = queryOutput.OutputToFile ? new StreamWriter(queryOutput.OutputFile.Path, false, encoding) : new StringWriter() as TextWriter)
            using (var reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            {
                using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Async = true, Indent = true }))
                {
                    await xmlWriter.WriteStartDocumentAsync();
                    await xmlWriter.WriteStartElementAsync("", queryOutput.XmlOutput.RootElementName, "");

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        // single row element container.
                        await xmlWriter.WriteStartElementAsync("", queryOutput.XmlOutput.RowElementName, "");

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            switch (reader.GetDataTypeName(i))
                            {
                                case "Decimal":
                                    var v = reader.GetOracleDecimal(i);
                                    var decimalValue = OracleDecimal.SetPrecision(v, 28);
                                    string decimalString = decimalValue.ToString();
                                    // Is decimal separator overwrite value given and query result value is not null?
                                    if (!string.IsNullOrWhiteSpace(queryOutput.XmlOutput.DecimalSeparator))
                                    {
                                        decimalString = decimalString
                                            .Replace(".", queryOutput.XmlOutput.DecimalSeparator)
                                            .Replace(",", queryOutput.XmlOutput.DecimalSeparator);
                                    }

                                    await xmlWriter.WriteElementStringAsync("", reader.GetName(i), "", decimalString);
                                    break;
                                case "Date":
                                case "TimeStamp":
                                case "TimeStampLTZ":
                                case "TimeStampTZ":
                                    string dateString = ParseOracleDate(reader,
                                                                        i,
                                                                        queryOutput.XmlOutput.DateTimeFomat);
                                    await xmlWriter.WriteElementStringAsync("", reader.GetName(i), "", dateString);
                                    break;

                                default:
                                    await xmlWriter.WriteElementStringAsync("", reader.GetName(i), "", reader.GetValue(i).ToString());
                                    break;
                            }
                        }

                        // Close single row element container.
                        await xmlWriter.WriteEndElementAsync();

                        // Write only complete elements, but stop if process was terminated.
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    await xmlWriter.WriteEndElementAsync();
                    await xmlWriter.WriteEndDocumentAsync();
                }

                if (queryOutput.OutputToFile)
                {
                    return queryOutput.OutputFile.Path;
                }
                else
                {
                    return writer.ToString();
                }
            }
        }

        /// <summary>
        /// Write query results to json string or file.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="queryOutput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>String</returns>
        public static async Task<string> ToJsonAsync(this OracleCommand command, QueryOutputProperties queryOutput, CancellationToken cancellationToken)
        {
            using (var reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            {
                var culture = string.IsNullOrWhiteSpace(queryOutput.JsonOutput.CultureInfo) ? CultureInfo.InvariantCulture : new CultureInfo(queryOutput.JsonOutput.CultureInfo);

                // UTF-8 as default encoding.
                var encoding = string.IsNullOrWhiteSpace(queryOutput.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(queryOutput.OutputFile.Encoding);

                // Create json result.
                using (var fileWriter = queryOutput.OutputToFile ? new StreamWriter(queryOutput.OutputFile.Path, false, encoding) : null)
                using (var writer = queryOutput.OutputToFile ? new JsonTextWriter(fileWriter) : new JTokenWriter() as JsonWriter)
                {
                    writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                    writer.Culture = culture;

                    // Start array.
                    await writer.WriteStartArrayAsync(cancellationToken);

                    while (reader.Read())
                    {
                        // Start row object.
                        await writer.WriteStartObjectAsync(cancellationToken);

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            // Add row element name.
                            await writer.WritePropertyNameAsync(reader.GetName(i), cancellationToken);

                            // Add row element value.
                            switch (reader.GetDataTypeName(i))
                            {
                                case "Decimal":
                                    // FCOM-204 fix; proper handling of decimal values and NULL values in decimal type fields.
                                    var v = reader.GetOracleDecimal(i);
                                    var FieldValue = OracleDecimal.SetPrecision(v, 28);

                                    if (!FieldValue.IsNull) await writer.WriteValueAsync((decimal)FieldValue, cancellationToken);
                                    else await writer.WriteValueAsync(string.Empty, cancellationToken);
                                    break;
                                case "Date":
                                case "TimeStamp":
                                case "TimeStampLTZ":
                                case "TimeStampTZ":
                                    string dateString = ParseOracleDate(reader, i, queryOutput.JsonOutput.DateTimeFomat);
                                    await writer.WriteValueAsync(dateString, cancellationToken);
                                    break;
                                default:
                                    await writer.WriteValueAsync(reader.GetValue(i) ?? string.Empty, cancellationToken);
                                    break;
                            }
                        }
                        cancellationToken.ThrowIfCancellationRequested();
                        // End row object.
                        await writer.WriteEndObjectAsync(cancellationToken);
                    }

                    // End array.
                    await writer.WriteEndArrayAsync(cancellationToken);

                    if (queryOutput.OutputToFile)
                    {
                        return queryOutput.OutputFile.Path;
                    }
                    else
                    {
                        return ((JTokenWriter)writer).Token.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Write query results to csv string or file.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="queryOutput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>String</returns>
        public static async Task<string> ToCsvAsync(this OracleCommand command, QueryOutputProperties queryOutput, CancellationToken cancellationToken)
        {
            var encoding = string.IsNullOrWhiteSpace(queryOutput.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(queryOutput.OutputFile.Encoding);

            using (var reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            using (var w = queryOutput.OutputToFile ? new StreamWriter(queryOutput.OutputFile.Path, false, encoding) : new StringWriter() as TextWriter)
            {
                var headerWritten = false;

                while (await reader.ReadAsync(cancellationToken))
                {
                    // Initiate string builder for the line.
                    var sb = new StringBuilder();

                    // Write csv header if necessary.
                    if (!headerWritten && queryOutput.CsvOutput.IncludeHeaders)
                    {
                        var fieldNames = new object[reader.FieldCount];
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var fieldName = reader.GetName(i);
                            sb.Append(fieldName);
                            if (i < reader.FieldCount - 1)
                            {
                                sb.Append(queryOutput.CsvOutput.CsvSeparator);
                            }
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        await w.WriteLineAsync(sb.ToString());
                        headerWritten = true;
                    }

                    sb = new StringBuilder();
                    var fieldValues = new object[reader.FieldCount];
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        switch (reader.GetDataTypeName(i))
                        {
                            case "Decimal":
                                var v = reader.GetOracleDecimal(i);
                                var decimalValue= OracleDecimal.SetPrecision(v, 28);
                                // Is decimal separator overwrite value given and query result value is not null?
                                if (!string.IsNullOrWhiteSpace(queryOutput.CsvOutput.DecimalSeparator) && !decimalValue.IsNull)
                                {
                                    fieldValues[i] = decimalValue.ToString()
                                        .Replace(".", queryOutput.CsvOutput.DecimalSeparator)
                                        .Replace(",", queryOutput.CsvOutput.DecimalSeparator);
                                }
                                else
                                    fieldValues[i] = decimalValue;
                                break;
                            case "Date":
                            case "TimeStamp":
                            case "TimeStampLTZ":
                            case "TimeStampTZ":
                                var dateString = ParseOracleDate(reader, i, queryOutput.CsvOutput.DateTimeFomat);
                                fieldValues[i] = dateString;
                                break;
                            default:
                                fieldValues[i] = reader.GetValue(i);
                                break;
                        }

                        var fieldValue = fieldValues[i].ToString();
                        // Remove possible line breaks.
                        fieldValue = fieldValue.Replace("\r\n"," ").Replace("\n"," ").Replace("\r"," ");

                        sb.Append(fieldValue);
                        if (i < reader.FieldCount - 1)
                        {
                            sb.Append(queryOutput.CsvOutput.CsvSeparator);
                        }
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    await w.WriteLineAsync(sb.ToString());
                }

                if (queryOutput.OutputToFile)
                {
                    return queryOutput.OutputFile.Path;
                }
                else
                {
                    return w.ToString();
                }
            }
        }

        /// <summary>
        /// Write query results to json string or file.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="queryOutput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>JToken</returns>
        public static async Task<object> MultiQueryToJsonAsync(this OracleCommand command, QueryOutputProperties queryOutput, CancellationToken cancellationToken)
        {
            using (var reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            {
                var culture = string.IsNullOrWhiteSpace(queryOutput.JsonOutput.CultureInfo) ? CultureInfo.InvariantCulture : new CultureInfo(queryOutput.JsonOutput.CultureInfo);

                // UTF-8 as default encoding.
                var encoding = string.IsNullOrWhiteSpace(queryOutput.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(queryOutput.OutputFile.Encoding);

                // Create json result.
                using (var writer = new JTokenWriter() as JsonWriter)
                {
                    writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                    writer.Culture = culture;

                    // Start array.
                    await writer.WriteStartArrayAsync(cancellationToken);

                    while (reader.Read())
                    {
                        // Start row object.
                        await writer.WriteStartObjectAsync(cancellationToken);

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            // Add row element name.
                            await writer.WritePropertyNameAsync(reader.GetName(i), cancellationToken);

                            // Add row element value.
                            switch (reader.GetDataTypeName(i))
                            {
                                case "Decimal":
                                    // FCOM-204 fix; proper handling of decimal values and NULL values in decimal type fields.
                                    var v = reader.GetOracleDecimal(i);
                                    var FieldValue = OracleDecimal.SetPrecision(v, 28);

                                    if (!FieldValue.IsNull) await writer.WriteValueAsync((decimal)FieldValue, cancellationToken);
                                    else await writer.WriteValueAsync(string.Empty, cancellationToken);
                                    break;
                                default:
                                    await writer.WriteValueAsync(reader.GetValue(i) ?? string.Empty, cancellationToken);
                                    break;
                            }

                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        // End row object.
                        await writer.WriteEndObjectAsync(cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    // End array.
                    await writer.WriteEndArrayAsync(cancellationToken);

                    return ((JTokenWriter)writer).Token;

                }
            }
        }

        /// <summary>
        /// Write query results to json string or file.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="queryOutput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>String</returns>
        public static async Task<string> MultiQueryToCSVAsync(this OracleCommand command, QueryOutputProperties queryOutput, CancellationToken cancellationToken)
        {
            using (var reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            {
                using (var writer = new StringWriter())
                {
                    var headerWritten = false;

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        // Initiate string builder for the line.
                        var sb = new StringBuilder();

                        // write csv header if necessary
                        if (!headerWritten && queryOutput.CsvOutput.IncludeHeaders)
                        {
                            var fieldNames = new object[reader.FieldCount];
                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                var fieldName = reader.GetName(i);
                                sb.Append(fieldName);
                                if (i < reader.FieldCount - 1)
                                {
                                    sb.Append(queryOutput.CsvOutput.CsvSeparator);
                                }
                                cancellationToken.ThrowIfCancellationRequested();
                            }
                            await writer.WriteLineAsync(sb.ToString());
                            headerWritten = true;
                        }

                        sb = new StringBuilder();
                        var fieldValues = new object[reader.FieldCount];
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.GetDataTypeName(i).Equals("Decimal"))
                            {
                                var v = reader.GetOracleDecimal(i);
                                fieldValues[i] = OracleDecimal.SetPrecision(v, 28);
                            }
                            else
                            {
                                fieldValues[i] = reader.GetValue(i);
                            }
                            var fieldValue = fieldValues[i].ToString();
                            sb.Append(fieldValue);
                            if (i < reader.FieldCount - 1)
                            {
                                sb.Append(queryOutput.CsvOutput.CsvSeparator);
                            }
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                        await writer.WriteLineAsync(sb.ToString());

                        // Write only complete rows, but stop if process was terminated.
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    return writer.ToString();

                }
            }
        }

        /// <summary>
        /// Write query results to csv string or file.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="queryOutput"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>String</returns>
        public static async Task<string> MultiQueryToXmlAsync(this OracleCommand command, QueryOutputProperties queryOutput, CancellationToken cancellationToken)
        {
            using (var writer = new StringWriter())
            {
                using (var reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
                {
                    using (var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Async = true, Indent = true }))
                    {
                        await xmlWriter.WriteStartDocumentAsync();
                        await xmlWriter.WriteStartElementAsync("", queryOutput.XmlOutput.RootElementName, "");

                        while (await reader.ReadAsync(cancellationToken))
                        {
                            // Single row element container.
                            await xmlWriter.WriteStartElementAsync("", queryOutput.XmlOutput.RowElementName, "");

                            for (var i = 0; i < reader.FieldCount; i++)
                            {
                                if (reader.GetDataTypeName(i).Equals("Decimal"))
                                {
                                    var v = reader.GetOracleDecimal(i);
                                    OracleDecimal.SetPrecision(v, 28);
                                    await xmlWriter.WriteElementStringAsync("", reader.GetName(i), "", v.ToString());
                                }
                                else
                                {
                                    await xmlWriter.WriteElementStringAsync("", reader.GetName(i), "", reader.GetValue(i).ToString());
                                }
                                cancellationToken.ThrowIfCancellationRequested();
                            }

                            // Close single row element container.
                            await xmlWriter.WriteEndElementAsync();

                            // Write only complete elements, but stop if process was terminated.
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        await xmlWriter.WriteEndElementAsync();
                        await xmlWriter.WriteEndDocumentAsync();
                    }

                    return writer.ToString();

                }

            }
        }


    }

}
