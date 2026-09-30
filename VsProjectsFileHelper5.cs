
namespace Aps.Projs._;
/// <summary>
/// Metody co pracují s projectCollection
/// </summary>
public partial class VsProjectsFileHelper
{
    static ProjectCollection projectCollection = new ProjectCollection();

    /// <summary>
    /// add properties to build and releases
    /// </summary>
    /// <param name="propertyGroups"></param>
    public static void AddProperties(List<string> projectList, Dictionary<string, string> propertyGroups)
    {
        bool setAny = false;

        foreach (var project in projectList)
        {
            var proj = projectCollection.LoadProject(project);
            // Select Debug configuration

            setAny = false;

            var xml = proj.Xml;
            var debugPropertyGroup = DebugProperties(xml);
            AddProperties(propertyGroups, debugPropertyGroup, ref setAny);
            debugPropertyGroup.SetProperty("TreatWarningsAsErrors", "true");
            debugPropertyGroup.SetProperty("RunCodeAnalysis", "true");

            // Select Release configuration
            var releasePropertyGroup = ReleaseProperties(xml);
            AddProperties(propertyGroups, releasePropertyGroup, ref setAny);

            if (!setAny)
            {
                //proj.Xml.ItemGroups.FirstOrDefault(e => e.ElementName == )
                var propertyGroup = proj.Xml.PropertyGroups.FirstOrDefault();

                AddProperties(propertyGroups, propertyGroup, ref setAny);
            }

            if (!setAny)
            {
                //////DebugLogger.Instance.WriteLine("Not set: " + project);
            }

            //Save
            proj.Save();
        }
    }

    /// <summary>
    /// Delete very good but something return to original shortly. Must use ordinal xml to delete
    /// </summary>
    /// <param name="ig"></param>
    /// <param name="fn"></param>
    public static void RemoveAllTags(ItemGroups ig, string fn)
    {
        var igts = ig.ToString();
        var csproj = projectCollection.LoadProject(fn);
        var proj = csproj.Xml;
        ICollection<ProjectItemGroupElement> el = proj.ItemGroups;

        CollectionWithoutDuplicates<string> c = new CollectionWithoutDuplicates<string>();

        foreach (var item in el)
        {
            // Items are the same as Children, AllChildren return more values but no one Compile

            // So I have to write with normal XML work.
            // First find whether I dont have already code for that
            foreach (var item2 in item.AllChildren)
            {
                c.Add(item2.ElementName);
            }

            var comp = item.AllChildren.Where(d => d.ElementName == igts);
            comp.ToList().ForEach(e => item.RemoveChild(e));
        }

        proj.Save();

        // Deprecated, use ProjectCollection
        //Engine eng = new Engine();
        //Project proj = new Project(eng);
        //proj.Load(FullProjectPath);
        //proj.SetProperty("SignAssembly", "true");
        //proj.Save(FullProjectPath);
    }


}
