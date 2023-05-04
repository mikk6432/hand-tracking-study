using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Collections;
using System.Globalization;
using System.Threading;
using System.Linq;
using UnityEngine;

namespace Logging
{
    public class AsyncHighFrequencyCSVLogger
    {
        public interface Row
        {
            void SetColumnValue<T>(string name, T value) where T : struct;
            void SetColumnValue(string name, string value);
            object GetColumnValue(string name);
            void LogAndClear();
        }

        public Row CurrentRow { get; private set; }

        private readonly List<string> _header = new(); // Is used to check the column names after the logger has been initialised
        private bool _headerHasBeenAdded;
        private readonly OrderedDictionary _templateRow = new();
        private readonly Queue<OrderedDictionary> _data = new();
        private readonly object _dataLock = new();

        private readonly StreamWriter _file;
        private readonly object _fileLock = new();

        private bool
            _initialised; // Initialisation implies that the user has specified all the columns which they will fill with data later

        private class DataRow : Row
        {
            private readonly AsyncHighFrequencyCSVLogger _logger;

            private OrderedDictionary _currentRow;

            public DataRow(AsyncHighFrequencyCSVLogger logger) => _logger = logger;

            public void SetColumnValue<T>(string name, T value) where T : struct
            {
                if (!_logger._initialised)
                    throw new InvalidOperationException(
                        $"The value of any column cannot be set until {nameof(AsyncHighFrequencyCSVLogger)} has been initialised");
                if (!value.GetType().IsPrimitive)
                    throw new ArgumentException(
                        $"{nameof(AsyncHighFrequencyCSVLogger)} does support only primitive data types as column values");
                if (!_currentRow.Contains(name))
                    throw new ArgumentException($"There is no column with the name \"{name}\"");

                _currentRow[name] = value;
            }

            public void SetColumnValue(string name, string value)
            {
                if (!_logger._initialised)
                    throw new InvalidOperationException(
                        $"The value of any column cannot be set until {nameof(AsyncHighFrequencyCSVLogger)} has been initialised");
                if (!_currentRow.Contains(name))
                    throw new ArgumentException($"There is no column with the name \"{name}\"");

                _currentRow[name] = value;
            }

            public object GetColumnValue(string name)
            {
                if (!_logger._initialised)
                    throw new InvalidOperationException(
                        $"The value of a column cannot be accessed until {nameof(AsyncHighFrequencyCSVLogger)} has been initialised");
                if (!_currentRow.Contains(name))
                    throw new ArgumentException($"There is no column with the name \"{name}\"");

                return _currentRow[name];
            }

            public void LogAndClear()
            {
                if (!_logger._initialised)
                    throw new InvalidOperationException(
                        $"The current row cannot be logged until {nameof(AsyncHighFrequencyCSVLogger)} has been initialised");

                // Put the current row into _data of the outer class
                lock (_logger._dataLock)
                    _logger._data.Enqueue(_currentRow);
                // Create a new row by template
                Initialise();
            }

            public void Initialise() => _currentRow = (OrderedDictionary)DeepCopy(_logger._templateRow);

            // The DeepCopy method is taken from https://stackoverflow.com/questions/18873152/deep-copy-of-ordereddictionary as is
            private object DeepCopy(object original)
            {
                // Construct a temporary memory stream
                MemoryStream stream = new MemoryStream();

                // Construct a serialization formatter that does all the hard work
                var formatter = new BinaryFormatter
                {
                    Context = new StreamingContext(StreamingContextStates.Clone)
                };

                // Serialize the object graph into the memory stream
                formatter.Serialize(stream, original);

                // Seek back to the start of the memory stream before deserializing
                stream.Position = 0;

                // Deserialize the graph into a new set of objects
                // and return the root of the graph (deep copy) to the caller
                return (formatter.Deserialize(stream));
            }
        }

        public AsyncHighFrequencyCSVLogger(string filename)
        {
            if (String.IsNullOrEmpty(filename))
                throw new ArgumentException($"Filename cannot be null or empty. Please provide a proper file name");

            // Init the current row
            CurrentRow = new DataRow(this);

            // Store a full path to the file which will be associated with this logger
            if (!filename.EndsWith(".csv"))
                filename += ".csv";
            var fullPath = Path.Combine(UnityEngine.Application.persistentDataPath, filename);

            // Let's check whether the file with the specified name already exists
            bool _fileExists = File.Exists(fullPath);
            if (_fileExists)
            {
                // Since it does, we need to restore the logger's state from it by reading its header
                using (var reader = new StreamReader(fullPath))
                {
                    var header = reader.ReadLine(); // We're interested only in a header
                    string[] columns;
                    // If there is no header (file exists but it's empty) or there are no column names in it, do nothing
                    if (!String.IsNullOrEmpty(header) &&
                        (columns = header.Split(',', StringSplitOptions.RemoveEmptyEntries)).Length > 0)
                    {
                        // If there are column names in the header, recreate _header and _templateRow out of it...
                        foreach (var column in columns)
                        {
                            _header.Add(RemoveTrailingQuotesIfAny(column));
                            _templateRow.Add(RemoveTrailingQuotesIfAny(column), string.Empty);
                        }

                        _headerHasBeenAdded = true;

                        // ...and init the logger
                        Initialise();
                    }
                }
            }
            else
            {
                var fs = new FileStream(fullPath, FileMode.Create);
                fs.Dispose();
            }

            // If we have successfully initialised the logger, open the file in the 'Append' mode, otherwise create file
            _file = new StreamWriter(fullPath, append: _headerHasBeenAdded, System.Text.Encoding.UTF8);
        }

