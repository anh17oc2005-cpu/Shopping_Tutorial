using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shopping_Tutorial.Models
{
    public class ProductWarrantyModel
    {
        public int Id { get; set; }
        public long ProductId { get; set; }

        [ForeignKey("ProductId")]
        public ProductModel Product { get; set; }

        [ForeignKey("UserId")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Yêu cầu tên mô tả bảo hành")]
        public string Description { get; set; }

        public DateTime DateStart { get; set; }
        public DateTime DateExpired { get; set; }
        public DateTime DateCreated { get; set; }
        public int Status { get; set; }


    }
}
