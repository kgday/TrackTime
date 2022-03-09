namespace TrackTime
open TrackTime.DataModels

type DialogResult =
        | Updated
        | Created of RecordId
        | Deleted
        | Cancelled
