namespace Morphia.Core.Models;

public abstract class MorphModel<K>
{
    public K ID { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public MorphModel(K id)
    {
        ID = id;
    }
}