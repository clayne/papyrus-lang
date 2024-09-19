using System;
using DarkId.Papyrus.LanguageService.Program;
using DarkId.Papyrus.Test.LanguageService.Program.TestHarness;

namespace DarkId.Papyrus.Test.LanguageService.Program
{
    public abstract class ProgramTestBase : IDisposable
    {
        private readonly PapyrusProgram _program;
        private readonly IServiceProvider _serviceProvider;
        protected PapyrusProgram Program => _program;
        protected IServiceProvider ServiceProvider => _serviceProvider;

        protected ProgramTestBase()
        {
            _program = ProgramTestHarness.CreateProgram();
            _program.ResolveSources().Wait();
            _serviceProvider = ProgramTestHarness.serviceProvider;
        }

        public void Dispose()
        {
            _program.Dispose();
        }
    }
}
