using PkKillTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.Common;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace PkKillTracker.DataAccess
{    
    public static class PkKillsDataAccess
    {
        private static IConfigurationRoot _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();

        private static string connString = _config["ACEDB"];
        private static string eventLogDbName = _config["ACEDB_event_log_db_name"];
        private static string shardDbName = _config["ACEDB_shard_db_name"];

        public static List<PkKill> GetPkKillsByMonth(DateTime monthDateFilter)
        {
            return GetPkKillsByFilter(null, monthDateFilter);
        }

        public static List<PkKill> GetPkKillsByPlayer(string playerFilter)
        {
            return GetPkKillsByFilter(playerFilter, null);
        }

        private static List<PkKill> GetPkKillsByFilter(string? playerFilter, DateTime? monthDateFilter)
        {
            List<PkKill> kills = new List<PkKill>();
            MySqlConnection conn = new MySqlConnection(connString);

            DateTime startDate = monthDateFilter.HasValue ? new DateTime(monthDateFilter.Value.Year, monthDateFilter.Value.Month, 1, 0, 0, 0) : DateTime.MinValue;
            DateTime endDate = monthDateFilter.HasValue ? startDate.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(59).AddSeconds(59) : DateTime.MaxValue;

            try
            {
                conn.Open();
                string sql = $@"SELECT
                                  kill.id,
                                  killerChar.name AS Killer,
                                  COALESCE(killerMonarch.name, 'No Clan') AS KillerClan,
                                  victimChar.name AS Victim,
                                  COALESCE(victimMonarch.name, 'No Clan') AS VictimClan,
                                  CASE 
                                    WHEN DATE(kill.kill_datetime) = '0001-01-01' THEN 'Not Recorded'
                                    ELSE kill.kill_datetime
                                  END AS KillDateTime
                                FROM {eventLogDbName}.pk_kills_log AS `kill`
                                JOIN {shardDbName}.character AS killerChar
                                ON kill.killer_id = killerChar.id
                                JOIN {shardDbName}.character AS victimChar
                                ON kill.victim_id = victimChar.id
                                LEFT JOIN {shardDbName}.character AS killerMonarch
                                ON kill.killer_monarch_id = killerMonarch.id
                                LEFT JOIN {shardDbName}.character AS victimMonarch
                                ON kill.victim_monarch_id = victimMonarch.id" + Environment.NewLine +
                                (!string.IsNullOrWhiteSpace(playerFilter) ?
                              @"WHERE killerChar.name = @playerFilter
                                OR victimChar.name = @playerFilter" :
                              @"WHERE kill.kill_datetime >= @startDate
                                AND kill.kill_datetime <= @endDate") + Environment.NewLine +
                               "ORDER BY kill.kill_datetime DESC";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                if (!string.IsNullOrWhiteSpace(playerFilter))
                {
                    cmd.Parameters.AddWithValue("@playerFilter", playerFilter);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@startDate", startDate);
                    cmd.Parameters.AddWithValue("@endDate", endDate);
                }
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var kill = new PkKill();
                    //kill.Id = reader.GetUInt32("id");
                    kill.KillerName = reader.GetString("Killer");
                    kill.VictimName = reader.GetString("Victim");
                    kill.KillerMonarchName = reader.GetString("KillerClan");
                    kill.VictimMonarchName = reader.GetString("VictimClan");
                    kill.KillDateTime = reader.GetDateTime("KillDateTime");

                    kills.Add(kill);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();

            return kills;
        }

        public static List<Character> GetKillsDeathsByCharacter()
        {
            List<Character> characters = new List<Character>();
            MySqlConnection conn = new MySqlConnection(connString);

            try
            {
                conn.Open();
                string sql = $@"SELECT
                                  player.name AS Player,
                                  monarchChar.name AS Clan,
                                  SUM(CASE WHEN kill.killer_id = player.id THEN 1 ELSE 0 END) AS TotalKills,
                                  SUM(CASE WHEN kill.victim_id = player.id THEN 1 ELSE 0 END) AS TotalDeaths
                                FROM {shardDbName}.character AS player
                                LEFT JOIN {eventLogDbName}.pk_kills_log AS `kill`
                                ON kill.killer_id = player.id
                                OR kill.victim_id = player.id
                                LEFT JOIN {shardDbName}.biota_properties_i_i_d monarchId
                                ON player.id = monarchId.object_Id
                                AND monarchId.type = 26
                                LEFT JOIN {shardDbName}.character AS monarchChar
                                ON monarchId.value = monarchChar.Id
                                GROUP BY player.name, monarchChar.name
                                HAVING SUM(CASE WHEN kill.killer_id = player.id THEN 1 ELSE 0 END) > 10 
                                OR SUM(CASE WHEN kill.victim_id = player.id THEN 1 ELSE 0 END) > 50
                                ORDER BY SUM(CASE WHEN kill.killer_id = player.id THEN 1 ELSE 0 END) DESC";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var character = new Character();
                    character.CharacterName = reader.GetString("Player");
                    character.ClanName = reader.IsDBNull("Clan") ? "No Clan" : reader.GetString("Clan");
                    character.Kills = reader.GetUInt32("TotalKills");
                    character.Deaths = reader.GetUInt32("TotalDeaths");

                    characters.Add(character);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();

            return characters;
        }

        public static List<Clan> GetKillsDeathsByClan()
        {
            List<Clan> clans = new List<Clan>();
            MySqlConnection conn = new MySqlConnection(connString);

            try
            {
                conn.Open();
                string sql = $@"SELECT
                                  player.name AS ClanName,
                                  SUM(CASE WHEN kill.killer_monarch_id = player.id THEN 1 ELSE 0 END) AS TotalKills,
                                  SUM(CASE WHEN kill.victim_monarch_id = player.id THEN 1 ELSE 0 END) AS TotalDeaths
                                FROM {shardDbName}.character AS player
                                JOIN {eventLogDbName}.pk_kills_log AS `kill`
                                ON kill.killer_monarch_id = player.id
                                OR kill.victim_monarch_id = player.id
                                GROUP BY player.name
                                HAVING SUM(CASE WHEN kill.killer_monarch_id = player.id THEN 1 ELSE 0 END) > 20 
                                OR SUM(CASE WHEN kill.victim_id = player.id THEN 1 ELSE 0 END) > 20
                                ORDER BY SUM(CASE WHEN kill.killer_monarch_id = player.id THEN 1 ELSE 0 END) DESC";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var clan = new Clan();
                    clan.ClanName = reader.GetString("ClanName");
                    clan.Kills = reader.GetUInt32("TotalKills");
                    clan.Deaths = reader.GetUInt32("TotalDeaths");

                    clans.Add(clan);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();

            return clans;
        }
    }
}
