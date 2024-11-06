namespace EInvoiceQuickBooks.Services
{
    public interface IQueueService
    {
        void Enqueue(string payload);
        string Dequeue();
        bool HasItems();
    }

    public class InMemoryQueueService : IQueueService
    {
        private readonly Queue<string> _queue = new();

        public void Enqueue(string payload)
        {
            lock (_queue)
            {
                _queue.Enqueue(payload);
            }
        }

        public string Dequeue()
        {
            lock (_queue)
            {
                return _queue?.Count > 0 ? _queue?.Dequeue() : null;
            }
        }

        public bool HasItems()
        {
            lock (_queue)
            {
                return _queue.Count > 0;
            }
        }
    }
}
