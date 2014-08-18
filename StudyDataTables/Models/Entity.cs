using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace StudyDataTables.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public int CustomerId { get; set; }
        public IEnumerable<OrderItem> OrderItems { get; set; }
    }

    public class OrderItem
    {

        public int OrderId { get; set; }
        [Key]
        public int Seq { get; set; }
        public int ProductId { get; set; }
        public int Qty { get; set; }
    }

    public class Product
    {
        public int Id { get; set; }
        public String ProductName { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }

    }
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public virtual ICollection<Product> Product { get; set; }
    }

    // for editable
    public class Company
    {
        static int nextID = 17;

        public Company()
        {
            ID = nextID++;
        }
        public int ID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Town { get; set; }
    }
}