using System;
using System.Collections.Generic;
using System.IO;
using IsntGwent.Scripts.Accounts.Core;
using Mirror;
using Newtonsoft.Json;
using IsntGwent.Scripts.Diagnostics;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Accounts.Server
{
    public class FileAccountStore : IAccountStore, IInitializable
    {
        private const string FolderName = "Accounts";

        private readonly Dictionary<string, AccountData> _accounts = new();

        private string _directory;

        public IReadOnlyCollection<AccountData> All => _accounts.Values;

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            _directory = Path.Combine(Application.persistentDataPath, FolderName);

            try
            {
                Directory.CreateDirectory(_directory);
            }
            catch (IOException e)
            {
                Log.Error(LogTag.Accounts, "Failed to create account folder: " + e.Message);
                return;
            }

            foreach (var file in Directory.GetFiles(_directory, "*.json"))
            {
                var account = Read(file);
                if (account != null) _accounts[account.Id] = account;
            }

            Log.Info(LogTag.Accounts, "Accounts loaded: " + _accounts.Count);
        }

        public AccountData Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            return _accounts.TryGetValue(id, out var account) ? account : null;
        }

        public AccountData GetOrCreate(string id, string nickname)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var existing = Find(id);
            if (existing != null) return existing;

            var account = new AccountData
            {
                Id = id,
                Nickname = nickname,
            };

            _accounts[id] = account;
            Write(account);

            return account;
        }

        public void Save(AccountData account)
        {
            if (account == null || string.IsNullOrEmpty(account.Id)) return;

            _accounts[account.Id] = account;
            Write(account);
        }

        public bool Rename(AccountData account, string newId, string newNickname)
        {
            if (account == null || string.IsNullOrEmpty(newId)) return false;
            if (_accounts.ContainsKey(newId)) return false;

            var oldId = account.Id;

            _accounts.Remove(oldId);

            account.Id = newId;
            account.Nickname = newNickname;

            _accounts[newId] = account;
            Write(account);
            Erase(oldId);

            return true;
        }

        private static AccountData Read(string path)
        {
            try
            {
                var account = JsonConvert.DeserializeObject<AccountData>(File.ReadAllText(path));
                return string.IsNullOrEmpty(account?.Id) ? null : account;
            }
            catch (Exception e)
            {
                Log.Error(LogTag.Accounts, "Failed to read account " + path + ": " + e.Message);
                return null;
            }
        }

        private void Write(AccountData account)
        {
            if (string.IsNullOrEmpty(_directory)) return;

            try
            {
                File.WriteAllText(FilePath(account.Id), JsonConvert.SerializeObject(account, Formatting.Indented));
            }
            catch (Exception e)
            {
                Log.Error(LogTag.Accounts, "Failed to save account " + account.Id + ": " + e.Message);
            }
        }

        private void Erase(string id)
        {
            if (string.IsNullOrEmpty(_directory) || string.IsNullOrEmpty(id)) return;

            try
            {
                var path = FilePath(id);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Log.Error(LogTag.Accounts, "Failed to delete account " + id + ": " + e.Message);
            }
        }

        private string FilePath(string id)
        {
            return Path.Combine(_directory, SafeName(id) + ".json");
        }

        private static string SafeName(string id)
        {
            var chars = id.ToCharArray();
            var invalid = Path.GetInvalidFileNameChars();

            for (var i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0) chars[i] = '_';
            }

            return new string(chars);
        }
    }
}
