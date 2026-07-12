namespace ServiceCenter.Models
{
    public class Statistics
    {
        public int TotalRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int WaitingRequests { get; set; }
        public double AvgCompletionTime { get; set; }
    }
}