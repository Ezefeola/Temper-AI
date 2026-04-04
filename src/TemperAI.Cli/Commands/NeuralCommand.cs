using Microsoft.Data.Sqlite;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace TemperAI.Cli.Commands;

public sealed class NeuralCommand : Command<NeuralCommand.Settings>
{
    private const string DefaultDbPath = "temper-neural.db";

    public sealed class Settings : CommandSettings
    {
        [CommandOption("--save")]
        [Description("Save an observation")]
        public bool Save { get; init; }

        [CommandOption("--recall")]
        [Description("Recall previous observations")]
        public bool Recall { get; init; }

        [CommandOption("--session")]
        [Description("Start a new session")]
        public bool Session { get; init; }

        [CommandOption("--type")]
        [Description("Observation type: Bugfix, Decision, Architecture, Discovery, Pattern, Config, Preference")]
        public string? Type { get; init; }

        [CommandOption("--title")]
        [Description("Observation title")]
        public string? Title { get; init; }

        [CommandOption("--content")]
        [Description("Observation content")]
        public string? Content { get; init; }

        [CommandOption("--project")]
        [Description("Project name")]
        public string? Project { get; init; }

        [CommandOption("--topic")]
        [Description("Topic key for grouping observations")]
        public string? Topic { get; init; }

        [CommandOption("--db")]
        [Description("Path to the SQLite database")]
        public string? DbPath { get; init; }

        [CommandOption("--limit")]
        [Description("Limit of observations to retrieve (default: 10)")]
        public int Limit { get; init; } = 10;

        [CommandOption("--topic-filter")]
        [Description("Filter observations by topic key")]
        public string? TopicFilter { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        string dbPath = settings.DbPath ?? DefaultDbPath;

        if (settings.Save)
        {
            return SaveObservation(dbPath, settings);
        }

        if (settings.Recall)
        {
            return RecallObservations(dbPath, settings);
        }

        if (settings.Session)
        {
            return StartSession(dbPath, settings);
        }

        ShowHelp();
        return 0;
    }

