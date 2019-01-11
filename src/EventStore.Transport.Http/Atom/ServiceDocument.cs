using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace EventStore.Transport.Http.Atom
{
    public class ServiceDocument : IXmlSerializable
    {
        public List<WorkspaceElement> Workspaces { get; set; }

        public ServiceDocument()
        {
            Workspaces = new List<WorkspaceElement>();
        }

        public void AddWorkspace(WorkspaceElement workspace)
        {
            if (null == workspace) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.workspace); }
            Workspaces.Add(workspace);
        }
 
        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            if (Workspaces.Count == 0)
                ThrowHelper.ThrowSpecificationViolation(ExceptionResource.An_appservice_element_MUST_contain_one_or_more_appworkspace_elements);

            writer.WriteStartElement("service", AtomSpecs.AtomPubV1Namespace);
            writer.WriteAttributeString("xmlns", "atom", null, AtomSpecs.AtomV1Namespace);
            Workspaces.ForEach(w => w.WriteXml(writer));

            writer.WriteEndElement();
        }
    }

    public class WorkspaceElement : IXmlSerializable
    {
        public string Title { get; set; }
        public List<CollectionElement> Collections { get; set; }

        public WorkspaceElement()
        {
            Collections = new List<CollectionElement>();
        }

        public void SetTitle(string title)
        {
            if (null == title) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.title); }
            Title = title;
        }

        public void AddCollection(CollectionElement collection)
        {
            if (null == collection) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection); }
            Collections.Add(collection);
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            if (string.IsNullOrEmpty(Title))
                ThrowHelper.ThrowSpecificationViolation(ExceptionResource.The_appworkspace_element_MUST_contain_one_atomtitle_element);

            writer.WriteStartElement("workspace");

            writer.WriteElementString("atom", "title", AtomSpecs.AtomV1Namespace, Title);
            Collections.ForEach(c => c.WriteXml(writer));

            writer.WriteEndElement();
        }
    }

    public class CollectionElement : IXmlSerializable
    {
        public string Title { get; set; }
        public string Uri { get; set; }

        public List<AcceptElement> Accepts { get; set; }

        public CollectionElement()
        {
            Accepts = new List<AcceptElement>();
        }

        public void SetTitle(string title)
        {
            if (null == title) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.title); }
            Title = title;
        }

        public void SetUri(string uri)
        {
            if (null == uri) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.uri); }
            Uri = uri;
        }

        public void AddAcceptType(string type)
        {
            if (null == type) { ThrowHelper.ThrowArgumentNullException(ExceptionArgument.type); }
            Accepts.Add(new AcceptElement(type));
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            if (string.IsNullOrEmpty(Title))
                ThrowHelper.ThrowSpecificationViolation(ExceptionResource.The_appcollection_element_MUST_contain_one_atomtitle_element);
            if (string.IsNullOrEmpty(Uri))
                ThrowHelper.ThrowSpecificationViolation(ExceptionResource.The_appcollection_element_MUST_contain_an_href_attribute);

            writer.WriteStartElement("collection");
            writer.WriteAttributeString("href", Uri);
            writer.WriteElementString("atom", "title", AtomSpecs.AtomV1Namespace, Title);
            Accepts.ForEach(a => a.WriteXml(writer));

            writer.WriteEndElement();
        }
    }

    public class AcceptElement : IXmlSerializable
    {
        public string Type { get; set; }

        public AcceptElement(string type)
        {
            Type = type;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            if (string.IsNullOrEmpty(Type))
                ThrowHelper.ThrowSpecificationViolation(ExceptionResource.atomaccept_element_MUST_contain_value);
            writer.WriteElementString("accept", Type);
        }
    }
}