using Newtonsoft.Json;

namespace IsntGwent.Scripts.Accounts.Core
{
    public class AccountData
    {
        /// Ключ аккаунта: "itch:<числовой user.id>" или "guest:<случайный>" у гостя.
        /// Ник ключом больше не является: на itch его можно сменить, id — нет.
        public string Id;
        public string Nickname;
        public int Points;
        public int Wins;
        public int Losses;
        public int Ties;
        public int WinStreak;
        public int LossStreak;

        /// Гость живёт только в памяти сервера: в файл не пишется и в таблицу лучших не попадает.
        [JsonIgnore]
        public bool IsGuest;
    }
}
