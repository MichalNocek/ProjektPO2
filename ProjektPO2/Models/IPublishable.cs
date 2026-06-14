namespace ProjektPO2.Models;

public interface IPublishable
{
    bool IsPublished { get; }
    void Publish();
    void Unpublish();
}
