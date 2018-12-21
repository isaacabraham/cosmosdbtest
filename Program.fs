// Learn more about F# at http://fsharp.org
module Program

open System
open Thoth.Json.Net
open Microsoft.Azure.Cosmos
open System.IO
open System.Text

/// A custom serializer using that uses the Thoth library
let thothSerializer =
    { new CosmosJsonSerializer() with
          
          member __.FromStream stream =
              use reader = new StreamReader(stream, Encoding.UTF8)
              let text = reader.ReadToEnd()
              printfn "THOTH: Deserializing %s" text
              text |> Decode.Auto.unsafeFromString
          
          member __.ToStream value =
              let json = Encode.Auto.toString (4, value)
              printfn "THOTH: Serialized an object to %s" json
              let byteArray = json |> Encoding.UTF8.GetBytes
              new MemoryStream(byteArray) :> _ }

[<AutoOpen>]
module CosmosStuff =
    let endpointUri = "https://localhost:8081"
    let primaryKey =
        "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="

    /// A Cosmos client that uses the Thoth serializer
    let cosmosClient =
        let config = CosmosConfiguration(endpointUri, primaryKey, CosmosJsonSerializer = thothSerializer)
        new CosmosClient(config)

    let testDb = cosmosClient.Databases.["testDatabase"]
    let testContainer = testDb.Containers.["testContainer"]

    /// Creates a database and container
    let initCosmos() = async {
        // We need to use the "standard" JSON serializer when doing management operations - Thoth doesn't work with classes.
        let client = new CosmosClient(CosmosConfiguration(endpointUri, primaryKey))
        do! client.Databases.CreateDatabaseIfNotExistsAsync(testDb.Id, Nullable 400) |> Async.AwaitTask |> Async.Ignore
        do! client.Databases.[testDb.Id].Containers.CreateContainerIfNotExistsAsync(testContainer.Id, "/Team") |> Async.AwaitTask |> Async.Ignore }

type Job = Developer of string | Manager | Sales
type Person =
    { id : string
      Job : Job
      Name : string
      Age : int
      Team : string }


[<EntryPoint>]
let main argv =
    printfn "Creating db and collection..."
    initCosmos() |> Async.RunSynchronously

    printfn "Now saving a record to Cosmos"
    let data = { id = "123"; Job = Developer "F#"; Name = "Isaac"; Age = 39; Team = "SuperTeam" }
    testContainer.Items.UpsertItemAsync(data.Team, data).Wait()
    
    printfn "Now reading the record back into memory"
    let query = testContainer.Items.CreateItemQuery<Person>("SELECT * FROM r", "SuperTeam")
    let result = query.FetchNextSetAsync().Result |> Seq.toArray
    
    printfn "Got back %d rows" result.Length
    printfn "Is result same as input? %b" (data = result.[0])

    0 // return an integer exit code
