﻿namespace TrackTime

open System
open System.ComponentModel.Design
open System

module DataModels =
    let CustomerNameLength = 50
    let CustomerEmailLength = 100
    let CustomerPhoneNoLength = 20

    let WorkItemTitleLength=50
    let WorkItemDescriptionLength=100
    let TimeEntryDescriptionLength=100


    type EmailAddressOptional =
        private
        | Valid of string option
        | Invalid of ErrMsg: string * invalidStr: string

        member this.Value =
            match this with
            | Valid optionalValue -> optionalValue
            | _ -> None

        member this.ErrorMsg: string option =
            match this with
            | Valid _ -> None
            | Invalid (errMsg, _) -> Some errMsg

        static member None = None |> Valid

        static member Create str =
            match str with
            |None -> None |> Valid
            |Some s ->
                if String.IsNullOrWhiteSpace s then
                    None |> Valid
                elif (String.length s) > CustomerEmailLength then
                    ($"The email address cannot have more than {CustomerEmailLength} characters", s)
                    |> Invalid
                elif System.Text.RegularExpressions.Regex.IsMatch(s, @"^\S+@\S+\.\S+$") then
                    s |> Some |> Valid
                else
                    Invalid("Email address must contain an @ sign", s)



        member this.IsValidValue =
            match this with
            | Valid value -> true
            | _ -> false

    type PhoneNoOptional =
        private
        | Valid of string option
        | Invalid of ErrMsg: string * invalidStr: string

        member this.Value =
            match this with
            | Valid optionalValue -> optionalValue
            | _ -> None

        member this.ErrorMsg: string option =
            match this with
            | Valid _ -> None
            | Invalid (errMsg, _) -> Some errMsg

        static member None = None |> Valid

        static member Create str =
            match str with
            |None -> None |> Valid
            |Some s ->
                if String.IsNullOrWhiteSpace s then
                    None |> Valid
                elif (String.length s) > CustomerPhoneNoLength then
                    ($"The phone no cannot have more than {CustomerPhoneNoLength} characters", s)
                    |> Invalid
                else
                    s |> Some |> Valid

        member this.IsValidValue =
            match this with
            | Valid value -> true
            | _ -> false




    type CustomerName =
        private
        | Valid of string
        | Invalid of ErrMsg: string * invalidStr: string

        member this.Value =
            match this with
            | Valid value -> value
            | _ -> ""

        member this.ErrorMsg: string option =
            match this with
            | Valid _ -> None
            | Invalid (errMsg, _) -> Some errMsg


        static member Create s =
            if String.IsNullOrWhiteSpace s then
                Invalid("A name is required.", s)
            elif (String.length s) > CustomerNameLength then
                Invalid($"A name cannot have more than {CustomerNameLength} characters", s)
            else
                Valid s

        member this.IsValidValue =
            match this with
            | Valid value -> true
            | _ -> false

    type WorkItemTitle =
        private
        | Valid of string
        | Invalid of ErrMsg: string * invalidStr: string

        member this.Value =
            match this with
            | Valid value -> value
            | _ -> ""

        member this.ErrorMsg: string option =
            match this with
            | Valid _ -> None
            | Invalid (errMsg, _) -> Some errMsg


        static member Create s =
            if String.IsNullOrWhiteSpace s then
                Invalid("A Title is required.", s)
            elif (String.length s) > WorkItemTitleLength then
                Invalid($"A Title cannot have more than {WorkItemTitleLength} characters", s)
            else
                Valid s

        member this.IsValidValue =
            match this with
            | Valid value -> true
            | _ -> false

    type WorkItemDescriptionOptional =
        private
        | Valid of string option
        | Invalid of ErrMsg: string * invalidStr: string

        member this.Value =
            match this with
            | Valid optionalValue ->optionalValue
            | _ -> None

        member this.ErrorMsg: string option =
            match this with
            | Valid _ -> None
            | Invalid (errMsg, _) -> Some errMsg


        static member Create str =
            match str with
            |None -> None |> Valid
            |Some s ->
                if String.IsNullOrWhiteSpace s then
                    None |> Valid
                elif (String.length s) > WorkItemDescriptionLength then
                    Invalid($"A Description cannot have more than {WorkItemDescriptionLength} characters", s)
                else
                    s |> Some |> Valid

        member this.IsValidValue =
            match this with
            | Valid value -> true
            | _ -> false

    type TimeEntryDescription =
        private
        | Valid of string
        | Invalid of ErrMsg: string * invalidStr: string

        member this.Value =
            match this with
            | Valid value -> value
            | _ -> ""

        member this.ErrorMsg: string option =
            match this with
            | Valid _ -> None
            | Invalid (errMsg, _) -> Some errMsg


        static member Create s =
            if String.IsNullOrWhiteSpace s then
                Invalid("A Description is required.", s)
            elif (String.length s) > TimeEntryDescriptionLength then
                Invalid($"A Description cannot have more than {TimeEntryDescriptionLength} characters", s)
            else
                Valid s

        member this.IsValidValue =
            match this with
            | Valid value -> true
            | _ -> false
            
    type CustomerState =
        | InActive = 0
        | Active = 1

    type CustomerId = int64
    type WorkItemId = int64
    type TimeEntryId = int64

    [<CLIMutable>]
    type Customer =
        { CustomerId: CustomerId
          Name: CustomerName
          Phone: PhoneNoOptional
          Email: EmailAddressOptional
          CustomerState: CustomerState
          Notes: string option}
        static member Empty =
            { CustomerId = 0L
              Name = CustomerName.Create ""
              Phone = PhoneNoOptional.None
              Email = EmailAddressOptional.None
              CustomerState = CustomerState.Active
              Notes = None }

        member this.IsValidValue =
            this.Name.IsValidValue
            || this.Phone.IsValidValue
            || this.Email.IsValidValue

        member this.ErrorMsgs =
            seq{
                yield this.Name.ErrorMsg
                yield this.Phone.ErrorMsg
                yield this.Email.ErrorMsg
            }
            |> Seq.filter (fun emsg -> emsg.IsSome)
            |> Seq.map (fun emsg -> emsg.Value)
            



    [<CLIMutable>]
    type WorkItem =
        { WorkItemId: WorkItemId
          Title: WorkItemTitle
          Description: WorkItemDescriptionOptional
          IsBillable: bool
          IsCompleted: bool
          IsFixedPrice: bool
          DateCreated: DateTime
          DueDate: DateTime option
          BeenBilled: bool
          Notes: string option
          CustomerId: CustomerId }

    [<CLIMutable>]
    type TimeEntry =
        { TimeEntryId: TimeEntryId
          Description: TimeEntryDescription
          TimeStart: DateTime
          TimeEnd: DateTime option
          Notes: string option
          BeenBilled: bool
          WorkItemId: WorkItemId }
