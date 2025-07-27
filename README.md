# Implement Server-Sent Events in ASP.NET Core 8

## Client (JavaScript)

-  `EventSource` API
```js
const es = new EventSource("/api/connect"); // Persistent connection

es.onmessage = function(e) {  // receive sse message from server
    //  e.data 
}   
es.onerror = function(e) { 
    console.error("sse Error", e)
    es.close();
} 
```

## Server corresponding API
- Usage of `StreamWriter`
```csharp
app.MapGet("/api/connect", async (HttpContext context) =>
{
    // SSE requires the Content-Type to be "text/event-stream"
    context.Response.Headers.Add("Content-Type", "text/event-stream"); 

    var writer = new StreamWriter(context.Response.Body);

    try
    {
        /*
            Write to the stream using Server-Sent Events format.

            Each event block can include:
            - data: <message>      // Required. The message payload. Must start with "data: ".
            - id: <id>             // Optional. Used by the browser to resume the connection.
            - event: <event-name>  // Optional. Specifies a custom event name.
            - retry: <milliseconds>// Optional. Tells the client how long to wait before reconnecting.
            - : <comment>          // Optional. Lines starting with ":" are treated as comments.

            An event ends when a blank line (\n\n) is sent and triggers 'es.onmessage' in the browser
        */

        /* Example: Send a single event to the client:
        await writer.WriteAsync("id: 42\n");
        await writer.WriteAsync("event: my-event\n");
        await writer.WriteAsync("data: some payload\n\n");
        await writer.FlushAsync(); // Ensure the event is sent to the client immediately.
        */


        // Keep the connection alive using a heartbeat:
        while (!context.RequestAborted.IsCancellationRequested)
        {
            await writer.WriteAsync(":\n\n"); // Sends a comment as a heartbeat
            await writer.FlushAsync();
            await Task.Delay(10000, context.RequestAborted); 
        }
    }
    catch (TaskCanceledException)
    {
        // Client disconnected or request was aborted
    }

});
```


## (optional) CORS 
SSE is affected by CORS policies. 
