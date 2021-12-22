namespace TrackTime

open System
open DataModels

module DataInterfaces =
    //type AddDBModel<'m> = 'm -> Result<string, Exception> //return the id
    //type UpdateDBModel<'m> = 'm -> Result<bool, Exception>
    //type DeleteDBModel = DBModels.Id -> Result<bool, Exception>
    //type GetOneDBModel<'m> = DBModels.Id -> Result<'m option, Exception>

    type ListResults<'a> =
        {
          TotalRecords: int64
          Results: 'a list}

    type PageRequest = { PageNo: int; ItemsPerPage: int }

    type WorkItemsRequest =
        { CustomerId: CustomerId option
          IncludeCompleted: bool }

    type PagedWorkItemsRequest =
        { WorkItemsRequest : WorkItemsRequest
          PageRequest : PageRequest}

    type CustomersRequest =
        { IncludeInactive: bool }

    type PagedCustomersRequest =
        { CustomersRequest : CustomersRequest
          PageRequest : PageRequest }

    type TimeEntriesRequest =
        { WorkItemId: WorkItemId option }

    type PagedTimeEntriesRequest =
        { TimeEntriesRequest : TimeEntriesRequest
          PageRequest : PageRequest }

