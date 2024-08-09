using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GameplayServices.Common
{
    public class FileHelper
    {
        private readonly string _directoryPath;
        private readonly string _directoryBadPath;
        private readonly List<string> _loadedFiles = new();
        
        private readonly string _extensionFilter = "*.json";

        public FileHelper(string directory)
        {
            _directoryPath = Path.Combine(Application.persistentDataPath, directory);
            _directoryBadPath = Path.Combine(Application.persistentDataPath, $"{directory}_bad");
            if (!Directory.Exists(_directoryPath)) Directory.CreateDirectory(_directoryPath);
            if (!Directory.Exists(_directoryBadPath)) Directory.CreateDirectory(_directoryBadPath);
        }
        
        public void SaveToFile<T>(T data)
        {
            var filePath = Path.Combine(_directoryPath, $"{Guid.NewGuid()}_{DateTime.Now:yyyy-MM-ddTHH:mm:ss}.json");
            var json = JsonUtility.ToJson(data);
            File.WriteAllText(filePath, json);
        }
        
        public bool IsEmpty => Directory.GetFiles(_directoryPath,_extensionFilter).Length == 0;
        
        public List<T> LoadFromFiles<T>(int sizeInBytes)
        {
            var allFiles = Directory.GetFiles(_directoryPath, _extensionFilter);
            
                // by necessity
                // .Select(file => new FileInfo(file))
                // .OrderBy(fileInfo => fileInfo.CreationTime)
                // .ToArray();
                
            if(allFiles.Length == 0) return default;
            
            var list = new List<T>();
            var totalFileSize = 0;
            
            foreach (var file in allFiles)
            {
                try
                {
                    list.Add(JsonUtility.FromJson<T>(File.ReadAllText(file)));
                    totalFileSize += GetFileSize(file);
                    if (totalFileSize >= sizeInBytes) break;
                    _loadedFiles.Add(file);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Error processing the file {file}: {ex.Message}");
                    CopyBadFiles(file);
                }
            }
            return list;
        }
        
        public void DeleteLoadedFiles()
        {
            _loadedFiles.ForEach(File.Delete);
            _loadedFiles.Clear();
        }
        private int GetFileSize(string filePath) => (int)(new FileInfo(filePath)).Length;
        
        private void CopyBadFiles(string filePath)
        {
            File.Copy(filePath, Path.Combine(_directoryBadPath, Path.GetFileName(filePath)), true);
        }
    }
}