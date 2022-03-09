namespace TrackTime

open System
open System.Linq
open System.Text
open System.IO
open DataInterfaces
open System.Data
open FSharp.Data
open FirebirdSql.Data.FirebirdClient


//The building blocks of the data access - which we can unit test
module AppDataDonaldSql =
    open DataModels
    open Donald


    let GetDbConnStrFromSettings (settings: Settings.Settings) =
        let dbSettings = settings.DBSettings

        let dbPort =
            match dbSettings.DbPort with
            | Some port -> port
            | None -> 3050

        let dbHost =
            match dbSettings.DbHost with
            | Some host -> host
            | None -> "localhost"

        //sprintf //postgresql
        //    "Username=%s;Password=%s;Database=%s;Host=%s;Port=%d;Pooling=true;ConnectionLifetime=60;Application Name=TrackTime"
        //    settings.DbUser
        //    settings.DbPassword
        //    settings.DbName
        //    dbHost
        //    dbPort

        sprintf //firebird
            "User=%s;Password=%s;Database=%s;DataSource=%s;Port=%d;Dialect=3;Charset=UTF8;Pooling=true;Connection Lifetime=60"
            dbSettings.DbUser
            dbSettings.DbPassword
            dbSettings.DbName
            dbHost
            dbPort

    type GetDbConnection = unit -> FbConnection

    let getDbConnectionWithConnStr connStr : FbConnection =
        let conn = new FbConnection(connStr)
        conn.TryOpenConnection()
        conn

    let private getConnStr = Settings.getSettings >> GetDbConnStrFromSettings

    let mutable private connStrCache: string option = None

    let private connStr () =
        match connStrCache with
        | Some cs -> cs
        | None ->
            let cs = getConnStr ()
            connStrCache <- Some(cs)
            cs

    let connectDB = connStr >> getDbConnectionWithConnStr

    let private dbErrorExn operationDescription (err: DbError) =
        let msg, e =
            match err with
            | DbConnectionError connError ->
                sprintf "Database connection error occured with connection string %s. %s" connError.ConnectionString connError.Error.Message, connError.Error
            | DbTransactionError tranError ->
                let stepName =
                    match tranError.Step with
                    | TxBegin -> "begin"
                    | TxCommit -> "commit"
                    | TxRollback -> "rollback"

                sprintf "Database transaction error occured at transaction %s. %s" stepName tranError.Error.Message, tranError.Error
            | DbExecutionError execError -> sprintf "Database execution error occured with statement: \n%s\n. %s" execError.Statement execError.Error.Message, execError.Error
            | DataReaderCastError readerCastError ->
                sprintf "Database data reader cast error occured with field %s. %s" readerCastError.FieldName readerCastError.Error.Message, readerCastError.Error
            | DataReaderOutOfRangeError readerRangeError ->
                sprintf "Database data reader out of range error occured with field %s. %s" readerRangeError.FieldName readerRangeError.Error.Message, readerRangeError.Error

        let combined = String.concat " " [ operationDescription; msg ]
        (combined, e) |> exn

    let private dbErrorResult operationDescription (err: DbError) = dbErrorExn operationDescription err |> Error

    let addCustomerToDB (getDbConnection: GetDbConnection) (model: Customer) =
        try
            let idOfDataReader (rd: IDataReader) : int64 = rd.ReadInt64 "CUSTOMER_ID"

            let sql =
                "INSERT INTO CUSTOMER (CUSTOMER_NAME, PHONE, EMAIL, CUSTOMER_STATE, NOTES)
                        VALUES (
                            @CUSTOMER_NAME,
                            @PHONE,
                            @EMAIL,
                            @CUSTOMER_STATE,
                            @NOTES
                        )
                        RETURNING CUSTOMER_ID"

            use conn = getDbConnection ()

            let newIdResult =
                conn
                |> Db.newCommand sql
                |> Db.setParams [ "CUSTOMER_NAME", SqlType.String model.Name.Value
                                  "PHONE",
                                  match model.Phone.Value with
                                  | Some ph -> SqlType.String ph
                                  | None -> SqlType.Null
                                  "EMAIL",
                                  match model.Email.Value with
                                  | Some eml -> SqlType.String eml
                                  | None -> SqlType.Null
                                  "CUSTOMER_STATE",
                                  SqlType.Int16(
                                      match model.CustomerState with
                                      | CustomerState.Active -> 1s
                                      | _ -> 0s
                                  )
                                  "NOTES",
                                  match model.Notes with
                                  | Some notes -> SqlType.String notes
                                  | None -> SqlType.Null ]
                |> Db.querySingle idOfDataReader

            let res =
                match newIdResult with
                | Ok idOption ->
                    match idOption with
                    | Some id -> Ok id
                    | None -> "Error inserting and returning new customer id." |> exn |> Error
                | Error dberror -> dbErrorResult "Error inserting customer and returning new id." dberror

            res
        with
        | ex -> Error ex

    let addWorkItemToDB (getDbConnection: GetDbConnection) (model: WorkItem) =
        try
            let sql =
                "INSERT INTO WORK_ITEM (TITLE, DESCRIPTION, IS_BILLABLE, IS_COMPLETED,
                            IS_FIXED_PRICE, DATE_CREATED, DUE_DATE, BEEN_BILLED, NOTES, CUSTOMER_ID)
                        VALUES (
                            @TITLE,
                            @DESCRIPTION,
                            @IS_BILLABLE,
                            @IS_COMPLETED,
                            @IS_FIXED_PRICE,
                            @DATE_CREATED,
                            @DUE_DATE,
                            @BEEN_BILLED,
                            @NOTES,
                            @CUSTOMER_ID
                        )
                        RETURNING WORK_ITEM_ID"

            use conn = getDbConnection ()
            let idOfDataReader (rd: IDataReader) : int64 = rd.ReadInt64 "WORK_ITEM_ID"

            let newIdResult =
                conn
                |> Db.newCommand sql
                |> Db.setParams [ "TITLE", SqlType.String model.Title.Value
                                  "DESCRIPTION",
                                  match model.Description.Value with
                                  | Some desc -> SqlType.String desc
                                  | None -> SqlType.Null
                                  "IS_BILLABLE", SqlType.Boolean model.IsBillable
                                  "IS_COMPLETED", SqlType.Boolean model.IsCompleted
                                  "IS_FIXED_PRICE", SqlType.Boolean model.IsFixedPrice
                                  "DATE_CREATED", SqlType.DateTime model.DateCreated
                                  "DUE_DATE",
                                  match model.DueDate with
                                  | Some dueDate -> SqlType.DateTime dueDate
                                  | None -> SqlType.Null
                                  "BEEN_BILLED", SqlType.Boolean model.BeenBilled
                                  "NOTES",
                                  match model.Notes with
                                  | Some notes -> SqlType.String notes
                                  | None -> SqlType.Null
                                  "CUSTOMER_ID", SqlType.Int64 model.CustomerId ]

                |> Db.querySingle idOfDataReader

            let res =
                match newIdResult with
                | Ok idOption ->
                    match idOption with
                    | Some id -> Ok id
                    | None -> "Error inserting and returning new work item id." |> exn |> Error
                | Error dberror -> dbErrorResult "Error inserting work item and returning new id" dberror

            res
        with
        | ex -> Error ex

    let addTimeEntryToDB (getDbConnection: GetDbConnection) (model: TimeEntry) =
        try
            let sql =
                "INSERT INTO TIME_ENTRY (DESCRIPTION, TIME_START, TIME_END, BEEN_BILLED, NOTES, IS_BILLABLE,
                            WORK_ITEM_ID)
                        VALUES (
                            @DESCRIPTION,
                            @TIME_START,
                            @TIME_END,
                            @BEEN_BILLED,
                            @NOTES,
                            @IS_BILLABLE,
                            @WORK_ITEM_ID
                        )
                        RETURNING TIME_ENTRY_ID"

            use conn = getDbConnection ()
            let idOfDataReader (rd: IDataReader) : int64 = rd.ReadInt64 "TIME_ENTRY_ID"

            let newIdResult =
                conn
                |> Db.newCommand sql
                |> Db.setParams [ "DESCRIPTION", SqlType.String model.Description.Value
                                  "TIME_START", SqlType.DateTime(model.TimeStart.ToUniversalTime())
                                  "TIME_END",
                                  match model.TimeEnd with
                                  | Some date -> SqlType.DateTime(date.ToUniversalTime())
                                  | None -> SqlType.Null
                                  "BEEN_BILLED", SqlType.Boolean model.BeenBilled
                                  "NOTES",
                                  match model.Notes with
                                  | Some notes -> SqlType.String notes
                                  | None -> SqlType.Null
                                  "IS_BILLABLE", SqlType.Boolean model.IsBillable
                                  "WORK_ITEM_ID", SqlType.Int64 model.WorkItemId ]

                |> Db.querySingle idOfDataReader

            match newIdResult with
            | Ok idOption ->
                match idOption with
                | Some id -> Ok id
                | None -> "Error inserting and returning new time entry id." |> exn |> Error
            | Error dberror -> dbErrorResult "Error inserting time entry and returning new id." dberror

        with
        | ex -> Error ex

    let updateCustomerToDB (getDbConnection: GetDbConnection) (model: Customer) =
        try
            let sql =
                "UPDATE CUSTOMER a
                        SET
                            a.CUSTOMER_NAME = @CUSTOMER_NAME,
                            a.PHONE = @PHONE,
                            a.EMAIL = @EMAIL,
                            a.CUSTOMER_STATE = @CUSTOMER_STATE,
                            a.NOTES = @NOTES
                       WHERE
                            a.CUSTOMER_ID = @CUSTOMER_ID"

            use conn = getDbConnection ()

            let qRes =
                conn
                |> Db.newCommand sql
                |> Db.setParams [ "CUSTOMER_NAME", SqlType.String model.Name.Value
                                  "PHONE",
                                  match model.Phone.Value with
                                  | Some ph -> SqlType.String ph
                                  | None -> SqlType.Null
                                  "EMAIL",
                                  match model.Email.Value with
                                  | Some eml -> SqlType.String eml
                                  | None -> SqlType.Null
                                  "CUSTOMER_STATE", model.CustomerState |> int |> SqlType.Int
                                  "NOTES",
                                  match model.Notes with
                                  | Some notes -> SqlType.String notes
                                  | None -> SqlType.Null
                                  "CUSTOMER_ID", SqlType.Int64 model.CustomerId ]
                |> Db.exec

            match qRes with
            | Ok _ -> Ok true
            | Error dberror -> dbErrorResult "Error updating customer." dberror
        with
        | ex -> Error ex

    let updateWorkItemToDB (getDbConnection: GetDbConnection) (model: WorkItem) =
        try
            let sql =
                "UPDATE WORK_ITEM a
                        SET
                            a.TITLE = @TITLE,
                            a.DESCRIPTION = @DESCRIPTION,
                            a.IS_BILLABLE = @IS_BILLABLE,
                            a.IS_COMPLETED = @IS_COMPLETED,
                            a.IS_FIXED_PRICE = @IS_FIXED_PRICE,
                            a.DATE_CREATED = @DATE_CREATED,
                            a.DUE_DATE = @DUE_DATE,
                            a.BEEN_BILLED = @BEEN_BILLED,
                            a.NOTES = @NOTES,
                            a.CUSTOMER_ID = @CUSTOMER_ID
                        WHERE
                            a.WORK_ITEM_ID = @WORK_ITEM_ID"

            use conn = getDbConnection ()

            let qRes =
                conn
                |> Db.newCommand sql
                |> Db.setParams [ "TITLE", SqlType.String model.Title.Value
                                  "DESCRIPTION",
                                  match model.Description.Value with
                                  | Some desc -> SqlType.String desc
                                  | None -> SqlType.Null
                                  "IS_BILLABLE", SqlType.Boolean model.IsBillable
                                  "IS_COMPLETED", SqlType.Boolean model.IsCompleted
                                  "IS_FIXED_PRICE", SqlType.Boolean model.IsFixedPrice
                                  "DATE_CREATED", SqlType.DateTime model.DateCreated
                                  "DUE_DATE",
                                  match model.DueDate with
                                  | Some dueDate -> SqlType.DateTime dueDate
                                  | None -> SqlType.Null
                                  "BEEN_BILLED", SqlType.Boolean model.BeenBilled
                                  "NOTES",
                                  match model.Notes with
                                  | Some notes -> SqlType.String notes
                                  | None -> SqlType.Null
                                  "CUSTOMER_ID", SqlType.Int64 model.CustomerId
                                  "WORK_ITEM_ID", SqlType.Int64 model.WorkItemId ]
                |> Db.exec

            match qRes with
            | Ok _ -> Ok true
            | Error dberror -> dbErrorResult "Error updating WORK_ITEM." dberror
        with
        | ex -> Error ex

    let updateTimeEntryToDB (getDbConnection: GetDbConnection) (model: TimeEntry) =
        try
            let sql =
                "UPDATE TIME_ENTRY a
                        SET
                            a.DESCRIPTION = @DESCRIPTION,
                            a.TIME_START = @TIME_START,
                            a.TIME_END = @TIME_END,
                            a.BEEN_BILLED = @BEEN_BILLED,
                            a.NOTES = @NOTES,
                            a.WORK_ITEM_ID = @WORK_ITEM_ID
                        WHERE
                            a.TIME_ENTRY_ID = @TIME_ENTRY_ID"

            use conn = getDbConnection ()

            let qRes =
                conn
                |> Db.newCommand sql
                |> Db.setParams [ "DESCRIPTION", SqlType.String model.Description.Value
                                  "TIME_START", SqlType.DateTime(model.TimeStart.ToUniversalTime())
                                  "TIME_END",
                                  match model.TimeEnd with
                                  | Some dueDate -> SqlType.DateTime(dueDate.ToUniversalTime())
                                  | None -> SqlType.Null
                                  "BEEN_BILLED", SqlType.Boolean model.BeenBilled
                                  "NOTES",
                                  match model.Notes with
                                  | Some notes -> SqlType.String notes
                                  | None -> SqlType.Null
                                  "WORK_ITEM_ID", SqlType.Int64 model.WorkItemId
                                  "TIME_ENTRY_ID", SqlType.Int64 model.TimeEntryId ]
                |> Db.exec

            match qRes with
            | Ok _ -> Ok true
            | Error dberror -> dbErrorResult "Error updating time entry table." dberror
        with
        | ex -> Error ex


    let deleteCustomerFromDB (getDbConnection: GetDbConnection) (model: Customer) =
        try
            let sql =
                "DELETE FROM CUSTOMER a
                        WHERE
                            a.CUSTOMER_ID = @CUSTOMER_ID"

            use conn = getDbConnection ()

            let qRes =
                conn
                |> Db.newCommand sql
                |> Db.setParams [ "CUSTOMER_ID", SqlType.Int64 model.CustomerId ]
                |> Db.exec

            match qRes with
            | Ok _ -> Ok true
            | Error dberror -> dbErrorResult "Error deleting customer." dberror
        with
        | ex -> Error ex

    let deleteWorkItemFromDB (getDbConnection: GetDbConnection) (model: WorkItem) =
        try
            let sql =
                "DELETE FROM  WORK_ITEM a
                        WHERE
                            a.WORK_ITEM_ID = @WORK_ITEM_ID"

            use conn = getDbConnection ()

            let qRes =
                conn
                |> Db.newCommand sql
                |> Db.setParams [ "WORK_ITEM_ID", SqlType.Int64 model.WorkItemId ]
                |> Db.exec

            match qRes with
            | Ok _ -> Ok true
            | Error dberror -> dbErrorResult "Error deleting work item." dberror
        with
        | ex -> Error ex

    let deleteTimeEntryFromDB (getDbConnection: GetDbConnection) (model: TimeEntry) =
        try
            let sql =
                "DELETE FROM TIME_ENTRY a
                        WHERE
                            a.TIME_ENTRY_ID = @TIME_ENTRY_ID"

            use conn = getDbConnection ()

            let qRes =
                conn
                |> Db.newCommand sql
                |> Db.setParams [ "TIME_ENTRY_ID", SqlType.Int64 model.TimeEntryId ]
                |> Db.exec

            match qRes with
            | Ok _ -> Ok true
            | Error dberror -> dbErrorResult "Error deleting time entry." dberror
        with
        | ex -> Error ex

    let private customerSelectSQl =
        "SELECT a.CUSTOMER_ID, a.CUSTOMER_NAME, a.PHONE, a.EMAIL, a.CUSTOMER_STATE,
                                a.NOTES
                            FROM CUSTOMER a "

    let private customerOrderByClause = "a.CUSTOMER_NAME"

    let private customerFromDataReader (rd: IDataReader) : Customer =
        { CustomerId = "CUSTOMER_ID" |> rd.ReadInt64
          Name = "CUSTOMER_NAME" |> rd.ReadString |> CustomerName.Create
          Phone = "PHONE" |> rd.ReadStringOption |> PhoneNoOptional.Create
          Email = "EMAIL" |> rd.ReadStringOption |> EmailAddressOptional.Create
          CustomerState = "CUSTOMER_STATE" |> rd.ReadInt32 |> enum<CustomerState>
          Notes = "NOTES" |> rd.ReadStringOption }


    let getOneCustomerFromDB (getDbConnection: GetDbConnection) (id: CustomerId) =
        try
            use conn = getDbConnection ()

            let sql = customerSelectSQl + " WHERE a.CUSTOMER_ID = @CUSTOMER_ID"

            let qRes =
                conn
                |> Db.newCommand sql
                |> Db.setParams [ "CUSTOMER_ID", SqlType.Int64 id ]
                |> Db.querySingle customerFromDataReader

            match qRes with
            | Ok item ->
                match item with
                | Some m -> Ok m
                | None -> $"Unable to find customer with Id {id}" |> exn |> Error
            | Error dberror -> dbErrorResult $"Error occured attempting load customer with Id {id}" dberror
        with
        | ex -> Error ex

    let private workItemSelectSql =
        "SELECT a.WORK_ITEM_ID, a.TITLE, a.DESCRIPTION, a.IS_BILLABLE, a.IS_COMPLETED,
                                    a.IS_FIXED_PRICE, a.DATE_CREATED, a.DUE_DATE, a.BEEN_BILLED, a.NOTES,
                                    a.CUSTOMER_ID
                                FROM WORK_ITEM a "

    let private workItemOrderByClause = "a.DATE_CREATED DESCENDING"

    let private workItemFromDataReader (rd: IDataReader) : WorkItem =
        { WorkItemId = "WORK_ITEM_ID" |> rd.ReadInt64
          Title = "TITLE" |> rd.ReadString |> WorkItemTitle.Create
          Description = "DESCRIPTION" |> rd.ReadStringOption |> WorkItemDescriptionOptional.Create
          IsBillable = "IS_BILLABLE" |> rd.ReadBoolean
          IsCompleted = "IS_COMPLETED" |> rd.ReadBoolean
          IsFixedPrice = "IS_FIXED_PRICE" |> rd.ReadBoolean
          DateCreated = "DATE_CREATED" |> rd.ReadDateTime
          DueDate = "DUE_DATE" |> rd.ReadDateTimeOption
          BeenBilled = "BEEN_BILLED" |> rd.ReadBoolean
          Notes = "NOTES" |> rd.ReadStringOption
          CustomerId = "CUSTOMER_ID" |> rd.ReadInt64 }

    let getOneWorkItemFromDB (getDbConnection: GetDbConnection) (id: WorkItemId) =
        try
            use conn = getDbConnection ()

            let sql = workItemSelectSql + " WHERE a.WORK_ITEM_ID = @WORK_ITEM_ID"

            let qRes =
                conn
                |> Db.newCommand sql
                |> Db.setParams [ "WORK_ITEM_ID", SqlType.Int64 id ]
                |> Db.querySingle workItemFromDataReader

            match qRes with
            | Ok item ->
                match item with
                | Some m -> Ok m
                | None -> $"Unable to find work item with Id {id}" |> exn |> Error
            | Error dberror -> dbErrorResult $"Error occured attempting load work item with Id {id}" dberror
        with
        | ex -> Error ex

    let private timeEntrySelectSql =
        "SELECT a.TIME_ENTRY_ID, a.DESCRIPTION, a.TIME_START, a.TIME_END, a.IS_BILLABLE, a.BEEN_BILLED,
                                    a.NOTES, a.WORK_ITEM_ID
                                FROM TIME_ENTRY a"

    let private timeEntryOrderByClause = "a.TIME_START DESCENDING"

    let private timeEntryFromDataReader (rd: FbDataReader) : TimeEntry =
        { TimeEntryId = "TIME_ENTRY_ID" |> rd.ReadInt64
          Description = "DESCRIPTION" |> rd.ReadString |> TimeEntryDescription.Create
          TimeStart = ("TIME_START" |> rd.ReadDateTime).ToLocalTime()
          TimeEnd =
              "TIME_END"
              |> rd.ReadDateTimeOption
              |> Option.map (fun te -> te.ToLocalTime())
          BeenBilled = "BEEN_BILLED" |> rd.ReadBoolean
          IsBillable = "IS_BILLABLE" |> rd.ReadBoolean
          Notes = "NOTES" |> rd.ReadStringOption
          WorkItemId = "WORK_ITEM_ID" |> rd.ReadInt64 }

    let getOneTimeEntryFromDB (getDbConnection: GetDbConnection) (id: TimeEntryId) =
        try
            use conn = getDbConnection ()

            let sql = timeEntrySelectSql + " WHERE a.TIME_ENTRY_ID = @TIME_ENTRY_ID"

            let qRes =
                conn
                |> Db.newCommand sql
                |> Db.setParams [ "TIME_ENTRY_ID", SqlType.Int64 id ]
                |> Db.querySingle timeEntryFromDataReader

            match qRes with
            | Ok item ->
                match item with
                | Some m -> Ok m
                | None -> $"Unable to find time entry with Id {id}." |> exn |> Error
            | Error dberror -> dbErrorResult $"Error occured attempting load time entry with Id {id}." dberror
        with
        | ex -> Error ex

    let private getCustomerWhere (request: CustomersRequest) =
        if request.IncludeInactive then
            None
        else
            int CustomerState.Active |> sprintf "a.CUSTOMER_STATE = %d" |> Some


    let private AddWhereClauseIfSome baseSql clause =
        match clause with
        | Some w -> baseSql + " WHERE " + w
        | None -> baseSql

    let getCustomerCountFromDB (conn: IDbConnection) (request: CustomersRequest) =
        try
            let where = getCustomerWhere request
            let baseSql = "SELECT COUNT(1) FROM CUSTOMER a"

            let sql = AddWhereClauseIfSome baseSql where

            let qRes =
                conn
                |> Db.newCommand sql
                |> Db.setParams []
                |> Db.scalar (fun o -> unbox<int64> o)

            match qRes with
            | Ok count -> Ok count
            | Error dberror -> dbErrorResult "Error occured attempting get the customer count." dberror
        with
        | ex -> Error ex

    let sqlPagingClause pageRequest =
        let pageNo = pageRequest.PageNo - 1
        let take = pageRequest.ItemsPerPage
        let skip = pageNo * pageRequest.ItemsPerPage
        let skipStr = if skip > 0 then $"OFFSET {skip} ROWS" else ""
        let fetchStr = sprintf "FETCH %s %d ROWS ONLY" (if skip > 0 then "NEXT" else "FIRST") take
        sprintf "%s %s" skipStr fetchStr

    let getCustomersFromDB (getDbConnection: GetDbConnection) (request: PagedCustomersRequest) : Result<ListResults<Customer>, exn> =
        try
            use conn = getDbConnection ()
            let countResult = getCustomerCountFromDB conn request.CustomersRequest

            match countResult with
            | Ok count ->
                if count = 0 then
                    Ok { TotalRecords = 0; Results = list.Empty }
                else
                    let where = getCustomerWhere request.CustomersRequest
                    let sql = $"{AddWhereClauseIfSome customerSelectSQl where} ORDER BY {customerOrderByClause} {sqlPagingClause request.PageRequest}"

                    let qRes =
                        conn
                        |> Db.newCommand sql
                        |> Db.setParams []
                        |> Db.query customerFromDataReader

                    match qRes with
                    | Ok items -> Ok { TotalRecords = count; Results = items }
                    | Error dberror -> dbErrorResult "Error occured attempting load customers." dberror
            | Error e -> Error e

        with
        | ex -> Error ex

    let private addSQLWhereWithOperator operatorString existingWhere clauseAddition =
        match existingWhere with
        | Some whereSoFar ->
            match clauseAddition with
            | Some addition -> Some <| whereSoFar + " " + operatorString + " " + addition
            | None -> existingWhere
        | None -> clauseAddition

    let private addSQLWhereWithAnd = addSQLWhereWithOperator "AND"
    let private addSQLWhereWithOr = addSQLWhereWithOperator "OR"

    let private getWorkItemWhere (request: WorkItemsRequest) =
        let custIdWhere =
            match request.CustomerId with
            | Some custId -> Some <| sprintf "(a.CUSTOMER_ID = %d)" custId
            | None -> None

        let is_completedWhere = if request.IncludeCompleted then None else Some "(NOT a.IS_COMPLETED)"


        addSQLWhereWithAnd custIdWhere is_completedWhere


    let getWorkItemCountFromDB (conn: IDbConnection) (request: WorkItemsRequest) =
        try
            let where = getWorkItemWhere request
            let baseSql = "SELECT COUNT(1) FROM WORK_ITEM a"

            let sql = AddWhereClauseIfSome baseSql where

            let qRes =
                conn
                |> Db.newCommand sql
                |> Db.setParams []
                |> Db.scalar (fun o -> unbox<int64> o)

            match qRes with
            | Ok count -> Ok count
            | Error dberror -> dbErrorResult "Error occured attempting get the work item count." dberror
        with
        | ex -> Error ex


    let getWorkItemsFromDB (getDbConnection: GetDbConnection) (request: PagedWorkItemsRequest) =
        try
            use conn = getDbConnection ()
            let countResult = getWorkItemCountFromDB conn request.WorkItemsRequest

            match countResult with
            | Ok count ->
                if count = 0 then
                    Ok { TotalRecords = 0; Results = list.Empty }
                else
                    let where = getWorkItemWhere request.WorkItemsRequest
                    let sql = $"{AddWhereClauseIfSome workItemSelectSql where} ORDER BY {workItemOrderByClause}  {sqlPagingClause request.PageRequest}"

                    let qRes =
                        conn
                        |> Db.newCommand sql
                        |> Db.setParams []
                        |> Db.query workItemFromDataReader

                    match qRes with
                    | Ok items -> Ok { TotalRecords = count; Results = items }
                    | Error dberror -> dbErrorResult "Error occured attempting load work items." dberror
            | Error e -> Error e

        with
        | ex -> Error ex


    let getTimeEntryWhere (request: TimeEntriesRequest) =
        match request.WorkItemId with
        | Some workItemId -> Some <| sprintf "(a.WORK_ITEM_ID = %d)" workItemId
        | None -> None

    let getTimeEntryCountFromDB (conn: IDbConnection) (request: TimeEntriesRequest) =
        try
            let where = getTimeEntryWhere request
            let baseSql = "SELECT COUNT(1) FROM TIME_ENTRY a"

            let sql = AddWhereClauseIfSome baseSql where

            let qRes =
                conn
                |> Db.newCommand sql
                |> Db.setParams []
                |> Db.scalar (fun o -> unbox<int64> o)

            match qRes with
            | Ok count -> Ok count
            | Error dberror -> dbErrorResult "Error occured attempting get the time entry count" dberror
        with
        | ex -> Error ex

    let getTimeEntriesFromDB (getDbConnection: GetDbConnection) (request: PagedTimeEntriesRequest) =
        try
            use conn = getDbConnection ()
            let countResult = getTimeEntryCountFromDB conn request.TimeEntriesRequest

            match countResult with
            | Ok count ->
                if count = 0 then
                    Ok { TotalRecords = 0; Results = list.Empty }
                else
                    let where = getTimeEntryWhere request.TimeEntriesRequest
                    


                    let sql = $"{AddWhereClauseIfSome timeEntrySelectSql where} ORDER BY {timeEntryOrderByClause} {sqlPagingClause request.PageRequest}"

                    let qRes =
                        conn
                        |> Db.newCommand sql
                        |> Db.setParams []
                        |> Db.query timeEntryFromDataReader

                    match qRes with
                    | Ok items -> Ok { TotalRecords = count; Results = items }
                    | Error dberror -> dbErrorResult "Error occured attempting load time entries" dberror
            | Error e -> Error e

        with
        | ex -> Error ex

    let billingDelailsSqlSelectWithWhereParts =
        "select c.CUSTOMER_ID, c.CUSTOMER_NAME, wi.WORK_ITEM_ID, wi.Title, wi.DATE_CREATED, wi.IS_COMPLETED, wi.IS_FIXED_PRICE,

            te.DESCRIPTION, te.TIME_START, te.TIME_END

        from CUSTOMER c join
             WORK_ITEM wi on (wi.CUSTOMER_ID = c.CUSTOMER_ID) join
             TIME_ENTRY te on (te.WORK_ITEM_ID = wi.WORK_ITEM_ID)

        where wi.IS_BILLABLE and not wi.BEEN_BILLED and not te.BEEN_BILLED and te.TIME_END is not null and
            (wi.IS_COMPLETED or (not wi.IS_FIXED_PRICE))"

    let billingDetailsSqlOrderByPart = "ORDER BY C.CUSTOMER_NAME, WI.DATE_CREATED, WI.TITLE, TE.DESCRIPTION, te.TIME_START"

    let billingSummarySqlSelectWithWhereParts =
        "select c.CUSTOMER_ID, c.CUSTOMER_NAME, wi.WORK_ITEM_ID, wi.Title, wi.DATE_CREATED, wi.IS_COMPLETED, wi.IS_FIXED_PRICE,

            te.DESCRIPTION, sum(cast(((te.TIME_END - te.TIME_START)*24*60) AS DOUBLE PRECISION)) as SUM_DURATION

        from CUSTOMER c join
             WORK_ITEM wi on (wi.CUSTOMER_ID = c.CUSTOMER_ID) join
             TIME_ENTRY te on (te.WORK_ITEM_ID = wi.WORK_ITEM_ID)

        where wi.IS_BILLABLE and not wi.BEEN_BILLED and not te.BEEN_BILLED and te.TIME_END is not null and
            (wi.IS_COMPLETED or (not wi.IS_FIXED_PRICE))"

    let billingSummarySqlGroupByAndOrderByParts =
        "group by c.CUSTOMER_ID, c.CUSTOMER_NAME, wi.WORK_ITEM_ID, wi.Title, wi.DATE_CREATED,  wi.IS_COMPLETED, wi.IS_FIXED_PRICE,

            te.DESCRIPTION

        ORDER BY C.CUSTOMER_NAME, WI.DATE_CREATED, WI.TITLE, TE.DESCRIPTION"

    let private billingSummaryDataFromDataReader (rd: FbDataReader) : BillingData =
        { CustomerId = "CUSTOMER_ID" |> rd.ReadInt64
          CustomerName = "CUSTOMER_NAME" |> rd.ReadString
          WorkItemId = "WORK_ITEM_ID" |> rd.ReadInt64
          WorkItemTitle = "Title" |> rd.ReadString
          WorkItemDateCreate = "DATE_CREATED" |> rd.ReadDateTime
          WorkItemIsCompleted = "IS_COMPLETED" |> rd.ReadBoolean
          WorkItemIsFixedPriced = "IS_FIXED_PRICE" |> rd.ReadBoolean
          TimeEntryDescription = "DESCRIPTION" |> rd.ReadString
          TimeEntryTimeStart = None
          TimeEntryTimeEnd = None
          TimeEntryDuration =
              "SUM_DURATION"
              |> rd.ReadDoubleOption
              |> Option.map (fun d -> TimeSpan.FromMinutes(d)) }

    let private billingDetailsDataFromDataReader (rd: FbDataReader) : BillingData =
        let durationMap (timeStart: DateTime) (timeEnd: DateTime) : TimeSpan =
            let duration = (timeStart - timeEnd).Duration()
            duration.TotalMinutes |> ceil |> TimeSpan.FromMinutes

        let duration timeStartOpt timeEndOpt = Option.map2 durationMap timeStartOpt timeEndOpt

        { CustomerId = "CUSTOMER_ID" |> rd.ReadInt64
          CustomerName = "CUSTOMER_NAME" |> rd.ReadString
          WorkItemId = "WORK_ITEM_ID" |> rd.ReadInt64
          WorkItemTitle = "Title" |> rd.ReadString
          WorkItemDateCreate = "DATE_CREATED" |> rd.ReadDateTime
          WorkItemIsCompleted = "IS_COMPLETED" |> rd.ReadBoolean
          WorkItemIsFixedPriced = "IS_FIXED_PRICE" |> rd.ReadBoolean
          TimeEntryDescription = "DESCRIPTION" |> rd.ReadString
          TimeEntryTimeStart =
              "TIME_START"
              |> rd.ReadDateTimeOption
              |> Option.map (fun te -> te.ToLocalTime())
          TimeEntryTimeEnd =
              "TIME_END"
              |> rd.ReadDateTimeOption
              |> Option.map (fun te -> te.ToLocalTime())
          TimeEntryDuration = duration ("TIME_START" |> rd.ReadDateTimeOption) ("TIME_END" |> rd.ReadDateTimeOption) }

    type recordMappingFunc = FbDataReader -> BillingData

    let private getBillingData
        (sqlSelectAndWherePart: string)
        (sqlGroupByAndOrderByPart: string)
        (mappingFunc: recordMappingFunc)
        (getDbConnection: GetDbConnection)
        (where: string option)
        (dbParamsOptional: RawDbParams option)
        =
        try
            let builder = StringBuilder(sqlSelectAndWherePart).AppendLine()
            let appendLine (s: string) = builder.AppendLine s |> ignore
            let append (s: string) = builder.Append s |> ignore

            match where with
            | Some whereToAdd ->
                append " and "
                appendLine whereToAdd
            | None -> ()

            appendLine sqlGroupByAndOrderByPart
            let sql = builder.ToString()

            let (dbParams: RawDbParams) =
                match dbParamsOptional with
                | Some p -> p
                | None -> []

            use conn = getDbConnection ()

            let qRes = conn |> Db.newCommand sql |> Db.setParams [] |> Db.query mappingFunc

            qRes
            |> Result.mapError
                (fun dberror ->
                    let whereStr =
                        match where with
                        | Some w -> w
                        | None -> "<none>"

                    let msg = sprintf "Error occured attempting load billing summary with additional where: %s." whereStr
                    dbErrorExn msg dberror)
        with
        | ex -> Error ex

    let private getBillingSummary =
        getBillingData billingSummarySqlSelectWithWhereParts billingSummarySqlGroupByAndOrderByParts billingSummaryDataFromDataReader

    let private getBillingDetails = getBillingData billingDelailsSqlSelectWithWhereParts billingDetailsSqlOrderByPart billingDetailsDataFromDataReader

    let getAllUnbilledBillingSummaryFromDb (getDbConnection: GetDbConnection) = getBillingSummary getDbConnection None None
    let getAllUnbilledBillingDetailsFromDb (getDbConnection: GetDbConnection) = getBillingDetails getDbConnection None None
