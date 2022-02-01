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
        public string name { get; set; }
        public List<AttributeCriteria> facets { get; set; }
        public List<AttributeCriteria> nodecriteria { get; set; }

        public EdgePath(string name)
        {
            facets = new List<AttributeCriteria>();
            nodecriteria = new List<AttributeCriteria>();
            this.name = name;
        }
        public void AddNodeCriteria(string attribute, string type, string function, string value)
        {
            nodecriteria.Add(new AttributeCriteria(attribute, type, function, value));
        }
        public void AddFacet(string attribute, string type, string function, string value)
        {
            facets.Add(new AttributeCriteria(attribute, type, function, value));
        }

    }
    public class Summary
    {
        public string func;
    }
    public class EntityConstraint
    {
        public string entityName { get; set; }
        public List<AttributeCriteria> nodecriteria { get; set; }
        public List<PathConstraint> musthavepath { get; set; }
        public EntityConstraint(string entityName)
        {
            this.entityName = entityName;
            this.nodecriteria = new List<AttributeCriteria>();
            this.musthavepath = new List<PathConstraint>();
        }
        public void AddNodeCriteria(string attribute, string type, string function, string value)
        {
            nodecriteria.Add(new AttributeCriteria(attribute, type, function, value));
        }
        public void AddPathConstraint(PathConstraint path)
        {
            musthavepath.Add(path);
        }
    }
    public class PathConstraint
    {
        public int mincount { get; set; }
        public int maxcount { get; set; }
        public List<EdgePath> edgepath {get; set;}

        public PathConstraint(int min=-1, int max=-1)
        {
            edgepath = new List<EdgePath>();
            this.mincount = min;
            this.maxcount = max;
        }
        public void AddEdge(EdgePath edge)
        {
            edgepath.Add(edge);
        }
       

    }
    public class GroupBy
    {
        public string entity { get; set; }
        public string groupByAttribute { get; set; }

    }
    /* 
     * PathHelper is used to accumulate many PathConstraint in a unique body
     * as PathConstraints may have common path part 
     */
    public class PathHelper
    {
        public Dictionary<string, PathHelper> pathmap;
        public List<AttributeCriteria> facets { get; set; }
        public List<AttributeCriteria> nodecriteria { get; set; }
        public PathHelper()
        {
            facets = new List<AttributeCriteria>();
            nodecriteria = new List<AttributeCriteria>();
            pathmap = new Dictionary<string,PathHelper>();
        }
        public  void SetConstraint(EdgePath constraint)
        {
            
            foreach (AttributeCriteria f in constraint.facets)
            {
                facets.Add(f);
            }
            foreach (AttributeCriteria c in constraint.nodecriteria)
            {
                nodecriteria.Add(c);
            }
            
        }
        public void AddPath(PathConstraint path, bool isGroupby = false)
        {
            if ((path.maxcount == -1) && ((path.mincount == -1) || (path.mincount == 1))) { // not a count condition
                PathHelper current = this;
                foreach( EdgePath e in path.edgepath)
                {
                    PathHelper ph;
                    if (!current.pathmap.ContainsKey(e.name))
                    {
                        ph = new PathHelper();
                        current.pathmap.Add(e.name,ph);
                    } else
                    {
                        ph = current.pathmap[e.name];
                    }
                    ph.SetConstraint(e);
                    current = current.pathmap[e.name];

                }                                                                                      
            }

        }
        public string ToDQL()
        {
            if (pathmap.Count == 0) { 
                return "";
            } else
            {
                string path = "";
                foreach (string h in pathmap.Keys)
                {
                    string filter = "";
                    if (pathmap[h].nodecriteria.Count > 0)
                    {
                        List<string> filters = new List<string>();
                        foreach (AttributeCriteria c in pathmap[h].nodecriteria)
                        {
                            if (c.attributeName == "uid")
                            {
                                filters.Add($"uid(\"{c.value}\")");
                            }
                            else
                            {
                                filters.Add($"{c.func}({c.attributeName},\"{c.value}\")");
                            }
                        }
                        filter = "@filter(" + string.Join(" and ", filters) + " )";
                    }
                    path += h+" "+filter+" { "+pathmap[h].ToDQL()+" }";
                }
                return path;
            }
        }

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
            // add filters
            if (this.entity.nodecriteria.Count > 0) {
                List<string> filters = new List<string>();
                
                foreach (AttributeCriteria item in this.entity.nodecriteria)
                {
                    if (item.attributeName == "uid")
                    {
                        dql = $"{{f0 as var(func:uid(\"{item.value}\")) \n";
                        // uid attribute only supports eq operator
                    }
                    else
                    {
                        filters.Add($"{item.func}({item.attributeName},\"{item.value}\")");
                    }
                }
                if (filters.Count > 0)
                {
                    string filterPart = string.Join(" and ", filters);
                    dql += $"@filter ( {filterPart}) ";
                }
            }
            // add all path 
            PathHelper body = new PathHelper();
            foreach( PathConstraint p in this.entity.musthavepath )
            {
                body.AddPath(p);
            }
            dql += "@cascade { " + body.ToDQL() + " }";
            // create info to be returned for each node 
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
