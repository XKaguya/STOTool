namespace STOTool.Class
{
    public class EventInfo
    {
        public string? Summary { get; set; }
        
        public string? StartDate { get; set; }
    
        public string? EndDate { get; set; }
    
        public string? TimeTillStart { get; set; }
    
        public string? TimeTillEnd { get; set; }
        
        public override string ToString()
        {
            return $"Start Date: {StartDate}\nEnd Date: {EndDate}\nSummary: {Summary}\nTime Till Start: {TimeTillStart}\nTime Till End: {TimeTillEnd}";
        }
    }
}