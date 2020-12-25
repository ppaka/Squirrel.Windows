using System.Linq;
using System.Xml.Linq;
using EnvDTE;

namespace NuGet.VisualStudio
{
    public class VcxProject
    {

        private readonly XDocument vcxFile;
        public VcxProject(string fullname)
        {
            vcxFile = Loader.Instance.LoadXml(fullname);
        }

        public bool HasClrSupport(Configuration config)
        {
            string filter = config.ConfigurationName + "|" + config.PlatformName;
            var elements = vcxFile.Descendants().Where(x => x.Name.LocalName == "PropertyGroup");
            var propertyGroups =
                elements.Where(x => x.Attribute("Label") != null && x.Attribute("Label").Value == "Configuration");

            var actualPropertyGroups =
                propertyGroups.Where(x => x.Attribute("Condition") != null && x.Attribute("Condition").Value.Contains(filter));
            var clritems = actualPropertyGroups.Elements().Where(e => e.Name.LocalName == "CLRSupport");
            var overrideitems = actualPropertyGroups.Elements().Where(e => e.Name.LocalName == "UseNativeNuGet");
            if (overrideitems.Any())
            {
                var useNativeNuget = overrideitems.First().Value;
                if (useNativeNuget.ToLower()=="true")
                    return false;
            }
            if (clritems.Any())
            {
                var clr = clritems.First();
                return clr.Value.ToLower() == "true";
            }
            return false;
        }


    }
}