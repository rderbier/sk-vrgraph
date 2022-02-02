using System;
using System.Collections.Generic;
using GraphQLParser;
using GraphQLParser.AST;
using System.Text.Json;
// structures used to parse dgraph data : schema and query

namespace RDR.Dgraph
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
    public enum NodeNature
    {
        Thing,
        Characteristic,
        Association
    }
    public class TypeElement
    {
       
        public string name { get; set; }

        public List<Field> fields { get; set; }
        public int relationCount { get; set; }
        public NodeNature nature { get; set; }
        public TypeElement(string name)
        {
            this.name = name;
            this.fields = new List<Field>();
        }
        public String GetDQLScalarFields()
        {
            String fields = " dgraph.type uid";


            foreach (var f in this.fields) // add all fields
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

            return fields;
        }
        public string GetDQLRelationInfo(Dictionary<string, TypeElement> NodeTypeMap)
        {
            string query = "";
            foreach (var f in this.fields) // add all fields
            {
                if (f.isRelation)
                {
                    if (f.name != f.predicateName)
                    {
                        query += $" {f.name}:{f.predicateName} {{";
                    }
                    else
                    {
                        query += $" {f.name} {{";
                    }
                    //add all scalar fields 
                    if (NodeTypeMap.ContainsKey(f.type))
                    {
                        query += NodeTypeMap[f.type].GetDQLScalarFields();
                    }
                    query += "}";
                }
            }
            return query;

        }
        public static Dictionary<string, TypeElement> parseSchema(string schema, Dictionary<string, Predicate> predicateMap)
        {
            Dictionary<string, TypeElement> NodeTypeMap = new Dictionary<string, TypeElement>();
            GraphQLDocument doc = Parser.Parse(schema);
            foreach (ASTNode n in doc.Definitions)
            {
                if (n.Kind == ASTNodeKind.ObjectTypeDefinition)
                {

                    var def = n as GraphQLObjectTypeDefinition;
                    var name = def.Name.Value.ToString();
                    TypeElement typeElement = new TypeElement(name);
                    
                    int relationCount = 0; // fields that are Object
                    int reverseCount = 0;
                    int scalarCount = 0; // fields that are scalar, excluding uid
                    foreach (GraphQLFieldDefinition f in def.Fields)
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
                            }
                            else
                            {
                                field.isRelation = false;
                                GraphQLNamedType nt = f.Type as GraphQLNamedType;
                                field.type = nt.Name.Value.ToString();
                                scalarCount += 1;
                            }
                            foreach (GraphQLDirective directive in f.Directives)
                            {
                                if (directive.Name.Value.ToString() == "dgraph")
                                {
                                    field.predicateName = (directive.Arguments[0].Value as GraphQLScalarValue).Value.ToString();
                                    if (field.predicateName.StartsWith("~") ){
                                        reverseCount += 1;
                                    }
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
                    if ((relationCount >0) && (reverseCount == relationCount))
                    {
                        typeElement.nature = NodeNature.Characteristic;
                    } else if (scalarCount == 0) 
                    {
                        typeElement.nature = NodeNature.Association;
                    } else
                    {
                        typeElement.nature = NodeNature.Thing;
                    }
                    typeElement.relationCount = relationCount;
                    NodeTypeMap.Add(name, typeElement);

                }
            }
            return NodeTypeMap;
        }

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
        public bool reverse { get; set; }
        public static Dictionary<string, Predicate> GetPredicateMap(string jsonString)
        {
            PredicateResponse resp = JsonSerializer.Deserialize<PredicateResponse>(jsonString);
            Dictionary<string, Predicate> predicateMap = new Dictionary<string, Predicate>();
            foreach (Predicate p in resp.data.schema)
            {
                predicateMap.Add(p.predicate, p);

            }
            return predicateMap;
        }
    }

}
