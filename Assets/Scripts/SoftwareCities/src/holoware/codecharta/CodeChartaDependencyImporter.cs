using System.IO;
using UnityEngine;

namespace SoftwareCities.src.holoware.codecharta
{
    public class CodeChartaDependencyImporter
    {
        public static void TransformEdges(string fileName)
        {
            using (StreamReader reader = new StreamReader(File.OpenRead(fileName)))
            {
                string jsonString = reader.ReadToEnd();
                CodeChartaEdges codeChartaEdges =
                    JsonUtility.FromJson<CodeChartaEdges>(jsonString);
                string outputFileName = Path.GetDirectoryName(fileName) + Path.DirectorySeparatorChar +
                                        "code-charta-edges.dot";
                Debug.Log("Transforming code charta edges to: " + outputFileName);
                // Create a file to write to.
                string header = "digraph \"code-charta-edges-converted\" { \n";
                File.WriteAllText(outputFileName, header);
                foreach (CodeChartaEdges.Edge edge in codeChartaEdges.edges)
                {
                    string dependencyLine =
                        $"\"{GetClassName(edge.fromNodeName)}\" -> \"{GetClassName(edge.toNodeName)}\";\n";
                    File.AppendAllText(outputFileName, dependencyLine);
                }

                File.AppendAllText(outputFileName, "}");
            }
        }

        private static string GetClassName(string fileName)
        {
            string dotBasedClassName = fileName.Replace('/', '.').Substring(1);
            return dotBasedClassName.Remove(dotBasedClassName.LastIndexOf('.'));
        }
    }
}