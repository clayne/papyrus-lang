using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DarkId.Papyrus.Common;
using DarkId.Papyrus.LanguageService.Program;
using DarkId.Papyrus.LanguageService.Projects;
using Newtonsoft.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace DarkId.Papyrus.Test.LanguageService.Program
{
    [TestClass]
    public class ProgramUtilitiesTests : ProgramTestBase
    {
        private readonly IFileSystem _fileSystem = new LocalFileSystem();
        private readonly static string _testFilesPath = @"..\..\..\..";

        private readonly static string _remotesPath = Path.Combine(_testFilesPath, "remotes");
        private readonly static string _remotesInfoPath = Path.Combine(_testFilesPath, @"scripts\RemoteAddresses.json");
        private readonly RemotesInfo remotes = 
            JsonConvert.DeserializeObject<RemotesInfo>(File.ReadAllText(_remotesInfoPath));

        private readonly static string _importsInfoPath = Path.Combine(_testFilesPath, @"scripts\Imports.json");
        private readonly static ImportsInfo imports = JsonConvert.DeserializeObject<ImportsInfo>(File.ReadAllText(_importsInfoPath));

        private void TestRemoteParsing(RemoteInfo remoteInfo)
        {
            var remoteUri = new Uri(remoteInfo.Remote);
            var args = new SourceInclude.RemoteArgs(remoteUri);

            Assert.AreEqual(remoteInfo.RemoteOwner, args.Owner);
            Assert.AreEqual(remoteInfo.RemoteName, args.RepoName);
            Assert.AreEqual(remoteInfo.RemoteHash, args.UriHash);
            Assert.AreEqual(remoteInfo.RemotePath, args.FilesPath);
            Assert.AreEqual(remoteUri, args.RemoteUri);
        }

        [TestMethod]
        public void ResolveSourceFiles_ParsesRemoteUri()
        {
            foreach (var remoteInfo in remotes.Remotes)
            {
                TestRemoteParsing(remoteInfo);
            }
        }

        private string RemoteInfoToPath(RemoteInfo remoteInfo)
        {
            return Path.GetFullPath(Path.Combine(
                _remotesPath,
                remoteInfo.RemoteHash,
                remoteInfo.RemoteOwner,
                remoteInfo.RemoteName,
                remoteInfo.RemotePath));
        }

        private void TestRemoteResolving(RemoteInfo remoteInfo)
        {
            var include = new SourceInclude(remoteInfo.Remote, true, _remotesPath);

            Assert.AreEqual(RemoteInfoToPath(remoteInfo), include.Path);
            Assert.IsTrue(include.IsRemote);
            Assert.IsTrue(include.IsImport);
            Assert.AreEqual(remoteInfo.RemoteName, include.Name);
        }

        [TestMethod]
        public void ResolveSourceFiles_ResolvesRemoteUri()
        {
            foreach (var remoteInfo in remotes.Remotes)
            {
                TestRemoteResolving(remoteInfo);
            }
        }

        private void AssertInclude(SourceInclude include, ImportInfo truth)
        {
            Assert.AreEqual(truth.Name, include.Name);
            Assert.AreEqual(Path.GetFullPath(Path.Combine(_testFilesPath, truth.Path)), include.Path);
            if (truth.IsRemote != null)
            {
                Assert.AreEqual(truth.IsRemote, include.IsRemote);
            }
        }

        [TestMethod]
        public async Task ResolveSourceFiles_ResolvesRemoteImport()
        {
            var project = await new FileSystemXmlProjectLoader(_fileSystem, new XmlProjectDeserializer())
                .LoadProject(Path.Combine(_testFilesPath, "scripts/Skyrim/Skyrim_3.ppj"));

            var builder = new ProgramOptionsBuilder();
            var programOptions = builder.WithRemotesInstallPath(_remotesPath).WithProject(project).Build();

            Assert.AreEqual("Skyrim_3", programOptions.Name);
            Assert.AreEqual(_remotesPath, programOptions.RemotesInstallPath);
            Assert.AreEqual("TESV_Papyrus_Flags.flg", programOptions.FlagsFileName);

            var includes = programOptions.Sources.Includes;
            includes.Reverse();

            Assert.IsTrue(includes.Count == (imports.Imports.Count() + 1));

            // Skip the default import (Skyrim)
            for (int i = 0; i < imports.Imports.Count(); i++)
            {
                AssertInclude(includes[i + 1], imports.Imports.ElementAt(i));
            }

            var program = ServiceProvider.CreateInstance<PapyrusProgram>(programOptions);
            var resolved = await program.ResolveSources();

            // Structured as follows:
            // { importName: { scriptFile: scriptFullPath } }
            var allResolvedScripts = new Dictionary<string, Dictionary<string, string>>();

            foreach (var source in resolved)
            {
                allResolvedScripts
                    .Add(source.Key.Name,
                        source.Value.Select(pathIdentifier => new KeyValuePair<string, string>(pathIdentifier.Key.ToScriptFilePath().Substring(1), pathIdentifier.Value)).ToDictionary());
            }

            // Structured same as above
            var allImports = new Dictionary<string, Dictionary<string, string>>();

            foreach (var import in imports.Imports)
            {
                allImports.Add(import.Name, import.Scripts.Select(script => new KeyValuePair<string, string>(script, Path.GetFullPath(Path.Combine(_testFilesPath, import.Path, script)))).ToDictionary());
                
            }

            foreach (var import in allImports)
            {
                Assert.IsTrue(allResolvedScripts.ContainsKey(import.Key), $"Import with name {import.Key} has not been resolved");
                // { scriptFile: scriptFullPath }
                // From the program
                var resolvedScripts = allResolvedScripts[import.Key];
                foreach (var script in import.Value)
                {
                    Assert.IsTrue(resolvedScripts.ContainsKey(script.Key), $"Script {script.Key} has not been resolved.");
                    Assert.AreEqual(script.Value, resolvedScripts[script.Key], $"Script {script.Key} has incorrect file path.");
                }
            }
        }
        private class RemotesInfo
        {
            public IEnumerable<RemoteInfo> Remotes { get; set; } = new List<RemoteInfo>();
        }
        private class RemoteInfo
        {
            public string Remote { get; set; }
            public string RemoteHash { get; set; }
            public string RemoteName { get; set; }
            public string RemoteOwner { get; set; }
            public string RemotePath { get; set; }
        }

        private class ImportsInfo
        {
            public IEnumerable<ImportInfo> Imports { get; set; } = new List<ImportInfo>();
        }

        private class ImportInfo
        {
            public string Name { get; set;}
            public string Path { get; set; }
            public IEnumerable<string> Scripts = new List<string>();
            public bool? IsRemote { get; set; }
        }
    }
}