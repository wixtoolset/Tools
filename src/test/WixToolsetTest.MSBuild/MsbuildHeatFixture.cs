// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MSBuild
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class MsbuildHeatFixture
    {
        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildHeatFilePackage(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData\HeatFilePackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");
                var projectPath = Path.Combine(baseFolder, "HeatFilePackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath);
                result.AssertSuccess();

                var heatCommandLines = MsbuildUtilities.GetToolCommandLines(result, "heat", "file", buildSystem, true);
                Assert.Single(heatCommandLines);

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, "x86", "Release", "_ProductComponents_INSTALLFOLDER_HeatFilePackage.wixproj_file.wxs");
                Assert.True(File.Exists(generatedFilePath));

                var generatedContents = File.ReadAllText(generatedFilePath);
                var testXml = generatedContents.GetTestXml();
                Assert.Equal(@"<Wix>" +
                    "<Fragment>" +
                    "<DirectoryRef Id='INSTALLFOLDER'>" +
                    "<Component Id='HeatFilePackage.wixproj' Guid='*'>" +
                    "<File Id='HeatFilePackage.wixproj' KeyPath='yes' Source='SourceDir\\HeatFilePackage.wixproj' />" +
                    "</Component>" +
                    "</DirectoryRef>" +
                    "</Fragment>" +
                    "<Fragment>" +
                    "<ComponentGroup Id='ProductComponents'>" +
                    "<ComponentRef Id='HeatFilePackage.wixproj' />" +
                    "</ComponentGroup>" +
                    "</Fragment>" +
                    "</Wix>", testXml);

                var pdbPath = Path.Combine(binFolder, "x86", "Release", "HeatFilePackage.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                Assert.Equal(@"SourceDir\HeatFilePackage.wixproj", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildHeatFileWithMultipleFilesPackage(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData\HeatFileMultipleFilesSameFileName");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");
                var projectPath = Path.Combine(baseFolder, "HeatFileMultipleFilesSameFileName.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath);
                result.AssertSuccess();

                var heatCommandLines = MsbuildUtilities.GetToolCommandLines(result, "heat", "file", buildSystem, true);
                Assert.Equal(2, heatCommandLines.Count());

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, "x86", "Release", "_TxtProductComponents_INSTALLFOLDER_MyProgram.txt_file.wxs");
                Assert.True(File.Exists(generatedFilePath));

                var generatedContents = File.ReadAllText(generatedFilePath);
                var testXml = generatedContents.GetTestXml();
                Assert.Equal("<Wix>" +
                    "<Fragment>" +
                    "<DirectoryRef Id='INSTALLFOLDER'>" +
                    "<Component Id='MyProgram.txt' Guid='*'>" +
                    @"<File Id='MyProgram.txt' KeyPath='yes' Source='SourceDir\MyProgram.txt' />" +
                    "</Component>" +
                    "</DirectoryRef>" +
                    "</Fragment>" +
                    "<Fragment>" +
                    "<ComponentGroup Id='TxtProductComponents'>" +
                    "<ComponentRef Id='MyProgram.txt' />" +
                    "</ComponentGroup>" +
                    "</Fragment>" +
                    "</Wix>", testXml);

                generatedFilePath = Path.Combine(intermediateFolder, "x86", "Release", "_JsonProductComponents_INSTALLFOLDER_MyProgram.json_file.wxs");
                Assert.True(File.Exists(generatedFilePath));

                generatedContents = File.ReadAllText(generatedFilePath);
                testXml = generatedContents.GetTestXml();
                Assert.Equal("<Wix>" +
                    "<Fragment>" +
                    "<DirectoryRef Id='INSTALLFOLDER'>" +
                    "<Component Id='MyProgram.json' Guid='*'>" +
                    @"<File Id='MyProgram.json' KeyPath='yes' Source='SourceDir\MyProgram.json' />" +
                    "</Component>" +
                    "</DirectoryRef>" +
                    "</Fragment>" +
                    "<Fragment>" +
                    "<ComponentGroup Id='JsonProductComponents'>" +
                    "<ComponentRef Id='MyProgram.json' />" +
                    "</ComponentGroup>" +
                    "</Fragment>" +
                    "</Wix>", testXml);

                var pdbPath = Path.Combine(binFolder, "x86", "Release", "HeatFileMultipleFilesSameFileName.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileSymbols = section.Symbols.OfType<FileSymbol>().ToArray();
                Assert.Equal(@"SourceDir\MyProgram.txt", fileSymbols[0][FileSymbolFields.Source].PreviousValue.AsPath().Path);
                Assert.Equal(@"SourceDir\MyProgram.json", fileSymbols[1][FileSymbolFields.Source].PreviousValue.AsPath().Path);
            }
        }
    }
}
