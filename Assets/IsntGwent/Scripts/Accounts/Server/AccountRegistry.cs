using System.Collections.Generic;
using IsntGwent.Scripts.Accounts.Core;
using Mirror;

namespace IsntGwent.Scripts.Accounts.Server
{
    public class AccountRegistry
    {
        private readonly Dictionary<NetworkConnectionToClient, AccountData> _byConnection = new();
        private readonly Dictionary<string, NetworkConnectionToClient> _byAccount = new();

        public void Login(NetworkConnectionToClient conn, AccountData account)
        {
            if (conn == null || account == null) return;

            Logout(conn);

            _byConnection[conn] = account;
            _byAccount[account.Id] = conn;
        }

        public AccountData Resolve(NetworkConnectionToClient conn)
        {
            if (conn == null) return null;

            return _byConnection.TryGetValue(conn, out var account) ? account : null;
        }

        public bool IsOnlineElsewhere(string accountId, NetworkConnectionToClient conn)
        {
            if (string.IsNullOrEmpty(accountId)) return false;
            if (!_byAccount.TryGetValue(accountId, out var owner)) return false;

            return owner != conn;
        }

        public void Logout(NetworkConnectionToClient conn)
        {
            if (conn == null) return;
            if (!_byConnection.TryGetValue(conn, out var account)) return;

            _byConnection.Remove(conn);

            if (_byAccount.TryGetValue(account.Id, out var owner) && owner == conn)
                _byAccount.Remove(account.Id);
        }

        public void Clear()
        {
            _byConnection.Clear();
            _byAccount.Clear();
        }
    }
}
