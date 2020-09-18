using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Dynamic;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Dapper;

#pragma warning disable 1591

namespace Frends.Community.Oracle
{
    public class OracleTasks
    {

        /// <summary>
        /// Task for performing queries in Oracle databases. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle.Query
        /// </summary>
        /// <param name="queryInput"></param>
        /// <param name="queryOutput"></param>
        /// <param name="queryOptions"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Object { bool Success, string Message, string Result }</returns>
        public static async Task<Output> Query(
            [PropertyTab] QueryProperties queryInput,
            [PropertyTab] QueryOutputProperties queryOutput,
            [PropertyTab] QueryOptions queryOptions,
            CancellationToken cancellationToken)
        {
            try
            {
                using (var c = new OracleConnection(queryInput.ConnectionString))
                {
                    try
                    {
                        await c.OpenAsync(cancellationToken);

                        using (var command = new OracleCommand(queryInput.Query, c))
                        {
                            command.CommandTimeout = queryOptions.TimeoutSeconds;
                            command.BindByName = true; // is this xmlCommand specific?

                            // check for command parameters and set them
                            if (queryInput.Parameters != null)
                                command.Parameters.AddRange(queryInput.Parameters.Select(p => CreateOracleParameter(p)).ToArray());

                            // declare Result object
                            string queryResult;

                            if (queryOptions.IsolationLevel == Oracle_IsolationLevel.None)
                            {
                                // set commandType according to ReturnType
                                switch (queryOutput.ReturnType)
                                {
                                    case QueryReturnType.Xml:
                                        queryResult = await command.ToXmlAsync(queryOutput, cancellationToken);
                                        break;
                                    case QueryReturnType.Json:
                                        queryResult = await command.ToJsonAsync(queryOutput, cancellationToken);
                                        break;
                                    case QueryReturnType.Csv:
                                        queryResult = await command.ToCsvAsync(queryOutput, cancellationToken);
                                        break;
                                    default:
                                        throw new ArgumentException("Task 'Return Type' was invalid! Check task properties.");
                                }
                                return new Output { Success = true, Result = queryResult };
                            }
                            else
                            {
                                OracleTransaction txn = c.BeginTransaction(queryOptions.IsolationLevel.GetTransactionIsolationLevel()); ;

                                try
                                {

                                    // set commandType according to ReturnType
                                    switch (queryOutput.ReturnType)
                                    {
                                        case QueryReturnType.Xml:
                                            queryResult = await command.ToXmlAsync(queryOutput, cancellationToken);
                                            txn.Commit();
                                            break;
                                        case QueryReturnType.Json:
                                            queryResult = await command.ToJsonAsync(queryOutput, cancellationToken);
                                            txn.Commit();
                                            break;
                                        case QueryReturnType.Csv:
                                            queryResult = await command.ToCsvAsync(queryOutput, cancellationToken);
                                            txn.Commit();
                                            break;
                                        default:
                                            throw new ArgumentException("Task 'Return Type' was invalid! Check task properties.");
                                    }
                                }
                                catch (Exception) {

                                    txn.Rollback();
                                    txn.Dispose();
                                    throw;
                                }

                                txn.Dispose();
                                return new Output { Success = true, Result = queryResult };
                            }
                        }
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
                if (queryOptions.ThrowErrorOnFailure)
                    throw ex;
                return new Output
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }


        /// <summary>
        /// Task for performing queries in Oracle databases. See documentation at https://github.com/CommunityHiQ/Frends.Community.Oracle.Query
        /// </summary>
        /// <param name="input">Input parameters</param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Object { bool Success, string Message, string Result }</returns>
        public static async Task<BatchOperationOutput> BatchOperation(
            [PropertyTab] InputBatchOperation input,
            [PropertyTab] BatchOptions options,
            CancellationToken cancellationToken)
        {
            try
            {
                using (var c = new OracleConnection(input.ConnectionString))
                {
                    try
                    {
                        await c.OpenAsync(cancellationToken);

                        using (var command = new OracleCommand(input.Query, c))
                        {
                            command.CommandTimeout = options.TimeoutSeconds;
                            command.BindByName = true; // is this xmlCommand specific?

                            // declare Result object
                            int queryResult;


                            if (options.IsolationLevel == Oracle_IsolationLevel.None)
                            {
                                var obj = JsonConvert.DeserializeObject<ExpandoObject[]>(input.InputJson,
                                    new ExpandoObjectConverter());
                                queryResult = await c.ExecuteAsync(
                                        input.Query,
                                        param: obj,
                                        commandTimeout: options.TimeoutSeconds,
                                        commandType: CommandType.Text)
                                    .ConfigureAwait(false);

                                return new BatchOperationOutput { Success = true, Result = queryResult };
                            }
                        
                            else
                            {
                                OracleTransaction txn =
                                    c.BeginTransaction(options.IsolationLevel.GetTransactionIsolationLevel());
                                try
                                {
                                    var obj = JsonConvert.DeserializeObject<ExpandoObject[]>(input.InputJson,
                                        new ExpandoObjectConverter());
                                    queryResult = await c.ExecuteAsync(
                                            input.Query,
                                            param: obj,
                                            commandTimeout: options.TimeoutSeconds,
                                            commandType: CommandType.Text,
                                            transaction: txn)
                                        .ConfigureAwait(false);

                                    txn.Commit();
                                    txn.Dispose();

                                    return new BatchOperationOutput {Success = true, Result = queryResult};
                                }
                                catch (Exception)
                                {
                                    txn.Rollback();
                                    txn.Dispose();
                                    throw;
                                }
                            }
                        }
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
                    throw;
                return new BatchOperationOutput
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
