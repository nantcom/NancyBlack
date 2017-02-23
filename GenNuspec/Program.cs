using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;

namespace GenNuspec
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = XDocument.Parse(File.ReadAllText(@"D:\NantCom\NancyBlack\NancyBlack.nuspec"));
            string[] files = Directory.GetFiles(@"D:\NantCom\NancyBlack\NantCom.NancyBlack", "*.*", SearchOption.AllDirectories);

            var ns = (XNamespace)"http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd";
            var filesNode = file.Root.Element(ns + "files");
            filesNode.RemoveNodes();

            var excluded = @".dll,.csproj,.user,.suo,.git,";
            foreach (var item in files)
            {
                if (item.Contains(@"\bin\") ||
                    item.Contains(@"\obj\") ||
                    item.Contains(@"\.svn\") ||
                    item.Contains(@"\_bin_deployableAssemblies\") ||
                    item.Contains(@"\Sites\") ||
                    item.Contains(@"\Properties\") ||
                    item.Contains(@"packages.config") ||
                    item.Contains(@"Web.Debug.config") ||
                    item.Contains(@"Web.Release.config") ||
                    item.Contains(@"\App_Data\Data.") )
                {
                    continue;
                }

                var ext = Path.GetExtension( item );
                if (excluded.Contains(ext + ","))
                {
                    continue;
                }

                if (item.Contains(@"\fonts\"))
                {

                    var folder = Path.GetDirectoryName(item);
                    if (folder.EndsWith(@"\fonts") == false)
                    {
                        // remove all custom fonts
                        continue;
                    }
                }

                if (item.Contains(@"\Site\"))
                {
                    if (Path.GetFileName(item) != "placeholder.txt")
                    {
                        // for site folder, exclude file other than placeholder.txt
                        continue;
                    }
                }

                if (item.Contains(@"deploy.gitignore"))
                {
                    var gitignore = new XElement(ns + "file");
                    gitignore.SetAttributeValue("src", item);
                    gitignore.SetAttributeValue("target", "content\\.gitignore");

                    filesNode.Add(gitignore);
                    continue;
                }

                var element = new XElement(ns + "file");
                element.SetAttributeValue("src", item);
                element.SetAttributeValue("target", item.Replace("D:\\NantCom\\NancyBlack\\NantCom.NancyBlack\\", "content\\"));

                filesNode.Add(element);

            }

            file.Save(@"D:\NantCom\NancyBlack\NancyBlack.nuspec");

        }
    }
}
