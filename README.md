# Frends.Community.Oracle.Query

FRENDS 4 Task for querying data from Oracle database

- [Installing](#installing)
- [Task](#tasks)
	- [Query](#query)
- [Building](#building)
- [Contributing](#contributing)
- [Change Log](#change-log)

# Installing

You can install the task via FRENDS UI Task View or you can find the nuget package from the following nuget feed
'Insert nuget feed here'

# Task

## Query

Executes query against Oracle database.

### Query Properties
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Query | string | The query to execute | `SELECT * FROM Table WHERE field = :paramName`|
| Parameters | array[Query Parameter] | Possible query parameters. See [Query Parameter](#query-parameter) |  |

#### Query Parameter

| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Name | string | Parameter name used in Query property | `username` |
| Value | string | Parameter value | `myUser` |
| Data type | enum<> | Parameter data type | `NVarchar2` |

### Output
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Return type | enum<Json, Xml, Csv> | Data return type format | `Json` |
| OutputToFile | bool | true to write results to a file, false to return results to executin process | `true` |

### XmlOutputProperties
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| RootElementName | string | Xml root element name | `items` |
| RowElementName |string | Xml row element name | `item` |
| MaximumRows | int | The maximum amount of rows to return; defaults to -1 eg. no limit | `1000` |

### JsonOutputProperties
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Culture info | string | Specify the culture info to be used when parsing result to JSON. If this is left empty InvariantCulture will be used. [List of cultures](https://msdn.microsoft.com/en-us/library/ee825488(v=cs.20).aspx) Use the Language Culture Name. | `fi-FI` |

### CsvOutputProperties
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| IncludeHeaders | bool | Include field names in the first row | `true` |
| CsvSeparator | string | Csv separator to use in headers and data items | `;` |

### OutputFileProperties
| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Path | string | Output path with file name | `c:\temp\output.json` |
| Encoding | string | Encoding to use for the output file | `utf-8` |

### Connection

| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Connection string | string | Oracle database connection string | `Data Source=(DESCRIPTION=(ADDRESS = (PROTOCOL = TCP)(HOST = oracleHost)(PORT = 1521))(CONNECT_DATA = (SERVICE_NAME = MYSERVICE)))` |
| Timeout seconds | int | Query timeout in seconds | `60` |

### Options

| Property    | Type       | Description     | Example |
| ------------| -----------| --------------- | ------- |
| Throw error on failure | bool | Specify if Exception should be thrown when error occurs. If set to *false*, task outcome can be checked from #result.Success property. | `false` |

#### Result

Object { bool Success, string Message, string Result }

If output type is file, then _Result_ indicates the written file path. Otherwise it will hold the query output in xml, json or csv.

Example result with return type JSON

*Success:* ``` True ```
*Message:* ``` null ```
*Result:* 
```
[ 
 {
  "Name": "Teela",
  "Age": 42,
  "Address": "Test road 123"
 },
 {
  "Name": "Adam",
  "Age": 42,
  "Address": null
 }
]
```


To access query result, use 
```
#result.Result
```

# Building

Clone a copy of the repo

`git clone https://github.com/CommunityHiQ/Frends.Community.Oracle.Query.git`

Restore dependencies

`nuget restore frends.community.oracle.query`

Rebuild the project

Run Tests with nunit3. Tests can be found under

`Frends.Community.Oracle.Query.Tests\bin\Release\Frends.Community.Oracle.Query.Tests.dll`

Create a nuget package

`nuget pack nuspec/Frends.Community.Oracle.Query.nuspec`

# Contributing
When contributing to this repository, please first discuss the change you wish to make via issue, email, or any other method with the owners of this repository before making a change.

1. Fork the repo on GitHub
2. Clone the project to your own machine
3. Commit changes to your own branch
4. Push your work back up to your fork
5. Submit a Pull request so that we can review your changes

NOTE: Be sure to merge the latest from "upstream" before making a pull request!

# Change Log

| Version | Changes |
| ----- | ----- |
| 1.0.0 | Initial version of Oracle Query Task |
| 2.0.0 | Breaking changes: target .netstandard, more user friendly task settings, csv output, all output types are now possibly to stream directly into a file |
