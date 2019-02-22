using Oracle.ManagedDataAccess.Client;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace Frends.Community.Oracle.Query
{
    public class QueryTask
    {
        /// <summary>
        /// Task for performing queries in Oracle databases. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle.Query
        /// </summary>
        /// <param name="query"></param>
        /// <param name="output"></param>
        /// <param name="connection"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Object { bool Success, string Message, string Result }</returns>
        public static async Task<Output> Query(
            [PropertyTab] QueryProperties query,
            [PropertyTab] OutputProperties output,
            [PropertyTab] ConnectionProperties connection,
            [PropertyTab] Options options,
            CancellationToken cancellationToken)
        {
            try
            {
                using (var c = new OracleConnection(connection.ConnectionString))
                {
                    try
                    {
                        await c.OpenAsync(cancellationToken);

                        using (var command = new OracleCommand(query.Query, c))
                        {
                            command.CommandTimeout = connection.TimeoutSeconds;
                            command.BindByName = true; // is this xmlCommand specific?

                            // check for command parameters and set them
                            if (query.Parameters != null)
                                command.Parameters.AddRange(query.Parameters.Select(p => CreateOracleParameter(p)).ToArray());

                            // declare Result object
                            string queryResult;

                            // set commandType according to ReturnType
                            switch (output.ReturnType)
                            {
                                case QueryReturnType.Xml:
                                    queryResult = await command.ToXmlAsync(output, cancellationToken);
                                    break;
                                case QueryReturnType.Json:
                                    queryResult = await command.ToJsonAsync(output, cancellationToken);
                                    break;
                                case QueryReturnType.Csv:
                                    queryResult = await command.ToCsvAsync(output, cancellationToken);
                                    break;
                                default:
                                    throw new ArgumentException("Task 'Return Type' was invalid! Check task properties.");
                            }

                            return new Output { Success = true, Result = queryResult };
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    finally
                    {
                        // Close connection
                        c.Dispose();
                        c.Close();
                        OracleConnection.ClearPool(c);
                    }
                }
            }
            catch (Exception ex)
            {
                if (options.ThrowErrorOnFailure)
                    throw ex;
                return new Output
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public static OracleParameter CreateOracleParameter(QueryParameter parameter)
        {
            return new OracleParameter()
            {
                ParameterName = parameter.Name,
                Value = parameter.Value,
                OracleDbType = parameter.DataType.ConvertEnum<OracleDbType>()
            };
        }
    }
}
