using Microsoft.Data.Sqlite;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

string dbPath = "music_library.db";
string albumsCsv = "albums.csv";
string songsCsv = "songs.csv";

var db = new DatabaseManager(dbPath);
db.InitializeDatabase(albumsCsv, songsCsv);

while (true)
{
    Console.WriteLine("\n╔══════════════════════════════════════╗");
    Console.WriteLine("║        УПРАВЛЕНИЕ ПЕСНЯМИ            ║");
    Console.WriteLine("╠══════════════════════════════════════╣");
    Console.WriteLine("║ 1 — Показать все альбомы             ║");
    Console.WriteLine("║ 2 — Показать все песни               ║");
    Console.WriteLine("║ 3 — Добавить песню                   ║");
    Console.WriteLine("║ 4 — Редактировать песню              ║");
    Console.WriteLine("║ 5 — Удалить песню                    ║");
    Console.WriteLine("║ 6 — Отчёты                           ║");
    Console.WriteLine("║ 7 — Фильтр по альбому                ║");
    Console.WriteLine("║ 0 — Выход                            ║");
    Console.WriteLine("╚══════════════════════════════════════╝");
    Console.Write("Ваш выбор: ");

    var choice = Console.ReadLine()?.Trim();
    Console.WriteLine();

    try
    {
        switch (choice)
        {
            case "1": ShowAlbums(db); break;
            case "2": ShowSongs(db); break;
            case "3": AddSong(db); break;
            case "4": EditSong(db); break;
            case "5": DeleteSong(db); break;
            case "6": ReportsMenu(db); break;
            case "7": FilterByAlbum(db); break;
            case "0": Console.WriteLine("До свидания!"); return;
            default: Console.WriteLine("Неверный пункт меню."); break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

// ========== МЕТОДЫ МЕНЮ ==========
static void ShowAlbums(DatabaseManager db)
{
    var albums = db.GetAllAlbums();
    Console.WriteLine("--- Альбомы ---");
    foreach (var a in albums) Console.WriteLine("  " + a);
    Console.WriteLine($"Всего: {albums.Count}");
}

static void ShowSongs(DatabaseManager db)
{
    var songs = db.GetAllSongs();
    Console.WriteLine("--- Песни ---");
    foreach (var s in songs) Console.WriteLine("  " + s);
    Console.WriteLine($"Всего: {songs.Count}");
}

static void AddSong(DatabaseManager db)
{
    Console.WriteLine("--- Добавление песни ---");
    var albums = db.GetAllAlbums();
    Console.WriteLine("Доступные альбомы:");
    foreach (var a in albums) Console.WriteLine("  " + a);

    Console.Write("ID альбома: ");
    if (!int.TryParse(Console.ReadLine(), out int albumId))
    { Console.WriteLine("Ошибка ввода ID."); return; }

    Console.Write("Название песни: ");
    string name = Console.ReadLine()?.Trim() ?? "";
    if (string.IsNullOrEmpty(name))
    { Console.WriteLine("Название не может быть пустым."); return; }

    Console.Write("Длительность (сек): ");
    if (!int.TryParse(Console.ReadLine(), out int duration))
    { Console.WriteLine("Ошибка ввода длительности."); return; }

    try
    {
        db.AddSong(new Song(0, albumId, name, duration));
        Console.WriteLine("Песня добавлена.");
    }
    catch (ArgumentException ex)
    {
        Console.WriteLine($"Ошибка: {ex.Message}");
    }
}

static void EditSong(DatabaseManager db)
{
    Console.Write("Введите ID песни для редактирования: ");
    if (!int.TryParse(Console.ReadLine(), out int id))
    { Console.WriteLine("Ошибка ввода."); return; }

    var song = db.GetSongById(id);
    if (song == null)
    { Console.WriteLine("Песня не найдена."); return; }

    Console.WriteLine($"Редактирование: {song}");
    Console.WriteLine("(Enter - оставить без изменений)");

    Console.Write($"Новое название [{song.Name}]: ");
    var input = Console.ReadLine()?.Trim();
    if (!string.IsNullOrEmpty(input)) song.Name = input;

    Console.Write($"Новый ID альбома [{song.AlbumId}]: ");
    input = Console.ReadLine()?.Trim();
    if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int newAlbumId))
        song.AlbumId = newAlbumId;

    Console.Write($"Новая длительность [{song.DurationSec}]: ");
    input = Console.ReadLine()?.Trim();
    if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int newDuration))
    {
        try { song.DurationSec = newDuration; }
        catch (ArgumentException ex)
        { Console.WriteLine($"Ошибка: {ex.Message}"); return; }
    }

    db.UpdateSong(song);
    Console.WriteLine("Песня обновлена.");
}

static void DeleteSong(DatabaseManager db)
{
    Console.Write("Введите ID песни для удаления: ");
    if (!int.TryParse(Console.ReadLine(), out int id))
    { Console.WriteLine("Ошибка ввода."); return; }

    db.DeleteSong(id);
    Console.WriteLine("Песня удалена.");
}

static void ReportsMenu(DatabaseManager db)
{
    Console.WriteLine("--- Отчёты ---");
    Console.WriteLine("1 - Список песен с альбомами");
    Console.WriteLine("2 - Количество песен в альбомах");
    Console.WriteLine("3 - Средняя длительность по альбомам");
    Console.Write("Ваш выбор: ");
    var choice = Console.ReadLine()?.Trim();

    if (choice == "1")
    {
        new ReportBuilder(db)
            .Query(@"SELECT s.song_name, a.album_name, s.duration_sec 
                     FROM song s JOIN album a ON s.album_id = a.album_id 
                     ORDER BY s.song_name")
            .Title("Песни по альбомам")
            .Header("Песня", "Альбом", "Длит. (сек)")
            .ColumnWidths(25, 25, 12)
            .Numbered()
            .Footer("Всего песен")
            .Print();
    }
    else if (choice == "2")
    {
        new ReportBuilder(db)
            .Query(@"SELECT a.album_name, COUNT(*) AS cnt 
                     FROM song s JOIN album a ON s.album_id = a.album_id 
                     GROUP BY a.album_name ORDER BY a.album_name")
            .Title("Количество песен в альбомах")
            .Header("Альбом", "Кол-во песен")
            .ColumnWidths(30, 15)
            .Print();
    }
    else if (choice == "3")
    {
        new ReportBuilder(db)
            .Query(@"SELECT a.album_name, ROUND(AVG(s.duration_sec), 1) AS avg_dur 
                     FROM song s JOIN album a ON s.album_id = a.album_id 
                     GROUP BY a.album_name ORDER BY avg_dur DESC")
            .Title("Средняя длительность песен по альбомам")
            .Header("Альбом", "Средняя длит. (сек)")
            .ColumnWidths(30, 20)
            .Print();
    }
}

static void FilterByAlbum(DatabaseManager db)
{
    var albums = db.GetAllAlbums();
    Console.WriteLine("Доступные альбомы:");
    foreach (var a in albums) Console.WriteLine("  " + a);

    Console.Write("Введите ID альбома для фильтрации: ");
    if (!int.TryParse(Console.ReadLine(), out int albumId))
    { Console.WriteLine("Ошибка ввода."); return; }

    var songs = db.GetSongsByAlbum(albumId);
    Console.WriteLine($"\nПесни в альбоме #{albumId}:");
    foreach (var s in songs) Console.WriteLine("  " + s);
    Console.WriteLine($"Найдено: {songs.Count}");
}