using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST10444488_POE.Models
{
    public class Customer : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";
        public string RowKey { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Cellnumber { get; set; }
        public string Address { get; set; }
        public string Document { get; set; }
        public string IdentityUserId { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        [NotMapped]
        public ETag ETag { get; set; }

    }
}
