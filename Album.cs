/// <summary>
/// Музыкальный альбом (справочная таблица, сторона «один»)
/// </summary>
public class Album
{
    public int Id { get; set; }
    public string Name { get; set; }

    public Album(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public Album() : this(0, string.Empty) { }

    public override string ToString() => $"[{Id}] {Name}";
}