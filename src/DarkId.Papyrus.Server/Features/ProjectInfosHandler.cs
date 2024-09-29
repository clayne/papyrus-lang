﻿using Antlr.Runtime;
using Antlr.Runtime.Tree;
using DarkId.Papyrus.Common;
using DarkId.Papyrus.LanguageService.External;
using DarkId.Papyrus.LanguageService.Program;
using DarkId.Papyrus.LanguageService.Program.Syntax;
using DarkId.Papyrus.Server.Protocol;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DarkId.Papyrus.Server.Features
{
    [Method("papyrus/projectInfos")]
    [Parallel]
    public class ProjectInfosHandler : IProjectInfosHandler
    {
        private readonly ProjectManager _projectManager;
        private readonly ILogger _logger;

        public ProjectInfosHandler(ProjectManager projectManager, ILogger<ProjectInfosHandler> logger)
        {
            _projectManager = projectManager;
            _logger = logger;
        }

        public Task<ProjectInfos> Handle(ProjectInfosParams request, CancellationToken cancellationToken)
        {
            if (!_projectManager.Projects.Any())
            {
                _projectManager.UpdateProjects();
            }

            return Task.FromResult(new ProjectInfos()
            {
                Projects = new Container<ProjectInfo>(_projectManager.Projects.AsParallel().AsOrdered().Select(p =>
                {
                    if (p.Sources == null)
                    {
                        p.ResolveSources();
                    }

                    return new ProjectInfo()
                    {
                        Name = p.Name,
                        SourceIncludes = new Container<ProjectInfoSourceInclude>(p.Sources != null ? p.Sources.Select(include =>
                        {
                            return new ProjectInfoSourceInclude()
                            {
                                Name = include.Key.Name,
                                FullPath = include.Key.Path,
                                IsImport = include.Key.IsImport,
                                IsRemote = include.Key.IsRemote,
                                Scripts = new Container<ProjectInfoScript>(include.Value.Select(script => new ProjectInfoScript()
                                {
                                    Identifier = script.Key,
                                    FilePath = script.Value
                                }))
                            };
                        }) : Enumerable.Empty<ProjectInfoSourceInclude>())
                    };
                }))
            });
        }

        public void SetCapability(ProjectInfosCapability capability)
        {
            capability.DynamicRegistration = true;
        }
    }
}
