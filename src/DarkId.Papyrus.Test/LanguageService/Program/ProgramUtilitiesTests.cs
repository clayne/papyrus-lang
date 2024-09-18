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

namespace DarkId.Papyrus.Test.LanguageService.Program
{
    [TestClass]
    public class ProgramUtilitiesTests : ProgramTestBase
    {
        private readonly IFileSystem _fileSystem = new LocalFileSystem();
        private readonly static string _remotesPath = "temp";
        private readonly static string _remotesInfoPath = @"..\..\..\..\scripts\RemoteAddresses.json";
        private readonly RemotesInfo remotes = 
            JsonConvert.DeserializeObject<RemotesInfo>(File.ReadAllText(_remotesInfoPath));

        //[TestMethod]
        //public async Task ResolveSourceFiles_ShouldResolveFiles()
        //{
        //    var project = await new FileSystemXmlProjectLoader(_fileSystem, new XmlProjectDeserializer())
        //        .LoadProject("../../../../scripts/Fallout 4/Fallout4.ppj");

        //    var programOptionsBuilder = new ProgramOptionsBuilder();
        //    var programOptions = programOptionsBuilder.WithProject(project).Build();

        //    var projectSourceFiles = await _fileSystem.ResolveSourceFiles(programOptions.Sources);

        //    // TODO: Assertion
        //}
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
        //[TestMethod]
        //public async Task ResolveSourceFiles_ResolvesRemoteImport()
        //{
        //    var project = await new FileSystemXmlProjectLoader(_fileSystem, new XmlProjectDeserializer())
        //        .LoadProject("../../../../scripts/Skyrim/Skyrim_3.ppj");

        //    var builder = new ProgramOptionsBuilder();
        //    var programOptions = builder.WithRemotesInstallPath(_remotesPath).WithProject(project).Build();

        //    Assert.AreEqual(programOptions.RemotesInstallPath, _remotesPath);

        //    Assert.IsTrue(programOptions.Sources.Includes.Count > 1);

        //    // TODO: properly test whether the Uri can retrieve scripts from a temp path
        //}
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
    }
}