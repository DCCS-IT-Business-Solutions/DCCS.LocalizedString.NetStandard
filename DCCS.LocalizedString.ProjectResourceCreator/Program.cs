using System;
using System.Linq;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Microsoft.Build.Construction;

namespace DCCS.LocalizedString.ProjectResourceCreator
{
    class Program
    {
        public static AppConfiguration Config = new AppConfiguration();

        static int Main(string[] args)
        {

            int errorCode = 0;
            InitConfiguration();

            try
            {                
                List<(string ProjectFile, string Configuration)> csProjects = new List<(string ProjectFile, string Configuration)>();
                string configuration = "";
                string defaultProjectConfiguration = "Debug|Any CPU";
                foreach (var arg in args)
                {
                    const string configFlag = "-config:";
                    if (arg.StartsWith(configFlag, StringComparison.InvariantCultureIgnoreCase))
                    {
                        configuration = arg.Substring(configFlag.Length);
                        if (!configuration.Contains("|"))
                            configuration += "|Any CPU";
                        defaultProjectConfiguration = configuration;
                    }
                }
                foreach (var arg in args)
                {
                    try
                    {
                        var extension = Path.GetExtension(arg);
                        if (extension.Equals(".csproj", StringComparison.InvariantCultureIgnoreCase))
                        {
                            csProjects.Add((arg, defaultProjectConfiguration));
                        }
                        else if (extension.Equals(".sln", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var file = SolutionFile.Parse(arg);
                            if (configuration == "")
                                configuration = file.GetDefaultConfigurationName() + "|" + file.GetDefaultPlatformName();
                            if (!file.SolutionConfigurations.Any(e => e.FullName == configuration))
                                throw new Exception($"Configuration '{configuration}' not found in solution '{arg}'");

                            foreach (var project in file.ProjectsInOrder)
                            {                               
                                string projectConfiguration = configuration;
                                if (project.ProjectConfigurations.TryGetValue(configuration, out var configurationInSolution))
                                {
                                    if (!configurationInSolution.IncludeInBuild)
                                        continue;
                                    projectConfiguration = configurationInSolution.ConfigurationName + "|" + configurationInSolution.PlatformName;
                                }

                                if (project.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                                {
                                    if (Path.GetExtension(project.AbsolutePath).Equals(".csproj", StringComparison.InvariantCultureIgnoreCase))
                                        csProjects.Add((project.AbsolutePath, projectConfiguration));
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Handling file '{arg}' failed.", e);
                    }
                }

                foreach ((string ProjectFile, string Configuration) csProject in csProjects)
                {
                    try
                    {
                        Console.WriteLine($"Translate '{Path.GetFileName(csProject.ProjectFile)}' for configuration '{csProject.Configuration}'");
                        var translator = new CSharpProjectTranslator(csProject.ProjectFile, csProject.Configuration);
                        translator.Translate();
                    }
                    catch (Exception e)
                    {
                        errorCode = 1;
                        Console.Error.WriteLine("Translate project file '{0}' for configuration '{1}' failed: {2}", csProject.ProjectFile, csProject.Configuration, e.Message);
                    }
                }
                Console.WriteLine("Finished");
            }
            catch (Exception e)
            {
                errorCode = 2;
                Console.Error.WriteLine("Error: {0}", e.Message);
            }

            return errorCode;
        }

        private static void InitConfiguration()
        {
            Config.SkipProjectsThatAreNotBuilt = AppConfigReader.GetBool("SkipProjectsThatAreNotBuilt", false);
            Config.CreateResXFileCodeGeneratorNode = AppConfigReader.GetBool("CreateResXFileCodeGeneratorNode", false);
        }


    }
}
