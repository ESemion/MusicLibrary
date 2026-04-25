using Microsoft.Data.Sqlite;

/// <summary>
/// Управление базой данных SQLite.
/// </summary>
public class DatabaseManager
{
    private readonly string _connectionString;

    public DatabaseManager(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public void InitializeDatabase(string albumsCsvPath, string songsCsvPath)
    {
        CreateTables();
        if (GetAllAlbums().Count == 0 && File.Exists(albumsCsvPath))
        {
            ImportAlbumsFromCsv(albumsCsvPath);
            Console.WriteLine($"[OK] Загружены альбомы из {albumsCsvPath}");
        }
        if (GetAllSongs().Count == 0 && File.Exists(songsCsvPath))
        {
            ImportSongsFromCsv(songsCsvPath);
            Console.WriteLine($"[OK] Загружены песни из {songsCsvPath}");
        }
    }

    private void CreateTables()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS album (
                album_id INTEGER PRIMARY KEY AUTOINCREMENT,
                album_name TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS song (
                song_id INTEGER PRIMARY KEY AUTOINCREMENT,
                album_id INTEGER NOT NULL,
                song_name TEXT NOT NULL,
                duration_sec INTEGER NOT NULL,
                FOREIGN KEY (album_id) REFERENCES album(album_id)
            );";
        cmd.ExecuteNonQuery();
    }

    private void ImportAlbumsFromCsv(string path)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var lines = File.ReadAllLines(path);
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(';');
            if (parts.Length < 2) continue;
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO album (album_id, album_name) VALUES (@id, @name)";
            cmd.Parameters.AddWithValue("@id", int.Parse(parts[0]));
            cmd.Parameters.AddWithValue("@name", parts[1]);
            cmd.ExecuteNonQuery();
        }
    }

    private void ImportSongsFromCsv(string path)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var lines = File.ReadAllLines(path);
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(';');
            if (parts.Length < 4) continue;
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO song (song_id, album_id, song_name, duration_sec)
                VALUES (@id, @albumId, @name, @duration)";
            cmd.Parameters.AddWithValue("@id", int.Parse(parts[0]));
            cmd.Parameters.AddWithValue("@albumId", int.Parse(parts[1]));
            cmd.Parameters.AddWithValue("@name", parts[2]);
            cmd.Parameters.AddWithValue("@duration", int.Parse(parts[3]));
            cmd.ExecuteNonQuery();
        }
    }

    public List<Album> GetAllAlbums()
    {
        var result = new List<Album>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT album_id, album_name FROM album ORDER BY album_id";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(new Album(reader.GetInt32(0), reader.GetString(1)));
        return result;
    }

    public List<Song> GetAllSongs()
    {
        var result = new List<Song>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT song_id, album_id, song_name, duration_sec FROM song ORDER BY song_id";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(new Song(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetInt32(3)));
        return result;
    }

    public void AddSong(Song song)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO song (album_id, song_name, duration_sec) 
                            VALUES (@albumId, @name, @duration)";
        cmd.Parameters.AddWithValue("@albumId", song.AlbumId);
        cmd.Parameters.AddWithValue("@name", song.Name);
        cmd.Parameters.AddWithValue("@duration", song.DurationSec);
        cmd.ExecuteNonQuery();
    }

    public void UpdateSong(Song song)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"UPDATE song SET album_id = @albumId, song_name = @name, duration_sec = @duration 
                            WHERE song_id = @id";
        cmd.Parameters.AddWithValue("@id", song.Id);
        cmd.Parameters.AddWithValue("@albumId", song.AlbumId);
        cmd.Parameters.AddWithValue("@name", song.Name);
        cmd.Parameters.AddWithValue("@duration", song.DurationSec);
        cmd.ExecuteNonQuery();
    }

    public void DeleteSong(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM song WHERE song_id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public Song? GetSongById(int id)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT song_id, album_id, song_name, duration_sec FROM song WHERE song_id = @id";
        cmd.Parameters.AddWithValue("@id", id);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
            return new Song(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetInt32(3));
        return null;
    }

    public (string[] columns, List<string[]> rows) ExecuteQuery(string sql)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        using var reader = cmd.ExecuteReader();

        var columns = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
            columns[i] = reader.GetName(i);

        var rows = new List<string[]>();
        while (reader.Read())
        {
            var row = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
                row[i] = reader.GetValue(i)?.ToString() ?? string.Empty;
            rows.Add(row);
        }
        return (columns, rows);
    }

    public List<Song> GetSongsByAlbum(int albumId)
    {
        var result = new List<Song>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT song_id, album_id, song_name, duration_sec 
                            FROM song WHERE album_id = @albumId ORDER BY song_name";
        cmd.Parameters.AddWithValue("@albumId", albumId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add(new Song(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetInt32(3)));
        return result;
    }
}