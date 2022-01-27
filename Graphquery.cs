

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using GraphQLParser;
using GraphQLParser.AST;

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
        public string predicateName { get; set; }
        public string type { get; set; }

        public bool isRelation { get; set; }
        public bool isIgnored { get; set; }
        public Predicate predicate { get; set; }

    }
    public class TypeElement
    {
        public string name { get; set; }
        
        public List<Field> fields { get; set; }
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
        public static async Task<JsonNode> BasicDQL(String query)
        {


            var data = new StringContent(query, Encoding.UTF8, "application/dql");
            // var request = new RestRequest("query").AddBody(body);
            // var response = await _client.PostAsync(request);
            // var result = response.Content;
            var response = await client.PostAsync("https://play.dgraph.io/query", data);

            string jsonString = response.Content.ReadAsStringAsync().Result;
            //DQLResponse resp = JsonSerializer.Deserialize<DQLResponse>(jsonString);
            JsonNode resp = JsonNode.Parse(jsonString);
            return resp;
        }
           
        public static async Task<List<Node>> DQL(String query)
        {

            List<Node> nodeList = new List<Node>();
            
            JsonNode resp = await BasicDQL(query);
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

            Console.Write("get predicates ...");
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
                
            }
            Console.WriteLine("Done");
            return predicateMap;
        }
        public static async Task<Dictionary<string, TypeElement>> GetSchema()
        {
            Console.WriteLine("get schema");
            Dictionary<string, Predicate> predicateMap = await GetPredicateSchema();
            // List<TypeElement> typeList = new List<TypeElement>();

            JsonNode resp = await BasicDQL("{ result(func:has(dgraph.graphql.schema), first:1) { dgraph.graphql.schema }}");
          
            if (resp["data"] != null)
            {
                JsonNode info = resp["data"]["result"].AsArray()[0];
                String schema = info["dgraph.graphql.schema"].AsValue().ToString();
                GraphQLDocument doc = Parser.Parse(schema);
                foreach(ASTNode n in doc.Definitions) {
                    if (n.Kind == ASTNodeKind.ObjectTypeDefinition)
                    {

                        var def = n as GraphQLObjectTypeDefinition;
                        var name = def.Name.Value.ToString();
                        TypeElement typeElement = new TypeElement();
                        typeElement.name = name;
                        typeElement.fields = new List<Field>();
                        int relationCount = 0; // fields that are Object

                        foreach(GraphQLFieldDefinition f in def.Fields)
                        {
                            
                            if ((f.Type.Kind == ASTNodeKind.ListType) || (f.Type.Kind == ASTNodeKind.NamedType))
                            {
                                Field field = new Field();

                                String fieldName = f.Name.Value.ToString();
                                field.name = fieldName;
                                if (f.Type.Kind == ASTNodeKind.ListType)
                                {
                                    field.isRelation = true;
                                    relationCount++;
                                    GraphQLListType tt = f.Type as GraphQLListType;
                                    field.type = (tt.Type as GraphQLNamedType).Name.Value.ToString();
                                } else
                                {
                                    field.isRelation = false;
                                    GraphQLNamedType nt = f.Type as GraphQLNamedType;
                                    field.type = nt.Name.Value.ToString();
                                }
                                foreach(GraphQLDirective directive in f.Directives)
                                {
                                    if (directive.Name.Value.ToString() == "dgraph")
                                    {
                                        field.predicateName = (directive.Arguments[0].Value as GraphQLScalarValue).Value.ToString();
                                        if (predicateMap.ContainsKey(field.predicateName))
                                        {
                                            field.predicate = predicateMap[field.predicateName];
                                        }
                                    }
                                }
                                if (field.predicateName == null) { field.predicateName = fieldName; }
                                
                                
                                typeElement.fields.Add(field);

                            }


                        }
                        typeElement.relationCount = relationCount;
                        NodeTypeMap.Add(name, typeElement);
                   
                    }
                }

                
            }

            

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
                        if (f.name != f.predicateName)
                        {
                            query += $" {f.name}:{f.predicateName} {{";
                        } else
                        {
                            query += $" {f.name} {{";
                        }
                        //add all scalar fields 
                        query += GetScalarFields(f.type);
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
                            fields += $" {f.predicateName}@en";
                        }
                        else
                        {
                            if (f.name != f.predicateName)
                            {
                                fields += $" {f.name}:{f.predicateName}"; 
                            }
                            else
                            {
                                fields += $" {f.name}";
                            }
                            
                        }
                    }

                }
            }
            return fields;
        }

    }
}
