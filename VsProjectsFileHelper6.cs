namespace Aps.Projs._;

/// <summary>
/// Working with Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection
/// </summary>
public partial class VsProjectsFileHelper
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="pathCsproj"></param>
    /// <param name="proj"></param>
    /// <param name="saveToFile"></param>
    public static void SaveXmlDoc(string pathCsproj, Project proj, bool saveToFile = false)
    {
        var xml = proj.Xml.RawXml;
        var xd = XmlHelper.CreateXmlDocument(xml);

        //XmlHelper.AddAttrsToRoot(ref xd, SolutionsIndexerConsts.ProjectsFolderName);
        XmlDocumentsCache.Set(pathCsproj, xd.OuterXml, saveToFile);
    }

    public static Project GetProject(string csproj)
    {
#if DEBUG
        if (csproj == @"E:\vs\Mono_Projects\monoConsoleSqlClient\consoleSqlClient\monoConsoleSqlClient.csproj")
        {

        }
#endif

        Project d2 = null;

        try
        {
            d2 = Microsoft.Build.Evaluation.ProjectCollection.GlobalProjectCollection.LoadedProjects.FirstOrDefault(d => d.FullPath == csproj) ??
            Project.FromFile(csproj, new ProjectOptions());
        }
        catch (Exception ex)
        {
            if (ex.Message.StartsWith("Invalid static method invocation syntax"))
            {
                /* Probably Mono - raise at monoAspTest.csproj etc.
                 *
                 * Invalid static method invocation syntax:
                 * "[MSBuild]::AreFeaturesEnabled('16.8')". Method
                 * '[MSBuild]::AreFeaturesEnabled' not found.
                 * Static method invocation should be of the form:
                 * $([FullTypeName]::Method()), e.g.
                 * $([System.IO.Path]::Combine(`a`, `b`)).
                 * Check that all parameters are defined,
                 * are of the correct type, and are specified
                 * in the right order.
                 * C:\Program Files (x86)\Microsoft Visual Studio
                 * \2019\Community\MSBuild\Current\Bin\amd64\
                 * Microsoft.Common.CurrentVersion.targets'
                 * */


            }
            else if (ex.Message.StartsWith("The imported project"))
            {
                //The imported project "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Microsoft\VisualStudio\v15.0\WebApplications\Microsoft.WebApplication.targets" was not found. Also, tried to find "Microsoft\VisualStudio\v15.0\WebApplications\Microsoft.WebApplication.targets" in the fallback search path(s) for $(MSBuildExtensionsPath32) - "C:\Program Files (x86)\MSBuild" . These search paths are defined in "E:\vs\Projects\AllProjectsSearch\AllProjectsSearch\bin\Debug\AllProjectsSearch.exe.Config". Confirm that the path in the <Import> declaration is correct, and that the file exists on disk in one of the search paths.  E:\vs\Mono_Projects\monoAspTest\monoAspTest\monoAspTest.csproj
            }
            return null;
        }

        return d2;
    }

    #region Pokud GetProejct nepotřebuji protože už projekt mám
    public static string GetElementFromCsproj(Project csproj, Configuration configuration, KeysInConfiguration key)
    {
        var projXml = csproj.Xml;
        var values = Properties(projXml, configuration);

        string value = string.Empty;

        if (values != null)
        {
            value = GetValueOfProperties(values, key);
        }

        return value;
    }
    #endregion

    #region Is using GetProject
    /// <summary>
    /// A2 = Release, Debug
    /// </summary>
    /// <param name="csproj"></param>
    /// <param name="configuration"></param>
    /// <param name="key"></param>
    public static void AddSemicolonDelimitedValue(string csproj, Configuration configuration, KeysInConfiguration key, string v)
    {
        string delimiter = ";";

        Project project = GetProject(csproj);
        if (project != null)
        {
            var value = GetElementFromCsproj(project, configuration, key);
            if (value != null)
            {
                var parts = SHSplit.Split(value, delimiter);
                if (!parts.Contains(v))
                {
                    value = SHTrim.TrimEnd(value, delimiter) + delimiter + v;
                    project.SetProperty(key.ToString(), value);

                    project.Save();
                }
            }
        }
    }

    public static void AddFileToCsproj(string fileToAdd, string csproj)
    {
        fileToAdd = FS.GetFileName(fileToAdd);
        Project project = GetProject(csproj);
        project.AddItem(GetItemType(FS.GetExtension(fileToAdd)), fileToAdd);

#if DEBUG
        //var a = project.AllEvaluatedProperties;
        //project.CreateProjectInstance();
        //var c = project.GlobalProperties;
        //var d = project.Imports;
        //var e = project.Properties;
        //var f = project.Targets;
        //var global = project.SetGlobalProperty("global", "value");
        Dictionary<string, string> dict = new Dictionary<string, string>();
        dict.Add("key", "value");

#endif

        project.SetProperty("TargetFramework", "netstandard2.0");

        var xml = project.Xml;
        var propertyGroups = xml.PropertyGroups;
        var debugPropertyGroup = propertyGroups.FirstOrDefault(
        e => ReplaceSpaceNearApostrphe(e.Condition) == "'$(Configuration)|$(Platform)'=='Debug|AnyCPU'");
        //AddProperties(propertyGroups, debugPropertyGroup, ref setAny);
        //foreach (var item in dict)
        //{
        //    var v = debugPropertyGroup.SetProperty(item.Key, item.Value);
        //    v.Condition = "condition";
        //    v.Label = "Label";
        //    //v.LabelLocation = "LabelLocation";
        //    v.Value = "Value";
        //}


        project.Save();
    }

    public static void SetTargetFrameworksUap(string csproj, string target, string min)
    {
        Project project = GetProject(csproj);
        if (project != null)
        {
            project.SetProperty(ProjectFrameworks.TargetPlatformMinVersion, min);
            project.SetProperty(ProjectFrameworks.TargetPlatformVersion, target);

            project.Save();
        }
    }

    public static string GetElementFromCsproj(string csproj, Configuration configuration, KeysInConfiguration key, out Project project)
    {
        project = GetProject(csproj);
        if (project != null)
        {
            return GetElementFromCsproj(project, configuration, key);
        }
        return null;
    }

    public static string GetElementFromCsproj(string csproj, Configuration configuration, KeysInConfiguration key)
    {
        Project project;
        return GetElementFromCsproj(csproj, configuration, key, out project);
    }
    #endregion

    #region Is using GetProject not directly
    /// <summary>
    /// A2 = Release, Debug
    /// </summary>
    /// <param name="csproj"></param>
    /// <param name="configuration"></param>
    /// <param name="key"></param>
    /// <param name="v"></param>
    public static void RemoveSemicolonDelimitedValue(string csproj, Configuration configuration, KeysInConfiguration key, string v)
    {
        string delimiter = ";";
        Project project = null;
        var value = GetElementFromCsproj(csproj, configuration, key, out project);

        if (project != null)
        {
            if (value != null)
            {
                var parts = SHSplit.Split(value, delimiter);
                if (parts.Contains(v))
                {
                    parts.Remove(v);
                    value = string.Join(delimiter, parts);
                    project.SetProperty(key.ToString(), value);

                    project.Save();
                }
            }
        }
    }

    /// <summary>
    /// A1 in relative path
    ///
    /// Pro přidávání rulesetů atd.
    /// </summary>
    /// <param name="fileToAdd"></param>
    /// <param name="sf"></param>
    /// <param name="relativeFromProjectFolder"></param>
    /// <param name="projectFolder"></param>
    /// <param name="slnFullPath"></param>
    public static void AddFileToCsproj(string fileToAdd, SolutionFolder sf)
    {
        string addBefore = "";

        if (sf.projectFolder != SolutionsIndexerConsts.ProjectsFolderName)
        {
            ThisApp.Error( "Files can be added only to Solution in Project folder");
            return;
        }

        Dictionary<string, string> propertyGroups = new Dictionary<string, string>();
        propertyGroups.Add(fileToAdd, "sunamo.ruleset");

        var projects = sf.projectsInSolution;

        foreach (var item in projects)
        {
            SolutionFolder.GetCsprojs(sf);
            foreach (var csproj in sf.projectsGetCsprojs)
            {
                AddFileToCsproj(fileToAdd, csproj);
            }
        }
    }


    #endregion
}