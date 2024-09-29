using System.Collections.Generic;
using System.IO;
using System.Linq;
using DarkId.Papyrus.Common;

namespace DarkId.Papyrus.LanguageService.Program
{
    public class ProgramOptionsBuilder
    {
        private readonly ProgramOptions _options = new ProgramOptions();

        public ProgramOptionsBuilder WithName(string name)
        {
            _options.Name = name;
            return this;
        }

        public ProgramOptionsBuilder WithFlagsFileName(string fileName)
        {
            _options.FlagsFileName = fileName;
            return this;
        }

        public ProgramOptionsBuilder WithSourceIncludes(params SourceInclude[] includes)
        {
            return WithSourceIncludes(includes.AsEnumerable());
        }

        public ProgramOptionsBuilder WithSourceIncludes(IEnumerable<SourceInclude> includes)
        {
            _options.Sources.Includes.AddRange(includes);
            return this;
        }

        public ProgramOptionsBuilder WithRemotesInstallPath(string remotesPath)
        {
            _options.RemotesInstallPath = remotesPath;
            return this;
        }

        public ProgramOptions Build()
        {
            return _options;
        }
    }
}