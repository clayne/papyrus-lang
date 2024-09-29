﻿using DarkId.Papyrus.LanguageService.Configuration.CreationKit;

namespace DarkId.Papyrus.Server
{
    public class PapyrusLanguageServerOptions
    {
        public string FlagsFileName { get; set; }
        public string AmbientProjectName { get; set; }
        public string RemotesInstallPath { get; set; }
        public CreationKitConfig DefaultCreationKitConfig { get; set; }
        public CreationKitIniLocations IniLocations { get; set; }
    }
}