using System.Globalization;
using Microsoft.Data.Sqlite;

namespace CrimeEmpire.Persistence;

/// <summary>
/// Reads and writes exactly one <see cref="SaveData"/> to exactly one SQLite file. No slot
/// management, no schema migration (ruling 4, ruling 8) — the one fixed path a caller supplies is
/// the whole address space this type knows about.
///
/// <b>Write is atomic by construction, not by discipline.</b> The new save is written complete, in a
/// single transaction, to a sibling <c>.tmp</c> file that nothing else ever reads; only once that
/// file exists whole and closed is it moved onto the real path in one filesystem operation. Anything
/// that goes wrong before that move — a thrown exception, a locked file, a process killed mid-write —
/// leaves the <c>.tmp</c> file damaged or absent and the real path exactly as it was. Ruling 7's
/// "failed/interrupted writes leave the previous valid save loadable" is this, not a recovery step
/// bolted on afterwards.
/// </summary>
public static class SaveStore
{
    /// <summary>
    /// The save file's own column/table shape. Bumped only when that shape changes — see
    /// <see cref="SaveData.SchemaVersion"/>.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Test-only synchronization seam. Invoked once, inside <see cref="WriteDatabase"/>, immediately
    /// after the fresh <c>.tmp</c> database's transaction is opened for the meta/commands inserts —
    /// the exact point a genuinely killed writer process leaves a real, incomplete <c>.tmp</c>
    /// artifact behind (schema created and durably committed in auto-commit mode; the transaction
    /// that would populate it never committed). Null — its default, and the only value any real save
    /// ever sees — makes this a no-op; nothing about a real save's behaviour changes because this
    /// field exists. Only a dedicated interrupted-write test harness
    /// (<c>CrimeEmpire.Persistence.InterruptedWriteHarness</c>) ever sets it, using it to signal a
    /// named cross-process event and then block forever, so a parent test process can kill the
    /// harness at a deterministic point via explicit synchronization rather than a timing guess. See
    /// milestone 015's correction for the full account of why this replaced a weaker, pre-write file
    /// lock as the proof of ruling 7's "failed/interrupted writes leave the previous valid save
    /// loadable".
    /// </summary>
    internal static Action? OnTransactionOpenedForTest;

    public static bool Exists(string path) => File.Exists(path);

    public static void Write(string path, SaveData data)
    {
        string tmpPath = path + ".tmp";
        TryDelete(tmpPath);

        try
        {
            WriteDatabase(tmpPath, data);
        }
        catch
        {
            TryDelete(tmpPath);
            throw;
        }

        File.Move(tmpPath, path, overwrite: true);
    }

    public static SaveData Read(string path)
    {
        if (!File.Exists(path))
            throw new SaveFormatException($"no save exists at '{path}'.");

        using var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");
        try
        {
            connection.Open();
        }
        catch (SqliteException ex)
        {
            throw new SaveFormatException($"'{path}' is not a readable save file.", ex);
        }

        SaveData meta = ReadMeta(path, connection);

        if (meta.SchemaVersion != CurrentSchemaVersion)
            throw new SaveFormatException(
                $"'{path}' was written with schema version {meta.SchemaVersion}, this build reads " +
                $"schema version {CurrentSchemaVersion}. Compatibility is same-build only — see " +
                "milestone 015 ruling 8.");

        if (!string.Equals(meta.BuildId, SimulationBuild.CurrentId, StringComparison.Ordinal))
            throw new SaveFormatException(
                $"'{path}' was written by a different simulation build ({meta.BuildId}) than the one " +
                $"running now ({SimulationBuild.CurrentId}). Compatibility is same-build only — see " +
                "milestone 015 ruling 8.");

        var commands = ReadCommands(path, connection);
        return meta with { Commands = commands };
    }

    // ---------------------------------------------------------------- write

