using System.Collections.Generic;
using IsntGwent.Scripts.Accounts.Core;

namespace IsntGwent.Scripts.Accounts.Server
{
    public interface IAccountStore
    {
        IReadOnlyCollection<AccountData> All { get; }

        AccountData Find(string id);

        AccountData GetOrCreate(string id, string nickname);

        void Save(AccountData account);

        bool Rename(AccountData account, string newId, string newNickname);
    }
}
