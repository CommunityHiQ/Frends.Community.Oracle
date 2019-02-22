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

namespace Frends.Community.Oracle.Query
{
    static class Extensions
    {
        public static TEnum ConvertEnum<TEnum>(this Enum source)
        {
            return (TEnum)Enum.Parse(typeof(TEnum), source.ToString(), true);
        }

        /// <summary>
        /// Write query results to csv string or file
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ToXmlAsync(this OracleCommand command, OutputProperties output, CancellationToken cancellationToken)
        {
            command.CommandType = CommandType.Text;
            
            // utf-8 as default encoding
            Encoding encoding = string.IsNullOrWhiteSpace(output.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(output.OutputFile.Encoding);

            using (TextWriter writer = output.OutputToFile ? new StreamWriter(output.OutputFile.Path, false, encoding) : new StringWriter() as TextWriter)
            using (OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { Async = true, Indent = true }))
                {
                    await xmlWriter.WriteStartDocumentAsync();
                    await xmlWriter.WriteStartElementAsync("", output.XmlOutput.RootElementName, "");

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        // single row element container
                        await xmlWriter.WriteStartElementAsync("", output.XmlOutput.RowElementName, "");

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            await xmlWriter.WriteElementStringAsync("", reader.GetName(i), "", reader.GetValue(i).ToString());
                        }

                        // close single row element container
                        await xmlWriter.WriteEndElementAsync();

                        // write only complete elements, but stop if process was terminated
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    await xmlWriter.WriteEndElementAsync();
                    await xmlWriter.WriteEndDocumentAsync();
                }

                if (output.OutputToFile)
                {
                    return output.OutputFile.Path;
                }
                else
                {
                    return writer.ToString();
                }
            }
        }

        /// <summary>
        /// Write query results to json string or file
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ToJsonAsync(this OracleCommand command, OutputProperties output, CancellationToken cancellationToken)
        {
            command.CommandType = CommandType.Text;
            
            using (OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            {
                var culture = String.IsNullOrWhiteSpace(output.JsonOutput.CultureInfo) ? CultureInfo.InvariantCulture : new CultureInfo(output.JsonOutput.CultureInfo);

                // utf-8 as default encoding
                Encoding encoding = string.IsNullOrWhiteSpace(output.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(output.OutputFile.Encoding);

                // create json result
                using (var fileWriter = output.OutputToFile ? new StreamWriter(output.OutputFile.Path, false, encoding) : null)
                using (var writer = output.OutputToFile ? new JsonTextWriter(fileWriter) : new JTokenWriter() as JsonWriter)
                {
                    writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                    writer.Culture = culture;

                    // start array
                    await writer.WriteStartArrayAsync(cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    while (reader.Read())
                    {
                        // start row object
                        await writer.WriteStartObjectAsync(cancellationToken);

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            // add row element name
                            await writer.WritePropertyNameAsync(reader.GetName(i), cancellationToken);
                            
                            // add row element value
                            switch (reader.GetDataTypeName(i))
                            {
                                case "Decimal":
                                    // FCOM-204 fix; proper handling of decimal values and NULL values in decimal type fields
                                    OracleDecimal v = reader.GetOracleDecimal(i);
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

                        await writer.WriteEndObjectAsync(cancellationToken); // end row object

                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    // end array
                    await writer.WriteEndArrayAsync(cancellationToken);

                    if (output.OutputToFile)
                    {
                        return output.OutputFile.Path;
                    }
                    else
                    {
                        return ((JTokenWriter)writer).Token.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Write query results to csv string or file
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ToCsvAsync(this OracleCommand command, OutputProperties output, CancellationToken cancellationToken)
        {
            command.CommandType = CommandType.Text;

            // utf-8 as default encoding
            Encoding encoding = string.IsNullOrWhiteSpace(output.OutputFile?.Encoding) ? Encoding.UTF8 : Encoding.GetEncoding(output.OutputFile.Encoding);

            using (OracleDataReader reader = await command.ExecuteReaderAsync(cancellationToken) as OracleDataReader)
            using (TextWriter w = output.OutputToFile ? new StreamWriter(output.OutputFile.Path, false, encoding) : new StringWriter() as TextWriter)
            {
                bool headerWritten = false;

                while (await reader.ReadAsync(cancellationToken))
                {
                    // write csv header if necessary
                    if (!headerWritten && output.CsvOutput.IncludeHeaders)
                    {
                        var fieldNames = new object[reader.FieldCount];
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            fieldNames[i] = reader.GetName(i);
                        }
                        await w.WriteLineAsync(string.Join(output.CsvOutput.CsvSeparator, fieldNames));
                        headerWritten = true;
                    }

                    var fieldValues = new object[reader.FieldCount];
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        fieldValues[i] = reader.GetValue(i);
                    }
                    await w.WriteLineAsync(string.Join(output.CsvOutput.CsvSeparator, fieldValues));

                    // write only complete rows, but stop if process was terminated
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (output.OutputToFile)
                {
                    return output.OutputFile.Path;
                }
                else
                {
                    return w.ToString();
                }
            }
        }
    }
}
