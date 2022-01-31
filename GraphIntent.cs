using System;
using System.Collections.Generic;
using System.Text;
using RDR.Dgraph;

// Intent is abstract way to describe a query in a graph oriented knowledge base
// 
namespace RDR
{
    public class AttributeCriteria
    {
        public string attributeName { get; set; } // attribute to be evaluated
        public string type { get; set; }
        public string func { get; set; } // binary test function
        public string value { get; set; } // value to test
        public AttributeCriteria (string attribute, string type, string function, string value)
        {
            this.func = function;
            this.attributeName = attribute;
            this.value = value;
            this.type = type;
        }
    }
    public class EdgePath
    {

    }
    public class Summary
    {
        public string func;
    }
    public class EntityConstraint
    {
        public string entityName { get; set; }
        public List<AttributeCriteria> filters { get; set; }
        public List<PathConstraint> musthavepath { get; set; }
        public EntityConstraint(string entityName)
        {
            this.entityName = entityName;
            this.filters = new List<AttributeCriteria>();
            this.musthavepath = new List<PathConstraint>();
        }
        public void addFilter(string attribute, string type, string function, string value)
        {
            filters.Add(new AttributeCriteria(attribute, type, function, value));
        }
    }
    public class PathConstraint
    {
        public int mincount { get; set; }
        public int maxcount { get; set; }
        public EntityConstraint entity { get; set; }
        public List<EdgePath> edgepath {get; set;}
       

    }
    public class GroupBy
    {
        public string entity { get; set; }
        public string groupByAttribute { get; set; }

    }
    public class GraphIntent
    {
        public string domain { get; set; } // identify which knowledge base we are querying
        public EntityConstraint entity { get; set; }

        public int first;
        public int offset;

        public GraphIntent (string domain, string entityName, int first = 25, int offset = 0)
        {
            this.domain = domain;
            this.entity = new EntityConstraint(entityName);
            this.offset = offset;
            this.first = first;
        }
        public string ToDQL(Dictionary<string, TypeElement> NodeTypeMap )
        {
           string dql = $"{{f0 as var(func:eq(dgraph.type,\"{this.entity.entityName}\")) \n";
           
           string info = "";
            if (NodeTypeMap.ContainsKey(this.entity.entityName))
            {
                info = NodeTypeMap[this.entity.entityName].GetDQLScalarFields();
                info += NodeTypeMap[this.entity.entityName].GetDQLRelationInfo(NodeTypeMap);
            }

            dql += $" stat(func:uid(f0)) {{ count(uid) }} \n list(func:uid(f0), first:{this.first}, offset:{this.offset}) {{ {info} }}";
            dql += "\n}";
            return dql;

        }

    }
}
