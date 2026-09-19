using Microsoft.Data.Sqlite;
using ZutrittsAuswertung.Models;

namespace ZutrittsAuswertung.Data;

/// <summary>
/// Liest lesend aus den beiden SQLite-Datenbanken des myaxxess/FEIG-Systems.
/// Öffnet Verbindungen ausdrücklich im ReadOnly-Modus, damit die App den
/// schreibenden Prozess (Home Assistant Addon) nicht blockiert.
/// </summary>
public class AuswertungsRepository
{
    private readonly string _accessConnectionString;
    private readonly string _membersConnectionString;

    public AuswertungsRepository(IConfiguration configuration)
    {
        var accessDbPath = configuration["Datenbanken:AccessHistoryDb"]
            ?? throw new InvalidOperationException("Datenbanken:AccessHistoryDb ist nicht konfiguriert (appsettings.json).");
        var membersDbPath = configuration["Datenbanken:MembersDb"]
            ?? throw new InvalidOperationException("Datenbanken:MembersDb ist nicht konfiguriert (appsettings.json).");

        _accessConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = accessDbPath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();

        _membersConnectionString = new SqliteConnectionStringBuilder
        {
            DataSource = membersDbPath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString();
    }

    public async Task<IReadOnlyList<AccessEventView>> SucheAsync(
        DateTime? von,
        DateTime? bis,
        string? suchtext,
        string statusFilter,
        int limit = 1000,
        CancellationToken ct = default)
    {
        var members = await LadeMitgliederAsync(ct);

        await using var connection = new SqliteConnection(_accessConnectionString);
        await connection.OpenAsync(ct);

        var sql = "SELECT timestamp, card_id, name, group_name, access, allowed " +
                   "FROM access_events WHERE 1 = 1";

        using var command = connection.CreateCommand();

        if (von.HasValue)
        {
            sql += " AND timestamp >= $von";
            command.Parameters.AddWithValue("$von", von.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
        }
        if (bis.HasValue)
        {
            sql += " AND timestamp <= $bis";
            command.Parameters.AddWithValue("$bis", bis.Value.ToString("yyyy-MM-ddTHH:mm:ss"));
        }
        if (!string.IsNullOrWhiteSpace(suchtext))
        {
            sql += " AND (name LIKE $suche OR card_id LIKE $suche)";
            command.Parameters.AddWithValue("$suche", $"%{suchtext.Trim()}%");
        }
        if (statusFilter == "erlaubt")
        {
            sql += " AND allowed = 1";
        }
        else if (statusFilter == "verweigert")
        {
            sql += " AND allowed = 0";
        }

        sql += " ORDER BY timestamp DESC LIMIT $limit";
        command.Parameters.AddWithValue("$limit", limit);
        command.CommandText = sql;

        var result = new List<AccessEventView>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var cardId = reader.GetString(1);
            members.TryGetValue(cardId, out var member);

            result.Add(new AccessEventView
            {
                Timestamp = DateTime.Parse(reader.GetString(0)),
                CardId = cardId,
                Name = reader.IsDBNull(2) ? (member?.FullName ?? cardId) : reader.GetString(2),
                GroupName = reader.IsDBNull(3) ? null : reader.GetString(3),
                Access = reader.GetString(4),
                Allowed = reader.GetInt64(5) == 1,
                PaymentStatus = member?.PaymentStatus,
                OpenAmount = member?.OpenAmount,
                AttentionStatus = member?.AttentionStatus,
                FreeAccess = member?.FreeAccess ?? false,
                TenPackUsed = member?.TenPackUsed,
                TenPackLimit = member?.TenPackLimit
            });
        }

        return result;
    }

    private async Task<Dictionary<string, Member>> LadeMitgliederAsync(CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_membersConnectionString);
        await connection.OpenAsync(ct);

        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT card_id, myaxxess_name, first_name, family_name,
                   payment_status, open_amount, access_rule, attention_status,
                   free_access, active_in_myaxxess, ten_pack_used, ten_pack_limit
            FROM members";

        var result = new Dictionary<string, Member>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var member = new Member
            {
                CardId = reader.GetString(0),
                MyaxxessName = reader.IsDBNull(1) ? null : reader.GetString(1),
                FirstName = reader.IsDBNull(2) ? null : reader.GetString(2),
                FamilyName = reader.IsDBNull(3) ? null : reader.GetString(3),
                PaymentStatus = reader.IsDBNull(4) ? null : reader.GetString(4),
                OpenAmount = reader.IsDBNull(5) ? null : reader.GetDouble(5),
                AccessRule = reader.IsDBNull(6) ? null : reader.GetString(6),
                AttentionStatus = reader.IsDBNull(7) ? null : reader.GetString(7),
                FreeAccess = !reader.IsDBNull(8) && reader.GetInt64(8) == 1,
                ActiveInMyaxxess = reader.IsDBNull(9) || reader.GetInt64(9) == 1,
                TenPackUsed = reader.IsDBNull(10) ? 0 : (int)reader.GetInt64(10),
                TenPackLimit = reader.IsDBNull(11) ? 10 : (int)reader.GetInt64(11)
            };
            result[member.CardId] = member;
        }
        return result;
    }
}
