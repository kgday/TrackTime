namespace TrackTime

open System
open System.Linq
open System.IO
open TrackTime
open DataInterfaces
open System.Data
open FSharp.Data
open FirebirdSql.Data.FirebirdClient


//The building blocks of the data access - which we can unit test
module AppDataLiteDb =
    open FSharp.Data
    open DataModels
    open FirebirdSql
    open Donald.Conection
    open Donald

    type Settings = JsonProvider<"sampleDbConfig.json", EmbeddedResource="TrackTime.Core, TrackTime.Core.sampleDbConfig.json">

    let GetDbContextConnStr () =
        let configfilePath () =
            let dir =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))

            Path.Combine(dir, "TrackTime", "settings.json")

        let settings = Settings.Load(configfilePath ())

        let dbPort =
            if settings.DbPort.JsonValue.AsString().Equals("") then
                //5432 //postgresql
                3050 //firebird
            else
                settings.DbPort.JsonValue.AsInteger()

        let dbHost = if settings.DbHost.Equals("") then "localhost" else settings.DbHost

        //sprintf //postgresql
        //    "Username=%s;Password=%s;Database=%s;Host=%s;Port=%d;Pooling=true;ConnectionLifetime=60;Application Name=TrackTime"
        //    settings.DbUser
        //    settings.DbPassword
        //    settings.DbName
        //    dbHost
        //    dbPort
        sprintf //firebird
            "User=%s;Password=%s;Database=%s;DataSource=%s;Port=%d;Dialect=3;Charset=UTF8;Pooling=true;Connection Lifetime=60"
            settings.DbUser
            settings.DbPassword
            settings.DbName
            dbHost
            dbPort


    type GetDbConnection = unit -> IDbConnection


    let getDbConnectionWithConnStr connStr : IDbConnection =
        let conn = new FbConnection(connStr)
        conn.TryOpenConnection()
        conn

    let connectDB = GetDbContextConnStr >> getDbConnectionWithConnStr

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
                |> Db.setParams
                    [ "CUSTOMER_NAME", SqlType.String model.Name.Value
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
                | Error e -> ("Error inserting customer and returning new id.", e.Error) |> exn |> Error

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
                |> Db.setParams
                    [ "TITLE", SqlType.String model.Title.Value
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
                    | None -> "Error inserting and returning new customer id." |> exn |> Error
                | Error e -> ("Error inserting customer and returning new id.", e.Error) |> exn |> Error

            res
        with
        | ex -> Error ex

    let addTimeEntryToDB (getDbConnection: GetDbConnection) (model: TimeEntry) =
        try
            let sql =
                "INSERT INTO TIME_ENTRY (DESCRIPTION, TIME_START, TIME_END, BEEN_BILLED, NOTES,
                            WORK_ITEM_ID)
                        VALUES (
                            @DESCRIPTION,
                            @TIME_START,
                            @TIME_END,
                            @BEEN_BILLED,
                            @NOTES,
                            @WORK_ITEM_ID
                        )
                        RETURNING TIME_ENTRY_ID"

            use conn = getDbConnection ()
            let idOfDataReader (rd: IDataReader) : int64 = rd.ReadInt64 "TIME_ENTRY_ID"

            let newIdResult =
                conn
                |> Db.newCommand sql
                |> Db.setParams
                    [ "DESCRIPTION", SqlType.String model.Description.Value
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
                      "WORK_ITEM_ID", SqlType.Int64 model.WorkItemId ]

                |> Db.querySingle idOfDataReader

            match newIdResult with
            | Ok idOption ->
                match idOption with
                | Some id -> Ok id
                | None -> "Error inserting and returning new customer id." |> exn |> Error
            | Error e -> ("Error inserting customer and returning new id.", e.Error) |> exn |> Error
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
                |> Db.setParams
                    [ "CUSTOMER_NAME", SqlType.String model.Name.Value
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
            | Error e -> ("Error inserting customer and returning new id.", e.Error) |> exn |> Error
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
                |> Db.setParams
                    [ "TITLE", SqlType.String model.Title.Value
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
            | Error e -> ("Error inserting WORK_ITEM and returning new id.", e.Error) |> exn |> Error
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
                |> Db.setParams
                    [ "DESCRIPTION", SqlType.String model.Description.Value
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
            | Error e -> ("Error inserting WORK_ITEM and returning new id.", e.Error) |> exn |> Error
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
            | Error e -> ("Error inserting customer and returning new id.", e.Error) |> exn |> Error
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
            | Error e -> ("Error inserting WORK_ITEM and returning new id.", e.Error) |> exn |> Error
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
            | Error e -> ("Error inserting WORK_ITEM and returning new id.", e.Error) |> exn |> Error
        with
        | ex -> Error ex

    let customerSelectSQl =
        "SELECT a.CUSTOMER_ID, a.CUSTOMER_NAME, a.PHONE, a.EMAIL, a.CUSTOMER_STATE,
                                a.NOTES
                            FROM CUSTOMER a "

    type Customer with
        static member ofDataReader(rd: IDataReader) : Customer =
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
                |> Db.querySingle Customer.ofDataReader

            match qRes with
            | Ok item ->
                match item with
                | Some m -> Ok m
                | None -> $"Unable to find customer with Id {id}" |> exn |> Error
            | Error e -> ($"Error occured attempting load customer with Id {id}", e.Error) |> exn |> Error
        with
        | ex -> Error ex

    let workItemSelectSql =
        "SELECT a.WORK_ITEM_ID, a.TITLE, a.DESCRIPTION, a.IS_BILLABLE, a.IS_COMPLETED,
                                    a.IS_FIXED_PRICE, a.DATE_CREATED, a.DUE_DATE, a.BEEN_BILLED, a.NOTES,
                                    a.CUSTOMER_ID
                                FROM WORK_ITEM a "

    type WorkItem with
        static member ofDataReader(rd: IDataReader) : WorkItem =
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

            let sql =
                workItemSelectSql
                + " WHERE
                                                a.WORK_ITEM_ID = @WORK_ITEM_ID"

            let qRes =
                conn
                |> Db.newCommand sql
                |> Db.setParams [ "WORK_ITEM_ID", SqlType.Int64 id ]
                |> Db.querySingle WorkItem.ofDataReader

            match qRes with
            | Ok item ->
                match item with
                | Some m -> Ok m
                | None -> $"Unable to find work item with Id {id}" |> exn |> Error
            | Error e -> ($"Error occured attempting load work item with Id {id}", e.Error) |> exn |> Error
        with
        | ex -> Error ex

    let timeEntrySelectSql =
        "SELECT a.TIME_ENTRY_ID, a.DESCRIPTION, a.TIME_START, a.TIME_END, a.BEEN_BILLED,
                                    a.NOTES, a.WORK_ITEM_ID
                                FROM TIME_ENTRY a"

    type TimeEntry with
        static member ofDataReader ofDataReader (rd: FbDataReader) : TimeEntry =
            { TimeEntryId = "TIME_ENTRY_ID" |> rd.ReadInt64
              Description = "DESCRIPTION" |> rd.ReadString |> TimeEntryDescription.Create
              TimeStart = ("TIME_START" |> rd.ReadDateTime).ToLocalTime()
              TimeEnd =
                  match ("TIME_END" |> rd.ReadDateTimeOption) with
                  | Some te -> Some(te.ToLocalTime())
                  | None -> None
              BeenBilled = "BEEN_BILLED" |> rd.ReadBoolean
              Notes = "NOTES" |> rd.ReadStringOption
              WorkItemId = "WORK_ITEM_ID" |> rd.ReadInt64 }

    let getOneTimeEntryFromDB (getDbConnection: GetDbConnection) (id: TimeEntryId) =
        try
            use conn = getDbConnection ()

            let itm = DbContextHelpers.tryFindEntity<TimeEntry> conn id

            Ok(itm)
        with
        | ex -> Error ex

    let private getCustomerQuery (conn: MyDbContext) (request: CustomersRequest) =
        if request.IncludeInactive then
            conn.Customers.AsQueryable()
        else
            conn.Customers.Where(fun cust -> cust.CustomerState = CustomerState.Active)

    let getCustomerCountFromDB (conn: MyDbContext) (request: CustomersRequest) =
        try
            let q = getCustomerQuery conn request
            let count = q.LongCount()
            Ok count
        with
        | ex -> Error ex

    let getCustomersFromDB (getDbConnection: GetDbConnection) (request: PagedCustomersRequest) : Result<ListResults<Customer>, exn> =
        try
            let pageNo = request.PageRequest.PageNo - 1
            let take = request.PageRequest.ItemsPerPage

            let skip = pageNo * request.PageRequest.ItemsPerPage

            use conn = getDbConnection ()

            let query = getCustomerQuery conn request.CustomersRequest

            let count = query.LongCount()

            let list =
                if count > 0L then
                    query.OrderBy(fun cust -> cust.Name).Skip(skip).Take(take) |> Seq.toList
                else
                    List.Empty

            let res = { TotalRecords = count; Results = list }
            Ok res
        with
        | ex -> Error ex

    let private getWorkItemQuery (conn: MyDbContext) (request: WorkItemsRequest) =
        let baseQuery =
            match request.CustomerId with
            | Some custId -> conn.WorkItems.Where(fun workitem -> workitem.CustomerId = custId)
            | None -> conn.WorkItems.AsQueryable()

        if request.IncludeCompleted then
            baseQuery
        else
            baseQuery.Where(fun workItem -> not workItem.IsCompleted)


    let getWorkItemCountFromDB (conn: MyDbContext) (request: WorkItemsRequest) =
        try
            let q = getWorkItemQuery conn request
            let res = q.LongCount()
            Ok(res)
        with
        | ex -> Error ex


    let getWorkItemsFromDB (getDbConnection: GetDbConnection) (request: PagedWorkItemsRequest) =
        try
            let pageNo = request.PageRequest.PageNo - 1
            let take = request.PageRequest.ItemsPerPage

            let skip = pageNo * request.PageRequest.ItemsPerPage

            use conn = getDbConnection ()

            let query = getWorkItemQuery conn request.WorkItemsRequest

            let count = query.LongCount()

            let list =
                if count > 0L then
                    query.OrderBy(fun workItem -> workItem.DueDate).Skip(skip).Take(take) |> Seq.toList
                else
                    List.Empty

            let res = { TotalRecords = count; Results = list }
            Ok res
        with
        | ex -> Error ex


    let getTimeEntryQuery (conn: MyDbContext) (request: TimeEntriesRequest) =
        match request.WorkItemId with
        | Some workItemId -> conn.TimeEntries.Where(fun timeEntry -> timeEntry.Id = workItemId)
        | None -> conn.TimeEntries.AsQueryable()

    let getTimeEntryCountFromDB (conn: MyDbContext) (request: TimeEntriesRequest) =
        try
            let query = getTimeEntryQuery conn request
            let res = query.LongCount()
            Ok(res)
        with
        | ex -> Error ex

    let getTimeEntriesFromDB (getDbConnection: GetDbConnection) (request: PagedTimeEntriesRequest) =
        try
            let pageNo = request.PageRequest.PageNo - 1
            let take = request.PageRequest.ItemsPerPage

            let skip = pageNo * request.PageRequest.ItemsPerPage

            use conn = getDbConnection ()

            let query = getTimeEntryQuery conn request.TimeEntriesRequest

            let count = query.LongCount()

            let list =
                if count > 0L then
                    query.OrderBy(fun timeEntry -> timeEntry.TimeStart).Skip(skip).Take(take) |> Seq.toList
                else
                    List.Empty

            let res = { TotalRecords = count; Results = list }
            Ok res
        with
        | ex -> Error ex
