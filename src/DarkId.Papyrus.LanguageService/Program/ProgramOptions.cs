using DarkId.Papyrus.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;

namespace DarkId.Papyrus.LanguageService.Program
{
    public class ProgramOptions : ICloneable
    {
        public string Name { get; set; }
        public string FlagsFileName { get; set; }
        /// <summary>
        /// Path to cached repositories
        /// </summary>
        public string RemotesInstallPath { get; set; }
        public ProgramSources Sources { get; set; } = new ProgramSources();

        public ProgramOptions Clone()
        {
            return new ProgramOptions()
            {
                Name = Name,
                FlagsFileName = FlagsFileName,
                Sources = new ProgramSources()
                {
                    Includes = Sources.Includes.Select(include => new SourceInclude()
                    {
                        Name = include.Name,
                        Path = include.Path,
                        Recursive = include.Recursive,
                        IsImport = include.IsImport,
                        IsRemote = include.IsRemote,
                        Scripts = include.Scripts
                    }).ToList()
                }
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }

    public class ProgramSources
    {
        public List<SourceInclude> Includes { get; set; } = new List<SourceInclude>();
    }

    public class SourceInclude
    {
        private string _path;

        /// <summary>
        /// Non-informative paths that should not be included in `Name`
        /// </summary>
        private static readonly string[] _genericPaths = new[] { "scripts", "source" };
        /// <summary>
        /// The index of a segment in a repository URL's path which is its name
        /// https://github.com/chesko256/Campfire/tree/master_fo4/Scripts/Source
        /// </summary>
        private static readonly int _repoNameIndex = 1;
        /// <summary>
        /// Name to display if none is found
        /// </summary>
        private static readonly string _defaultName = "Unknown";

        /// <summary>
        /// Name of this import (to appear in Project Explorer)
        /// </summary>
        public string Name { get; set; }
        public string Path 
        { get => this._path; 
          set
            {
                // Process as remote if `value` is URI
                Uri remoteAddress;
                this.IsRemote = Uri.TryCreate(value, UriKind.Absolute, out remoteAddress)
                    && (remoteAddress.Scheme == Uri.UriSchemeHttp || remoteAddress.Scheme == Uri.UriSchemeHttps);
                if (!IsRemote)
                {
                    this._path = value;
                    // Set `Name` to first non-generic folder name
                    this.Name = GetNameFromFilePath(value);
                } else
                {
                    var remoteArgs = new RemoteArgs(remoteAddress);
                    // set `Name` based on remote's name
                    this.Name = remoteArgs.RepoName;

                    // set file path as per Pyro's algorithm
                    this._path = System.IO.Path.Combine(
                        this.RemotesInstallPath, 
                        remoteArgs.UriHash,
                        remoteArgs.Owner,
                        remoteArgs.RepoName,
                        remoteArgs.FilesPath
                        );
                }
            }
        }
        public bool Recursive { get; set; } = true;
        public bool IsImport { get; set; }
        /// <summary>
        /// Whether the source is a remote (e.g. GitHub repo)
        /// </summary>
        public bool IsRemote { get; set; }
        public string RemotesInstallPath { get; set; }

        public List<string> Scripts { get; set; } = new List<string>();

        public SourceInclude()
        {

        }

        public SourceInclude(string path, bool isImport, string remotesInstallPath)
        {
            RemotesInstallPath = remotesInstallPath;
            IsImport = isImport;
            Path = path;
        }

        /// <summary>
        /// Gets human-readable name of Include from given `path`
        /// </summary>
        /// <param name="path">The path of the `Import` or `Folder`</param>
        /// <returns>Comprehensible name, or `Unknown` if none found</returns>
        private static string GetNameFromFilePath(string path)
        {
            var splitPath = path.Split(System.IO.Path.PathSeparator).ToList();
            if (!splitPath.Any())
            {
                return _defaultName;
            }
            splitPath.RemoveAt(splitPath.Count - 1);
            // Return first non-generic folder name
            return splitPath.FindLast(folder => !_genericPaths.Contains(folder.ToLower()));
        }

        /// <summary>
        /// Gets human-readable name of Include from give `URI`
        /// </summary>
        /// <param name="uri">The GitHub or Bitbucket remote Uri</param>
        /// <returns>The name of the repo from its Uri</returns>
        private static string GetNameFromRemoteUri(Uri uri)
        {
            // Example address will look like this:
            // https://github.com/chesko256/Campfire/tree/master_fo4/Scripts/Source
            // or this:
            // https://api.github.com/repos/chesko256/Campfire/contents/Scripts/Source?ref=master_fo4
            var nameIndex = _repoNameIndex;
            if (uri.Segments[0].ToLower() == "repos") nameIndex += 1;
            return uri.Segments.ElementAt(nameIndex);
        }

        private class RemoteArgs
        {
            /// <summary>
            /// Repo's name (from the Uri)
            /// </summary>
            public string RepoName { get; set; }
            /// <summary>
            /// Repo's owner (from the Uri)
            /// </summary>
            public string Owner { get; set; }
            /// <summary>
            /// First 8 characters of the Uri's SHA1 hash
            /// </summary>
            public string UriHash { get; set; }
            /// <summary>
            /// The remote's Uri
            /// </summary>
            public Uri RemoteUri { get; set; }
            /// <summary>
            /// Path to import files from repo root
            /// </summary>
            public string FilesPath { get; set; }

            /// <summary>
            /// How much the repo owner is offset among the Uri's path segments
            /// </summary>
            private readonly Dictionary<string, int> _repoOwnerOffset = new Dictionary<string, int>()
            {
                { "repos", 1 },
                { "default", 0 }
            };

            /// <summary>
            /// How much the include paths are offset among the Uri's path segments
            /// </summary>
            private readonly Dictionary<string, int> _repoPathOffset = new Dictionary<string, int>()
            {
                { "tree", 2 },
                { "contents", 1 },
                { "default", 0 }
            };

            public RemoteArgs(Uri remoteUri)
            {
                RemoteUri = remoteUri;

                // Pyro turns the URL into an 8-character SHA1 hash, so we do the same
                var sha = new SHA1CryptoServiceProvider();
                var hashedRemote = sha.ComputeHash(Encoding.UTF8.GetBytes(RemoteUri.OriginalString));
                UriHash = Encoding.UTF8.GetString(hashedRemote).Take(8).ToString();

                // Repositories usually have owner/reponame in their Uri, so we can parse that
                var segments = RemoteUri.Segments;
                if (!_repoOwnerOffset.TryGetValue(segments[0].ToLower(), out var ownerIndex)) {
                    ownerIndex = _repoOwnerOffset["default"];
                }
                Owner = segments[ownerIndex];
                RepoName = segments[ownerIndex + 1];

                // Skip to our paths
                if (!_repoPathOffset.TryGetValue(segments[ownerIndex + 2].ToLower(), out var pathIndex))
                {
                    ownerIndex = _repoPathOffset["default"];
                }
                pathIndex += 2 + ownerIndex;
                FilesPath = System.IO.Path.Combine(segments.Skip(pathIndex).ToArray());
            }
        }
    }
}