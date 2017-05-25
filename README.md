# Back-end Developer Challenge: Follower Maze

## The Challenge
The challenge proposed here is to build a system which acts as a socket
server, reading events from an *event source* and forwarding them when
appropriate to *user clients*.

Clients will connect through TCP and use the simple protocol described in a
section below. There will be two types of clients connecting to your server:

- **One** *event source*: It will send you a
stream of events which may or may not require clients to be notified
- **Many** *user clients*: Each one representing a specific user,
these wait for notifications for events which would be relevant to the
user they represent

### The Protocol
The protocol used by the clients is string-based (i.e. a `CRLF` control
character terminates each message). All strings are encoded in `UTF-8`.

The *event source* **connects on port 9090** and will start sending
events as soon as the connection is accepted.

The many *user clients* will **connect on port 9099**. As soon
as the connection is accepted, they will send to the server the ID of
the represented user, so that the server knows which events to
inform them of. For example, once connected a *user client* may send down:
`2932\r\n`, indicating that they are representing user 2932.

After the identification is sent, the *user client* starts waiting for
events to be sent to them. Events coming from *event source* should be
sent to relevant *user clients* exactly like read, no modification is
required or allowed.

### The Events
There are five possible events. The table below describe payloads
sent by the *event source* and what they represent:

| Payload    | Sequence #| Type         | From User Id | To User Id |
|------------|-----------|--------------|--------------|------------|
|666&#124;F&#124;60&#124;50 | 666       | Follow       | 60           | 50         |
|1&#124;U&#124;12&#124;9    | 1         | Unfollow     | 12           | 9          |
|542532&#124;B    | 542532    | Broadcast    | -            | -          |
|43&#124;P&#124;32&#124;56  | 43        | Private Msg  | 32           | 56         |
|634&#124;S&#124;32    | 634       | Status Update| 32           | -          |

Using the verification program supplied, you will receive exactly 10000000 events,
with sequence number from 1 to 10000000. **The events will arrive out of order**.

*Note: Please do not assume that your code would only handle a finite sequence
of events, **we expect your server to handle an arbitrarily large events stream**
(i.e. you would not be able to keep all events in memory or any other storage)*

Events may generate notifications for *user clients*. **If there is a
*user client* ** connected for them, these are the users to be
informed for different event types:

* **Follow**: Only the `To User Id` should be notified
* **Unfollow**: No clients should be notified
* **Broadcast**: All connected *user clients* should be notified
* **Private Message**: Only the `To User Id` should be notified
* **Status Update**: All current followers of the `From User ID` should be notified

If there are no *user client* connected for a user, any notifications
for them must be silently ignored. *user clients* expect to be notified of
events **in the correct order**, regardless of the order in which the
*event source* sent them.

## The Solution
Developed with C# 6.0, without any external dependency. It utilizes asynchronous processing using BackgroundWorker (plus [Dispose pattern](https://msdn.microsoft.com/en-us/library/b1yfkh5e(v=vs.110).aspx)), Threads, and EventHandler pattern to avoid blocking and react swiftly to events as soon as they happen. Object-oriented principles were applied in designing the classes to eliminate duplication, ensure code reuse and maintainability.

Method-by-method documentation can be viewed [in markdown format](Docs/FollowerMazeServer.GeneratedXmlDoc.md). Code style has been checked with Visual Studio's built-in code analysis tool. Code cleanup was done with [Code Maid extension](http://www.codemaid.net/).

### Testing
Unit test framework chosen: NUnit 2 (instead of Visual Studio's unit test framework) for interoperability on both Linux and Windows. The latest NUnit version (3) was not chosen because Travis CI doesn't support it.

Full unit test coverage was implemented. Classes are tested for their behavior (black box test), interaction test for clients is implemented with simple sockets; for event listener it is implemented with a simulated event source(*TestEventSource*) and clients (*TestClient*).

List of test items
* **Payload**: Parsing behavior for each field, and each type pass/fail cases
* **DummyClient** and **AsbtractClient**: Creation, Follower and Message management, concurrency support
* **ConnectedClient**: Same as above, also includes message sequence test since this class can send messages
* **EventListener**: Creation, event source connection, client connection, message dispatching

Unit tests are automatically run after each commit with Travis CI. Current CI Status: ![CI Status](https://travis-ci.org/thanhphu/FollowerMaze.svg?branch=master)

Ready built server and manual test client can be downloaded from [FollowerMazeTest](FollowerMazeTest/Manual), [JRE](http://www.oracle.com/technetwork/java/javase/downloads/jre8-downloads-2133155.html) is required

### Performance
Measured in seconds, run on 4 cores of an i7-6700HQ @2.6GHz, 4GB of RAM in a VirtualBox VM

| Run | Events     | Time | Configuration |
|----:|-----------:|-----:|--------------:|
| 1   | 200,000    | 16   |Debug|
| 2   | 200,000    | 12   |Debug|
| 3   | 200,000    | 17   |Debug|
| 4   | 10,000,000 | 519  |Debug|
| 5   | 10,000,000 | 283  |Release|

No timeout encountered, all events are received in the correct order.

Performance tuning was done with CPU Profiler. Events are disposed of as they are processed to not take up space. Typical memory usage for 10,000,000 events hover around 70MB.

### Building

>git clone https://github.com/thanhphu/FollowerMaze.git
>
>cd FollowerMaze
>

Windows with msbuild
>msbuild.exe FollowerMaze.sln /t:Build /p:Configuration=Release

Linux with mono and xbuild
>xbuild /p:Configuration=Release FollowerMaze.sln
>

### Running
Run FollowerMazeServer.exe first, and then followermaze.cmd (manual test client).

Press Enter in FollowerMazeServer to terminate it after the event source closes.

### Classes' description
#### The EventListener class
Controller class, manage the following listeners that continously run in the thread pool

* **EventDispatchWorker**: Check the event list and queue events sequentially to clients
* **EventListenerWorker**: Listens for events from event source, parse them and add them to the event list
* **ClientHandlingWorker**: Handles connection from client and create client instances for them

#### The Client classes

* **DummyClient**: Represents the "clients" in the event stream doesn't actually connects, only referenced. Contains the list of followers and messages intended for them.
* **ConnectedClient**: Represents clients that actually connect to ClientHandlingWorker's listener, can take over data from DummyClient
* **AbstractClient**: Common data shared between Dummy and Connected type

