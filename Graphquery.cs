

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Generic;

namespace RDR
{
    public class DQLData
    {
        public Object[] result { get; set; }
    }
    public class DQLResponse
    {
        public DQLData data { get; set; }
    }
    public class GraphQLRequest
    {
        public string query { get; set; }
    }
    public class SchemaElement
    {
        public TypeElement[] types { get; set; }
    }
    public class FieldType
    {
        public string name { get; set; }
        public string kind { get; set; }
        public FieldType ofType { get; set; }
    }
    public class Field
    {
        public string name { get; set; }
        public FieldType type { get; set; }
        public bool isRelation { get; set; }
        public bool isIgnored { get; set; }
        public Predicate predicate { get; set; }

    }
    public class TypeElement
    {
        public string name { get; set; }
        public Field[] fields { get; set; }
        public int relationCount { get; set; }

    }
    public class GraphSchema
    {
        public SchemaElement __schema { get; set; }


    }
    public class SchemaResponse
    {
        public GraphSchema data { get; set; }
    }
    public class PredicateResponse
    {
        public PrediateSchema data { get; set; }
    }
    public class PrediateSchema
    {
        public Predicate[] schema { get; set; }

    }
    public class Predicate
    {
        public string predicate { get; set; }
        public string type { get; set; }
        public bool list { get; set; }
        public bool lang { get; set; }
        public bool index { get; set; }
    }

