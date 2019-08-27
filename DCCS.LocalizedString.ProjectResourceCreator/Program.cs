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
            Console.WriteLine($"Started in '{ Directory.GetCurrentDirectory() }'");
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
                bool cultureSwitch = false;
                string cultures = null;
                foreach (var arg in args)
                {
                    try
                    {
                        if (cultureSwitch)
                        {
                            cultures = arg;
                            cultureSwitch = false;
                            continue;
                        }
                        if (arg == "-c" || arg == "--cultures")
                        {
                            cultureSwitch = true;
                            continue;
                        }
                        var extension = Path.GetExtension(arg);
                        if (extension.Equals(".csproj", StringComparison.InvariantCultureIgnoreCase))
                        {
                            csProjects.Add((arg, defaultProjectConfiguration));
                        }
                        else if (extension.Equals(".sln", StringComparison.InvariantCultureIgnoreCase))
                        {
                            configuration = AddSolutionProjects(csProjects, configuration, arg);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Handling file '{arg}' failed.", e);
                    }
                }
                if (csProjects.Count == 0)
                {
                    foreach (var slnFile in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.sln"))
                    {
                        configuration = AddSolutionProjects(csProjects, configuration, slnFile);
                    }
                    if (csProjects.Count == 0)
                    {
                        foreach (var project in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj", SearchOption.AllDirectories))
                        {
                            csProjects.Add((project, defaultProjectConfiguration));
                        }
                    }
                }
                if (csProjects.Count == 0)
                {
                    throw new Exception("Not *.csproj files found or specified");
                }

                foreach ((string ProjectFile, string Configuration) csProject in csProjects)
                {
                    try
                    {
                        Console.WriteLine($"Translate '{Path.GetFileName(csProject.ProjectFile)}' for configuration '{csProject.Configuration}'");
                        var translator = new CSharpProjectTranslator(csProject.ProjectFile, csProject.Configuration, cultures);
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
                Console.WriteLine();
                Console.WriteLine("Specify the path to the .sln or .csproj file in the command line ");
                Console.WriteLine("or run the tool inside of your solution or project dirctory");
            }

            return errorCode;
        }

        private static string AddSolutionProjects(List<(string ProjectFile, string Configuration)> csProjects, string configuration, string arg)
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

            return configuration;
        }

        private static void InitConfiguration()
        {
            Config.SkipProjectsThatAreNotBuilt = AppConfigReader.GetBool("SkipProjectsThatAreNotBuilt", false);
            Config.CreateResXFileCodeGeneratorNode = AppConfigReader.GetBool("CreateResXFileCodeGeneratorNode", false);
        }


    }
}
