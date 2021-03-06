

using System;
using System.Collections.Generic;

// structures to represent instance of graph  : List of Nodes each nodes having attributes and relations

namespace RDR
{
    public class NodeScalarAttribute
    {
        public string name { get; set; }
        public string value { get; set; }
        public  NodeScalarAttribute(String _name, String _value)
        {
            value = _value;
            name = _name;
        }
    }
    public class NodeRelation
    {
        public String predicate { get; set; }
        public Node node { get; set; }
        public NodeRelation(String _name, Node _node)
        {
            predicate = _name;
            node = _node;
        }

    }
    public class Node
    {
        public string type { get; set; }
        public string uid { get; set; }
        public string name { get; set; } // euristic define a name for nodes if possible.
        public List<NodeScalarAttribute> attributes { get; set; }
        public Dictionary<string, List<Node>> edges { get; set; }
        public List<NodeRelation> relations { get; set; }

        public  Node(String _type, String _uid)
        {
            type = _type;
            uid = _uid;
            attributes = new List<NodeScalarAttribute>();
            relations = new List<NodeRelation>();
            edges = new Dictionary<string, List<Node>>();
        }
        public void AddRelation(String predicate, Node n)
        {
            if (!edges.ContainsKey(predicate))
            {
                edges.Add(predicate, new List<Node>());
            }
            edges[predicate].Add(n);

        }

    }
    

}
