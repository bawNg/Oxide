﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using Oxide.Core.Configuration;

namespace Oxide.Core
{
    /// <summary>
    /// Manages all data files
    /// </summary>
    public class DataFileSystem
    {
        /// <summary>
        /// Gets the directory that this system works in
        /// </summary>
        public string Directory { get; private set; }

        // All currently loaded datafiles
        private readonly Dictionary<string, DynamicConfigFile> _datafiles;

        private readonly JsonSerializerSettings _settings;

        /// <summary>
        /// Initializes a new instance of the DataFileSystem class
        /// </summary>
        /// <param name="directory"></param>
        public DataFileSystem(string directory)
        {
            Directory = directory;
            _datafiles = new Dictionary<string, DynamicConfigFile>();
            var converter = new KeyValuesConverter();
            _settings = new JsonSerializerSettings();
            _settings.Converters.Add(converter);
        }

        /// <summary>
        /// Makes the specified name safe for use in a filename
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string SanitiseName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            name = name.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            name = Regex.Replace(name, @"[:,]", "_");
            name = Regex.Replace(name, @"\.+", @"\.");
            return name.TrimStart('.', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Check if datafile exists
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ExistsDatafile(string name)
        {
            name = SanitiseName(name);
            DynamicConfigFile datafile;
            if (!_datafiles.TryGetValue(name, out datafile))
            {
                datafile = new DynamicConfigFile(Path.Combine(Directory, string.Format("{0}.json", name)));
                _datafiles.Add(name, datafile);
            }
            return datafile.Exists();
        }

        /// <summary>
        /// Gets a datafile
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DynamicConfigFile GetDatafile(string name)
        {
            name = SanitiseName(name);
            // See if it already exists
            DynamicConfigFile datafile;
            if (!_datafiles.TryGetValue(name, out datafile))
            {
                datafile = new DynamicConfigFile(Path.Combine(Directory, string.Format("{0}.json", name)));
                _datafiles.Add(name, datafile);
            }

            // Does it exist?
            if (datafile.Exists())
            {
                // Load it
                datafile.Load();
            }
            else
            {
                // Just make a new one
                datafile.Save();
            }

            return datafile;
        }

        /// <summary>
        /// Saves the specified datafile
        /// </summary>
        /// <param name="name"></param>
        public void SaveDatafile(string name)
        {
            name = SanitiseName(name);
            // Get the datafile
            DynamicConfigFile datafile;
            if (!_datafiles.TryGetValue(name, out datafile)) return;
            // Save it
            datafile.Save();
        }

        public T ReadObject<T>(string name)
        {
            if (ExistsDatafile(name))
                return GetDatafile(name).ReadObject<T>();
            var instance = Activator.CreateInstance<T>();
            WriteObject(name, instance);
            return instance;
        }

        public void WriteObject<T>(string name, T Object, bool sync = false)
        {
            GetDatafile(name).WriteObject(Object, sync);
        }
    }
}
