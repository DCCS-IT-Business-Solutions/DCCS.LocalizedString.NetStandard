using DCCS.LocalizedString.NetStandard;
using DCCS.LocalizedString.NetStandard.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace DCCS.LocalizedString.ProjectResourceCreator
{
    class CSharpProjectTranslator
    {
        const string MissingPrefix = "#MISSING";
        const string ErrorPrefix = "#ERROR";
        const string UnusedPrefix = "#UNUSED";

        private readonly string _projectFile;
        private readonly string _configurationAndPlatform;
        private readonly string _configurationAndPlatformString;
        private readonly string _configuration;
        private readonly string _culturesNames;

        public CSharpProjectTranslator(string projectFile, string configurationAndPlatform, string cultureNames)
        {            
            _projectFile = Path.GetFullPath(projectFile);
            _configurationAndPlatform = configurationAndPlatform;
            _configurationAndPlatformString = "'" + _configurationAndPlatform + "'";
            _configuration = configurationAndPlatform.Split('|')[0];
            _culturesNames = cultureNames;
        }

        public void Translate()
        {
            ReadProjectFile(out var projectXmlDocument, out var extension, out var assemblyName, out var outputDirectory, out var namespaceManager, out var projectType);
            if (projectType == ProjectType.NetCore)
            {
                string directory = Path.GetDirectoryName(_projectFile);
                string projectFile = Path.GetFileName(_projectFile);
                Console.WriteLine($"Run build for {Path.GetFileName(_projectFile)}");
                var processStart = new ProcessStartInfo("dotnet", $"build -c {_configuration} {projectFile}");
                processStart.WorkingDirectory = directory;
                processStart.UseShellExecute = false;
                using (Process process = Process.Start(processStart))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Build for {_projectFile} failed");
                    }
                }
            }
            // setup
            string projectDir = Path.GetDirectoryName(_projectFile);

            string workingDirectory = Directory.GetCurrentDirectory();
            string dllPath = Path.Combine(workingDirectory, assemblyName + extension);
            if (!File.Exists(dllPath))
            {
                dllPath = Path.Combine(projectDir, outputDirectory, assemblyName + extension);
                if (!File.Exists(dllPath))
                {
                    throw new Exception($"Compiled Debug build assembly {dllPath} not found");
                }
            }
            // load assembly and check needed entries
            Assembly assembly = Assembly.LoadFrom(dllPath);
            var requiredEntries = LocalizerUsageSearcher.GetLocalizerEntries(assembly).ToList();

            List<CultureInfo> cultures = new List<CultureInfo>();
            cultures.Add(CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(_culturesNames))
            {
                cultures.Add(CultureInfo.CurrentUICulture.Parent != null && CultureInfo.CurrentUICulture.Parent != CultureInfo.InvariantCulture ? CultureInfo.CurrentUICulture.Parent : CultureInfo.CurrentUICulture);
            }
            else
            {
                foreach (var cultureName in _culturesNames.Split(',', ';'))
                {
                    cultures.Add(CultureInfo.GetCultureInfo(cultureName));
                }
            }

            foreach (CultureInfo culture in cultures)
            {
                // setup
                string languageFileNamePart = "";
                if (culture.LCID != CultureInfo.InvariantCulture.LCID)
                {
                    languageFileNamePart = "." + culture.Name;
                }

                string relativeResourcePath = Path.Combine("Properties", "Resources" + languageFileNamePart + ".resx");
                string resourceFilePath = Path.Combine(projectDir, relativeResourcePath);
                Dictionary<string, DataNode> existingEntries = new Dictionary<string, DataNode>(StringComparer.InvariantCultureIgnoreCase);
                List<DataNode> nodesToWrite = new List<DataNode>();

                // Load resource file
                XmlDocument resourceDocument = LoadOrCreateResourceXml(resourceFilePath);

                // read entries from resource file
                ReadExistingResourceEntries(resourceDocument, existingEntries);
              
                // update the entries
                UpdateOrCreateEntries(requiredEntries, existingEntries, culture, nodesToWrite);

                // update unused entries
                UpdateUnusedEntries(existingEntries, nodesToWrite);

                // write
                if (nodesToWrite.Count > 0)
                {
                    UpdateResourceFile(nodesToWrite, resourceDocument, resourceFilePath);
                    UpdateProjectFile(projectXmlDocument, namespaceManager, projectType, relativeResourcePath, culture);
                }
            }
        }

        private static XmlDocument LoadOrCreateResourceXml(string resourceFilePath)
        {
            var resourceDocument = new XmlDocument();
            if (File.Exists(resourceFilePath))
            {
                resourceDocument.Load(resourceFilePath);
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly();
                var names = assembly.GetManifestResourceNames();

                using (Stream stream = assembly.GetManifestResourceStream(names.First(n => n.Contains("EmptyResources"))))
                {
                    resourceDocument.Load(stream);
                }
            }

            return resourceDocument;
        }

        private static void UpdateOrCreateEntries(List<LocalizerEntry> requiredEntries, Dictionary<string, DataNode> existingEntries, CultureInfo culture, List<DataNode> nodesToWrite)
        {
            foreach (var entry in requiredEntries)
            {
                string textToWrite = null;
                StringBuilder commentBuilder = new StringBuilder();
                string resouceKey = ResourceTranslationProvider.BuildResourceKey(entry.Key);
                if (existingEntries.TryGetValue(resouceKey, out DataNode existingEntry))
                {
                    existingEntries.Remove(existingEntry.Name);
                    textToWrite = existingEntry.Value;
                    if (textToWrite.StartsWith(ResourceTranslationProvider.CommentPrefix))
                        textToWrite = null;
                }

                if (string.IsNullOrEmpty(textToWrite))
                {
                    if (culture.LCID != CultureInfo.InvariantCulture.LCID)
                    {
                        commentBuilder.Append(MissingPrefix);
                        commentBuilder.Append(" ");
                    }

                    textToWrite = ResourceTranslationProvider.CommentPrefix + entry.Default;
                }
                else
                {
                    // ReSharper disable once CoVariantArrayConversion
                    StringTools.SafeFormat(out Exception e, null, textToWrite, entry.ParameterNames);
                    if (e != null)
                    {
                        commentBuilder.Append(ErrorPrefix);
                        commentBuilder.Append(" (");
                        commentBuilder.Append(e.Message);
                        commentBuilder.Append(") ");
                    }
                }

                commentBuilder.Append(entry.Default);
                if (entry.ParameterNames.Length > 0)
                {
                    commentBuilder.Append(" (");
                    commentBuilder.AppendJoin(", ", entry.ParameterNames);
                    commentBuilder.Append(")");
                }

                if (existingEntry == null)
                    existingEntry = new DataNode();
                existingEntry.Name = resouceKey;
                existingEntry.Value = textToWrite;
                existingEntry.Comment = commentBuilder.ToString();
                nodesToWrite.Add(existingEntry);
            }
        }

        private static void UpdateUnusedEntries(Dictionary<string, DataNode> existingEntries, List<DataNode> nodesToWrite)
        {
            foreach (var entry in existingEntries)
            {
                var node = entry.Value;
                string comment = node.Comment;
                if (comment != null)
                {
                    if (comment.StartsWith(ErrorPrefix, StringComparison.InvariantCultureIgnoreCase))
                        comment = comment.Substring(ErrorPrefix.Length);
                    if (comment.StartsWith(MissingPrefix, StringComparison.InvariantCultureIgnoreCase))
                        comment = comment.Substring(MissingPrefix.Length);
                    if (comment.StartsWith(UnusedPrefix, StringComparison.InvariantCultureIgnoreCase))
                        comment = comment.Substring(MissingPrefix.Length);
                }
                else
                {
                    comment = "";
                }

                node.Comment = UnusedPrefix + " " + comment.Trim();
                nodesToWrite.Add(node);
            }
        }

        private static void ReadExistingResourceEntries(XmlDocument resourceDocument, Dictionary<string, DataNode> existingEntries)
        {
            foreach (var node in resourceDocument.SelectNodes("/root/data").OfType<XmlNode>())
            {
                var nameAttribute = node.Attributes["name"];
                if (nameAttribute != null && node.Attributes["type"] == null)
                {
                    var valueNode = node.SelectSingleNode("value");
                    if (valueNode != null)
                    {
                        string value = valueNode.InnerText;
                        var dataNode = new DataNode();
                        dataNode.Name = nameAttribute.Value;
                        dataNode.Value = value;

                        var commentNode = node.SelectSingleNode("comment");
                        if (commentNode != null)
                            dataNode.Comment = commentNode.InnerText;
                        dataNode.ValueNode = valueNode;
                        dataNode.CommentNode = commentNode;
                        existingEntries.Add(dataNode.Name, dataNode);
                    }
                }
            }
        }

        private void UpdateProjectFile(XmlDocument projectXmlDocument, XmlNamespaceManager namespaceManager, ProjectType projectType, string relativeResourcePath, CultureInfo culture)
        {
            string attributeName = projectType == ProjectType.NetCore ? "Update" : "Include";
            string nameSpace = projectType == ProjectType.NetCore ? "" : "http://schemas.microsoft.com/developer/msbuild/2003";
            var resourceNode = projectXmlDocument.SelectSingleNode($"/x:Project/x:ItemGroup/x:EmbeddedResource[@{attributeName}='{relativeResourcePath}']", namespaceManager);
            if (resourceNode == null)
            {
                // No resource nodes, add a new

                // create compile node for invariant culture
                if (culture.LCID == CultureInfo.InvariantCulture.LCID)
                {
                    var compilesItemGroup = projectXmlDocument.SelectSingleNode("/x:Project/x:ItemGroup/x:Compile/..", namespaceManager);
                    if (compilesItemGroup == null)
                    {
                        compilesItemGroup = projectXmlDocument.CreateElement("", "ItemGroup", nameSpace);
                        var projectNode = projectXmlDocument.SelectSingleNode("/x:Project", namespaceManager);
                        if (projectNode == null)
                            throw new Exception("Project node not found");
                        projectNode.AppendChild(compilesItemGroup);
                    }

                    var compileNode = projectXmlDocument.CreateElement("", "Compile", nameSpace);
                    compilesItemGroup.AppendChild(compileNode);
                    var updateAttributeCompile = projectXmlDocument.CreateAttribute(attributeName);
                    updateAttributeCompile.Value = @"Properties\Resources.Designer.cs";
                    compileNode.Attributes.Append(updateAttributeCompile);

                    var designTimeNode = projectXmlDocument.CreateElement("", "DesignTime", nameSpace);
                    designTimeNode.InnerText = "True";
                    compileNode.AppendChild(designTimeNode);

                    var autoGenerateNode = projectXmlDocument.CreateElement("", "AutoGen", nameSpace);
                    autoGenerateNode.InnerText = "True";
                    compileNode.AppendChild(autoGenerateNode);

                    var dependentUponNode = projectXmlDocument.CreateElement("", "DependentUpon", nameSpace);
                    dependentUponNode.InnerText = "Resources.resx";
                    compileNode.AppendChild(dependentUponNode);
                }

                // create resource parent node
                var resourcesItemGroup = projectXmlDocument.SelectSingleNode("/x:Project/x:ItemGroup/x:EmbeddedResource/..", namespaceManager);
                if (resourcesItemGroup == null)
                {
                    resourcesItemGroup = projectXmlDocument.CreateElement("", "ItemGroup", nameSpace);
                    var projectNode = projectXmlDocument.SelectSingleNode("/x:Project", namespaceManager);
                    if (projectNode == null)
                        throw new Exception("Project node not found");
                    projectNode.AppendChild(resourcesItemGroup);
                }

                // create resource node
                resourceNode = projectXmlDocument.CreateElement("", "EmbeddedResource", nameSpace);

                resourcesItemGroup.AppendChild(resourceNode);
                var generatorNode = projectXmlDocument.CreateElement("", "Generator", nameSpace);
                resourceNode.AppendChild(generatorNode);

                if (culture.LCID == CultureInfo.InvariantCulture.LCID && Program.Config.CreateResXFileCodeGeneratorNode)
                {
                    generatorNode.InnerText = "ResXFileCodeGenerator";
                    var lastGenOutputNode = projectXmlDocument.CreateElement("", "LastGenOutput", nameSpace);
                    resourceNode.AppendChild(lastGenOutputNode);
                    lastGenOutputNode.InnerText = "Resources.Designer.cs";
                }

                var updateAttribute = projectXmlDocument.CreateAttribute(attributeName);
                updateAttribute.Value = relativeResourcePath;
                resourceNode.Attributes.Append(updateAttribute);

                // save the resource file
                projectXmlDocument.Save(_projectFile);
            }
        }

        private static void UpdateResourceFile(List<DataNode> nodesToWrite, XmlDocument resourceDocument, string resourceFilePath)
        {
            var rootNode = resourceDocument.SelectSingleNode("root");
            if (rootNode == null)
                throw new Exception("root node not found in resource file");

            foreach (var node in nodesToWrite)
            {
                if (node.ValueNode == null)
                {
                    var dataNode = resourceDocument.CreateElement("data");
                    rootNode.AppendChild(dataNode);
                    var nameAttribute = resourceDocument.CreateAttribute("name");
                    nameAttribute.InnerText = node.Name;
                    dataNode.Attributes.Append(nameAttribute);
                    var valueNode = resourceDocument.CreateElement("value");
                    node.ValueNode = valueNode;
                    dataNode.AppendChild(valueNode);
                }

                if (node.CommentNode == null)
                {
                    var dataNode = node.ValueNode.ParentNode;
                    var commentNode = resourceDocument.CreateElement("comment");
                    node.CommentNode = commentNode;
                    dataNode.AppendChild(commentNode);
                }

                node.ValueNode.InnerText = node.Value;
                node.CommentNode.InnerText = node.Comment;
            }
            var dir = Path.GetDirectoryName(resourceFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            resourceDocument.Save(resourceFilePath);
        }

        private void ReadProjectFile(out XmlDocument projectXmlDocument, out string extension, out string assemblyName, out string outputDirectory, out XmlNamespaceManager namespaceManager, out ProjectType projectType)
        {
            projectXmlDocument = new XmlDocument();
            projectXmlDocument.Load(_projectFile);

            var projectNode = projectXmlDocument.SelectSingleNode("/Project");
            namespaceManager = new XmlNamespaceManager(projectXmlDocument.NameTable);
            namespaceManager.AddNamespace("x", "");

            if (projectNode == null)
            {
                // .NET Project (old .csproj file format)
                namespaceManager.AddNamespace("", "");
                namespaceManager.AddNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");
                projectNode = projectXmlDocument.SelectSingleNode("/x:Project", namespaceManager);
                if (projectNode == null)
                    throw new Exception("Unknown project type");
                projectType = ProjectType.Net;
                var outputTypeNode = projectXmlDocument.SelectSingleNode("/x:Project/x:PropertyGroup/x:OutputType", namespaceManager);
                extension = outputTypeNode != null && (string.Equals(outputTypeNode.InnerText, "Exe", StringComparison.InvariantCultureIgnoreCase) || string.Equals(outputTypeNode.InnerText, "winexe", StringComparison.InvariantCultureIgnoreCase) ) ? ".exe" : ".dll";

                var assemblyNameNode = projectXmlDocument.SelectSingleNode("/x:Project/x:PropertyGroup/x:AssemblyName", namespaceManager);
                if (assemblyNameNode != null && !string.IsNullOrEmpty(assemblyNameNode.InnerText))
                    assemblyName = assemblyNameNode.InnerText;
                else
                    assemblyName = Path.GetFileNameWithoutExtension(_projectFile);                
                outputDirectory = Path.Combine("bin", _configuration);
                foreach (var outputPathNode in projectXmlDocument.SelectNodes("/x:Project/x:PropertyGroup/x:OutputPath", namespaceManager).OfType<XmlNode>())
                {
                    if (!string.IsNullOrEmpty(outputPathNode.InnerText))
                    {
                        string condition = outputPathNode.ParentNode.Attributes["Condition"].InnerText;
                        if (condition != null && condition.Contains(_configurationAndPlatformString, StringComparison.InvariantCultureIgnoreCase))
                        {
                            outputDirectory = Path.Combine(outputPathNode.InnerText);
                            break;
                        }
                    }
                }
            }
            else
            {
                // .NET Core (new .csproj file format)
                projectType = ProjectType.NetCore;
                var targetFrameworkNode = projectXmlDocument.SelectSingleNode("/x:Project/x:PropertyGroup/x:TargetFramework", namespaceManager);
                string targetFramework;
                if (targetFrameworkNode == null)
                {
                    var targetFrameworksNode = projectXmlDocument.SelectSingleNode("/x:Project/x:PropertyGroup/x:TargetFrameworks", namespaceManager);
                    if (targetFrameworksNode == null)
                        throw new Exception("TargetFramework node not found in project file");
                    targetFramework = targetFrameworksNode.InnerText.Split(';')[0].Trim();
                }
                else
                {
                    targetFramework = targetFrameworkNode.InnerText;
                }

                if (string.IsNullOrEmpty(targetFramework))
                    throw new Exception("TargetFramework is emptry");
                if (targetFramework.StartsWith("netcoreapp", StringComparison.InvariantCultureIgnoreCase)) 
                {
                    extension = ".dll";
                }
                else
                {
                    var outputTypeNode = projectXmlDocument.SelectSingleNode("/x:Project/x:PropertyGroup/x:OutputType", namespaceManager);
                    extension = outputTypeNode != null && string.Equals(outputTypeNode.InnerText, "Exe", StringComparison.InvariantCultureIgnoreCase) ? ".exe" : ".dll";
                }

                var assemblyNameNode = projectXmlDocument.SelectSingleNode("/x:Project/x:PropertyGroup/x:AssemblyName", namespaceManager);
                if (assemblyNameNode != null && !string.IsNullOrEmpty(assemblyNameNode.InnerText))
                    assemblyName = assemblyNameNode.InnerText;
                else
                    assemblyName = Path.GetFileNameWithoutExtension(_projectFile);

                outputDirectory = Path.Combine("bin", _configuration, targetFramework);
                foreach (var outputPathNode in projectXmlDocument.SelectNodes("/x:Project/x:PropertyGroup/x:OutputPath", namespaceManager).OfType<XmlNode>())
                {
                    if (!string.IsNullOrEmpty(outputPathNode.InnerText))
                    {
                        string condition = outputPathNode.ParentNode.Attributes["Condition"].InnerText;
                        if (condition != null && condition.Contains(_configurationAndPlatformString, StringComparison.InvariantCultureIgnoreCase))
                        {
                            outputDirectory = Path.Combine(outputPathNode.InnerText, targetFramework);
                            break;
                        }
                    }
                }
            }
        }
    }

    enum ProjectType
    {
        Net,
        NetCore
    }

    class DataNode
    {
        public string Name;
        public string Value;
        public string Comment;
        public XmlNode ValueNode;
        public XmlNode CommentNode;
    }
}