    private static void WriteDatabase(string path, SaveData data)
    {
        using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
        connection.Open();

        using (var create = connection.CreateCommand())
        {
            create.CommandText = """
                CREATE TABLE save_meta (
                    id INTEGER PRIMARY KEY CHECK (id = 1),
                    schema_version INTEGER NOT NULL,
                    build_id TEXT NOT NULL,
                    seed INTEGER NOT NULL,
                    variant TEXT NOT NULL,
                    controlled_id TEXT NULL,
                    viewpoint_id TEXT NOT NULL
                );
                CREATE TABLE save_commands (
                    ordinal INTEGER NOT NULL PRIMARY KEY,
                    kind TEXT NOT NULL,
                    arg TEXT NULL
                );
                """;
            create.ExecuteNonQuery();
        }

        using var transaction = connection.BeginTransaction();

        // Test-only synchronization seam — see OnTransactionOpenedForTest's own doc comment. Null,
        // and therefore free, on every real save.
        OnTransactionOpenedForTest?.Invoke();

        using (var insertMeta = connection.CreateCommand())
        {
            insertMeta.Transaction = transaction;
            insertMeta.CommandText = """
                INSERT INTO save_meta (id, schema_version, build_id, seed, variant, controlled_id, viewpoint_id)
                VALUES (1, $schema, $build, $seed, $variant, $controlled, $viewpoint);
                """;
            insertMeta.Parameters.AddWithValue("$schema", data.SchemaVersion);
            insertMeta.Parameters.AddWithValue("$build", data.BuildId);
            insertMeta.Parameters.AddWithValue("$seed", data.Seed);
            insertMeta.Parameters.AddWithValue("$variant", data.Variant);
            insertMeta.Parameters.AddWithValue("$controlled", (object?)data.ControlledCharacterId ?? DBNull.Value);
            insertMeta.Parameters.AddWithValue("$viewpoint", data.ViewpointCharacterId);
            insertMeta.ExecuteNonQuery();
        }

        using (var insertCommand = connection.CreateCommand())
        {
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = "INSERT INTO save_commands (ordinal, kind, arg) VALUES ($ordinal, $kind, $arg);";

            var ordinalParam = insertCommand.CreateParameter();
            ordinalParam.ParameterName = "$ordinal";
            insertCommand.Parameters.Add(ordinalParam);

            var kindParam = insertCommand.CreateParameter();
            kindParam.ParameterName = "$kind";
            insertCommand.Parameters.Add(kindParam);

            var argParam = insertCommand.CreateParameter();
            argParam.ParameterName = "$arg";
            insertCommand.Parameters.Add(argParam);

            for (int i = 0; i < data.Commands.Count; i++)
            {
                var command = data.Commands[i];
                ordinalParam.Value = i;
                kindParam.Value = command.Kind.ToString();
                argParam.Value = (object?)ArgOf(command) ?? DBNull.Value;
                insertCommand.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    private static string? ArgOf(SessionCommand command) => command.Kind switch
    {
        SessionCommandKind.AdvanceDays => command.Days!.Value.ToString(CultureInfo.InvariantCulture),
        SessionCommandKind.Choose => command.OptionToken,
        _ => null,
    };

    // ---------------------------------------------------------------- read

    private static SaveData ReadMeta(string path, SqliteConnection connection)
    {
        try
        {
            using var metaCommand = connection.CreateCommand();
            metaCommand.CommandText =
                "SELECT schema_version, build_id, seed, variant, controlled_id, viewpoint_id FROM save_meta WHERE id = 1;";
            using var reader = metaCommand.ExecuteReader();
            if (!reader.Read())
                throw new SaveFormatException($"'{path}' has no save_meta row.");

            return new SaveData(
                SchemaVersion: reader.GetInt32(0),
                BuildId: reader.GetString(1),
                Seed: reader.GetInt32(2),
                Variant: reader.GetString(3),
                ControlledCharacterId: reader.IsDBNull(4) ? null : reader.GetString(4),
                ViewpointCharacterId: reader.GetString(5),
                Commands: Array.Empty<SessionCommand>());
        }
        catch (SqliteException ex)
        {
            throw new SaveFormatException($"'{path}' does not have the expected save schema.", ex);
        }
    }

    private static List<SessionCommand> ReadCommands(string path, SqliteConnection connection)
    {
        var commands = new List<SessionCommand>();
        try
        {
            using var commandsCommand = connection.CreateCommand();
            commandsCommand.CommandText = "SELECT ordinal, kind, arg FROM save_commands ORDER BY ordinal ASC;";
            using var reader = commandsCommand.ExecuteReader();

            long expected = 0;
            while (reader.Read())
            {
                long ordinal = reader.GetInt64(0);
                if (ordinal != expected)
                    throw new SaveFormatException(
                        $"'{path}' has a reordered or gapped command ordinal — expected {expected}, found {ordinal}.");

                string kindText = reader.GetString(1);
                if (!Enum.TryParse<SessionCommandKind>(kindText, out var kind))
                    throw new SaveFormatException(
                        $"'{path}' has an unrecognised command kind '{kindText}' at ordinal {ordinal}.");

                string? arg = reader.IsDBNull(2) ? null : reader.GetString(2);
                commands.Add(kind switch
                {
                    SessionCommandKind.StepEvent => SessionCommand.StepEvent(),
                    SessionCommandKind.AdvanceDays => ParseAdvanceDays(path, ordinal, arg),
                    SessionCommandKind.Choose => ParseChoose(path, ordinal, arg),
                    _ => throw new SaveFormatException(
                        $"'{path}' has an unrecognised command kind '{kindText}' at ordinal {ordinal}."),
                });

                expected++;
            }
        }
        catch (SqliteException ex)
        {
            throw new SaveFormatException($"'{path}' does not have the expected save schema.", ex);
        }

        return commands;
    }

    private static SessionCommand ParseAdvanceDays(string path, long ordinal, string? arg)
    {
        if (arg is null || !int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out int days) || days < 1)
            throw new SaveFormatException($"'{path}' has an invalid AdvanceDays argument '{arg}' at ordinal {ordinal}.");
        return SessionCommand.AdvanceDays(days);
    }

    private static SessionCommand ParseChoose(string path, long ordinal, string? arg)
    {
        if (string.IsNullOrEmpty(arg))
            throw new SaveFormatException($"'{path}' has a missing option token at ordinal {ordinal}.");
        return SessionCommand.Choose(arg);
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort — the caller's own write or throw is what matters */ }
    }
}
