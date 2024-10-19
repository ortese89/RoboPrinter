using System.Reflection;
namespace FrontEnds.RoboPrinter.Utility
{
    public class VersionService
    {
        public string GetVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? version.ToString() : "Versione non disponibile";
        }
    }
}
