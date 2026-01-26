// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

var builder = DistributedApplication.CreateBuilder(args);

// Add Seq for log collection and visualization
var seq = builder.AddSeq("seq")
    .WithDataVolume();

// Add the Akka.NET demo service with Seq reference for OTLP export
builder.AddProject<Projects.Akka_Hosting_OpenTelemetry_Demo>("demo")
    .WithReference(seq);

builder.Build().Run();
