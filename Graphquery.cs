

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;

namespace RDR
{
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
    public static class Graphquery
    {
        private static HttpClient client = new HttpClient();


        public static async Task<String> GetData()
        {


            var data = new StringContent("schema{}", Encoding.UTF8, "application/dql");
            // var request = new RestRequest("query").AddBody(body);
            // var response = await _client.PostAsync(request);
            // var result = response.Content;
            var response = await client.PostAsync("https://play.dgraph.io/query", data);
            string result = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Graph response " + result);
            return result;
        }
        public static async Task<List<TypeElement>> GetSchema()
        {
            List<TypeElement> typeList = new List<TypeElement>();
            Dictionary<string, TypeElement> typeMap = new Dictionary<string, TypeElement>();
            
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
                } else { 
                    t.relationCount = 0;
                    typeList.Add(t);
                    typeMap.Add(t.name, t);
                    foreach (Field f in t.fields)
                    {
                        if ((f.type.name == null) && (f.type.ofType.name != "ID")) {
                        f.isRelation = true;
                                t.relationCount++;  
                        } else
                        {
                            Console.WriteLine("predicate not found " + f.name);
                        }
                    }
                    
                }
               
            }
            Console.WriteLine("Graph response " + jsonString);

            return typeList;

        }


    }

}
