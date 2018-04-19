using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

#pragma warning disable 1591

namespace Frends.Community.Oracle.Query
{
    public enum QueryReturnType { Json, Xml};
    
    /// <summary>
    /// Enumerator representing oracle parameter data types
    /// </summary>
    public enum QueryParameterType
    {
        NVarchar2, Varchar2, NChar, Char, Int16, Int32, Int64, Double, Decimal, Long, LongRaw, Boolean, Date, TimeStamp, TimeStampLTZ, TimeStampTZ, XmlType, Raw, BFile, BinaryDouble, BinaryFloat, Blob, Byte, Clob, NClob, IntervalDS, IntervalYM, RefCursor, Single
    }

    public class ConnectionProperties
    {
        /// <summary>
        /// Oracle connection string
        /// </summary>
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=MyHost)(PORT=MyPort))(CONNECT_DATA=(SERVICE_NAME=MyOracleSID)));User Id=myUsername;Password=myPassword;")]
        public string ConnectionString { get; set; }

        /// <summary>
        /// Timeout value in seconds
        /// </summary>
        [DefaultValue(30)]
        public int TimeoutSeconds { get; set; }

    }
    public class QueryProperties
    {
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("SELECT ColumnName FROM TableName")]
        public string Query { get; set; }

        [DefaultValue(QueryReturnType.Xml)]
        public QueryReturnType ReturnType { get; set; }

        /// <summary>
        /// Xml root element name
        /// </summary>
        [UIHint(nameof(ReturnType), "", QueryReturnType.Xml)]
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("ROWSET")]
        public string RootElementName { get; set; }

        /// <summary>
        /// Xml row element name
        /// </summary>
        [UIHint(nameof(ReturnType), "", QueryReturnType.Xml)]
        [DisplayFormat(DataFormatString = "Text")]
        [DefaultValue("ROW")]
        public string RowElementName { get; set; }

        /// <summary>
        /// The maximum amount of rows to return; defaults to -1 eg. no limit
        /// </summary>
        [DefaultValue(-1)]
        [UIHint(nameof(ReturnType), "", QueryReturnType.Xml)]
        public int MaxmimumRows { get; set; }

        /// <summary>
        /// Specify the culture info to be used when parsing result to JSON. If this is left empty InvariantCulture will be used. List of cultures: https://msdn.microsoft.com/en-us/library/ee825488(v=cs.20).aspx Use the Language Culture Name.
        /// </summary>
        [UIHint(nameof(ReturnType), "", QueryReturnType.Json)]
        [DisplayFormat(DataFormatString = "Text")]
        public string CultureInfo { get; set; }

        /// <summary>
        /// Parameters for the database query
        /// </summary>
        public QueryParameter[] Parameters { get; set; }
    }

    public class QueryParameter
    {
        /// <summary>
        /// The name of the parameter
        /// </summary>
        [DefaultValue("ParameterName")]
        [DisplayFormat(DataFormatString = "Text")]
        public string Name { get; set; }

        /// <summary>
        /// The value of the parameter
        /// </summary>
        [DefaultValue("Parameter value")]
        [DisplayFormat(DataFormatString = "Text")]
        public dynamic Value { get; set; }

        /// <summary>
        /// The type of the parameter
        /// </summary>
        [DefaultValue(QueryParameterType.NVarchar2)]
        public QueryParameterType DataType { get; set; }
    }

    public class Options
    {
        /// <summary>
        /// Choose if error should be thrown if Task failes.
        /// Otherwise returns Object {Success = false }
        /// </summary>
        [DefaultValue(true)]
        public bool ThrowErrorOnFailure { get; set; }
    }

    public class Output
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Result { get; set; }
    }
}
