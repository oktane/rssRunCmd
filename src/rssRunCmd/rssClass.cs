using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Net;
using System.Xml;


namespace RSS
{
    public class RssChannel
    {
        public string Title { get; set; }
        public System.Uri Link { get; set; }
        public string Description { get; set; }
        public RssDocument Rss { get; set; }

        public RssChannel()
        {
            this.Rss = new RssDocument();
        }
        public static RssChannel Read(System.Uri url)
        {
            WebResponse response = null;
            WebRequest request = null;
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                request = WebRequest.Create(url.AbsoluteUri);
                response = request.GetResponse();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to fetch feed. Check Feed URL in INI.");
                Console.WriteLine(e.Message);
            }

            try
            {
                xmlDoc.Load(response.GetResponseStream());
                XmlElement rssElement = xmlDoc["rss"];
                if (rssElement == null)
                {
                    return null;
                }

                XmlElement channelElement = rssElement["channel"];

                if (channelElement != null)
                {
                    // Create the channel and set attributes
                    RssChannel rssChannel = new RssChannel();
                    rssChannel.Title = channelElement["title"].InnerText;
                    rssChannel.Link = new Uri(channelElement["link"].InnerText);
                    rssChannel.Description = channelElement["description"].InnerText;

                    // Read the content
                    XmlNodeList itemElements =
                  channelElement.GetElementsByTagName("item");

                    foreach (XmlElement itemElement in itemElements)
                    {
                        Item item = new Item()
                        {
                            Title = itemElement["title"].InnerText,
                            Link = itemElement["link"].InnerText,
                            Description = itemElement["description"].InnerText,
                            PubDate = itemElement["pubDate"].InnerText
                        };
                        rssChannel.Rss.Items.Add(item);
                    }
                    return rssChannel;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }

    public class RssDocument
    {
        public List<Item> Items { get; set; }

        public RssDocument()
        {
            this.Items = new List<Item>();
        }
    }

    public class Item
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public string PubDate { get; set; }
    }


}