    public static class Graphquery
    {
        private static HttpClient client = new HttpClient();
        private static Dictionary<string, TypeElement> NodeTypeMap = new Dictionary<string, TypeElement>();
        public static async Task<List<Node>> DQL(String query)
        {

            List<Node> nodeList = new List<Node>();
            var data = new StringContent(query, Encoding.UTF8, "application/dql");
            // var request = new RestRequest("query").AddBody(body);
            // var response = await _client.PostAsync(request);
            // var result = response.Content;
            var response = await client.PostAsync("https://play.dgraph.io/query", data);

            string jsonString = response.Content.ReadAsStringAsync().Result;
            //DQLResponse resp = JsonSerializer.Deserialize<DQLResponse>(jsonString);
            JsonNode resp = JsonNode.Parse(jsonString);
            if (resp["data"] != null)
            {
                foreach (JsonNode o in resp["data"]["result"].AsArray())
                {

                    JsonValue v = o["dgraph.type"].AsArray()[0].AsValue();
                    String uid = o["uid"].AsValue().ToString();
                    String type = v.ToString();
                    var node = nodeFromJson(type, uid, o);


                    nodeList.Add(node);

                }
            }
            return nodeList;
        }
        private static Node nodeFromJson(String type, String uid, JsonNode jsondata)
        {
            var node = new Node(type, uid);
            if (NodeTypeMap.ContainsKey(type))
            {
                TypeElement t = NodeTypeMap[type];
                foreach (var f in t.fields)
                {
                    if ((!f.isRelation) && (!f.isIgnored))
                    {  // scalar
                        var attributeName = f.name;
                        var resultName = f.name;
                        if ((f.predicate != null) && (f.predicate.lang))
                        {
                            resultName += "@en";
                        }
                        JsonNode attribute = jsondata[resultName];
                        if (attribute != null)
                        {
                            String attributeAsString = jsondata[resultName].AsValue().ToString();
                            node.attributes.Add(new NodeScalarAttribute(attributeName, attributeAsString));
                            if (attributeName == "name") // heuristic for node name use field name
                            {
                                node.name = attributeAsString;
                            }
                        }
                    }
                    else
                    { //adding a relation
                        JsonNode child = jsondata[f.name];
                        if (child != null)
                        {
                            JsonArray relatedNodes = child.AsArray();
                            foreach (JsonNode relatedNode in relatedNodes)
                            {
                                if ((relatedNode["dgraph.type"] != null) && (relatedNode["uid"] != null))
                                {
                                    String ruid = relatedNode["uid"].AsValue().ToString();
                                    JsonValue v = relatedNode["dgraph.type"].AsArray()[0].AsValue();
                                    String relatedNodeType = v.ToString();
                                    node.relations.Add(new NodeRelation(f.name, nodeFromJson(relatedNodeType, ruid, relatedNode)));
                                }
                            }
                        }
                    }

                }
                if (node.attributes.Count == 1) { // heuristic 2 : node name is field if unique field
                    node.name = node.attributes[0].value;                         
                }
                if (node.name == null)
                {
                    node.name = node.type + ":" + node.uid;
                }
            }
            return node;

        }
        public static async Task<Dictionary<string, Predicate>> GetPredicateSchema()
        {


            var data = new StringContent("schema{}", Encoding.UTF8, "application/dql");
            // var request = new RestRequest("query").AddBody(body);
            // var response = await _client.PostAsync(request);
            // var result = response.Content;
            var response = await client.PostAsync("https://play.dgraph.io/query", data);

            string jsonString = response.Content.ReadAsStringAsync().Result;
            PredicateResponse resp = JsonSerializer.Deserialize<PredicateResponse>(jsonString);
            Dictionary<string, Predicate> predicateMap = new Dictionary<string, Predicate>();
            foreach (Predicate p in resp.data.schema)
            {
                predicateMap.Add(p.predicate, p);
                Console.WriteLine("Graph predicate " + p.predicate);
            }

            return predicateMap;
        }
        public static async Task<Dictionary<string, TypeElement>> GetSchema()
        {
            Dictionary<string, Predicate> predicateMap = await GetPredicateSchema();
            // List<TypeElement> typeList = new List<TypeElement>();


            var data = new StringContent("{__schema{types { name fields { name type{name kind ofType{name kind} }} }} }", Encoding.UTF8, "application/graphql");
            // var request = new RestRequest("query").AddBody(body);
            // var response = await _client.PostAsync(request);
            // var result = response.Content;
            var response = await client.PostAsync("https://play.dgraph.io/graphql", data);
            string jsonString = response.Content.ReadAsStringAsync().Result;
            SchemaResponse resp = JsonSerializer.Deserialize<SchemaResponse>(jsonString);
            foreach (TypeElement t in resp.data.__schema.types)
            {
                if (t.name.StartsWith("dgraph")
                    || t.name.StartsWith("Query")
                    || t.name.StartsWith("__")
                    || t.name.EndsWith("Payload")
                    || t.name.EndsWith("Filter")
                    || t.name.EndsWith("Orderable")
                    || t.name.EndsWith("Input")
                    || t.name.EndsWith("Result")
                    || t.name.EndsWith("Orderable")
                    || t.name.EndsWith("Order")
                    || t.name.EndsWith("Ref")
                    || t.name.EndsWith("Patch")
                    )
                {
                    // ignore those internal types
                }
                else
                {
                    t.relationCount = 0;
                    //typeList.Add(t);
                    //typeMap.Add(t.name, t);
                    foreach (Field f in t.fields)
                    {
                        if ((f.type.name == null) && (f.type.ofType.name != "ID"))
                        {
                            // workaround for Film DB error
                            if (t.name == "Performance")
                            {
                                if (f.name == "films")
                                {
                                    f.name = "performance.film";

                                }
                                else
                                {
                                    f.name = "performance." + f.name;
                                } 
                            }
                            // end of workaround
                            f.isRelation = true;
                            f.isIgnored = false;
                            t.relationCount++;
                        }
                        else
                        {
                            
                            if (predicateMap.ContainsKey(f.name))
                            {
                                f.predicate = predicateMap[f.name];
                                f.isIgnored = false;
                            }
                            else
                            {
                                f.isIgnored = true;
                                Console.WriteLine("predicate not found " + f.name);
                            }
                        }

                    }
                    NodeTypeMap.Add(t.name, t);


                }

            }
            Console.WriteLine("Graph response " + jsonString);
            //

            return NodeTypeMap;

        }

        public static String BuildQuery(String type)
        {
            String query = "";
            // return a DQL query 
            // implemented :
            // - get first 100 of the given type
            if (NodeTypeMap.ContainsKey(type))
            {
                TypeElement t = NodeTypeMap[type];
                query = $"{{result(func: eq(dgraph.type, \"{type}\"), first: 10) {{ ";
                query += GetScalarFields(type);
                foreach (var f in t.fields) // add all fields
                {
                    if (f.isRelation)
                    {
                        query += $" {f.name} {{";
                        //add all scalar fields 
                        query += GetScalarFields(f.type.ofType.name);
                        query += "}";
                    }
                }
                query += "}}";

            }
            return query;
        }

        private static String GetScalarFields(String type)
        {
            String fields = " dgraph.type uid";
            TypeElement t = NodeTypeMap[type];

            if (t != null)
            {
                

                foreach (var f in t.fields) // add all fields
                {
                    if ((!f.isRelation) && (!f.isIgnored))
                    {
                        if ((f.predicate != null) && (f.predicate.lang == true))
                        {
                            fields += $" {f.name}@en";
                        }
                        else
                        {
                            fields += $" {f.name}";
                        }
                    }

                }
            }
            return fields;
        }

    }
}
