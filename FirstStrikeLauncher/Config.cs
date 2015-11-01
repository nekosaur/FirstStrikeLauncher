using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using LicenseCommon;

namespace FirstStrikeLauncher
{
    [Serializable]
    public class Config
    {
        [XmlElement]
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        private string _name;

        [XmlElement]
        public string ParentPath
        {
            get { return _parentPath; }
            set { _parentPath = value; }
        }

        private string _parentPath;

        [XmlElement]
        public List<string> UpdatesUrl
        {
            get { return _updatesUrl; }
            set { _updatesUrl = value; }
        }
        private List<string> _updatesUrl;

        [XmlElement]
        public string ApplicationUpdatesFeed
        {
            get { return _applicationUpdatesFeed; }
            set { _applicationUpdatesFeed = value; }
        }
        private string _applicationUpdatesFeed;

        [XmlElement]
        public bool UpdateBeforeLaunch
        {
            get { return _updateBeforeLaunch; }
            set { _updateBeforeLaunch = value; }
        }
        private bool _updateBeforeLaunch;

        [XmlElement]
        public SerializableDictionary<string, bool> Options
        {
            get { return _options; }
            set { _options = value; }
        }

        private SerializableDictionary<string, bool> _options;

        [XmlElement]
        public SerializableDictionary<string, string> Arguments
        {
            get { return _arguments; }
            set { _arguments = value; }
        }
        private SerializableDictionary<string, string> _arguments;

        public List<string> Resolutions
        {
            get { return _resolutions; }
            set { _resolutions = value; }
        }
        [XmlIgnore]
        private List<string> _resolutions = new List<string>();

        public Config()
        {
            Options = new SerializableDictionary<string, bool>();
            Arguments = new SerializableDictionary<string, string>();
        }

        public static Config Deserialize(string filePath)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(Config));

            using (TextReader reader = new StreamReader(filePath))
            {
                return (Config)deserializer.Deserialize(reader);
            }
        }

        public static Config DeserializeFromString(string xml)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(Config));

            Config config;

            using (TextReader reader = new StringReader(xml))
            {
                config = (Config)deserializer.Deserialize(reader);
            }

            deserializer = null;

            return config;
            
        }

        public void Serialize(string filePath)
        {
            try
            {

                XmlSerializer serializer = new XmlSerializer(typeof(Config));

                using (TextWriter writer = new StreamWriter(filePath))
                {

                    serializer.Serialize(writer, this);


                }

                serializer = null;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

