

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using GraphQLParser;
using GraphQLParser.AST;
using RDR.Dgraph;

namespace RDR
{
   

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
                foreach (JsonNode o in resp["data"]["list"].AsArray())
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
            return Predicate.GetPredicateMap(jsonString);
            
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
                NodeTypeMap = TypeElement.parseSchema(schema, predicateMap);
            }

            

            return NodeTypeMap;

        }
        public static String BuildQuery(String type, int page, int pageSize)
        {
            GraphIntent intent = new GraphIntent("Demo", type, 4 * pageSize, page * pageSize);
            return intent.ToDQL(NodeTypeMap);
        }
        public static String BuildQueryOld(String type, int page, int pageSize)
        {
            String query = "";
            // return a DQL query 
            // implemented :
            // - get first 100 of the given type
            int offset = page * pageSize;
            int first = 4 * pageSize; // we read 4 pages 
            if (NodeTypeMap.ContainsKey(type))
            {
                TypeElement t = NodeTypeMap[type];
                query = $"{{list(func: eq(dgraph.type, \"{type}\"), first: {first}, offset: {offset}) {{ ";
                query += t.GetDQLScalarFields();
                query += t.GetDQLRelationInfo(NodeTypeMap);
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
