using STOTool.Enum;

namespace STOTool.Class
{
    public class MaintenanceInfo
    {
        public MaintenanceTimeType ShardStatus { get; set; } = MaintenanceTimeType.None;

        public int Days { get; set; }

        public int Hours { get; set; }

        public int Minutes { get; set; }

        public int Seconds { get; set; }
        
        public string Message { get; set; } = string.Empty;

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Message))
            {
                string shardStatusString = ShardStatus.ToString();

                string timeString = $"{Days} days, {Hours} hours, {Minutes} minutes, {Seconds} seconds";

                return $"Shard Status: {shardStatusString}, Maintenance Time: {timeString}";
            }
            
            return Message;
        }
    }
}
