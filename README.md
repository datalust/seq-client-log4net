seq-client
==========

An event sink for [Serilog](http://serilog.net) that writes to the [Seq](http://getseq.net) event server.

Installation
------------

From the NuGet Package Manager Console:

```
PM> Install-Package Seq.Client.FullNetFx
```

The package uses the `Seq` namespace:

```
using Seq;
```

Configure Serilog using the `WriteTo.Seq()` extension method on `LoggerConfiguration`:

```
Log.Logger = new LoggerConfiguration()
  .WriteTo.Seq("http://my-seq-server")
  .CreateLogger();
```

In Serilog's defaut configuration, events with `Information` level and above will be recorded. If
you choose to increase the base logging level, e.g. to `Debug`, you can set the Seq sink to
`Information`-level to prevent large volumes of network traffic:

```
  .WriteTo.Seq("http://my-seq-server", minimumLevel: LogEventLevel.Information)
```

Event delivery
--------------

The sink transmits log events to the server asynchronously to avoid heavy performance penalties. 
The buffer will be flushed and any outstanding network requests will be completed when the hosting
application terminates normally. If the application is terminated via an attached debugger, the task
manager or as a result of hard termination such as stack overflow, events in transit may be lost.

It is therefore recommended that if events must be captured at all costs, a local log file should be 
used in conjunction with this sink.

License
-------

The client software included in this repository is provided under the terms of
the [Apache 2.0](http://apache.org/licenses/LICENSE-2.0.html) license.

This does **not** extend to any third-party packages, Serilog or the Seq server, which
are distributed by their respective owners under their own licenses.
