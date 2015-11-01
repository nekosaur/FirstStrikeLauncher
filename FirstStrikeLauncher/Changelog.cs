using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace FirstStrikeLauncher
{
    public static class Changelog
    {
        public static TreeNode[] GetPublicLog(string url)
        {
            List<TreeNode> nodes = new List<TreeNode>();

            List<string> log = new List<string>();

            using (WebClient client = new WebClient())
            {
                try
                {
                    string file = client.DownloadString(new Uri(url));
                    log.AddRange(file.Split('\n'));
                }
                catch (WebException)
                {
                    return new List<TreeNode>() { new TreeNode("ERROR: Could not find changelog") }.ToArray();
                }

                int topNode = -1;
                int categoryNode = -1;

                foreach (string line in log)
                {
                    TreeNode node = new TreeNode();

                    if (line.StartsWith("+"))
                    {
                        node.Text = line.Substring(1);

                        nodes.Add(node);

                        topNode++;
                        categoryNode = -1;
                    }
                    else if (line.StartsWith("."))
                    {
                        node.Text = line.Substring(1);

                        nodes[topNode].Nodes.Add(node);

                        categoryNode++;
                    }
                    else if (line.StartsWith("-"))
                    {
                        node.Text = line.Substring(1);

                        nodes[topNode].Nodes[categoryNode].Nodes.Add(node);
                    }
                }
            }

            return nodes.ToArray();
        }

        public static TreeNode[] GetTesterLog(int start, int count)
        {
            return new List<TreeNode>() { new TreeNode("ERROR: Could not find changelog") }.ToArray();
        }
    }
}
