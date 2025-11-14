using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ST10444488_POE.Models
{
    public class Product : ITableEntity
    {
        [BindNever]
        public string PartitionKey { get; set; } = "Product";
        [BindNever]
        public string RowKey { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero")]
        public double Price { get; set; }

        [Required]
        public string Category { get; set; }

        public string Sizes { get; set; }

        [BindNever]
        public string ImageUrl { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        [NotMapped]
        public ETag ETag { get; set; }
    }

}
