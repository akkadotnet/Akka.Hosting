// -----------------------------------------------------------------------
//  <copyright file="XmlHelper.cs" company="Akka.NET Project">
//      Copyright 2021 Maintainers of NUKE.
//      Copyright (C) 2013-2022 .NET Foundation <https://github.com/akkadotnet/akka.net>
//      Distributed under the MIT License.
//      https://github.com/nuke-build/nuke/blob/master/LICENSE
//  </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Nuke.Common;

public static class XmlHelper
{
    public static void XmlPoke(string path, string xpath, string value, params (string prefix, string uri)[] namespaces)
    {
        XmlPoke(path, xpath, value, Encoding.UTF8, namespaces);
    }

    public static void XmlPoke(string path, string xpath, string value, Encoding encoding, params (string prefix, string uri)[] namespaces)
    {
        var document = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var (elements, attributes) = GetObjects(document, xpath, namespaces);
        Assert.True((elements.Count == 1 || attributes.Count == 1) && !(elements.Count == 0 && attributes.Count == 0));

        if (value.Contains('<') || value.Contains('>'))
        {
            elements.SingleOrDefault()?.SetValue("");
            elements.SingleOrDefault()?.RemoveAll();
            elements.SingleOrDefault()?.Add(new XCData(value));
            attributes.SingleOrDefault()?.SetValue(value);
        }
        else
        {
            elements.SingleOrDefault()?.SetValue(value);
            attributes.SingleOrDefault()?.SetValue(value);
        }

        var writerSettings = new XmlWriterSettings { OmitXmlDeclaration = document.Declaration == null, Encoding = encoding };
        using var xmlWriter = XmlWriter.Create(path, writerSettings);
        document.Save(xmlWriter);
    }
    
    private static (IReadOnlyCollection<XElement> Elements, IReadOnlyCollection<XAttribute> Attributes) GetObjects(
        XDocument document,
        string xpath,
        params (string prefix, string uri)[] namespaces)
    {
        XmlNamespaceManager xmlNamespaceManager = null;

        if (namespaces?.Length > 0)
        {
            var reader = document.CreateReader();
            if (reader.NameTable != null)
            {
                xmlNamespaceManager = new XmlNamespaceManager(reader.NameTable);
                foreach (var (prefix, uri) in namespaces)
                    xmlNamespaceManager.AddNamespace(prefix, uri);
            }
        }

        var objects = ((IEnumerable) document.XPathEvaluate(xpath, xmlNamespaceManager)).Cast<XObject>().ToList();
        return (objects.OfType<XElement>().ToList().AsReadOnly(),
            objects.OfType<XAttribute>().ToList().AsReadOnly());
    }

}