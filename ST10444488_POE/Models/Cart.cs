namespace ST10444488_POE.Models
{
    public class Cart
    {
        public string RowKey { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }

        public decimal LineTotal => Price * Quantity;
    }
}
