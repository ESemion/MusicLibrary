/// <summary>
/// Песня (основная таблица, сторона «много»)
/// </summary>
public class Song
{
    public int Id { get; set; }
    public int AlbumId { get; set; }
    public string Name { get; set; }

    private int _durationSec;

    public int DurationSec
    {
        get => _durationSec;
        set
        {
            if (value < 0)
                throw new ArgumentException("Длительность не может быть отрицательной");
            _durationSec = value;
        }
    }

    public Song(int id, int albumId, string name, int durationSec)
    {
        Id = id;
        AlbumId = albumId;
        Name = name;
        DurationSec = durationSec;
    }

    public Song() : this(0, 0, string.Empty, 0) { }

    public override string ToString() => $"[{Id}] {Name} (Альбом #{AlbumId}), {DurationSec} сек.";
}