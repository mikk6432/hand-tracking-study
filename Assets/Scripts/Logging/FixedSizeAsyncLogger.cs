
using System;
using System.IO;
using System.Linq;
using System.Threading;

public class FixedSizeAsyncLogger
{
    struct Value
    {
        public string stringValue;
        public int? intValue;
        public float? floatValue;
    }
    private readonly string[] header;
    private readonly int bufferSize = 100_000;
    private readonly Value[][] buffer;
    private readonly object bufferLock = new();
    private readonly StreamWriter file;
    private readonly object fileLock = new();

    private int _currentRow = 0;

    public FixedSizeAsyncLogger(string[] header, string filename)
    {
        if (String.IsNullOrEmpty(filename))
            throw new ArgumentException($"Filename cannot be null or empty. Please provide a proper file name");

        if (!filename.EndsWith(".csv"))
            filename += ".csv";

        var fullPath = Path.Combine(UnityEngine.Application.persistentDataPath, filename);

        var headerAdded = false;
        bool _fileExists = File.Exists(fullPath);
        if (_fileExists)
        {
            // Since it does, we need to restore the logger's state from it by reading its header
            using (var reader = new StreamReader(fullPath))
            {
                var line = reader.ReadLine(); // We're interested only in a header
                string[] columns;
                // If there is no header (file exists but it's empty) or there are no column names in it, do nothing
                if (!String.IsNullOrEmpty(line) &&
                    (columns = line.Split(',', StringSplitOptions.RemoveEmptyEntries)).Length > 0)
                {
                    for (var i = 0; i < columns.Length; i++)
                    {
                        if (columns[i] != header[i])
                        {
                            throw new ArgumentException($"Header mismatch. Expected: {header[i]}, got: {columns[i]}");
                        }
                    }
                    headerAdded = true;
                }
            }
        }
        else
        {
            var fs = new FileStream(fullPath, FileMode.Create);
            fs.Dispose();
        }
        file = new StreamWriter(fullPath, append: headerAdded, System.Text.Encoding.UTF8);
        this.header = header;
        buffer = new Value[bufferSize][];
        for (var i = 0; i < bufferSize; i++)
        {
            buffer[i] = new Value[header.Length];
            for (var j = 0; j < header.Length; j++)
            {
                var v = new Value
                {
                    floatValue = null,
                    intValue = null,
                    stringValue = null
                };
                buffer[i][j] = v;
            }
        }
        if (!headerAdded)
        {
            file.WriteLine(string.Join(",", header));
        }
    }

    public void SetColumnValue(string name, string value)
    {
        var index = Array.IndexOf(header, name);
        if (index == -1)
            throw new ArgumentException($"Column {name} not found in the header");
        lock (bufferLock)
        {
            buffer[_currentRow][index].stringValue = value;
        }
    }

    public void SetColumnValue(string name, float value)
    {
        var index = Array.IndexOf(header, name);
        if (index == -1)
            throw new ArgumentException($"Column {name} not found in the header");
        lock (bufferLock)
        {
            buffer[_currentRow][index].floatValue = value;
        }
    }

    public void SetColumnValue(string name, int value)
    {
        var index = Array.IndexOf(header, name);
        if (index == -1)
            throw new ArgumentException($"Column {name} not found in the header");
        lock (bufferLock)
        {
            buffer[_currentRow][index].intValue = value;
        }
    }

    public void LogAndClear() // misnomer just imitating original
    {
        lock (bufferLock)
        {
            _currentRow++;
            if (_currentRow == bufferSize)
            {
                throw new InvalidOperationException("Buffer overflow");
            }
        }
    }

    public void ClearBuffer()
    {
        lock (bufferLock)
        {
            for (var i = 0; i < _currentRow + 1; i++)
            {
                for (var j = 0; j < header.Length; j++)
                {
                    buffer[i][j].floatValue = null;
                    buffer[i][j].intValue = null;
                    buffer[i][j].stringValue = null;
                }
            }
            _currentRow = 0;
        }
    }

    public Action DataSavedToDiskCallback;
    public void WriteDataToFile()
    {
        new Thread(() =>
        {
            lock (fileLock)
            {
                for (var i = 0; i < _currentRow; i++)
                {
                    file.WriteLine(string.Join(",", buffer[i].Select(v =>
                    {
                        if (v.intValue.HasValue)
                            return v.intValue.Value.ToString();
                        if (v.floatValue.HasValue)
                            return v.floatValue.Value.ToString();
                        return v.stringValue;
                    })));
                }
                file.Flush();
                ClearBuffer();

                var random = new System.Random();
                int timeToSleep = random.Next(1000, 2000);
                Thread.Sleep(timeToSleep);

                DataSavedToDiskCallback?.Invoke();
            }
        }).Start();
    }

    ~FixedSizeAsyncLogger()
    {
        lock (fileLock)
        {
            file.Close();
        }
    }
}