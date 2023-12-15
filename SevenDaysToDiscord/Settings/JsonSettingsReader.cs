using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SevenDaysToDiscord.Settings
{
    internal class JsonSettingsReader : ISettingsReader
    {
        private readonly string _configurationFile;

        public JsonSettingsReader(string configurationFile)
        {
            _configurationFile = configurationFile ?? throw new ArgumentNullException(nameof(configurationFile));
        }

        public ISettings<T> Load<T>() where T : class
        {
            return new JsonSettings<T>(_configurationFile);
        }

        public ISettings<T> LoadSection<T>(string sectionName = null) where T : class
        {
            if (string.IsNullOrEmpty(sectionName))
                sectionName = typeof(T).Name;

            return new JsonSettings<T>(_configurationFile, sectionName);
        }

        private class JsonSettings<T> : ISettings<T> where T : class
        {
            private readonly string _file;
            private readonly string _section;

            private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ContractResolver = new JsonSettingsReaderContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            };

            private static readonly JsonSerializer Serializer = JsonSerializer.CreateDefault(JsonSerializerSettings);

            public T Value { get; private set; }

            public JsonSettings(string file, string section = null)
            {
                _file = file ?? throw new ArgumentNullException(nameof(file));
                _section = section;

                Value = ReadValue();
            }

            public void Save()
            {
                var obj = JObject.FromObject(Value, Serializer);

                // No section, overwrite entire file
                if (string.IsNullOrEmpty(_section))
                {
                    File.WriteAllText(_file, obj.ToString());
                    return;
                }

                // Lock and read entire file, overwrite section, and overwrite entire file
                using (var fs = File.Open(_file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                using (var reader = new StreamReader(fs))
                using (var writer = new StreamWriter(fs))
                {
                    var contents = reader.ReadToEnd();

                    var rootObj = !string.IsNullOrEmpty(contents) ? JObject.Parse(contents) : new JObject();
                    rootObj[_section] = obj;

                    fs.SetLength(0);
                    writer.Write(rootObj.ToString());
                }
            }

            private T ReadValue()
            {
                // New instance on empty file
                if (!File.Exists(_file))
                    return Activator.CreateInstance<T>();

                var contents = File.ReadAllText(_file);
                var rootObject = JObject.Parse(contents);

                // Deserialize entire file
                if (string.IsNullOrEmpty(_section))
                    return rootObject.ToObject<T>(Serializer);

                // Handle section
                var sectionObject = rootObject[_section] as JObject;
                var obj = sectionObject?.ToObject<T>(Serializer);

                if (sectionObject == null || obj == null)
                    return Activator.CreateInstance<T>();

                return obj;
            }

            private class JsonSettingsReaderContractResolver : DefaultContractResolver
            {
                protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
                {
                    // Support all public, protected, prviate properties as well as fields (minus compiler generated such as backingfields)
                    var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Select(p => CreateProperty(p, memberSerialization))
                        .Union(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(f => !f.IsDefined(typeof(CompilerGeneratedAttribute), inherit: true))
                            .Select(f => CreateProperty(f, memberSerialization)))
                        .ToList();

                    props.ForEach(p =>
                    {
                        p.Writable = true;
                        p.Readable = true;
                    });

                    return props;
                }
            }
        }
    }
}
