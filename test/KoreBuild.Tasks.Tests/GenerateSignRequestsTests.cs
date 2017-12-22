﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.IO;
using System.Text;
using BuildTools.Tasks.Tests;
using Microsoft.Build.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace KoreBuild.Tasks.Tests
{
    public class GenerateSignRequestsTests
    {
        private readonly ITestOutputHelper _output;

        public GenerateSignRequestsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ItCreatesSignRequest()
        {
            var nupkgPath = Path.Combine(AppContext.BaseDirectory, "build", "MyLib.nupkg");
            var requests = new[]
            {
                new TaskItem(Path.Combine(AppContext.BaseDirectory, "build", "ZZApp.vsix"),
                    new Hashtable
                    {
                        ["IsContainer"] = "true",
                        ["Certificate"] = "Cert4",
                    }),
                new TaskItem(nupkgPath,
                    new Hashtable
                    {
                        ["IsContainer"] = "true",
                        ["Type"] = "zip",
                    }),
                new TaskItem("lib/netstandard2.0/MyLib.dll",
                    new Hashtable
                    {
                        ["Container"] = nupkgPath,
                        ["Certificate"] = "Cert1",
                        ["StrongName"] = "Key1",
                    }),
                new TaskItem(Path.Combine(AppContext.BaseDirectory, "build", "MyLib.dll"),
                    new Hashtable
                    {
                        ["Certificate"] = "Cert1",
                    }),
            };

            var exclusions = new[]
            {
                new TaskItem("lib/NotMyLib.dll",
                    new Hashtable
                    {
                        ["Container"] = nupkgPath,
                    })
            };

            var task = new GenerateSignRequests
            {
                Requests = requests,
                BasePath = AppContext.BaseDirectory,
                Exclusions = exclusions,
                BuildEngine = new MockEngine(_output),
            };

            var sb = new StringBuilder();

            Assert.True(task.Execute(() => new StringWriter(sb)), "Task should pass");

            var expected = $@"<SignRequests>
  <File Path=`build/MyLib.dll` Certificate=`Cert1` />
  <Container Path=`build/MyLib.nupkg` Type=`zip`>
    <ExcludedFile Path=`lib/NotMyLib.dll` />
    <File Path=`lib/netstandard2.0/MyLib.dll` Certificate=`Cert1` StrongName=`Key1` />
  </Container>
  <Container Path=`build/ZZApp.vsix` Type=`vsix` Certificate=`Cert4` />
</SignRequests>".Replace('`', '"');
            _output.WriteLine(sb.ToString());

            Assert.Equal(expected, sb.ToString(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }
    }
}
