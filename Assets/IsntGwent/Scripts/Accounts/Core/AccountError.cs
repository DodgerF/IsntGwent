namespace IsntGwent.Scripts.Accounts.Core
{
    public enum AccountError
    {
        None,

        /// Сессии с таким токеном сервер не знает: истекла, или сервер перезапускался.
        BadSession,

        /// Этот же аккаунт уже держит другое соединение.
        AlreadyOnline,

        /// Сервер поднялся, но сцена меню ещё не собрала сервисы аккаунтов — клиент повторит.
        NotReady,

        Unknown
    }
}
