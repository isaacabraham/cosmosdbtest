#load "Refs.fsx" "Program.fs"

open Program

// 1. Create the db / collection
initCosmos() |> Async.Start

// 2. Now save the record to the DB
let data = { id = "123"; Job = Developer "F#"; Name = "Isaac"; Age = 39; Team = "SuperTeam" }
let insertTask = testContainer.Items.UpsertItemAsync(data.Team, data)

// Check Cosmos and confirm the record is there. Observe that the JSON has been serialized using Thoth, and NOT Newtonsoft.

// Now read back
let query = testContainer.Items.CreateItemQuery<Person>("SELECT * FROM r", "SuperTeam")
let queryResponseTask = query.FetchNextSetAsync()

// Now print the results out (this will pop with a Newtonsoft serialization error)
queryResponseTask.Result |> Seq.toArray

