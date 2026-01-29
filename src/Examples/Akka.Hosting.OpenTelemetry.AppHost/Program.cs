// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Akka.NET Project">
//      Copyright (C) 2013-2024 .NET Foundation <https://github.com/akkadotnet/akka.net>
//  </copyright>
// -----------------------------------------------------------------------

var builder = DistributedApplication.CreateBuilder(args);

// Add the Akka.NET demo service
builder.AddProject<Projects.Akka_Hosting_OpenTelemetry_Demo>("demo");

builder.Build().Run();
