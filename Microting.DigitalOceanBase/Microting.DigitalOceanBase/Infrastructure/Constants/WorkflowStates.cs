namespace Microting.DigitalOceanBase.Infrastructure.Constants
{
    public static class WorkflowStates
    {
        public const string Active = "active";
        public const string Created = "created";
        public const string ToBeProcessed = "to_be_processed";
        public const string Processing = "processing";
        public const string Removed = "removed";
        public const string FailedToSync = "failed_to_sync";
    }
}