        public void AddColumn(string name)
        {
            if (_initialised)
                throw new InvalidOperationException(
                    $"Changes cannot be made to {nameof(AsyncHighFrequencyCSVLogger)} after it has been initialised");
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("The name of a column cannot be null or empty");
            if (_header.Contains(name))
                throw new Exception($"The column with the name \"{name}\" already exists");

            _header.Add(name);
            _templateRow.Add(name, string.Empty);
        }

        public void AddColumns(string[] names)
        {
            if (_initialised)
                throw new InvalidOperationException(
                    $"Changes cannot be made to {nameof(AsyncHighFrequencyCSVLogger)} after it has been initialised");
            foreach (string name in names)
            {
                if (string.IsNullOrEmpty(name))
                    throw new ArgumentException("One of the values of the array is null or empty which is not allowed");
                if (_header.Contains(name))
                    throw new Exception(
                        $"The array passed as the method's argument contains the name which already exists in the current {nameof(AsyncHighFrequencyCSVLogger)}. The operation is aborted");
            }

            foreach (string name in names)
            {
                _header.Add(name);
                _templateRow.Add(name, string.Empty);
            }
        }

        public bool DoesColumnExist(string name) => _header.Contains(name);

        public void Initialise()
        {
            if (_initialised)
                throw new InvalidOperationException(
                    $"The {nameof(AsyncHighFrequencyCSVLogger)} has already been initialised");
            if (_header.Count == 0)
                throw new InvalidOperationException(
                    $"{nameof(AsyncHighFrequencyCSVLogger)} without columns cannot be initialised. Please add at least one column first");

            // Init the current row...
            var currentRow = (DataRow)CurrentRow;
            currentRow.Initialise();

            // ...and rise the flag
            _initialised = true;
        }

        public bool HasBeenInitialised() => _initialised;

        public bool HasUnsavedData()
        {
            if (!_initialised) return false;

            bool res;
            lock (_dataLock)
                res = _data.Count > 0;
            return res;
        }

        public Action DataSavedToDiskCallback;
        public void SaveDataToDisk()
        {
            if (!_initialised)
                throw new InvalidOperationException(
                    $"Data of uninitialised {nameof(AsyncHighFrequencyCSVLogger)} cannot be saved");
            if (!HasUnsavedData()) return;

            var thread = new Thread(WriteDataToFile);
            thread.Start();
        }

        private void WriteDataToFile()
        {
            lock (_fileLock)
            {
                // Even if we just created the file, we still write the header only once
                if (!_headerHasBeenAdded)
                {
                    _file.WriteLine(ToString(_header));
                    _headerHasBeenAdded = true;
                }

                // Dump all the data line by line
                lock (_dataLock)
                    while (_data.TryDequeue(out OrderedDictionary row))
                        _file.WriteLine(ToString(row.Values));
                // Store it to the disk
                _file.Flush();

                var random = new System.Random();
                int timeToSleep = random.Next(1000, 2000);
                Debug.Log($"Imitating thread sleep {timeToSleep}ms inside logger");
                Thread.Sleep(timeToSleep);
                
                DataSavedToDiskCallback?.Invoke();
            }
        }

        public bool ClearUnsavedData()
        {
            if (!HasUnsavedData()) return false;

            lock (_dataLock)
                _data.Clear();
            return true;
        }

        private static string ToString(IEnumerable collection)
        {
            string res = String.Empty;

            foreach (var item in collection)
                res += ToString(item) + ",";

            // We don't need the last comma
            return res.Remove(res.Length - 1);
        }

        private static string ToString(object value)
        {
            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.String:
                    var s = (string)value;
                    // Wrap the string in quotes, if there are spaces or commas inside
                    if (s.Contains(' ') || s.Contains(',')) // TODO: we don't deal with quote symbols yet
                        return "\"" + value + "\"";
                    else
                        return value.ToString();
                case TypeCode.Single:
                    var f = (float)value;
                    return
                        f.ToString(CultureInfo.InvariantCulture); // We want to use a dot as a decimal delimiter always
                case TypeCode.Double:
                    var d = (double)value;
                    return d.ToString(CultureInfo.InvariantCulture);
                case TypeCode.Decimal:
                    var de = (decimal)value;
                    return de.ToString(CultureInfo.InvariantCulture);
                default:
                    return value.ToString();
            }
        }

        private string RemoveTrailingQuotesIfAny(string value)
        {
            if (value.First() == '"' && value.Last() == '"')
                return value.Substring(1, value.Length - 2);
            else
                return value;
        }

        ~AsyncHighFrequencyCSVLogger()
        {
            lock (_fileLock)
            {
                _file.Close(); // The Dispose method is called inside automatically
            }
        }
    }
}