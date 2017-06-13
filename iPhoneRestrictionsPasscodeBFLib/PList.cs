using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace iPhoneRestrictionsPasscodeBFLib
{
    public class PList : Dictionary<string, dynamic>
    {
        public PList() : base(StringComparer.OrdinalIgnoreCase)
        { }

        public PList(string plistContent) : this()
        {
            LoadFromContent(plistContent);
        }

        public PList(Uri file) : this()
        {
            LoadFromFile(file);
        }

        private void LoadFromFile(Uri file)
        {
            LoadFromContent(File.ReadAllText(file.LocalPath));
        }

        private void LoadFromContent(string content)
        {
            Clear();

            XDocument doc = XDocument.Parse(content);
            XElement plist = doc.Element("plist");
            XElement dict = plist.Element("dict");

            var dictElements = dict.Elements();
            Parse(this, dictElements);
        }

        private static void Parse(PList dict, IEnumerable<XElement> elements)
        {
            for (int i = 0; i < elements.Count(); i += 2)
            {
                XElement key = elements.ElementAt(i);
                XElement val = elements.ElementAt(i + 1);

                dict[key.Value] = ParseValue(val);
            }
        }

        private static List<dynamic> ParseArray(IEnumerable<XElement> elements)
        {
            List<dynamic> list = new List<dynamic>();
            foreach (XElement e in elements)
            {
                dynamic one = ParseValue(e);
                list.Add(one);
            }

            return list;
        }

        private static dynamic ParseValue(XElement val)
        {
            switch (val.Name.ToString().ToLowerInvariant())
            {
                case "data":
                case "string":
                    return val.Value;
                case "integer":
                    return int.Parse(val.Value);
                case "real":
                    return float.Parse(val.Value);
                case "true":
                    return true;
                case "false":
                    return false;
                case "dict":
                    PList plist = new PList();
                    Parse(plist, val.Elements());
                    return plist;
                case "array":
                    List<dynamic> list = ParseArray(val.Elements());
                    return list;
                default:
                    throw new ArgumentException("Unsupported");
            }
        }
    }
}
