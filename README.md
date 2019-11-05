# Seq.Client.Log4Net [![Build status](https://ci.appveyor.com/api/projects/status/sxw4n1a6v9o7db2i?svg=true)](https://ci.appveyor.com/project/datalust/seq-client)

An Apache log4net appender that writes events to Seq.

### Getting started

The Seq appender for log4net supports both .NET Framework 4+ and .NET Core Application 

To install the package from NuGet, at the Visual Studio Package Manager console, type:


```powershell
Install-Package Seq.Client.Log4Net
```

Then, add the appender to your log4net configuration:

```xml
<appender name="SeqAppender" type="Seq.Client.Log4Net.SeqAppender, Seq.Client.Log4Net" >
  <bufferSize value="1" />
  <serverUrl value="http://my-seq" />
  <apiKey value="" />
</appender>
```

Set the `serverUrl` value to the address of your Seq server.

Set the `apiKey` value to the address of your Seq server api key if it exist.

Finally, add a reference to the appender in the appropriate configuration section:

```xml
<root>
  <level value="INFO" />
  <appender-ref ref="SeqAppender" />
</root>
```

### Writing events

That's it! When you write log events to your log4net `ILogger`:

```csharp
log.InfoFormat("Hello, {0}, from log4net!", Environment.UserName);
```

They'll appear beautifully in Seq.

### Performance

By default, the appender is synchronous. This can lead to application slowdowns.

For acceptable production performance, we recommend the use of [_Log4Net.Async_](https://github.com/cjbhaines/Log4Net.Async)
and a buffer size of 100 or greater.

> **Note regarding NLog 4.0 and SLAB clients:**
>
> This repository originally included _Seq.Client.NLog_, targeting NLog 4.0, and _Seq.Client.Slab_, 
> targeting Microsoft SLAB. The NLog client has been replaced with the much-improved 
> [_NLog.Targets.Seq_](https://github.com/datalust/nlog-targets-seq), while the SLAB client is now 
> deprecated with no replacement currently planned. Both of these projects can be viewed in the 
> `deprecated-nlog-slab` branch.
