# FollowerMazeServer #

## T:Controllers.AbstractClient

 Common method and data shared between dummy and connnected client 



---
## T:Controllers.DummyClient

 "Dummy" client is a client that doesn't connect but is referenced in the events, we should keep a list of followers and messages intended for it, should it connects later. If it connects, it will hand over the collect data to Connected Client 



---
## T:ConnectedClient

 Repersents a client that actually connect to the server, as opposed to a dummy client 



---
#### M:ConnectedClient.TakeOverFrom(FollowerMazeServer.Controllers.AbstractClient)

 Take data from another client, the other client should be discarded shortly after so this instance can take oker 

|Name | Description |
|-----|------|
|Other: |Client to take followers and messages from|


---
#### M:ConnectedClient.ProcessClientID(System.IAsyncResult)

 Asynchronous method called when a client return its ID 

|Name | Description |
|-----|------|
|AR: ||


---
#### M:ConnectedClient.ClientMessageHandling

 Handle messages to and from clients, started in a thread and keep looping until shutdown or client disconnects 



---
#### F:Constants.StatusInterval

 On screen tatus update interval 



---
#### F:Constants.BufferSize

 Buffer size for reading from network 



---
#### F:Constants.WorkerDelay

 Time between each iteration inside client's thread. Messages will only be sent out once during this perioud 



---
#### F:Constants.IP

 Listening IP Address 



---
## T:EventListener

 Controller class, manage all listeners 



---
#### M:EventListener.CheckAndCreateDummyClient(System.Int32)

 Checks if a client already exists, if not, create a dummy client 

|Name | Description |
|-----|------|
|ID: |ID of client to create|


---
#### M:EventListener.IsPayloadHandled(FollowerMazeServer.Payload)

 Handle a payload, returns true if it can be processed now, false otherwise 

|Name | Description |
|-----|------|
|P: |payload to handle|
Returns: true if payload has been handled, false if retry is needed



---
#### M:EventListener.Instance_IDAvailable(System.Object,FollowerMazeServer.IDEventArgs)

 Called when a connected client sends it ID 

|Name | Description |
|-----|------|
|sender: ||
|Name | Description |
|-----|------|
|e: ||


---
#### M:EventListener.Instance_OnDisconnect(System.Object,FollowerMazeServer.IDEventArgs)

 Called when a connected client disconnects 

|Name | Description |
|-----|------|
|sender: ||
|Name | Description |
|-----|------|
|e: ||


---
#### M:EventListener.StatusTimer_Elapsed(System.Object,System.Timers.ElapsedEventArgs)

 Update status on screen 

|Name | Description |
|-----|------|
|sender: ||
|Name | Description |
|-----|------|
|e: ||


---
#### M:EventListener.Dispose

 Implements dispose pattern for the worker objects 



---
## T:IDEventArgs

 Data class used in events to pass client ID around 



---
## T:Payload

 Represent a parsed event sent from event source, support parsing via factory method Payload.Create 



---
#### M:Payload.#ctor

 Hidden constructor. Error may happen during initialization, so it's best to use factory pattern 



---
#### M:Payload.Create(System.String)

 Factory method to create a payload instance from event's string representation 

|Name | Description |
|-----|------|
|raw: |raw payload data|
Returns: instance if data is valid, null otherwise



---
#### M:Payload.ToString

 Returns the original string unmodified to client 

Returns: 



---
## T:Utils

 Utility methods 



---
#### M:Utils.Log(System.String)

 Write log to file, with buffer 

|Name | Description |
|-----|------|
|Message: |string to write|


---
#### M:Utils.Log(System.Byte[])

 Write log to file 

|Name | Description |
|-----|------|
|Array: |array to write|


---
#### M:Utils.Status(System.String)

 Write a status message on screen, can be updated inline 

|Name | Description |
|-----|------|
|Message: ||


---


