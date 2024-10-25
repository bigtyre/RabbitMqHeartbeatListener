public class EventBus
{
    public event EventHandler? ServiceAdded;
    public event EventHandler? ServiceUpdated;
    public event EventHandler? ServiceDeleted;

    public void OnServiceAdded()
    {
        ServiceAdded?.Invoke(this, EventArgs.Empty);
    }

    public void OnServiceUpdated()
    {
        ServiceUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void OnServiceDeleted()
    {
        ServiceDeleted?.Invoke(this, EventArgs.Empty);
    }
}