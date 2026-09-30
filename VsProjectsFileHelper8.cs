
namespace Aps.Projs._;
using AllProjectsSearch.Enums;

/// <summary>
/// Only private
/// </summary>
public partial class VsProjectsFileHelper
{
    [Obsolete]
    /// <summary>
    /// NEPOUŽÍVAT NA SDK STYLE. Microsoft.Build je s ním absolutně nekompatibilní a to i přesto že máme 26-1-22 a mám nejnovější verzi 17.0.
    ///
    /// Used in:
    /// ReplaceNugetPackageByDllUC
    ///
    /// Work with Microsoft.Build class
    /// Used for using reference to dll on drive
    ///
    /// If you want to add csproj reference, use CsprojFile instead of VsProjectFileHelper
    /// and ProjectReference insted of ReferenceItemGroup
    /// </summary>
    /// <param name="pathCsproj"></param>
    /// <param name="ig"></param>
    /// <param name="ige"></param>
    public static
#if ASYNC
        async Task
#else
        void
#endif
             AddItemGroupNoSdkStyle(string pathCsproj, ItemGroups ig, ItemGroupElement ige)
    {
        #region Load xml project
        var projectCollection = new ProjectCollection();

        ResultWithException<XmlDocument> x = null;

        x =
#if ASYNC
            await
#endif
XmlDocumentsCache.Get(pathCsproj);

        if (MayExcHelper.MayExc(x))
        {
            return;
        }

        var sdkAttr = XmlHelper.Attr(x.Data.DocumentElement, sdkAttrName);

        if (!string.IsNullOrEmpty(sdkAttr))
        {
            //XmlHelper.RemoveAttrsFromRoot(ref x, SolutionsIndexerConsts.ProjectsFolderName, sdkAttrName);

        }

        XmlNodeReader xr = new XmlNodeReader(x.Data.DocumentElement);

        #region MyRegion
        /*
             * Pokud neodstraním  Sdk="Microsoft.NET.Sdk", vznikne mi toto:
             *
             'SDK Resolver Failure: "The SDK resolver "NuGetSdkResolver" failed while attempting to resolve the SDK "Microsoft.NET.Sdk". Exception: "System.ArgumentNullException: Value cannot be null.
        Parameter name: path
        at System.IO.Directory.GetParent(String path)
        at Microsoft.Build.NuGetSdkResolver.GlobalJsonReader.GetMSBuildSdkVersions(SdkResolverContext context)
        at Microsoft.Build.NuGetSdkResolver.NuGetSdkResolver.TryGetNuGetVersionForSdk(String id, String version, SdkResolverContext context, Object& parsedVersion)
        at Microsoft.Build.NuGetSdkResolver.NuGetSdkResolver.Resolve(SdkReference sdkReference, SdkResolverContext resolverContext, SdkResultFactory factory)
        at Microsoft.Build.BackEnd.SdkResolution.SdkResolverService.ResolveSdk(Int32 submissionId, SdkReference sdk, LoggingContext loggingContext, ElementLocation sdkReferenceLocation, String solutionPath, String projectPath, Boolean interactive, Boolean isRunningInVisualStudio) in /_/src/Build/BackEnd/Components/SdkResolution/SdkResolverService.cs:line 118""'*/
        #endregion

        var proj = projectCollection.LoadProject(xr);

        var xml = proj.Xml;
        #endregion

        #region Find itemgroup by A2
        string wanted = ig.ToString();

        List<ProjectItemGroupElement> itemGroupData = new List<ProjectItemGroupElement>();

        foreach (var itemGroup in xml.ItemGroups)
        {
            var firstElement = itemGroup.FirstChild;
            if (firstElement != null)
            {
                if (firstElement.ElementName == wanted)
                {
                    itemGroupData.Add((ProjectItemGroupElement)itemGroup);
                }
            }
        }

        if (itemGroupData.Count == 0)
        {
            // Really cant create new instance. Ctor is private. CreateNewInstance is protected
            //ProjectItemGroupElement projectItemGroupElement = new ProjectItemGroupElement();
            //xml.ItemGroups.Add(projectItemGroupElement);
            var first = xml.ItemGroups.FirstOrDefault();
            if (first != null)
            {
                itemGroupData.Add(first);
            }
        }

        if (itemGroupData.Count == 0)
        {
            // Jako je to v AddItemGroup to být nemůže, potřebuji objekt
            //XmlNamespaceManager nsmgr;
            //XmlNode itemGroup2, parent, project;
            //LoadXml(ig, x, x.OuterXml, out nsmgr, out itemGroup2, out parent, out project);
            //itemGroup2 = AddNewItemGroup(pathCsproj, x, nsmgr, itemGroup2, project);

            //itemGroupData.Add(new ProjectItemGroupElement())
            //xml.ItemGroups.A
            ProjectItemGroupElement s = xml.AddItemGroup();

            itemGroupData.Add(s);
        }
        #endregion


        List<string> list = new List<string>();
        #region Add to list all of theirs include
        // Užívá se AddItem, Children, RemoveChild
        foreach (var itemGroupData2 in itemGroupData)
        {
            list.Clear();

            foreach (var item in itemGroupData2.Children)
            {
                list.Add(((ProjectItemElement)item).Include);
            }
            #endregion

            Type tIge = ige.GetType();

            #region Add Reference
            if (tIge == typeof(ReferenceItemGroup))
            {
                #region ReferenceItemGroup
                ReferenceItemGroup referenceItemGroup = (ReferenceItemGroup)ige;
                referenceItemGroup.Include = Path.GetFileNameWithoutExtension(referenceItemGroup.Include);

                #region Remove duplicates. If contains, return
                // not start always with comma
                //+ AllStrings.comma
                var list2 = itemGroupData2.Children.Where(d => ((ProjectItemElement)d).Include.StartsWith(ige.Include)).ToList();

                list2.Skip(1).ToList().ForEach(k => itemGroupData2.RemoveChild(k));

                if (CAG.IsEqualToAnyElement<string>(ige.Include, list))
                {
                    if (list2.Count > 0)
                    {
                        SaveXmlDoc(pathCsproj, proj);
                    }
                    return;
                }
                #endregion

                //referenceItemGroup.HintPath = ApsHelper.ci.GetRelativePathToProject(referenceItemGroup._FullPathDll, FS.MakeFromLastPartFile(referenceItemGroup._FullPath, AllExtensions.csproj));
                //referenceItemGroup.HintPath = referenceItemGroup._RelativePathDllToProjectFolderHintPath;

                Dictionary<string, string> dict = new Dictionary<string, string>();
                var hp = referenceItemGroup._RelativePathDllToProjectFolderHintPath;
                if (!string.IsNullOrEmpty(hp))
                {
                    dict.Add("HintPath", hp);
                }

                itemGroupData2.AddItem(ItemGroups.Reference.ToString(), referenceItemGroup.Include, dict);
                #endregion ReferenceItemGroup
            }
            else if (tIge == typeof(CompileItemGroup))
            {
                CompileItemGroup compileItemGroup = (CompileItemGroup)ige;

                #region Remove duplicates. If contains, return
                // not start always with comma
                //+ AllStrings.comma
                var list2 = itemGroupData2.Children.Where(d => ((ProjectItemElement)d).Include.StartsWith(ige.Include)).ToList();

                list2.ForEach(k => itemGroupData2.RemoveChild(k));

                if (CA.IsEqualToAnyElement<string>(ige.Include, list))
                {
                    if (list2.Count > 0)
                    {
                        SaveXmlDoc(pathCsproj, proj);
                    }
                    return;
                }
                #endregion

                Dictionary<string, string> dict = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(compileItemGroup.Link))
                {
                    dict.Add("Link", compileItemGroup.Link);
                }

                string include = string.Empty;
                // originally was here this. But dont know purpose of it. If I add reference to sunamo from sunamo.web need it without Substring(6)
                //include = compileItemGroup.Include.Substring(6);
                include = compileItemGroup.Include;

                itemGroupData2.AddItem(ItemGroups.Compile.ToString(), include, dict);
            }
            #endregion
        }
        SaveXmlDoc(pathCsproj, proj);

    }

    public static void CreateCsproj(string csprojPath, params string[] filesToCompile)
    {
        string path = FS.WithEndSlash(FS.GetDirectoryName(csprojPath));

        var root = ProjectRootElement.Create();
        var group = root.AddPropertyGroup();
        group.AddProperty("Configuration", "Debug");
        group.AddProperty("Platform", "x64");

        // references
        AddItems(root, ItemGroupsConsts.Reference, "System", "System.Core");

        foreach (var item in filesToCompile)
        {
            // items to compile
            AddItems(root, ItemGroupsConsts.Compile, item.Replace(path, string.Empty));
        }

        var target = root.AddTarget("Build");
        var task = target.AddTask("Csc");
        task.SetParameter("Sources", "@(Compile)");
        task.SetParameter("OutputAssembly", "test.dll");

        root.Save(csprojPath);

    }

    /// <summary>
    /// A2 = DefineConstants
    /// </summary>
    /// <param name="values"></param>
    /// <param name="key"></param>
    private static string GetValueOfProperties(ProjectPropertyGroupElement values, KeysInConfiguration key)
    {
        var keyS = key.ToString();
        foreach (var item in values.Properties)
        {
            if (item.Name == keyS)
            {
                return item.Value;
            }
        }
        return null;
    }

    private static string GetItemType(string extension)
    {
        switch (extension)
        {
            case ".cs":
                return VsProjectItemTypes.Compile;
            default:
                return VsProjectItemTypes.Content;

                //ThrowEx.NotImplementedCase(Exc.GetStackTrace(),type, "GetItemType");
        }
    }

    static string ReplaceSpaceNearApostrphe(string s)
    {
        return SHReplace.ReplaceAll(s, "'", " '", "' ");
    }

    static ProjectPropertyGroupElement DebugProperties(ProjectRootElement projXml)
    {
        return Properties(projXml, Configuration.Debug);
    }

    static ProjectPropertyGroupElement ReleaseProperties(ProjectRootElement projXml)
    {
        return Properties(projXml, Configuration.Release);
    }

    private static ProjectPropertyGroupElement Properties(ProjectRootElement projXml, Configuration configuration)
    {
        return projXml.PropertyGroups.FirstOrDefault(
                        e => ReplaceSpaceNearApostrphe(e.Condition) == "'$(Configuration)|$(Platform)'=='" + configuration.ToString() + "|AnyCPU'");
    }

    private static void AddProperties(Dictionary<string, string> propertyGroups, ProjectPropertyGroupElement debugPropertyGroup, ref bool addAny)
    {
        if (debugPropertyGroup != null)
        {
            foreach (var item in propertyGroups)
            {
                var v = debugPropertyGroup.SetProperty(item.Key, item.Value);


                //debugPropertyGroup.AddProperty(item.Key, item.Value);
                addAny = true;
            }
        }
    }

    private static XmlNode AddNewItemGroup(string pathCsproj, XmlDocument xd, XmlNamespaceManager nsmgr, XmlNode itemGroup, XmlNode project)
    {
        if (project == null)
        {
            AllProjectsSearchThrow.IsNotValidProjectFile(type, "AddItemGroup", pathCsproj);
        }
        else
        {
            XmlGenerator xg = new XmlGenerator();
            xg.WriteElement("ItemGroup", string.Empty);
            var xn = XH.ReturnXmlNode(xg.ToString(), xd);
            xn = xd.ImportNode(xn, true);
            project.AppendChild(xn);

            itemGroup = xd.SelectSingleNode("//Project/ItemGroup", nsmgr);
        }

        return itemGroup;
    }


    private static void ReplaceProjectTemplateParameter(ref string c, VsProjectTemplateParameters guid1, object guid)
    {
        c = c.Replace(SH.WrapWith(guid1.ToString(), AllChars.dollar), guid.ToString());
    }

    private static void AddItems(ProjectRootElement elem, string groupName, params string[] items)
    {
        var group = elem.AddItemGroup();
        foreach (var item in items)
        {
            group.AddItem(groupName, item);
        }
    }

    private static void CreateCsprojWithFile(string filePath)
    {
        string folder = FS.GetDirectoryName(filePath);

        string file = Path.GetFileNameWithoutExtension(filePath);

        string csProj = Path.Combine(folder, file + AllExtensions.csproj);

        //ApsProjsHelper.SaveEmptyTestProject(csProj);
        ApsProjsHelper.SaveEmptyFullNetProject(csProj, file);

        VsProjectsFileHelper.AddFileToCsproj(filePath, csProj);

        //var msWorkspace = MSBuildWorkspace.Create();
    }
}
