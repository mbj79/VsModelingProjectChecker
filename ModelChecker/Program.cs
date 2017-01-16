using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ModelChecker
{
    class Program
    {
        private string _modelProjPath;
        private string _projDir;
        private XmlDocument _modelProj;
        private IEnumerable<string> _itemPaths;

        static void Main(string[] args)
        {
            new Program().Run(args);
        }

        private void Run(string[] args)
        {
            ParseArgs(args);

            _projDir = Path.GetDirectoryName(Path.GetFullPath(_modelProjPath));
            _modelProj = LoadXml(_modelProjPath);

            _itemPaths = SelectNodeValues(_modelProj, "/ns:Project/ns:ItemGroup/*[local-name()!='Folder']/@Include");

            var modelPaths = GetFullPathForItemsEndingWith(".uml");
            var diagramPaths = GetFullPathForItemsEndingWith("diagram");

            var models = modelPaths.Select(LoadXml).ToList();
            var diagrams = diagramPaths.Select(LoadXml).ToList();
            var allDocs = models.Concat(diagrams).ToList();

            var diagramRefs = diagrams.SelectMany(diag => SelectNodes(diag, "//ns:elementDefinition[@Id]")).ToList();
            foreach (var elementRef in diagramRefs)
            {
                CheckElementDefinition(models, elementRef);
            }

            var monikers = allDocs.SelectMany(doc => SelectNodes(doc, "//*").Where(n => n.LocalName.EndsWith("Moniker"))).ToList();
            foreach (var moniker in monikers)
            {
                CheckMoniker(allDocs, moniker);
            }

            Console.WriteLine("Press any key to continue ...");
            Console.ReadKey(true);
        }

        private void CheckMoniker(List<XmlDocument> docs, XmlNode refElement)
        {
            var refId = refElement.Attributes["Id"].Value;

            var refDoc = refElement.OwnerDocument.DocumentElement;
            var refDocType = refDoc.LocalName;
            var refDocName = refDoc.Attributes["name"].Value;
            var refType = refElement.LocalName.Substring(0, refElement.LocalName.Length - 7);

            var result = docs
                .SelectMany(m => SelectNodes(m, $"//*[local-name()=\"{refType}\" and @Id=\"{refId}\"]"))
                .ToList();

            var defElement = result.SingleOrDefault();
            if (defElement == null)
            {
                Console.WriteLine($"{refDocType} {refDocName} / {refType} {refId} --> {result.Count} found!");
            }

            //var defDoc = defElement.OwnerDocument.DocumentElement;
            //var defDocType = defDoc.LocalName;
            //var defDocName = defDoc.Attributes["name"].Value;
            //Console.WriteLine($"{defDocType} {defDocName}");
        }

        private void CheckElementDefinition(List<XmlDocument> models, XmlNode refElement)
        {
            var refId = refElement.Attributes["Id"].Value;

            var refDoc = refElement.OwnerDocument.DocumentElement;
            var refDocType = refDoc.LocalName;
            var refDocName = refDoc.Attributes["name"].Value;
            var refType = refElement.ParentNode.LocalName;

            var result = models
                .SelectMany(model => SelectNodes(model, $"//*[@Id=\"{refId}\"]"))
                .Where(element => !element.LocalName.EndsWith("Moniker"))
                .ToList();

            var defElement = result.SingleOrDefault();
            if (defElement == null)
            {
                Console.WriteLine($"{refDocType} {refDocName} / {refType} {refId} --> {result.Count} found!");
            }

            //var defDoc = defElement.OwnerDocument.DocumentElement;
            //var defDocType = defDoc.LocalName;
            //var defDocName = defDoc.Attributes["name"].Value;
            //Console.WriteLine($"{defDocType} {defDocName}");
        }


        private static List<string> SelectNodeValues(XmlDocument xmlDocument, string xPath)
        {
            var nodes = SelectNodes(xmlDocument, xPath);
            var values = nodes.Select(a => a.Value).ToList();

            return values;
        }

        private static List<XmlNode> SelectNodes(XmlDocument xmlDocument, string xPath)
        {
            var nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("ns", xmlDocument.DocumentElement.NamespaceURI);

            var nodes = xmlDocument.DocumentElement.SelectNodes(xPath, nsmgr).Cast<XmlNode>().ToList();
            return nodes;
        }

        private List<string> GetFullPathForItemsEndingWith(string ending)
        {
            return _itemPaths
                .Where(p => p.EndsWith(ending, StringComparison.InvariantCultureIgnoreCase))
                .Select(p => Path.GetFullPath(Path.Combine(_projDir, p)))
                .ToList();
        }

        private XmlDocument LoadXml(string path)
        {
            var xml = new XmlDocument();
            xml.Load(path);
            return xml;
        }

        private void ParseArgs(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("Usage: {0} modelproj");
                Environment.Exit(1);
            }

            _modelProjPath = args[0];
        }
    }
}