    private static int SaveObservation(string dbPath, Settings settings)
    {
        if (string.IsNullOrEmpty(settings.Title) || string.IsNullOrEmpty(settings.Content))
        {
            AnsiConsole.MarkupLine("[red]--title and --content are required to save.[/]");
            return 1;
        }

        string type = settings.Type ?? "Discovery";
        string project = settings.Project ?? "Unknown";
        string topic = settings.Topic ?? string.Empty;

        EnsureDatabase(dbPath);

        Guid sessionId = GetOrCreateSessionId(dbPath, project);

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Observations (SessionId, Type, Title, Content, Project, TopicKey, RevisionCount, CreatedAt)
            VALUES ($sessionId, $type, $title, $content, $project, $topicKey, 0, $createdAt)";

        command.Parameters.AddWithValue("$sessionId", sessionId);
        command.Parameters.AddWithValue("$type", type);
        command.Parameters.AddWithValue("$title", settings.Title);
        command.Parameters.AddWithValue("$content", settings.Content);
        command.Parameters.AddWithValue("$project", project);
        command.Parameters.AddWithValue("$topicKey", (object?)topic ?? DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", DateTime.UtcNow);

        command.ExecuteNonQuery();

        AnsiConsole.MarkupLine($"[green]✓[/] [bold]Observation saved:[/]");
        AnsiConsole.MarkupLine($"  [dim]Type:[/]    {type}");
        AnsiConsole.MarkupLine($"  [dim]Title:[/]   {settings.Title}");
        AnsiConsole.MarkupLine($"  [dim]Project:[/] {project}");
        if (!string.IsNullOrEmpty(topic))
        {
            AnsiConsole.MarkupLine($"  [dim]Topic:[/]   {topic}");
        }
        AnsiConsole.MarkupLine($"  [dim]Content:[/] {settings.Content[..Math.Min(100, settings.Content.Length)]}{(settings.Content.Length > 100 ? "..." : "")}");
        AnsiConsole.WriteLine();

        return 0;
    }

    private static int RecallObservations(string dbPath, Settings settings)
    {
        if (!File.Exists(dbPath))
        {
            AnsiConsole.MarkupLine("[dim]No previous observations. Database does not exist.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        EnsureDatabase(dbPath);

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();

        if (!string.IsNullOrEmpty(settings.TopicFilter))
        {
            command.CommandText = @"
                SELECT Type, Title, Content, Project, TopicKey, CreatedAt
                FROM Observations
                WHERE TopicKey = $topicKey
                ORDER BY CreatedAt DESC
                LIMIT $limit";
            command.Parameters.AddWithValue("$topicKey", settings.TopicFilter);
        }
        else
        {
            command.CommandText = @"
                SELECT Type, Title, Content, Project, TopicKey, CreatedAt
                FROM Observations
                ORDER BY CreatedAt DESC
                LIMIT $limit";
        }

        command.Parameters.AddWithValue("$limit", settings.Limit);

        using var reader = command.ExecuteReader();

        var observations = new List<Dictionary<string, string>>();

        while (reader.Read())
        {
            observations.Add(new Dictionary<string, string>
            {
                ["Type"] = reader.GetString(0),
                ["Title"] = reader.GetString(1),
                ["Content"] = reader.GetString(2),
                ["Project"] = reader.GetString(3),
                ["TopicKey"] = reader.IsDBNull(4) ? "—" : reader.GetString(4),
                ["CreatedAt"] = reader.GetDateTime(5).ToString("yyyy-MM-dd HH:mm")
            });
        }

        if (observations.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No previous observations.[/]");
            AnsiConsole.WriteLine();
            return 0;
        }

        AnsiConsole.MarkupLine($"[bold]Found {observations.Count} observation(s):[/]");
        AnsiConsole.WriteLine();

        Table table = new Table();
        table.AddColumn(new TableColumn("[bold]Type[/]"));
        table.AddColumn(new TableColumn("[bold]Title[/]"));
        table.AddColumn(new TableColumn("[bold]Content[/]"));
        table.AddColumn(new TableColumn("[bold]Project[/]"));
        table.AddColumn(new TableColumn("[bold]Date[/]"));

        foreach (var obs in observations)
        {
            string content = obs["Content"];
            if (content.Length > 80)
            {
                content = content[..80] + "...";
            }

            table.AddRow(
                obs["Type"],
                obs["Title"],
                content,
                obs["Project"],
                obs["CreatedAt"]);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        return 0;
    }

    private static int StartSession(string dbPath, Settings settings)
    {
        string project = settings.Project ?? "Unknown";

        EnsureDatabase(dbPath);

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Sessions (Id, Project, Directory, StartedAt, Status)
            VALUES ($id, $project, $directory, $startedAt, $status)";

        command.Parameters.AddWithValue("$id", Guid.NewGuid());
        command.Parameters.AddWithValue("$project", project);
        command.Parameters.AddWithValue("$directory", Directory.GetCurrentDirectory());
        command.Parameters.AddWithValue("$startedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("$status", "Active");

        command.ExecuteNonQuery();

        AnsiConsole.MarkupLine($"[green]✓[/] [bold]Session started:[/]");
        AnsiConsole.MarkupLine($"  [dim]Project:[/] {project}");
        AnsiConsole.MarkupLine($"  [dim]Directory:[/] {Directory.GetCurrentDirectory()}");
        AnsiConsole.WriteLine();

        return 0;
    }

    private static void EnsureDatabase(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Sessions (
                Id TEXT PRIMARY KEY,
                Project TEXT NOT NULL,
                Directory TEXT NOT NULL,
                StartedAt TEXT NOT NULL,
                EndedAt TEXT,
                Summary TEXT,
                Status TEXT NOT NULL DEFAULT 'Active'
            );

            CREATE TABLE IF NOT EXISTS Observations (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SessionId TEXT NOT NULL,
                Type TEXT NOT NULL,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL,
                Project TEXT NOT NULL,
                TopicKey TEXT,
                RevisionCount INTEGER NOT NULL DEFAULT 0,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT,
                FOREIGN KEY (SessionId) REFERENCES Sessions(Id)
            );

            CREATE INDEX IF NOT EXISTS IX_Observations_SessionId ON Observations(SessionId);
            CREATE INDEX IF NOT EXISTS IX_Observations_TopicKey ON Observations(TopicKey);
            CREATE INDEX IF NOT EXISTS IX_Observations_Project ON Observations(Project);
        ";

        command.ExecuteNonQuery();
    }

    private static Guid GetOrCreateSessionId(string dbPath, string project)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM Sessions WHERE Project = $project AND Status = 'Active' ORDER BY StartedAt DESC LIMIT 1";
        command.Parameters.AddWithValue("$project", project);

        var result = command.ExecuteScalar();

        if (result is string sessionIdStr && Guid.TryParse(sessionIdStr, out Guid existingId))
        {
            return existingId;
        }

        Guid newId = Guid.NewGuid();

        using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO Sessions (Id, Project, Directory, StartedAt, Status) VALUES ($id, $project, $directory, $startedAt, 'Active')";
        insertCommand.Parameters.AddWithValue("$id", newId);
        insertCommand.Parameters.AddWithValue("$project", project);
        insertCommand.Parameters.AddWithValue("$directory", Directory.GetCurrentDirectory());
        insertCommand.Parameters.AddWithValue("$startedAt", DateTime.UtcNow);
        insertCommand.ExecuteNonQuery();

        return newId;
    }

    private static void ShowHelp()
    {
        AnsiConsole.MarkupLine("[bold]NeuralCore CLI — Session tracking & observations[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Commands:[/]");
        AnsiConsole.MarkupLine("[dim]  temper-ai neural --save --title \"Fix null ref\" --content \"Fixed null in...\" --type Bugfix --project MyProject[/]");
        AnsiConsole.MarkupLine("[dim]  temper-ai neural --recall --limit 20[/]");
        AnsiConsole.MarkupLine("[dim]  temper-ai neural --recall --topic-filter null-ref-fix[/]");
        AnsiConsole.MarkupLine("[dim]  temper-ai neural --session --project MyProject[/]");
        AnsiConsole.WriteLine();
    }
}
