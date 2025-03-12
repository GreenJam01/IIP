using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticSearch
{
    public class Node
    {
        public string Title { get; set; } 

        public Node( string name)
        {
            Title = name;
        }
    }

    public class Relation
    {
        public string FromNode { get; set; } 
        public string ToNode { get; set; } 
        public string RelationType { get; set; } 

        public Relation(string from, string to, string relationType)
        {
            FromNode = from;
            ToNode = to;
            RelationType = relationType;
        }
    }

    public class Graph
    {
        public Dictionary<string, Node> Nodes { get; set; } 
        public List<Relation> Relations { get; set; } 

        public Graph()
        {
            Nodes = new Dictionary<string, Node>();
            Relations = new List<Relation>();
        }

        public void AddNode(Node node)
        {
            if (!Nodes.ContainsKey(node.Title))
            {
                Nodes[node.Title] = node;
            }
        }
        public void AddRelationship(Relation relationship)
        {
            Relations.Add(relationship);
        }

        public List<Relation> GetRelationshipsForCharacter(string title)
        {
            return Relations
                .Where(r => r.FromNode == title)
                .ToList();
        }

        public static Graph ParseGraphFromFile(string filePath)
        {
            var graph = new Graph();
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { "->", ":" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3)
                    continue;

                var fromCharacterName = parts[0].Trim();
                var toCharacterName = parts[1].Trim();
                var relationType = parts[2].Trim();

                var fromCharacterId = fromCharacterName.ToLower().Replace(" ", "_");
                var toCharacterId = toCharacterName.ToLower().Replace(" ", "_");


                graph.AddNode(new Node(fromCharacterName));
                graph.AddNode(new Node(toCharacterName));
                graph.AddRelationship(new Relation(fromCharacterId, toCharacterId, relationType));
            }
            return graph;
        }
    }
}
