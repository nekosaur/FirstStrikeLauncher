using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Net;
using System.IO;
using FirstStrikeLauncher.Properties;

namespace FirstStrikeLauncher
{
    public class RSSItem
    {
        public string Title
        {
            get { return _title; }
            set { _title = value; }
        }
        private string _title;

        public string Link
        {
            get { return _link; }
            set { _link = value; }
        }
        private string _link;

        public string Date
        {
            get { return _date; }
            set { _date = value; }
        }
        private string _date;

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }
        private string _description;

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("<b><a href='");
            builder.Append(_link);
            builder.Append("' target='_top'>");
            builder.Append(_title);
            builder.Append("</a></b><br><small>");
            builder.Append(_date);
            builder.Append("</small><br><br>");

            return builder.ToString();
        }

    }

    public class RSS
    {
        private XmlDocument feed = new XmlDocument();

        public List<RSSItem> Items
        {
            get { return _rssItems; }
        }
        private List<RSSItem> _rssItems = new List<RSSItem>();

        public RSS(string url, int count)
        {
            Load(url, count);
        }

        public void Load(string url, int count)
        {
            string xml;

            using (WebClient client = new WebClient())
            {
                xml = client.DownloadString(new Uri(url));
            }

            feed.LoadXml(xml);

            XmlNodeList list = feed.SelectNodes("rss/channel/item");

            StringBuilder builder = new StringBuilder();

            int counter = 0;

            foreach (XmlNode node in list)
            {
                counter++;

                if (counter >= 5)
                    break;

                RSSItem item = new RSSItem();

                XmlNode subNode = node.SelectSingleNode("title");
                item.Title = subNode != null ? subNode.InnerText : "";
                subNode = node.SelectSingleNode("description");
                item.Description = subNode != null ? subNode.InnerText : "";
                subNode = node.SelectSingleNode("pubDate");
                item.Date = subNode != null ? subNode.InnerText : "";
                subNode = node.SelectSingleNode("link");
                item.Link = subNode != null ? subNode.InnerText : "";

                _rssItems.Add(item);
            }
        }

        public string GetHtml(int items)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < items; i++)
            {
                builder.Append(_rssItems[i].ToString());
            }

            return builder.ToString();
        }

        public string GetHtml()
        {
            StringBuilder builder = new StringBuilder();

            foreach (RSSItem item in _rssItems)
            {
                builder.Append(item.ToString());
            }

            return builder.ToString();
        }

    }
}
